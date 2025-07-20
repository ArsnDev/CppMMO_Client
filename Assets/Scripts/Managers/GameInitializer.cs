using UnityEngine;
using SimpleMMO.Network;

namespace SimpleMMO.Managers
{
    public class GameInitializer : MonoBehaviour
    {
        private static bool _hasInitialized = false;
        
        [Header("Initialization")]
        [SerializeField] private bool initializeOnAwake = true;
        
        void Awake()
        {
            if (initializeOnAwake)
            {
                if (_hasInitialized)
                {
                    Debug.LogWarning("GameInitializer: Managers already initialized, skipping");
                    return;
                }
                InitializeManagers();
                _hasInitialized = true;
            }
        }

        /// <summary>
        /// Initialize all core managers in dependency order.
        /// Initialization order: GameFlowManager → SessionManager → PlayerDataManager → GameServerClient
        /// GameFlowManager: No dependencies (scene management)
        /// SessionManager: No dependencies (session storage)
        /// PlayerDataManager: Depends on SessionManager for user context
        /// GameServerClient: Depends on SessionManager for authentication
        /// </summary>
        public static void InitializeManagers()
        {
            // Check if already initialized to prevent redundant work
            if (AreManagersInitialized())
            {
                Debug.LogWarning("GameInitializer: All managers already initialized, skipping");
                return;
            }
            
            Debug.Log("GameInitializer: Starting manager initialization...");
            
            try
            {
                // Initialize in dependency order
                GameFlowManager.Initialize();     // No dependencies
                SessionManager.Initialize();     // No dependencies  
                PlayerDataManager.Initialize();  // Depends on SessionManager
                GameServerClient.Initialize();   // Depends on SessionManager
                
                Debug.Log("GameInitializer: All managers initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GameInitializer: Failed to initialize managers: {e.Message}");
                throw;
            }
        }

        public static bool AreManagersInitialized()
        {
            return GameFlowManager.Instance != null &&
                   SessionManager.Instance != null &&
                   PlayerDataManager.Instance != null &&
                   GameServerClient.Instance != null;
        }
    }
}