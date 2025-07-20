using UnityEngine;
using SimpleMMO.Network;

namespace SimpleMMO.Managers
{
    public class PlayerInputManager : MonoBehaviour
    {
        public static PlayerInputManager Instance { get; private set; }
        
        private Camera mainCamera;

        public static void Initialize()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("PlayerInputManager");
                Instance = obj.AddComponent<PlayerInputManager>();
                DontDestroyOnLoad(obj);
                Debug.Log("PlayerInputManager initialized");
            }
        }

        void Start()
        {
            // 메인 카메라 캐싱
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("PlayerInputManager: Main camera not found");
                }
            }
        }

        public void SendInput(byte inputFlags, uint sequenceNumber = 0)
        {
            // 연결 상태 확인
            if (GameServerClient.Instance == null)
            {
                Debug.LogWarning("PlayerInputManager: GameServerClient not available");
                return;
            }

            // 마우스 월드 좌표 변환
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            
            // GameServerClient가 시퀀스 번호를 자체 관리하므로 직접 호출
            GameServerClient.Instance.SendPlayerInput(inputFlags, mouseWorldPos);
            
            Debug.Log($"Input sent: Flags=0x{inputFlags:X2}, MousePos={mouseWorldPos}");
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (mainCamera != null)
            {
                Vector3 mouseScreenPos = Input.mousePosition;
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
                mouseWorldPos.z = 0f; // 2D 게임이므로 z=0
                return mouseWorldPos;
            }
            else
            {
                // 카메라가 없으면 기본값 반환
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
