using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleMMO.Network;
using CppMMO.Protocol;

namespace SimpleMMO.UI
{
    public class ChatManager : MonoBehaviour
    {
        [Header("Chat UI Elements")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private ScrollRect chatScrollView;
        [SerializeField] private Transform chatContentParent;
        [SerializeField] private TMP_InputField chatInputField;
        [SerializeField] private Button sendButton;
        
        [Header("Chat Message Prefab")]
        [SerializeField] private GameObject chatMessagePrefab;
        
        [Header("Settings")]
        [SerializeField] private int maxChatMessages = 100;
        [SerializeField] private bool showTimestamp = true;
        
        private List<GameObject> chatMessages = new List<GameObject>();
        private bool isChatPanelVisible = false;
        
        // Cache for player names
        private Dictionary<ulong, string> playerNameCache = new Dictionary<ulong, string>();
        
        private static ChatManager _instance;
        public static ChatManager Instance => _instance;
        
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
                return;
            }
        }
        
        void Start()
        {
            InitializeUI();
            SubscribeToChatEvents();
        }
        
        void OnDestroy()
        {
            UnsubscribeFromChatEvents();
            if (_instance == this)
                _instance = null;
        }
        
        private void InitializeUI()
        {
            // Setup send button
            if (sendButton != null)
                sendButton.onClick.AddListener(SendChatMessage);
            
            // Setup input field
            if (chatInputField != null)
            {
                chatInputField.onEndEdit.AddListener(OnChatInputEndEdit);
                chatInputField.characterLimit = 200;
            }
            
            // Initially hide chat panel or set default visibility
            if (chatPanel != null)
                chatPanel.SetActive(isChatPanelVisible);
        }
        
        private void SubscribeToChatEvents()
        {
            if (GameServerClient.Instance != null)
            {
                GameServerClient.Instance.OnChatReceived += OnChatMessageReceived;
                GameServerClient.Instance.OnPlayerJoined += OnPlayerJoined;
                GameServerClient.Instance.OnPlayerLeft += OnPlayerLeft;
            }
        }
        
        private void UnsubscribeFromChatEvents()
        {
            if (GameServerClient.Instance != null)
            {
                GameServerClient.Instance.OnChatReceived -= OnChatMessageReceived;
                GameServerClient.Instance.OnPlayerJoined -= OnPlayerJoined;
                GameServerClient.Instance.OnPlayerLeft -= OnPlayerLeft;
            }
        }
        
