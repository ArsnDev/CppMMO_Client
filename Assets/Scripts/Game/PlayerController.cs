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

        void Start()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        void Update()
        {
            if (isLocalPlayer)
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            byte inputFlags = 0;
            if (Input.GetKey(KeyCode.W))
            {
                inputFlags |= 0x01; // Move Up
            }
            if (Input.GetKey(KeyCode.S))
            {
                inputFlags |= 0x02; // Move Down
            }
            if (Input.GetKey(KeyCode.A))
            {
                inputFlags |= 0x04; // Move Left
            }
            if (Input.GetKey(KeyCode.D))
            {
                inputFlags |= 0x08; // Move Right
            }
            if (inputFlags != 0)
            {
                SendPlayerInput(inputFlags);
            }
        }

        private void SendPlayerInput(byte inputFlags)
        {
            SimpleMMO.Managers.PlayerInputManager.Instance?.SendInput(inputFlags);
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