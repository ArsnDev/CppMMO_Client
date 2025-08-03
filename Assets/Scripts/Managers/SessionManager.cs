using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleMMO.Managers
{
    public class SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;
        private static readonly object _lock = new object();
        
        public static SessionManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        Debug.LogError("SessionManager: Instance not initialized. Call Initialize() from main thread first.");
                    }
                    return _instance;
                }
            }
        }
        
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    Debug.LogWarning("SessionManager: Already initialized");
                    return;
                }
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("SessionManager");
                    _instance = go.AddComponent<SessionManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("SessionManager: Initialized successfully");
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
                    Debug.LogWarning("SessionManager: Duplicate instance detected, destroying");
                    Destroy(gameObject);
                    return;
                }
            }
        }

        [Header("Session Data")]
        [SerializeField] private string sessionTicket;
        [SerializeField] private ulong selectedPlayerId;

        public string SessionTicket
        {
            get => sessionTicket;
            private set => sessionTicket = value;
        }

        public ulong SelectedPlayerId
        {
            get => selectedPlayerId;
            private set => selectedPlayerId = value;
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(sessionTicket);
        public bool HasSelectedCharacter => selectedPlayerId > 0;

        public void SaveSession(string ticket)
        {
            sessionTicket = ticket;
            Debug.Log("Session saved: [REDACTED]");
        }

        public void SelectCharacter(ulong playerId)
        {
            selectedPlayerId = playerId;
            Debug.Log($"Character selected: playerId={playerId}");
        }

        public void ClearSession()
        {
            sessionTicket = string.Empty;
            selectedPlayerId = 0;
            Debug.Log("Session cleared");
        }

        public bool ValidateSession()
        {
            if (string.IsNullOrEmpty(sessionTicket))
            {
                Debug.LogWarning("Session is not valid. Please log in again.");
                return false;
            }
            Debug.Log("Session is valid.");
            return true;
        }

        private void OnApplicationPause(bool pause)
        {
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && IsLoggedIn)
            {
                ValidateSession();
            }
        }
    }
}