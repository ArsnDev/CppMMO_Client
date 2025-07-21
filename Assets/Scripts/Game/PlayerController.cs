using UnityEngine;

namespace SimpleMMO.Game
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        public float moveSpeed = 5f;
        public bool isLocalPlayer = false;

        [Header("Visual")]
        public SpriteRenderer spriteRenderer;
        public TMPro.TextMeshPro nameText;

        public ulong PlayerId { get; private set; }
        public string PlayerName { get; private set; }

        void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            if (nameText == null)
            {
                nameText = GetComponentInChildren<TMPro.TextMeshPro>();
            }
        }

        void Start()
        {
            // Additional initialization if needed
        }

        void Update()
        {
            if (isLocalPlayer)
            {
                HandleInput();
            }
        }

        [Header("Input Configuration")]
        private const byte INPUT_UP = 0x01;
        private const byte INPUT_DOWN = 0x02;
        private const byte INPUT_LEFT = 0x04;
        private const byte INPUT_RIGHT = 0x08;

        private void HandleInput()
        {
            byte inputFlags = 0;
            if (Input.GetKey(KeyCode.W))
            {
                inputFlags |= INPUT_UP;
            }
            if (Input.GetKey(KeyCode.S))
            {
                inputFlags |= INPUT_DOWN;
            }
            if (Input.GetKey(KeyCode.A))
            {
                inputFlags |= INPUT_LEFT;
            }
            if (Input.GetKey(KeyCode.D))
            {
                inputFlags |= INPUT_RIGHT;
            }
            if (inputFlags != 0)
            {
                SendPlayerInput(inputFlags);
            }
        }

        private void SendPlayerInput(byte inputFlags)
        {
            if (SimpleMMO.Managers.PlayerInputManager.Instance != null)
            {
                SimpleMMO.Managers.PlayerInputManager.Instance.SendInput(inputFlags);
            }
            else
            {
                Debug.LogWarning("PlayerController: PlayerInputManager not available, input dropped");
            }
        }

        public void Initialize(ulong playerId, string playerName, bool isLocal = false)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            isLocalPlayer = isLocal;

            if (nameText != null)
            {
                nameText.text = playerName;
            }
            else
            {
                Debug.LogWarning("PlayerController: Name text is not assigned.");
            }

            if (isLocalPlayer && spriteRenderer != null)
            {
                spriteRenderer.color = Color.green;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            else
            {
                Debug.LogWarning("PlayerController: SpriteRenderer is not assigned.");
            }
        }

        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        public void UpdateVelocity(Vector3 velocity)
        {
            if (velocity.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }
}