using UnityEngine;
using SimpleMMO.Managers;
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
            
            GameFlowManager.Initialize();
            SessionManager.Initialize();
            PlayerDataManager.Initialize();
            GameServerClient.Initialize();
            
            Debug.Log("GameInitializer: All managers initialized successfully");
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