using System.Collections.Generic;
using UnityEngine;
using SimpleMMO.Network;

namespace SimpleMMO.Managers
{
    public class PlayerDataManager : MonoBehaviour
    {
        private static PlayerDataManager _instance;
        private static readonly object _lock = new object();
        
        public static PlayerDataManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        Debug.LogError("PlayerDataManager: Instance not initialized. Call Initialize() from main thread first.");
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
                    Debug.LogWarning("PlayerDataManager: Already initialized");
                    return;
                }
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerDataManager");
                    _instance = go.AddComponent<PlayerDataManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("PlayerDataManager: Initialized successfully");
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
                    Debug.LogWarning("PlayerDataManager: Duplicate instance detected, destroying");
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private const int MAX_CHARACTER_SLOTS = 3;

        [Header("Character Data")]
        [SerializeField] private PlayerInfoDto[] characterSlots = new PlayerInfoDto[MAX_CHARACTER_SLOTS];
        [SerializeField] private PlayerInfoDto selectedCharacter;
        [SerializeField] private int selectedSlotIndex = -1;

        public PlayerInfoDto SelectedCharacter 
        { 
            get => selectedCharacter;
            private set => selectedCharacter = value;
        }

        public int SelectedSlotIndex
        {
            get => selectedSlotIndex;
            private set => selectedSlotIndex = value;
        }

        public void SetCharacterList(List<PlayerInfoDto> characterList)
        {
            for (int i = 0; i < MAX_CHARACTER_SLOTS; i++)
            {
                characterSlots[i] = null;
            }

            if (characterList != null)
            {
                int count = Mathf.Min(characterList.Count, MAX_CHARACTER_SLOTS);
                for (int i = 0; i < count; i++)
                {
                    if (IsValidCharacter(characterList[i]))
                    {
                        characterSlots[i] = characterList[i];
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid character data at index {i}, skipping");
                    }
                }

                if (characterList.Count > MAX_CHARACTER_SLOTS)
                {
                    Debug.LogWarning($"Server returned {characterList.Count} characters, using first {MAX_CHARACTER_SLOTS}");
                }

                Debug.Log($"Character slots updated: {GetCharacterCount()}/{MAX_CHARACTER_SLOTS} slots filled");
            }
        }

        public void SetSelectedCharacter(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_CHARACTER_SLOTS)
            {
                Debug.LogError($"Invalid slot index: {slotIndex}. Must be 0-{MAX_CHARACTER_SLOTS - 1}");
                return;
            }

            if (IsSlotEmpty(slotIndex))
            {
                Debug.LogError($"Cannot select character: Slot {slotIndex} is empty");
                return;
            }

            selectedCharacter = characterSlots[slotIndex];
            selectedSlotIndex = slotIndex;
            Debug.Log($"Selected character: {selectedCharacter.name} (ID: {selectedCharacter.playerId}) in slot {slotIndex}");
        }

        public void SetSelectedCharacter(PlayerInfoDto character)
        {
            if (character == null)
            {
                ClearSelection();
                return;
            }

            for (int i = 0; i < MAX_CHARACTER_SLOTS; i++)
            {
                if (characterSlots[i] != null && characterSlots[i].playerId == character.playerId)
                {
                    SetSelectedCharacter(i);
                    return;
                }
            }

            Debug.LogError($"Character {character.name} not found in any slot");
        }

        public PlayerInfoDto GetCharacterAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_CHARACTER_SLOTS)
            {
                return null;
            }
            return characterSlots[slotIndex];
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            return GetCharacterAtSlot(slotIndex) == null;
        }

        public int GetEmptySlotIndex()
        {
            for (int i = 0; i < MAX_CHARACTER_SLOTS; i++)
            {
                if (IsSlotEmpty(i))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool CanCreateNewCharacter()
        {
            return GetEmptySlotIndex() != -1;
        }

        public int GetCharacterCount()
        {
            int count = 0;
            for (int i = 0; i < MAX_CHARACTER_SLOTS; i++)
            {
                if (!IsSlotEmpty(i))
                {
                    count++;
                }
            }
            return count;
        }

        public int GetRemainingSlots()
        {
            return MAX_CHARACTER_SLOTS - GetCharacterCount();
        }

        public PlayerInfoDto GetSelectedCharacter()
        {
            return selectedCharacter;
        }

        public bool HasSelectedCharacter()
        {
            return selectedCharacter != null;
        }

        public bool HasCharacters()
        {
            return GetCharacterCount() > 0;
        }

        public void ClearSelection()
        {
            selectedCharacter = null;
            selectedSlotIndex = -1;
            Debug.Log("Character selection cleared");
        }

        public void ClearAllData()
        {
            for (int i = 0; i < MAX_CHARACTER_SLOTS; i++)
            {
                characterSlots[i] = null;
            }
            ClearSelection();
            Debug.Log("All player data cleared");
        }

        public PlayerInfoDto GetCharacterById(ulong playerId)
        {
            if (playerId <= 0) return null;

            for (int i = 0; i < MAX_CHARACTER_SLOTS; i++)
            {
                if (characterSlots[i] != null && characterSlots[i].playerId == playerId)
                {
                    return characterSlots[i];
                }
            }
            return null;
        }

        public void UpdateSelectedCharacterData(PlayerInfoDto updatedData)
        {
            if (selectedCharacter != null && updatedData != null && 
                selectedCharacter.playerId == updatedData.playerId)
            {
                selectedCharacter = updatedData;
                if (selectedSlotIndex >= 0 && selectedSlotIndex < MAX_CHARACTER_SLOTS)
                {
                    characterSlots[selectedSlotIndex] = updatedData;
                }
                Debug.Log($"Updated character data for: {updatedData.name}");
            }
        }

        private bool IsValidCharacter(PlayerInfoDto character)
        {
            return character != null &&
                   character.playerId > 0 &&
                   !string.IsNullOrWhiteSpace(character.name) &&
                   character.hp >= 0 &&
                   character.maxHp > 0 &&
                   character.hp <= character.maxHp;
        }

        public List<PlayerInfoDto> GetAllCharacters()
        {
            List<PlayerInfoDto> characters = new List<PlayerInfoDto>();
            for (int i = 0; i < MAX_CHARACTER_SLOTS; i++)
            {
                if (!IsSlotEmpty(i))
                {
                    characters.Add(characterSlots[i]);
                }
            }
            return characters;
        }
    }
}