using UnityEngine;

namespace SimpleMMO.Game
{
    /// <summary>
    /// Controls player character behavior, input handling, and visual representation.
    /// Supports both local and remote player functionality with animation and networking integration.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        public float moveSpeed = 5f;
        public bool isLocalPlayer = false;

        [Header("Visual")]
        public SpriteRenderer spriteRenderer;
        public TMPro.TextMeshPro nameText;
        
        [Header("Animation")]
        public Sprite[] idleSprites;
        public Sprite[] movingSprites;
        public float animationSpeed = 0.15f;

        /// <summary>
        /// Unique identifier for this player across the game server.
        /// </summary>
        public ulong PlayerId { get; private set; }
        
        /// <summary>
        /// Display name of the player character.
        /// </summary>
        public string PlayerName { get; private set; }
        
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private bool isMoving = false;

        /// <summary>
        /// Unity Awake callback that initializes component references.
        /// Automatically finds SpriteRenderer and TextMeshPro components if not assigned.
        /// </summary>
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

        /// <summary>
        /// Unity Start callback that configures application settings.
        /// Ensures the game continues running when not in focus (important for multiplayer).
        /// </summary>
        void Start()
        {
            // Allow game to continue running in background
            Application.runInBackground = true;
        }

        /// <summary>
        /// Unity Update callback that handles per-frame logic.
        /// Processes input for local players and updates animations for all players.
        /// </summary>
        void Update()
        {
            if (isLocalPlayer)
            {
                HandleInput();
            }
            
            UpdateAnimation();
        }

        [Header("Input Configuration")]
        private const byte INPUT_UP = 0x01;
        private const byte INPUT_DOWN = 0x02;
        private const byte INPUT_LEFT = 0x04;
        private const byte INPUT_RIGHT = 0x08;

        private byte lastInputFlags = 0;
        private float inputSendInterval = 1.0f / 20.0f; // 20Hz (0.05초마다)
        private float lastInputSendTime = 0f;

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
            
            // 애니메이션을 위한 이동 상태 업데이트
            isMoving = inputFlags != 0;
            
            // 입력 상태가 변했거나 일정 시간이 지났을 때만 전송
            bool inputChanged = inputFlags != lastInputFlags;
            bool timeToSend = Time.time - lastInputSendTime >= inputSendInterval;
            
            if (inputFlags != 0 && (inputChanged || timeToSend))
            {
                SendPlayerInput(inputFlags);
                lastInputFlags = inputFlags;
                lastInputSendTime = Time.time;
            }
            else if (inputFlags == 0 && lastInputFlags != 0)
            {
                // Send immediately when keys are released
                SendPlayerInput(inputFlags);
                lastInputFlags = inputFlags;
                lastInputSendTime = Time.time;
            }
        }

        /// <summary>
        /// Sends player input data to the server through the PlayerInputManager.
        /// Handles graceful fallback when the input manager is not available.
        /// </summary>
        /// <param name="inputFlags">Bit flags representing current input state</param>
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
        
        /// <summary>
        /// Updates the player's sprite animation based on movement state.
        /// Cycles through idle or moving sprites at the specified animation speed.
        /// </summary>
        private void UpdateAnimation()
        {
            animationTimer += Time.deltaTime;
            
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0f;
                
                Sprite[] currentSprites = isMoving ? movingSprites : idleSprites;
                
                if (currentSprites != null && currentSprites.Length > 0)
                {
                    currentFrame = (currentFrame + 1) % currentSprites.Length;
                    
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = currentSprites[currentFrame];
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the player controller with provided player data.
        /// Sets up player ID, name, local/remote status, and visual appearance.
        /// </summary>
        /// <param name="playerId">Unique identifier for the player</param>
        /// <param name="playerName">Display name for the player</param>
        /// <param name="isLocal">Whether this is the local player (controlled by this client)</param>
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

        /// <summary>
        /// Updates the player's world position.
        /// Used primarily for remote players to sync their position from server data.
        /// </summary>
        /// <param name="position">The new world position for the player</param>
        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// Updates the player's velocity and movement state for animation purposes.
        /// Calculates facing direction and updates movement animation state.
        /// </summary>
        /// <param name="velocity">The player's current velocity vector</param>
        public void UpdateVelocity(Vector3 velocity)
        {
            isMoving = velocity.magnitude > 0.1f;
            
            if (isMoving)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }
}