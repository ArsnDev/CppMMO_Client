using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using SimpleMMO.Game;

namespace SimpleMMO.Network
{
    [Serializable]
    public class LoginRequest
    {
        public string username { get; set; }
        public string password { get; set; }
        
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(username) && 
                   !string.IsNullOrWhiteSpace(password) &&
                   username.Length >= 3 && 
                   password.Length >= 6;
        }
    }

    [Serializable]
    public class LoginResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string sessionTicket { get; set; }
        public PlayerInfoDto playerInfo { get; set; }
    }

    [Serializable]
    public class PlayerInfoDto
    {
        public ulong playerId { get; set; }
        public string name { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public int hp { get; set; }
        public int maxHp { get; set; }
    }

    [Serializable]
    public class CharacterListResponse
    {
        public bool success { get; set; }
        public List<PlayerInfoDto> characters { get; set; }
        public string message { get; set; }
    }

    [Serializable]
    public class CharacterCreateRequest
    {
        public string characterName { get; set; }
        
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(characterName) && 
                   characterName.Length >= 2 && 
                   characterName.Length <= 20;
        }
    }

    [Serializable]
    public class CharacterCreateResponse
    {
        public bool success { get; set; }
        public PlayerInfoDto character { get; set; }
        public string message { get; set; }
    }

    [Serializable]
    public class VerifyRequest
    {
        public string sessionTicket { get; set; }
        public ulong playerId { get; set; }
        
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(sessionTicket) && playerId > 0;
        }
    }

    [Serializable]
    public class VerifyResponse
    {
        public bool success { get; set; }
        public PlayerInfoDto playerInfo { get; set; }
        public string message { get; set; }
    }

    public class AuthApiClient : MonoBehaviour
    {
        private static AuthApiClient _instance;
        private static readonly object _lock = new object();
        
        public static AuthApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject go = new GameObject("AuthApiClient");
                            _instance = go.AddComponent<AuthApiClient>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
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
                    Destroy(gameObject);
                    return;
                }
            }
        }

        public void Register(string username, string password, Action<LoginResponse> onSuccess, Action<string> onError)
        {
            var request = new LoginRequest { username = username, password = password };
            if (!request.IsValid())
            {
                onError?.Invoke("Username must be at least 3 characters and password at least 6 characters");
                return;
            }
            
            StartCoroutine(SendPostRequest("register", request, onSuccess, onError));
        }

        public void Login(string username, string password, Action<LoginResponse> onSuccess, Action<string> onError)
        {
            var request = new LoginRequest { username = username, password = password };
            if (!request.IsValid())
            {
                onError?.Invoke("Username must be at least 3 characters and password at least 6 characters");
                return;
            }
            
            StartCoroutine(SendPostRequest("login", request, onSuccess, onError));
        }

        public void GetCharacters(string sessionTicket, Action<CharacterListResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(sessionTicket))
            {
                onError?.Invoke("Session ticket is required");
                return;
            }
            
            StartCoroutine(SendGetRequest("characters", sessionTicket, onSuccess, onError));
        }

        public void CreateCharacter(string sessionTicket, string characterName, Action<CharacterCreateResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(sessionTicket))
            {
                onError?.Invoke("Session ticket is required");
                return;
            }
            
            var request = new CharacterCreateRequest { characterName = characterName };
            if (!request.IsValid())
            {
                onError?.Invoke("Character name must be 2-20 characters long");
                return;
            }
            
            StartCoroutine(SendPostRequestWithHeader("characters", sessionTicket, request, onSuccess, onError));
        }

        public void VerifyPlayer(string sessionTicket, ulong playerId, Action<VerifyResponse> onSuccess, Action<string> onError)
        {
            var request = new VerifyRequest { sessionTicket = sessionTicket, playerId = playerId };
            if (!request.IsValid())
            {
                onError?.Invoke("Session ticket is required and player ID must be greater than 0");
                return;
            }
            
            StartCoroutine(SendPostRequest("verify", request, onSuccess, onError));
        }

        private IEnumerator SendRequest<TResponse>(UnityWebRequest webRequest, string endpoint, 
            Action<TResponse> onSuccess, Action<string> onError)
        {
            Debug.Log($"AuthAPI: Sending {webRequest.method} {endpoint} request to {webRequest.url}");
            
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = webRequest.downloadHandler.text;
                    Debug.Log($"AuthAPI: {endpoint} response: {responseText}");
                    
                    TResponse response = JsonConvert.DeserializeObject<TResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"AuthAPI: Failed to parse {endpoint} response: {e.Message}");
                    onError?.Invoke($"Failed to parse response: {e.Message}");
                }
            }
            else
            {
                string errorMessage = $"HTTP {webRequest.responseCode}: {webRequest.error}";
                if (!string.IsNullOrEmpty(webRequest.downloadHandler.text))
                {
                    errorMessage += $"\n{webRequest.downloadHandler.text}";
                }
                Debug.LogError($"AuthAPI: {endpoint} failed: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
        }

        private IEnumerator SendPostRequest<TRequest, TResponse>(string endpoint, TRequest requestData, Action<TResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(ServerConfig.AuthServerUrl))
            {
                onError?.Invoke("Auth server URL not configured");
                yield break;
            }
            
            string url = $"{ServerConfig.AuthServerUrl}/api/auth/{endpoint}";
            
            string jsonData;
            try
            {
                jsonData = JsonConvert.SerializeObject(requestData);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Failed to serialize request data: {e.Message}");
                yield break;
            }

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return SendRequest<TResponse>(webRequest, endpoint, onSuccess, onError);
            }
        }

        private IEnumerator SendPostRequestWithHeader<TRequest, TResponse>(string endpoint, string sessionTicket, TRequest requestData, Action<TResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(ServerConfig.AuthServerUrl))
            {
                onError?.Invoke("Auth server URL not configured");
                yield break;
            }
            
            string url = $"{ServerConfig.AuthServerUrl}/api/auth/{endpoint}";
            
            string jsonData;
            try
            {
                jsonData = JsonConvert.SerializeObject(requestData);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Failed to serialize request data: {e.Message}");
                yield break;
            }

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("sessionTicket", sessionTicket);

                yield return SendRequest<TResponse>(webRequest, endpoint, onSuccess, onError);
            }
        }

        private IEnumerator SendGetRequest<TResponse>(string endpoint, string sessionTicket, Action<TResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(ServerConfig.AuthServerUrl))
            {
                onError?.Invoke("Auth server URL not configured");
                yield break;
            }
            
            string url = $"{ServerConfig.AuthServerUrl}/api/auth/{endpoint}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("sessionTicket", sessionTicket);

                yield return SendRequest<TResponse>(webRequest, endpoint, onSuccess, onError);
            }
        }
    }
}