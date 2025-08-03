using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleMMO.UI
{
    /// <summary>
    /// Utility script to help setup chat UI in the editor
    /// This creates a basic chat UI structure programmatically
    /// </summary>
    public class ChatUISetup : MonoBehaviour
    {
        [Header("Setup Settings")]
        [SerializeField] private bool autoSetupOnStart = false;
        [SerializeField] private Canvas targetCanvas;
        
        [Header("UI Styling")]
        [SerializeField] private Color chatPanelColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color inputFieldColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Vector2 chatPanelSize = new Vector2(400, 300);
        [SerializeField] private Vector2 chatPanelPosition = new Vector2(-200, 100);
        
        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupChatUI();
            }
        }
        
        [ContextMenu("Setup Chat UI")]
        public void SetupChatUI()
        {
            if (targetCanvas == null)
                targetCanvas = FindObjectOfType<Canvas>();
            
            if (targetCanvas == null)
            {
                Debug.LogError("ChatUISetup: No Canvas found! Please assign a target canvas.");
                return;
            }
            
            // Create chat panel
            GameObject chatPanel = CreateChatPanel();
            
            // Create scroll view for messages
            GameObject scrollView = CreateScrollView(chatPanel);
            
            // Create input field
            GameObject inputField = CreateInputField(chatPanel);
            
            // Create send button
            GameObject sendButton = CreateSendButton(chatPanel);
            
            // Setup ChatManager component
            ChatManager chatManager = targetCanvas.GetComponent<ChatManager>();
            if (chatManager == null)
            {
                chatManager = targetCanvas.gameObject.AddComponent<ChatManager>();
            }
            
            // Assign references using reflection or manual assignment
            SetupChatManagerReferences(chatManager, chatPanel, scrollView, inputField, sendButton);
            
            Debug.Log("Chat UI setup complete! Don't forget to create a chat message prefab.");
        }
        
        private GameObject CreateChatPanel()
        {
            GameObject chatPanel = new GameObject("ChatPanel");
            chatPanel.transform.SetParent(targetCanvas.transform);
            
            RectTransform rectTransform = chatPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = chatPanelPosition;
            rectTransform.sizeDelta = chatPanelSize;
            
            Image panelImage = chatPanel.AddComponent<Image>();
            panelImage.color = chatPanelColor;
            
            return chatPanel;
        }
        
        private GameObject CreateScrollView(GameObject parent)
        {
            GameObject scrollView = new GameObject("ChatScrollView");
            scrollView.transform.SetParent(parent.transform);
            
            RectTransform rectTransform = scrollView.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(5, 35); // Leave space for input field
            rectTransform.offsetMax = new Vector2(-5, -5);
            
            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            // Create content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            
            ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = contentRect;
            
            return scrollView;
        }
        
        private GameObject CreateInputField(GameObject parent)
        {
            GameObject inputField = new GameObject("ChatInputField");
            inputField.transform.SetParent(parent.transform);
            
            RectTransform rectTransform = inputField.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(5, 5);
            rectTransform.sizeDelta = new Vector2(-80, 25);
            
            Image fieldImage = inputField.AddComponent<Image>();
            fieldImage.color = inputFieldColor;
            
            TMP_InputField tmpInputField = inputField.AddComponent<TMP_InputField>();
            
            // Create text area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputField.transform);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = Vector2.zero;
            textAreaRect.offsetMin = new Vector2(5, 2);
            textAreaRect.offsetMax = new Vector2(-5, -2);
            
            // Create placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform);
            RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Type your message...";
            placeholderText.color = Color.gray;
            placeholderText.fontSize = 14;
            
            // Create text component
            GameObject text = new GameObject("Text");
            text.transform.SetParent(textArea.transform);
            RectTransform textRect = text.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
            textComponent.text = "";
            textComponent.color = Color.white;
            textComponent.fontSize = 14;
            
            tmpInputField.textViewport = textAreaRect;
            tmpInputField.textComponent = textComponent;
            tmpInputField.placeholder = placeholderText;
            
            return inputField;
        }
        
        private GameObject CreateSendButton(GameObject parent)
        {
            GameObject sendButton = new GameObject("SendButton");
            sendButton.transform.SetParent(parent.transform);
            
            RectTransform rectTransform = sendButton.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(1, 0);
            rectTransform.anchoredPosition = new Vector2(-5, 5);
            rectTransform.sizeDelta = new Vector2(70, 25);
            
            Image buttonImage = sendButton.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.5f, 1f, 1f);
            
            Button button = sendButton.AddComponent<Button>();
            
            // Create button text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(sendButton.transform);
            
            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = buttonText.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Send";
            textComponent.color = Color.white;
            textComponent.fontSize = 12;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return sendButton;
        }
        
        private void SetupChatManagerReferences(ChatManager chatManager, GameObject chatPanel, 
            GameObject scrollView, GameObject inputField, GameObject sendButton)
        {
            // This would require reflection or public fields to set references
            // For now, this serves as a guide for manual setup
            Debug.Log("Please manually assign the following references in ChatManager:");
            Debug.Log($"- Chat Panel: {chatPanel.name}");
            Debug.Log($"- Chat Scroll View: {scrollView.name}");
            Debug.Log($"- Chat Input Field: {inputField.name}");
            Debug.Log($"- Send Button: {sendButton.name}");
            Debug.Log("- Don't forget to create and assign a chat message prefab!");
        }
    }
}