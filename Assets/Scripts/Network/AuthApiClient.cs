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
        public string username;
        public string password;
    }

    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string message;
        public string sessionTicket;
        public PlayerInfoDto playerInfo;
    }

    [Serializable]
    public class PlayerInfoDto
    {
        public ulong playerId;
        public string name;
        public float posX;
        public float posY;
        public int hp;
        public int maxHp;
    }

    [Serializable]
    public class CharacterListResponse
    {
        public bool success;
        public List<PlayerInfoDto> characters;
        public string message;
    }

    [Serializable]
    public class CharacterCreateRequest
    {
        public string characterName;
    }

    [Serializable]
    public class CharacterCreateResponse
    {
        public bool success;
        public PlayerInfoDto character;
        public string message;
    }

    [Serializable]
    public class VerifyRequest
    {
        public string sessionTicket;
        public ulong playerId;
    }

    [Serializable]
    public class VerifyResponse
    {
        public bool success;
        public PlayerInfoDto playerInfo;
        public string message;
    }

    public class AuthApiClient : MonoBehaviour
    {
        private static AuthApiClient _instance;
        public static AuthApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AuthApiClient");
                    _instance = go.AddComponent<AuthApiClient>();
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

        public void Register(string username, string password, Action<LoginResponse> onSuccess, Action<string> onError)
        {
            var request = new LoginRequest { username = username, password = password };
            StartCoroutine(SendPostRequest("register", request, onSuccess, onError));
        }

        public void Login(string username, string password, Action<LoginResponse> onSuccess, Action<string> onError)
        {
            var request = new LoginRequest { username = username, password = password };
            StartCoroutine(SendPostRequest("login", request, onSuccess, onError));
        }

        public void GetCharacters(string sessionTicket, Action<CharacterListResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SendGetRequest("characters", sessionTicket, onSuccess, onError));
        }

        public void CreateCharacter(string sessionTicket, string characterName, Action<CharacterCreateResponse> onSuccess, Action<string> onError)
        {
            var request = new CharacterCreateRequest { characterName = characterName };
            StartCoroutine(SendPostRequestWithHeader("characters", sessionTicket, request, onSuccess, onError));
        }

        public void VerifyPlayer(string sessionTicket, ulong playerId, Action<VerifyResponse> onSuccess, Action<string> onError)
        {
            var request = new VerifyRequest { sessionTicket = sessionTicket, playerId = playerId };
            StartCoroutine(SendPostRequest("verify", request, onSuccess, onError));
        }

        private IEnumerator SendPostRequest<TRequest, TResponse>(string endpoint, TRequest requestData, Action<TResponse> onSuccess, Action<string> onError)
        {
            string url = $"{ServerConfig.AuthServerUrl}/api/auth/{endpoint}";
            string jsonData = JsonConvert.SerializeObject(requestData);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                Debug.Log($"AuthAPI: Sending {endpoint} request to {url}");

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
        }

        private IEnumerator SendPostRequestWithHeader<TRequest, TResponse>(string endpoint, string sessionTicket, TRequest requestData, Action<TResponse> onSuccess, Action<string> onError)
        {
            string url = $"{ServerConfig.AuthServerUrl}/api/auth/{endpoint}";
            string jsonData = JsonConvert.SerializeObject(requestData);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("sessionTicket", sessionTicket);

                Debug.Log($"AuthAPI: Sending {endpoint} request to {url} with session ticket");

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
        }

        private IEnumerator SendGetRequest<TResponse>(string endpoint, string sessionTicket, Action<TResponse> onSuccess, Action<string> onError)
        {
            string url = $"{ServerConfig.AuthServerUrl}/api/auth/{endpoint}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("sessionTicket", sessionTicket);

                Debug.Log($"AuthAPI: Sending GET {endpoint} request to {url}");

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
        }
    }
}