using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleMMO.Managers
{
    public class  SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;
        public static SessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SessionManager");
                    _instance = go.AddComponent<SessionManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        [Header("Session Data")]
        [SerializeField] private string sessionTicket;

        public string SessionTicket
        {
            get => sessionTicket;
            private set => sessionTicket = value;
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(sessionTicket);

        public void SaveSession(string ticket)
        {
            sessionTicket = ticket;
            Debug.Log($"Session saved: {(ticket.Length > 8 ? ticket.Substring(0, 8) + "..." : ticket)}");
        }

        public void ClearSession()
        {
            sessionTicket = string.Empty;
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

        private void OnApplicationFocus(bool focus)
        {
        }
    }
    }
}