using UnityEngine;
using SimpleMMO.Network;

namespace SimpleMMO.Managers
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputManager : MonoBehaviour
    {
        public static PlayerInputManager Instance { get; private set; }
        
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            lock (_lock)
            {
                if (Instance == null)
                {
                    GameObject obj = new GameObject("PlayerInputManager");
                    Instance = obj.AddComponent<PlayerInputManager>();
                    DontDestroyOnLoad(obj);
                    Debug.Log("PlayerInputManager initialized");
                }
                else
                {
                    Debug.LogWarning("PlayerInputManager: Already initialized");
                }
            }
        }


        public void SendInput(byte inputFlags)
        {
            // Check connection status
            if (GameServerClient.Instance == null)
            {
                Debug.LogWarning("PlayerInputManager: GameServerClient not available");
                return;
            }

            // Check if connected
            if (!GameServerClient.Instance.IsConnected)
            {
                Debug.LogWarning($"PlayerInputManager: GameServerClient not connected, dropping input 0x{inputFlags:X2}");
                return;
            }
            
            // Call directly as GameServerClient manages sequence numbers internally
            GameServerClient.Instance.SendPlayerInput(inputFlags);
            
            Debug.Log($"Input sent: Flags=0x{inputFlags:X2}");
        }


        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