        void Update()
        {
            // Toggle chat panel with Enter key
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!isChatPanelVisible)
                {
                    ShowChatPanel();
                }
                else if (chatInputField != null && chatInputField.isFocused)
                {
                    SendChatMessage();
                }
                else
                {
                    FocusOnInput();
                }
            }
            
            // Hide chat panel with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideChatPanel();
            }
        }
        
        public void ShowChatPanel()
        {
            isChatPanelVisible = true;
            if (chatPanel != null)
                chatPanel.SetActive(true);
            FocusOnInput();
        }
        
        public void HideChatPanel()
        {
            isChatPanelVisible = false;
            if (chatPanel != null)
                chatPanel.SetActive(false);
            if (chatInputField != null)
            {
                chatInputField.text = "";
                chatInputField.DeactivateInputField();
            }
        }
        
        public void ToggleChatPanel()
        {
            if (isChatPanelVisible)
                HideChatPanel();
            else
                ShowChatPanel();
        }
        
        private void FocusOnInput()
        {
            if (chatInputField != null)
            {
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }
        }
        
        private void OnChatInputEndEdit(string message)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendChatMessage();
            }
        }
        
        public void SendChatMessage()
        {
            if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text))
                return;
            
            string message = chatInputField.text.Trim();
            if (message.Length == 0)
                return;
            
            // Send message to server
            if (GameServerClient.Instance != null && GameServerClient.Instance.IsConnected)
            {
                GameServerClient.Instance.SendChat(message);
                chatInputField.text = "";
                FocusOnInput();
            }
            else
            {
                Debug.LogWarning("Cannot send chat message: Not connected to server");
                AddSystemMessage("Not connected to server");
            }
        }
        
        private void OnChatMessageReceived(S_Chat chatData)
        {
            // S_Chat is a struct, so we need to check if it has valid data
            if (string.IsNullOrEmpty(chatData.Message))
                return;
            
            string playerName = GetPlayerNameById((ulong)chatData.PlayerId);
            string message = chatData.Message ?? "";
            
            AddChatMessage(playerName, message, false);
        }
        
        private void OnPlayerJoined(S_PlayerJoined playerJoinedData)
        {
            Debug.Log($"[ChatManager] OnPlayerJoined called");
            
            if (playerJoinedData.PlayerInfo.HasValue)
            {
                var playerInfo = playerJoinedData.PlayerInfo.Value;
                var playerId = playerInfo.PlayerId;
                var playerName = playerInfo.Name;
                
                Debug.Log($"[ChatManager] Player joined - ID: {playerId}, Name: '{playerName}'");
                
                if (!string.IsNullOrEmpty(playerName))
                {
                    playerNameCache[playerId] = playerName;
                    Debug.Log($"[ChatManager] Cached player name: {playerName} for ID: {playerId}");
                }
                
                AddSystemMessage($"{playerName ?? $"Player{playerId}"} joined the game");
            }
            else
            {
                Debug.LogWarning("[ChatManager] PlayerJoined event received but PlayerInfo is null");
            }
        }
        
        private void OnPlayerLeft(S_PlayerLeft playerLeftData)
        {
            ulong playerId = playerLeftData.PlayerId;
            string playerName = GetPlayerNameById(playerId);
            
            // Remove from cache
            playerNameCache.Remove(playerId);
            
            AddSystemMessage($"{playerName} left the game");
        }
        
        private string GetPlayerNameById(ulong playerId)
        {
            // First check our cache
            if (playerNameCache.TryGetValue(playerId, out string cachedName))
            {
                return cachedName;
            }
            
            // If this is our own message, try to get from selected character first
            if (SimpleMMO.Managers.PlayerDataManager.Instance != null)
            {
                var selectedChar = SimpleMMO.Managers.PlayerDataManager.Instance.GetSelectedCharacter();
                if (selectedChar != null && selectedChar.playerId == playerId)
                {
                    // Cache it for future use
                    playerNameCache[playerId] = selectedChar.name;
                    Debug.Log($"Found own character name: {selectedChar.name} for ID: {playerId}");
                    return selectedChar.name;
                }
            }
            
            // Try to get player name from PlayerDataManager
            if (SimpleMMO.Managers.PlayerDataManager.Instance != null)
            {
                var character = SimpleMMO.Managers.PlayerDataManager.Instance.GetCharacterById(playerId);
                if (character != null && !string.IsNullOrEmpty(character.name))
                {
                    // Cache it for future use
                    playerNameCache[playerId] = character.name;
                    Debug.Log($"Found cached character name: {character.name} for ID: {playerId}");
                    return character.name;
                }
            }
            
            // Fallback to a more user-friendly name
            string fallbackName = $"User{playerId}";
            playerNameCache[playerId] = fallbackName;
            Debug.LogWarning($"Could not find player name for ID: {playerId}, using fallback: {fallbackName}");
            return fallbackName;
        }
        
        public void CachePlayerName(ulong playerId, string playerName)
        {
            if (!string.IsNullOrEmpty(playerName))
            {
                playerNameCache[playerId] = playerName;
            }
        }
        
        public void AddChatMessage(string playerName, string message, bool isSystemMessage = false)
        {
            if (chatMessagePrefab == null || chatContentParent == null)
                return;
            
            // Create new chat message
            GameObject messageObj = Instantiate(chatMessagePrefab, chatContentParent);
            
            // Configure message text
            TextMeshProUGUI messageText = messageObj.GetComponent<TextMeshProUGUI>();
            if (messageText == null)
                messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (messageText != null)
            {
                string timestamp = showTimestamp ? $"[{DateTime.Now:HH:mm}] " : "";
                string formattedMessage;
                
                if (isSystemMessage)
                {
                    formattedMessage = $"{timestamp}<color=yellow>{message}</color>";
                }
                else
                {
                    formattedMessage = $"{timestamp}<color=white>{playerName}:</color> {message}";
                }
                
                messageText.text = formattedMessage;
            }
            
            chatMessages.Add(messageObj);
            
            // Remove old messages if we exceed the limit
            while (chatMessages.Count > maxChatMessages)
            {
                GameObject oldMessage = chatMessages[0];
                chatMessages.RemoveAt(0);
                if (oldMessage != null)
                    Destroy(oldMessage);
            }
            
            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (chatScrollView != null)
                chatScrollView.verticalNormalizedPosition = 0f;
        }
        
        public void AddSystemMessage(string message)
        {
            AddChatMessage("", message, true);
        }
        
        public void ClearChat()
        {
            foreach (var message in chatMessages)
            {
                if (message != null)
                    Destroy(message);
            }
            chatMessages.Clear();
        }
    }
}