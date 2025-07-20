using UnityEngine;
using SimpleMMO.Network;

namespace SimpleMMO.Managers
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Initialization")]
        [SerializeField] private bool initializeOnAwake = true;
        
        void Awake()
        {
            if (initializeOnAwake)
            {
                InitializeManagers();
            }
        }

        public static void InitializeManagers()
        {
            Debug.Log("GameInitializer: Starting manager initialization...");
            
            try
            {
                GameFlowManager.Initialize();
                SessionManager.Initialize();
                PlayerDataManager.Initialize();
                GameServerClient.Initialize();
                
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