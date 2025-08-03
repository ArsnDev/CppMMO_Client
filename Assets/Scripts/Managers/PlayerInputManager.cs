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


        public uint SendInput(byte inputFlags)
        {
            // Check connection status
            if (GameServerClient.Instance == null)
            {
                Debug.LogWarning("PlayerInputManager: GameServerClient not available");
                return 0;
            }

            // Check if connected
            if (!GameServerClient.Instance.IsConnected)
            {
                Debug.LogWarning($"PlayerInputManager: GameServerClient not connected, dropping input 0x{inputFlags:X2}");
                return 0;
            }
            
            // Call directly as GameServerClient manages sequence numbers internally
            uint sequenceNumber = GameServerClient.Instance.SendPlayerInput(inputFlags);
            
            Debug.Log($"Input sent: Flags=0x{inputFlags:X2}, Sequence={sequenceNumber}");
            return sequenceNumber;
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
