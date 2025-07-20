using CppMMO.Protocol;
using Google.FlatBuffers;
using SimpleMMO.Game;
using SimpleMMO.Protocol.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using UnityEngine;

namespace SimpleMMO.Network
{
    public class GameServerClient : MonoBehaviour
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private volatile bool _isConnected = false;
        private ConcurrentQueue<byte[]> incomingPackets = new ConcurrentQueue<byte[]>();

        private int sequenceNumber = 0;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<S_LoginSuccess> OnLoginSuccess;
        public event Action<S_LoginFailure> OnLoginFailure;
        public event Action<S_ZoneEntered> OnZoneEntered;
        public event Action<S_WorldSnapshot> OnWorldSnapshot;
        public event Action<S_PlayerJoined> OnPlayerJoined;
        public event Action<S_PlayerLeft> OnPlayerLeft;
        public event Action<S_Chat> OnChatReceived;
        public event Action<S_StateCorrection> OnStateCorrection;
        public event Action<S_GameTick> OnGameTick;

        private static GameServerClient _instance;
        private static readonly object _lock = new object();
        
        public static GameServerClient Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        Debug.LogError("GameServerClient: Instance not initialized. Call Initialize() from main thread first.");
                    }
                    return _instance;
                }
            }
        }
        
        /// <summary>
        /// Initialize the singleton instance. Must be called from Unity main thread.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    Debug.LogWarning("GameServerClient: Already initialized");
                    return;
                }
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameServerClient");
                    _instance = go.AddComponent<GameServerClient>();
                    DontDestroyOnLoad(go);
                    Debug.Log("GameServerClient: Initialized successfully");
                }
            }
        }

        void Awake()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = this;
                    DontDestroyOnLoad(gameObject);
                }
                else if (_instance != this)
                {
                    Debug.LogWarning("GameServerClient: Duplicate instance detected, destroying");
                    Destroy(gameObject);
                    return;
                }
            }
        }

        public void Update()
        {
            ProcessIncomingPackets();
        }

        public void Connect()
        {
            if (_isConnected)
            {
                Debug.LogWarning("Already connected to the server.");
                return;
            }
            try
            {
                string host = ServerConfig.GameServerHost;
                int port = ServerConfig.GameServerPort;

                _client = new TcpClient();
                _client.Connect(host, port);
                _stream = _client.GetStream();
                _isConnected = true;

                _receiveThread = new Thread(ReceiveData);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                Debug.Log($"Connected to game server at {host}:{port}");
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to game server: {ex.Message}");
                _isConnected = false;
                OnDisconnected?.Invoke();
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            try
            {
                _stream?.Close();
                _client?.Close();
                
                if (_receiveThread != null && _receiveThread.IsAlive)
                {
                    _receiveThread.Interrupt();
                    _receiveThread.Join(1000);
                }
                _isConnected = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during disconnection: {ex.Message}");
            }
            Debug.Log("Disconnected from game server");
            OnDisconnected?.Invoke();
        }

        public void SendLogin(string sessionTicket, ulong playerId)
        {
            var packet = PacketExtensions.CreateLoginPacket(sessionTicket, playerId);
            SendPacket(packet);
        }

        public void SendEnterZone(int zoneId = 1)
        {
            var packet = PacketExtensions.CreateEnterZonePacket(zoneId);
            SendPacket(packet);
        }

        public void SendPlayerInput(byte inputFlags, Vector3 mousePosition)
        {
            var packet = PacketExtensions.CreatePlayerInputPacket(inputFlags, mousePosition, (uint)Interlocked.Increment(ref sequenceNumber));
            SendPacket(packet);
        }

        public void SendChat(string message)
        {
            var packet = PacketExtensions.CreateChatPacket(message);
            SendPacket(packet);
        }

        private void SendPacket(byte[] packetData)
        {
            if (!_isConnected || _stream == null)
            {
                Debug.LogWarning("Cannot send packet, not connected to the server.");
                return;
            }

            try
            {
                byte[] sizeBytes = BitConverter.GetBytes(packetData.Length);
                _stream.Write(sizeBytes, 0, 4);
                
                _stream.Write(packetData, 0, packetData.Length);
                _stream.Flush();
                
                Debug.Log($"Sent packet: {packetData.Length} bytes");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending packet: {ex.Message}");
                Disconnect();
            }
        }

        private void ReceiveData()
        {
            try
            {
                while (_isConnected && _stream != null)
                {
                    byte[] sizeBuffer = new byte[4];
                    int bytesRead = 0;
                    while (bytesRead < 4)
                    {
                        int read = _stream.Read(sizeBuffer, bytesRead, 4 - bytesRead);
                        if (read == 0)
                        {
                            Debug.LogWarning("Connection closed while reading packet size.");
                            Disconnect();
                            return;
                        }
                        bytesRead += read;
                    }

                    int packetSize = BitConverter.ToInt32(sizeBuffer, 0);
                    if (packetSize <= 0 || packetSize > 1024 * 1024)
                    {
                        Debug.LogWarning($"Invalid packet size: {packetSize}");
                        Disconnect();
                        return;
                    }
                    byte[] packetBuffer = new byte[packetSize];
                    bytesRead = 0;
                    while (bytesRead < packetSize)
                    {
                        int read = _stream.Read(packetBuffer, bytesRead, packetSize - bytesRead);
                        if (read == 0)
                        {
                            Debug.LogWarning("Connection closed while reading packet data.");
                            Disconnect();
                            return;
                        }
                        bytesRead += read;
                    }
                    incomingPackets.Enqueue(packetBuffer);
                }
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    Debug.LogError($"Error in receive thread: {ex.Message}");
                    Disconnect();
                }
            }
        }

        private void ProcessIncomingPackets()
        {
            while (incomingPackets.Count > 0)
            {
                try
                {
                    if (incomingPackets.TryDequeue(out var packetData))
                    {
                        ProcessPacket(packetData);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing incoming packet: {ex.Message}");
                }
            }
        }

        private void ProcessPacket(byte[] packetData)
        {
            if(!PacketExtensions.TryParsePacket(packetData, out UnifiedPacket packet))
            {
                Debug.LogWarning("Failed to parse incoming packet.");
                return;
            }

            PacketId packetId = packet.Id;
            Debug.Log($"Received packet: {packetId}");

            switch(packetId)
            {
                case PacketId.S_LoginSuccess:
                    OnLoginSuccess?.Invoke(packet.DataAsS_LoginSuccess());
                    break;
                case PacketId.S_LoginFailure:
                    OnLoginFailure?.Invoke(packet.DataAsS_LoginFailure());
                    break;
                case PacketId.S_ZoneEntered:
                    OnZoneEntered?.Invoke(packet.DataAsS_ZoneEntered());
                    break;
                case PacketId.S_WorldSnapshot:
                    OnWorldSnapshot?.Invoke(packet.DataAsS_WorldSnapshot());
                    break;
                case PacketId.S_PlayerJoined:
                    OnPlayerJoined?.Invoke(packet.DataAsS_PlayerJoined());
                    break;
                case PacketId.S_PlayerLeft:
                    OnPlayerLeft?.Invoke(packet.DataAsS_PlayerLeft());
                    break;
                case PacketId.S_Chat:
                    OnChatReceived?.Invoke(packet.DataAsS_Chat());
                    break;
                case PacketId.S_StateCorrection:
                    OnStateCorrection?.Invoke(packet.DataAsS_StateCorrection());
                    break;
                case PacketId.S_GameTick:
                    OnGameTick?.Invoke(packet.DataAsS_GameTick());
                    break;
                default:
                    Debug.LogWarning($"Unhandled packet type: {packetId}");
                    break;
            }
        }

        void OnDestroy()
        {
            Disconnect();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && !Application.isEditor)
                Disconnect();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !Application.isEditor)
                Disconnect();
        }

        public bool IsConnected => _isConnected;
        public uint CurrentSequenceNumber => (uint)sequenceNumber;
    }
}