using UnityEngine;
using SimpleMMO.Network;

namespace SimpleMMO.UI
{
    /// <summary>
    /// Simple integration script to handle chat functionality in game
    /// Add this to your GamePlayScene to enable chat
    /// </summary>
    public class ChatIntegration : MonoBehaviour
    {
        [Header("Chat Settings")]
        [SerializeField] private KeyCode chatToggleKey = KeyCode.T;
        
        private ChatManager chatManager;
        
        void Start()
        {
            // Find or create chat manager
            chatManager = FindObjectOfType<ChatManager>();
            if (chatManager == null)
            {
                Debug.LogWarning("ChatIntegration: No ChatManager found in scene!");
            }
            
            // Subscribe to connection events
            if (GameServerClient.Instance != null)
            {
                GameServerClient.Instance.OnConnected += OnServerConnected;
                GameServerClient.Instance.OnDisconnected += OnServerDisconnected;
            }
        }
        
        void Update()
        {
            if (chatManager == null) return;
            
            // Toggle chat with T key
            if (Input.GetKeyDown(chatToggleKey))
            {
                chatManager.ToggleChatPanel();
            }
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (GameServerClient.Instance != null)
            {
                GameServerClient.Instance.OnConnected -= OnServerConnected;
                GameServerClient.Instance.OnDisconnected -= OnServerDisconnected;
            }
        }
        
        private void OnServerConnected()
        {
            if (chatManager != null)
            {
                chatManager.AddSystemMessage("Connected to server");
            }
        }
        
        private void OnServerDisconnected()
        {
            if (chatManager != null)
            {
                chatManager.AddSystemMessage("Disconnected from server");
            }
        }
        
        // Public methods for UI buttons
        public void OpenChat()
        {
            chatManager?.ShowChatPanel();
        }
        
        public void CloseChat()
        {
            chatManager?.HideChatPanel();
        }
        
        public void SendQuickMessage(string message)
        {
            if (GameServerClient.Instance != null && GameServerClient.Instance.IsConnected)
            {
                GameServerClient.Instance.SendChat(message);
            }
        }
    }
}