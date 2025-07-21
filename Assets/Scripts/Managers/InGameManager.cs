using UnityEngine;
using SimpleMMO.Network;
using SimpleMMO.Game;
using CppMMO.Protocol;
using SimpleMMO.Protocol.Extensions;

namespace SimpleMMO.Managers
{
    [DefaultExecutionOrder(-50)]
    public class InGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform playerSpawnPoint;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // Game State
        private PlayerController localPlayer;
        private bool isGameStarted = false;
        private bool isConnecting = false;
        
        // Singleton for scene-specific management
        public static InGameManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("InGameManager: Initialized for GameScene");
            }
            else
            {
                Debug.LogWarning("InGameManager: Multiple instances detected, destroying duplicate");
                Destroy(gameObject);
            }
        }

        void Start()
        {
            StartGameSequence();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                UnsubscribeFromEvents();
            }
        }

        private void StartGameSequence()
        {
            if (isConnecting)
            {
                Debug.LogWarning("InGameManager: Already connecting to game server");
                return;
            }

            if (!GameInitializer.AreManagersInitialized())
            {
                Debug.LogError("InGameManager: Core managers not initialized");
                return;
            }

            if (!ValidateGameData())
            {
                Debug.LogError("InGameManager: Invalid game data, cannot start game");
                return;
            }

            Debug.Log("InGameManager: Starting game sequence...");
            isConnecting = true;

            SubscribeToGameServerEvents();

            ConnectToGameServer();
        }

        private bool ValidateGameData()
        {
            string sessionTicket = SessionManager.Instance?.SessionTicket;
            if (string.IsNullOrEmpty(sessionTicket))
            {
                Debug.LogError("InGameManager: No valid session ticket");
                return false;
            }

            var selectedCharacter = PlayerDataManager.Instance?.GetSelectedCharacter();
            if (selectedCharacter == null)
            {
                Debug.LogError("InGameManager: No character selected");
                return false;
            }

            LogDebug($"Session: [REDACTED], Character: {selectedCharacter.name} (ID: {selectedCharacter.playerId})");
            return true;
        }

        private void SubscribeToGameServerEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient == null)
            {
                Debug.LogError("InGameManager: GameServerClient not available");
                return;
            }

            gameClient.OnConnected += OnGameServerConnected;
            gameClient.OnDisconnected += OnGameServerDisconnected;

            gameClient.OnLoginSuccess += OnLoginSuccess;
            gameClient.OnLoginFailure += OnLoginFailure;

            gameClient.OnZoneEntered += OnZoneEntered;
            gameClient.OnWorldSnapshot += OnWorldSnapshot;
            gameClient.OnPlayerJoined += OnPlayerJoined;
            gameClient.OnPlayerLeft += OnPlayerLeft;

            LogDebug("Subscribed to GameServer events");
        }

        private void UnsubscribeFromEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient == null) return;

            gameClient.OnConnected -= OnGameServerConnected;
            gameClient.OnDisconnected -= OnGameServerDisconnected;
            gameClient.OnLoginSuccess -= OnLoginSuccess;
            gameClient.OnLoginFailure -= OnLoginFailure;
            gameClient.OnZoneEntered -= OnZoneEntered;
            gameClient.OnWorldSnapshot -= OnWorldSnapshot;
            gameClient.OnPlayerJoined -= OnPlayerJoined;
            gameClient.OnPlayerLeft -= OnPlayerLeft;

            LogDebug("Unsubscribed from GameServer events");
        }

        private void ConnectToGameServer()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient == null)
            {
                Debug.LogError("InGameManager: GameServerClient not available");
                isConnecting = false;
                return;
            }

            LogDebug("Connecting to game server...");
            gameClient.Connect();
        }

        #region GameServer Event Handlers

        private void OnGameServerConnected()
        {
            LogDebug("Connected to game server, sending login...");

            var sessionTicket = SessionManager.Instance?.SessionTicket;
            var selectedCharacter = PlayerDataManager.Instance?.GetSelectedCharacter();

            if (!string.IsNullOrEmpty(sessionTicket) && selectedCharacter != null)
            {
                GameServerClient.Instance?.SendLogin(sessionTicket, selectedCharacter.playerId);
            }
            else
            {
                Debug.LogError("InGameManager: Missing session or character data for login");
                isConnecting = false;
            }
        }

        private void OnGameServerDisconnected()
        {
            Debug.LogWarning("InGameManager: Disconnected from game server");
            isConnecting = false;
            isGameStarted = false;
        }

        private void OnLoginSuccess(S_LoginSuccess loginData)
        {
            LogDebug($"Login successful! Entering zone...");

            GameServerClient.Instance?.SendEnterZone(1);
        }

        private void OnLoginFailure(S_LoginFailure loginFailure)
        {
            Debug.LogError($"InGameManager: Login failed - {loginFailure.ErrorMessage}");
            isConnecting = false;

        }

        private void OnZoneEntered(S_ZoneEntered zoneData)
        {
            LogDebug($"Entered zone {zoneData.ZoneId}");

            SpawnLocalPlayer(zoneData.MyPlayer);

            if (zoneData.OtherPlayersLength > 0)
            {
                LogDebug($"Found {zoneData.OtherPlayersLength} other players in zone");
                for (int i = 0; i < zoneData.OtherPlayersLength; i++)
                {
                    var otherPlayer = zoneData.OtherPlayers(i);
                    if (otherPlayer.HasValue)
                    {
                        LogDebug($"Other player: {otherPlayer.Value.Name} at {otherPlayer.Value.Position}");
                    }
                }
            }

            isConnecting = false;
            isGameStarted = true;
            LogDebug("Game started successfully!");
        }

        private void OnWorldSnapshot(S_WorldSnapshot snapshot)
        {
            // Delegated to WorldSyncManager - this method is no longer needed
            // WorldSyncManager directly subscribes to OnWorldSnapshot event
        }

        private void OnPlayerJoined(S_PlayerJoined playerJoined)
        {
            LogDebug($"Player joined: {playerJoined.PlayerInfo?.Name}");
            // TODO: MultiplayerManager.SpawnRemotePlayer(playerJoined.PlayerInfo);
        }

        private void OnPlayerLeft(S_PlayerLeft playerLeft)
        {
            LogDebug($"Player left: ID {playerLeft.PlayerId}");
            // TODO: MultiplayerManager.DespawnRemotePlayer(playerLeft.PlayerId);
        }

        #endregion

        private void SpawnLocalPlayer(PlayerInfo? playerInfo)
        {
            if (!playerInfo.HasValue)
            {
                Debug.LogError("InGameManager: Invalid player info for local player");
                return;
            }

            var info = playerInfo.Value;
            Vector3 spawnPosition = info.Position?.ToUnityVector3() ?? Vector3.zero;

            if (playerPrefab != null)
            {
                GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                localPlayer = playerObj.GetComponent<PlayerController>();

                if (localPlayer != null)
                {
                    localPlayer.Initialize(info.PlayerId, info.Name, isLocal: true);
                    LogDebug($"Local player spawned: {info.Name} at {spawnPosition}");

                    // Register local player with WorldSyncManager
                    if (WorldSyncManager.Instance != null)
                    {
                        WorldSyncManager.Instance.SetLocalPlayer(localPlayer);
                        // Ensure WorldSyncManager is properly subscribed to events
                        WorldSyncManager.Instance.RetrySubscription();
                    }

                    SetupCameraFollow(playerObj.transform);
                }
                else
                {
                    Debug.LogError("InGameManager: PlayerController component not found on player prefab");
                }
            }
            else
            {
                Debug.LogError("InGameManager: Player prefab not assigned");
            }
        }

        private void SetupCameraFollow(Transform playerTransform)
        {
            // TODO: CameraController 구현 후 적용
            Camera.main.transform.position = new Vector3(
                playerTransform.position.x, 
                playerTransform.position.y, 
                -10f
            );
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[InGameManager] {message}");
            }
        }

        #region Public API

        public bool IsGameStarted => isGameStarted;
        public bool IsConnecting => isConnecting;
        public PlayerController GetLocalPlayer() => localPlayer;

        public void DisconnectFromGame()
        {
            if (GameServerClient.Instance != null)
            {
                GameServerClient.Instance.Disconnect();
            }
            
            isGameStarted = false;
            isConnecting = false;
        }

        #endregion
    }
}
