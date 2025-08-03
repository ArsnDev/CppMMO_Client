using UnityEngine;
using System;

namespace SimpleMMO.Game
{
    [Serializable]
    public class GameConfig
    {
        [Serializable]
        public class MapConfig
        {
            public float width;
            public float height;
        }

        [Serializable]
        public class GameplayConfig
        {
            public float chat_range;
            public float aoi_range;
            public float move_speed;
            public int tick_rate;
        }

        [Serializable]
        public class NetworkConfig
        {
            public int snapshot_rate;
            public int reconnect_timeout_minutes;
        }


        public MapConfig map;
        public GameplayConfig gameplay;
        public NetworkConfig network;

        private static GameConfig _instance;
        public static GameConfig Instance
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

        private static GameConfig LoadConfig()
        {
            try
            {
                var configText = Resources.Load<TextAsset>("config/game_config");
                if (configText == null)
                {
                    Debug.LogError("GameConfig: game_config.json not found in Resources/config/");
                    return CreateDefaultConfig();
                }

                var config = JsonUtility.FromJson<GameConfig>(configText.text);
                Debug.Log($"GameConfig loaded: Map={config.map.width}x{config.map.height}, MoveSpeed={config.gameplay.move_speed}, TickRate={config.gameplay.tick_rate}");
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"GameConfig: Failed to load config - {e.Message}");
                return CreateDefaultConfig();
            }
        }

        private static GameConfig CreateDefaultConfig()
        {
            Debug.LogWarning("GameConfig: Using default configuration");
            return new GameConfig
            {
                map = new MapConfig { width = 200f, height = 200f },
                gameplay = new GameplayConfig 
                { 
                    chat_range = 50f, 
                    aoi_range = 30f, 
                    move_speed = 5f, 
                    tick_rate = 30 
                },
                network = new NetworkConfig 
                { 
                    snapshot_rate = 60, 
                    reconnect_timeout_minutes = 5 
                }
            };
        }

        public static float MoveSpeed => Instance.gameplay.move_speed;
        public static float AOIRange => Instance.gameplay.aoi_range;
        public static int TickRate => Instance.gameplay.tick_rate;
        public static Vector2 MapSize => new Vector2(Instance.map.width, Instance.map.height);
    }
}