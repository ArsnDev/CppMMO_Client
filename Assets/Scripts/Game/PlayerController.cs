using UnityEngine;
using System.Collections.Generic;
using SimpleMMO.Network;

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
        public TMPro.TextMeshPro hpText;
        
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
        
        /// <summary>
        /// Current health points of the player.
        /// </summary>
        public int CurrentHp { get; private set; } = 100;
        
        /// <summary>
        /// Maximum health points of the player.
        /// </summary>
        public int MaxHp { get; private set; } = 100;
        
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
            
            // Find HP text component (could be on a child object)
            if (hpText == null)
            {
                var hpTexts = GetComponentsInChildren<TMPro.TextMeshPro>();
                foreach (var text in hpTexts)
                {
                    if (text.gameObject.name.Contains("HP") || text != nameText)
                    {
                        hpText = text;
                        break;
                    }
                }
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
            
            // Initialize client prediction for local player
            if (isLocalPlayer && enableClientPrediction)
            {
                predictedPosition = transform.position;
                serverPosition = transform.position;
                Debug.Log($"ClientPrediction initialized for local player at {predictedPosition}");
            }
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

        [Header("Client Prediction")]
        [SerializeField] private bool enableClientPrediction = true;
        [SerializeField] private float predictionMoveSpeed = 5.0f;

        // Client prediction state
        private Vector3 predictedPosition;
        private Vector3 serverPosition;
        private bool hasServerPosition = false;
        
        // TODO: Future sequence-based reconciliation
        // private struct InputRecord
        // {
        //     public uint sequenceNumber;
        //     public byte inputFlags;
        //     public Vector3 position;
        //     public float timestamp;
        // }
        // 
        // private List<InputRecord> inputHistory = new List<InputRecord>();
        // private const int MAX_INPUT_HISTORY = 60;

        // Direction lookup table (same as server)
        private static readonly float SQRT2_HALF = Mathf.Sqrt(2f) / 2f;
        private static readonly Vector3[] DIRECTION_TABLE = {
            new Vector3(0.0f, 0.0f, 0.0f),                    // 0000: None
            new Vector3(0.0f, 1.0f, 0.0f),                    // 0001: W (Up)
            new Vector3(0.0f, -1.0f, 0.0f),                   // 0010: S (Down)
            new Vector3(0.0f, 0.0f, 0.0f),                    // 0011: W+S (Cancel out)
            new Vector3(-1.0f, 0.0f, 0.0f),                   // 0100: A (Left)
            new Vector3(-SQRT2_HALF, SQRT2_HALF, 0.0f),       // 0101: W+A (Up-Left)
            new Vector3(-SQRT2_HALF, -SQRT2_HALF, 0.0f),      // 0110: S+A (Down-Left)
            new Vector3(-1.0f, 0.0f, 0.0f),                   // 0111: W+S+A (A only)
            new Vector3(1.0f, 0.0f, 0.0f),                    // 1000: D (Right)
            new Vector3(SQRT2_HALF, SQRT2_HALF, 0.0f),        // 1001: W+D (Up-Right)
            new Vector3(SQRT2_HALF, -SQRT2_HALF, 0.0f),       // 1010: S+D (Down-Right)
            new Vector3(1.0f, 0.0f, 0.0f),                    // 1011: W+S+D (D only)
            new Vector3(0.0f, 0.0f, 0.0f),                    // 1100: A+D (Cancel out)
            new Vector3(0.0f, 1.0f, 0.0f),                    // 1101: W+A+D (W only)
            new Vector3(0.0f, -1.0f, 0.0f),                   // 1110: S+A+D (S only)
            new Vector3(0.0f, 0.0f, 0.0f)                     // 1111: All directions (Cancel out)
        };

        /// <summary>
        /// Converts input flags to movement direction vector (same as server)
        /// </summary>
        private Vector3 InputFlagsToDirection(byte inputFlags)
        {
            return DIRECTION_TABLE[inputFlags & 0x0F];
        }
        
        // TODO: Future sequence-based reconciliation method
        // private uint RecordInputHistory(byte inputFlags, Vector3 positionBeforeMovement)
        // {
        //     uint sequenceNum = 0;
        //     if (GameServerClient.Instance != null)
        //     {
        //         sequenceNum = (uint)(GameServerClient.Instance.CurrentSequenceNumber + 1);
        //     }
        //     
        //     InputRecord record = new InputRecord
        //     {
        //         sequenceNumber = sequenceNum,
        //         inputFlags = inputFlags,
        //         position = positionBeforeMovement,
        //         timestamp = Time.time
        //     };
        //     
        //     inputHistory.Add(record);
        //     
        //     if (inputHistory.Count > MAX_INPUT_HISTORY)
        //     {
        //         inputHistory.RemoveAt(0);
        //     }
        //     
        //     return sequenceNum;
        // }

        private void HandleInput()
        {
            byte inputFlags = 0;
            
            // Movement input
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
            
            // Client-side prediction: 즉시 로컬 움직임
            if (enableClientPrediction)
            {
                Vector3 direction = InputFlagsToDirection(inputFlags);
                Vector3 movement = direction * predictionMoveSpeed * Time.deltaTime;
                predictedPosition += movement;
                transform.position = predictedPosition;
            }
            
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
                // Send immediately when movement keys are released
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

            // Initialize HP display
            UpdateHpDisplay();
        }

        /// <summary>
        /// Updates the player's world position.
        /// Used primarily for remote players to sync their position from server data.
        /// For local players with prediction, updates both transform and predicted position.
        /// </summary>
        /// <param name="position">The new world position for the player</param>
        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
            
            // For local player with client prediction, update predicted position to match server
            if (isLocalPlayer && enableClientPrediction)
            {
                predictedPosition = position;
                serverPosition = position;
                hasServerPosition = true;
            }
        }
        
        // TODO: Future sequence-based reconciliation method
        // public void PerformReconciliation(Vector3 serverPosition, uint lastProcessedSequence)
        // {
        //     if (!isLocalPlayer || !enableClientPrediction)
        //         return;
        //         
        //     this.serverPosition = serverPosition;
        //     hasServerPosition = true;
        //     
        //     // Find matching sequence and replay unprocessed inputs
        //     int reconcileIndex = -1;
        //     for (int i = 0; i < inputHistory.Count; i++)
        //     {
        //         if (inputHistory[i].sequenceNumber == lastProcessedSequence)
        //         {
        //             reconcileIndex = i;
        //             break;
        //         }
        //     }
        //     
        //     if (reconcileIndex >= 0)
        //     {
        //         inputHistory.RemoveRange(0, reconcileIndex + 1);
        //         
        //         Vector3 correctedPosition = serverPosition;
        //         foreach (var input in inputHistory)
        //         {
        //             Vector3 direction = InputFlagsToDirection(input.inputFlags);
        //             Vector3 movement = direction * predictionMoveSpeed * Time.fixedDeltaTime;
        //             correctedPosition += movement;
        //         }
        //         
        //         predictedPosition = correctedPosition;
        //         transform.position = predictedPosition;
        //     }
        //     else
        //     {
        //         predictedPosition = serverPosition;
        //         transform.position = predictedPosition;
        //         inputHistory.Clear();
        //     }
        // }

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

        /// <summary>
        /// Updates the player's health points and refreshes HP display.
        /// </summary>
        /// <param name="currentHp">Current health points</param>
        /// <param name="maxHp">Maximum health points</param>
        public void UpdateHp(int currentHp, int maxHp)
        {
            CurrentHp = Mathf.Clamp(currentHp, 0, maxHp);
            MaxHp = maxHp;
            UpdateHpDisplay();
        }

        /// <summary>
        /// Updates the HP display text.
        /// </summary>
        private void UpdateHpDisplay()
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {CurrentHp}/{MaxHp}";
                
                // Change color based on HP percentage
                float hpPercentage = (float)CurrentHp / MaxHp;
                if (hpPercentage > 0.6f)
                {
                    hpText.color = Color.green;
                }
                else if (hpPercentage > 0.3f)
                {
                    hpText.color = Color.yellow;
                }
                else
                {
                    hpText.color = Color.red;
                }
            }
        }

        /// <summary>
        /// Gets the current HP percentage (0.0 to 1.0).
        /// </summary>
        public float GetHpPercentage()
        {
            return MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;
        }

        /// <summary>
        /// Checks if the player is alive (HP > 0).
        /// </summary>
        public bool IsAlive()
        {
            return CurrentHp > 0;
        }
    }
}