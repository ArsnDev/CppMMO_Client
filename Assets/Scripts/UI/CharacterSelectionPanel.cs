using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleMMO.Managers;
using SimpleMMO.Network;
using System.Collections.Generic;

namespace SimpleMMO.UI
{
    public class CharacterSelectionPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button[] characterSlotButtons = new Button[3];
        [SerializeField] private TextMeshProUGUI[] characterNameTexts = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] characterInfoTexts = new TextMeshProUGUI[3];
        [SerializeField] private Button[] createCharacterButtons = new Button[3];
        [SerializeField] private Button[] deleteCharacterButtons = new Button[3];
        [SerializeField] private Button selectButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Character Creation")]
        [SerializeField] private GameObject characterCreationPanel;
        [SerializeField] private TMP_InputField characterNameInput;
        [SerializeField] private Button confirmCreateButton;
        [SerializeField] private Button cancelCreateButton;

        private int selectedSlotIndex = -1;
        private int creatingSlotIndex = -1;

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            LoadCharacterList();
        }

        private void InitializeUI()
        {
            SetLoadingState(false);
            SetStatusText("Select a character or create a new one.", Color.white);
            
            if (selectButton != null) selectButton.interactable = false;
            
            if (characterCreationPanel != null) characterCreationPanel.SetActive(false);
            
            Debug.Log("CharacterSelectionPanel: UI initialized");
        }

        private void SetupEventListeners()
        {
            for (int i = 0; i < characterSlotButtons.Length; i++)
            {
                int slotIndex = i;
                if (characterSlotButtons[i] != null)
                    characterSlotButtons[i].onClick.AddListener(() => OnCharacterSlotClicked(slotIndex));
                
                if (createCharacterButtons[i] != null)
                    createCharacterButtons[i].onClick.AddListener(() => OnCreateCharacterClicked(slotIndex));
                
                if (deleteCharacterButtons[i] != null)
                    deleteCharacterButtons[i].onClick.AddListener(() => OnDeleteCharacterClicked(slotIndex));
            }

            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            if (confirmCreateButton != null)
                confirmCreateButton.onClick.AddListener(OnConfirmCreateClicked);
            
            if (cancelCreateButton != null)
                cancelCreateButton.onClick.AddListener(OnCancelCreateClicked);
        }

        private void LoadCharacterList()
        {
            SetLoadingState(true);
            SetStatusText("Loading characters...", Color.white);
            
            Debug.Log("CharacterSelectionPanel: Loading character list");
            
            string sessionTicket = SessionManager.Instance.SessionTicket;
            AuthApiClient.Instance.GetCharacters(sessionTicket, OnCharacterListSuccess, OnCharacterListFailure);
        }

        private void OnCharacterListSuccess(CharacterListResponse response)
        {
            SetLoadingState(false);
            
            if (response.success)
            {
                PlayerDataManager.Instance.SetCharacterList(response.characters);
                
                UpdateCharacterSlots();
                
                SetStatusText($"Loaded {response.characters?.Count ?? 0} characters.", Color.green);
                Debug.Log($"CharacterSelectionPanel: Loaded {response.characters?.Count ?? 0} characters");
            }
            else
            {
                SetStatusText($"Failed to load characters: {response.message}", Color.red);
            }
        }

        private void OnCharacterListFailure(string error)
        {
            SetLoadingState(false);
            SetStatusText($"Failed to load characters: {error}", Color.red);
            Debug.LogError($"CharacterSelectionPanel: Load failed - {error}");
        }

        private void UpdateCharacterSlots()
        {
            for (int i = 0; i < 3; i++)
            {
                var character = PlayerDataManager.Instance.GetCharacterAtSlot(i);
                bool hasCharacter = character != null;

                if (characterNameTexts[i] != null)
                {
                    characterNameTexts[i].gameObject.SetActive(hasCharacter);
                    if (hasCharacter) characterNameTexts[i].text = character.name;
                }

                if (characterInfoTexts[i] != null)
                {
                    characterInfoTexts[i].gameObject.SetActive(hasCharacter);
                    if (hasCharacter) 
                        characterInfoTexts[i].text = $"HP: {character.hp}/{character.maxHp}\nPos: ({character.posX:F1}, {character.posY:F1})";
                }

                if (createCharacterButtons[i] != null)
                    createCharacterButtons[i].gameObject.SetActive(!hasCharacter);

                if (deleteCharacterButtons[i] != null)
                    deleteCharacterButtons[i].gameObject.SetActive(hasCharacter);

                if (characterSlotButtons[i] != null)
                    characterSlotButtons[i].interactable = hasCharacter;
            }

            selectedSlotIndex = -1;
            if (selectButton != null) selectButton.interactable = false;
        }

        private void OnCharacterSlotClicked(int slotIndex)
        {
            var character = PlayerDataManager.Instance.GetCharacterAtSlot(slotIndex);
            if (character == null) return;

            selectedSlotIndex = slotIndex;
            PlayerDataManager.Instance.SetSelectedCharacter(slotIndex);
            
            if (selectButton != null) selectButton.interactable = true;
            
            SetStatusText($"Selected: {character.name}", Color.green);
            Debug.Log($"CharacterSelectionPanel: Selected character {character.name} in slot {slotIndex}");
        }

        private void OnCreateCharacterClicked(int slotIndex)
        {
            if (!PlayerDataManager.Instance.IsSlotEmpty(slotIndex))
            {
                SetStatusText("Slot is not empty!", Color.red);
                return;
            }

            creatingSlotIndex = slotIndex;
            ShowCharacterCreationPanel(true);
        }

        private void OnDeleteCharacterClicked(int slotIndex)
        {
            var character = PlayerDataManager.Instance.GetCharacterAtSlot(slotIndex);
            if (character == null) return;

            SetStatusText($"Delete feature not implemented yet for {character.name}", Color.yellow);
        }

        private void OnSelectButtonClicked()
        {
            if (selectedSlotIndex < 0 || !PlayerDataManager.Instance.HasSelectedCharacter())
            {
                SetStatusText("No character selected!", Color.red);
                return;
            }

            var selectedCharacter = PlayerDataManager.Instance.GetSelectedCharacter();
            SetStatusText($"Entering game with {selectedCharacter.name}...", Color.green);
            
            Debug.Log($"CharacterSelectionPanel: Entering game with character {selectedCharacter.name}");
            
            Invoke(nameof(LoadGameScene), 1.0f);
        }

        private void OnBackButtonClicked()
        {
            Debug.Log("CharacterSelectionPanel: Going back to login");
            
            SessionManager.Instance.ClearSession();
            PlayerDataManager.Instance.ClearAllData();
            
            GameFlowManager.Instance.LoadLoginScene();
        }

        private void ShowCharacterCreationPanel(bool show)
        {
            if (characterCreationPanel != null)
            {
                characterCreationPanel.SetActive(show);
                if (show && characterNameInput != null)
                {
                    characterNameInput.text = "";
                    characterNameInput.Select();
                }
            }
        }

        private void OnConfirmCreateClicked()
        {
            string characterName = characterNameInput?.text?.Trim();
            
            if (string.IsNullOrWhiteSpace(characterName))
            {
                SetStatusText("Character name cannot be empty!", Color.red);
                return;
            }

            if (characterName.Length < 2 || characterName.Length > 15)
            {
                SetStatusText("Character name must be 2-15 characters!", Color.red);
                return;
            }

            SetLoadingState(true);
            SetStatusText("Creating character...", Color.white);
            
            Debug.Log($"CharacterSelectionPanel: Creating character '{characterName}' in slot {creatingSlotIndex}");
            
            string sessionTicket = SessionManager.Instance.SessionTicket;
            AuthApiClient.Instance.CreateCharacter(sessionTicket, characterName, OnCharacterCreateSuccess, OnCharacterCreateFailure);
        }

        private void OnCancelCreateClicked()
        {
            ShowCharacterCreationPanel(false);
            creatingSlotIndex = -1;
        }

        private void OnCharacterCreateSuccess(CharacterCreateResponse response)
        {
            SetLoadingState(false);
            ShowCharacterCreationPanel(false);
            
            if (response.success)
            {
                SetStatusText("Character created successfully!", Color.green);
                
                LoadCharacterList();
            }
            else
            {
                SetStatusText($"Failed to create character: {response.message}", Color.red);
            }
            
            creatingSlotIndex = -1;
        }

        private void OnCharacterCreateFailure(string error)
        {
            SetLoadingState(false);
            ShowCharacterCreationPanel(false);
            SetStatusText($"Failed to create character: {error}", Color.red);
            Debug.LogError($"CharacterSelectionPanel: Create failed - {error}");
            
            creatingSlotIndex = -1;
        }

        private void LoadGameScene()
        {
            Debug.Log("CharacterSelectionPanel: Loading game scene");
            GameFlowManager.Instance.LoadGameScene();
        }

        private void SetLoadingState(bool isLoading)
        {
            for (int i = 0; i < characterSlotButtons.Length; i++)
            {
                if (characterSlotButtons[i] != null) characterSlotButtons[i].interactable = !isLoading;
                if (createCharacterButtons[i] != null) createCharacterButtons[i].interactable = !isLoading;
                if (deleteCharacterButtons[i] != null) deleteCharacterButtons[i].interactable = !isLoading;
            }

            if (selectButton != null) selectButton.interactable = !isLoading && selectedSlotIndex >= 0;
            if (backButton != null) backButton.interactable = !isLoading;
            if (confirmCreateButton != null) confirmCreateButton.interactable = !isLoading;
            if (cancelCreateButton != null) cancelCreateButton.interactable = !isLoading;

            if (loadingIndicator != null) loadingIndicator.SetActive(isLoading);
        }

        private void SetStatusText(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            
            Debug.Log($"CharacterSelectionPanel Status: {message}");
        }

        private void OnDestroy()
        {
            for (int i = 0; i < characterSlotButtons.Length; i++)
            {
                if (characterSlotButtons[i] != null) characterSlotButtons[i].onClick.RemoveAllListeners();
                if (createCharacterButtons[i] != null) createCharacterButtons[i].onClick.RemoveAllListeners();
                if (deleteCharacterButtons[i] != null) deleteCharacterButtons[i].onClick.RemoveAllListeners();
            }

            if (selectButton != null) selectButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
            if (confirmCreateButton != null) confirmCreateButton.onClick.RemoveAllListeners();
            if (cancelCreateButton != null) cancelCreateButton.onClick.RemoveAllListeners();
        }
    }
}