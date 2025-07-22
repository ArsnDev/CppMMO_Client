using UnityEngine;
using SimpleMMO.Network;

namespace SimpleMMO.Managers
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputManager : MonoBehaviour
    {
        public static PlayerInputManager Instance { get; private set; }
        
        private static readonly object _lock = new object();
        private Camera mainCamera;

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

        void Start()
        {
            StartCoroutine(InitializeCamera());
        }

        private System.Collections.IEnumerator InitializeCamera()
        {
            // Try to cache main camera with retries
            int retries = 5;
            while (mainCamera == null && retries > 0)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    retries--;
                    if (retries > 0)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }
                    else
                    {
                        Debug.LogWarning("PlayerInputManager: Main camera not found after retries");
                    }
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

            // Convert mouse position to world coordinates
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            
            // Call directly as GameServerClient manages sequence numbers internally
            GameServerClient.Instance.SendPlayerInput(inputFlags, mouseWorldPos);
            
            Debug.Log($"Input sent: Flags=0x{inputFlags:X2}, MousePos={mouseWorldPos}");
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (mainCamera != null)
            {
                Vector3 mouseScreenPos = Input.mousePosition;
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
                mouseWorldPos.z = 0f; // 2D game, z=0
                return mouseWorldPos;
            }
            else
            {
                // Return default if no camera
                return Vector3.zero;
            }
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
