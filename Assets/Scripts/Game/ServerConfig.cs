using UnityEngine;
using System;

namespace SimpleMMO.Game
{
    [Serializable]
    public class ServerConfig
    {
        [Serializable]
        public class ServerInfo
        {
            public string host;
            public int port;
        }

        public ServerInfo auth_server;
        public ServerInfo game_server;

        private static ServerConfig _instance;
        public static ServerConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadConfig();
                }
                return _instance;
            }
        }

        private static ServerConfig LoadConfig()
        {
            try
            {
                var configText = Resources.Load<TextAsset>("config/server_config");
                if (configText == null)
                {
                    Debug.LogError("ServerConfig: server_config.json not found in Resources/config/");
                    return CreateDefaultConfig();
                }

                var config = JsonUtility.FromJson<ServerConfig>(configText.text);
                Debug.Log($"ServerConfig loaded: AuthServer={config.auth_server.host}:{config.auth_server.port}, GameServer={config.game_server.host}:{config.game_server.port}");
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"ServerConfig: Failed to load config - {e.Message}");
                return CreateDefaultConfig();
            }
        }

        private static ServerConfig CreateDefaultConfig()
        {
            Debug.LogWarning("ServerConfig: Using default configuration");
            return new ServerConfig
            {
                auth_server = new ServerInfo { host = "127.0.0.1", port = 5278 },
                game_server = new ServerInfo { host = "127.0.0.1", port = 8080 }
            };
        }

        // 편의 메서드
        public static string AuthServerUrl => $"http://{Instance.auth_server.host}:{Instance.auth_server.port}";
        public static string GameServerHost => Instance.game_server.host;
        public static int GameServerPort => Instance.game_server.port;
    }
}