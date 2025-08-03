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
                
                // Initialize input history to avoid stale data
                for (int i = 0; i < INPUT_HISTORY_SIZE; i++)
                {
                    inputHistory[i] = new InputRecord { sequenceNumber = 0 };
                }
                
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
        
        // Input History for Server Reconciliation
        private struct InputRecord
        {
            public uint sequenceNumber;
            public byte inputFlags;
            public Vector3 positionBeforeInput;
            public float timestamp;
            public float deltaTime;
        }
        
        private const int INPUT_HISTORY_SIZE = 60; // 3초분 (20Hz 기준)
        private InputRecord[] inputHistory = new InputRecord[INPUT_HISTORY_SIZE];
        private int historyHead = 0;
        

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
            
            // 입력 상태가 변했거나 일정 시간이 지났을 때만 전송
            bool inputChanged = inputFlags != lastInputFlags;
            bool timeToSend = Time.time - lastInputSendTime >= inputSendInterval;
            
            if (inputFlags != 0 && (inputChanged || timeToSend))
            {
                ProcessAndSendInput(inputFlags);
            }
            else if (inputFlags == 0 && lastInputFlags != 0)
            {
                // Send immediately when movement keys are released
                ProcessAndSendInput(inputFlags);
            }
        }
        
        private void ProcessAndSendInput(byte inputFlags)
        {
            // Record position before applying input for reconciliation
            Vector3 positionBeforeInput = enableClientPrediction ? predictedPosition : transform.position;
            float currentDeltaTime = Time.deltaTime;
            
            // Client-side prediction: 즉시 로컬 움직임
            if (enableClientPrediction)
            {
                Vector3 direction = InputFlagsToDirection(inputFlags);
                Vector3 movement = direction * predictionMoveSpeed * currentDeltaTime;
                predictedPosition += movement;
                transform.position = predictedPosition;
            }
            
            // Send input to server and record in history
            uint sequenceNumber = SendPlayerInput(inputFlags);
            RecordInput(sequenceNumber, inputFlags, positionBeforeInput, currentDeltaTime);
            
            lastInputFlags = inputFlags;
            lastInputSendTime = Time.time;
        }

        /// <summary>
        /// Sends player input data to the server through the PlayerInputManager.
        /// Handles graceful fallback when the input manager is not available.
        /// </summary>
        /// <param name="inputFlags">Bit flags representing current input state</param>
        /// <returns>Sequence number of the sent input</returns>
        private uint SendPlayerInput(byte inputFlags)
        {
            if (SimpleMMO.Managers.PlayerInputManager.Instance != null)
            {
                SimpleMMO.Managers.PlayerInputManager.Instance.SendInput(inputFlags);
                // Get sequence number from GameServerClient
                if (GameServerClient.Instance != null)
                {
                    return GameServerClient.Instance.CurrentSequenceNumber;
                }
            }
            else
            {
                Debug.LogWarning("PlayerController: PlayerInputManager not available, input dropped");
            }
            return 0; // Invalid sequence number
        }
        
        /// <summary>
        /// Records input in the circular buffer for server reconciliation
        /// </summary>
        private void RecordInput(uint sequenceNumber, byte inputFlags, Vector3 positionBeforeInput, float deltaTime)
        {
            if (sequenceNumber == 0) return; // Invalid sequence
            
            // Record in circular buffer
            inputHistory[historyHead] = new InputRecord
            {
                sequenceNumber = sequenceNumber,
                inputFlags = inputFlags,
                positionBeforeInput = positionBeforeInput,
                timestamp = Time.time,
                deltaTime = deltaTime
            };
            
            // Move to next position in circular buffer
            historyHead = (historyHead + 1) % INPUT_HISTORY_SIZE;
            
            Debug.Log($"Input recorded: seq={sequenceNumber}, flags={inputFlags:X2}, pos={positionBeforeInput}, dt={deltaTime:F4}");
        }
        
        /// <summary>
        /// Finds input record by sequence number for reconciliation
        /// </summary>
        private int FindInputBySequence(uint sequenceNumber)
        {
            for (int i = 0; i < INPUT_HISTORY_SIZE; i++)
            {
                if (inputHistory[i].sequenceNumber == sequenceNumber)
                {
                    return i;
                }
            }
            return -1; // Not found
        }
        
        /// <summary>
        /// Gets all unprocessed inputs after the given sequence number
        /// </summary>
        private List<InputRecord> GetUnprocessedInputs(uint lastProcessedSequence)
        {
            List<InputRecord> unprocessed = new List<InputRecord>();
            float currentTime = Time.time;
            const float MAX_INPUT_AGE = 5.0f; // Discard inputs older than 5 seconds
            
            for (int i = 0; i < INPUT_HISTORY_SIZE; i++)
            {
                InputRecord record = inputHistory[i];
                if (record.sequenceNumber > lastProcessedSequence && 
                    record.sequenceNumber != 0 &&
                    (currentTime - record.timestamp) < MAX_INPUT_AGE)
                {
                    unprocessed.Add(record);
                }
            }
            
            // Sort by sequence number to maintain order
            unprocessed.Sort((a, b) => a.sequenceNumber.CompareTo(b.sequenceNumber));
            return unprocessed;
        }
        
        /// <summary>
        /// Performs server reconciliation by replaying unprocessed inputs from server position
        /// </summary>
        /// <param name="serverPosition">Authoritative position from server</param>
        /// <param name="lastProcessedSequence">Last sequence number processed by server</param>
        public void PerformReconciliation(Vector3 serverPosition, uint lastProcessedSequence)
        {
            if (!isLocalPlayer || !enableClientPrediction)
                return;
                
            // Check if reconciliation is needed
            float distanceError = Vector3.Distance(serverPosition, predictedPosition);
            const float RECONCILIATION_THRESHOLD = 0.5f; // 0.5 unit 차이부터 보정
            
            if (distanceError < RECONCILIATION_THRESHOLD)
            {
                // Small difference, just update server position for reference
                this.serverPosition = serverPosition;
                hasServerPosition = true;
                return;
            }
            
            Debug.Log($"Reconciliation needed: distance error = {distanceError:F3}, threshold = {RECONCILIATION_THRESHOLD}");
            
            // Get all unprocessed inputs after last processed sequence
            List<InputRecord> unprocessedInputs = GetUnprocessedInputs(lastProcessedSequence);
            
            // Start from server's authoritative position
            Vector3 correctedPosition = serverPosition;
            
            // Replay all unprocessed inputs
            foreach (var input in unprocessedInputs)
            {
                Vector3 direction = InputFlagsToDirection(input.inputFlags);
                Vector3 movement = direction * predictionMoveSpeed * input.deltaTime;
                correctedPosition += movement;
                
                Debug.Log($"Replaying input seq={input.sequenceNumber}, flags={input.inputFlags:X2}, movement={movement}, dt={input.deltaTime:F4}");
            }
            
            // Update positions
            predictedPosition = correctedPosition;
            this.serverPosition = serverPosition;
            hasServerPosition = true;
            transform.position = predictedPosition;
            
            Debug.Log($"Reconciliation complete: server={serverPosition}, corrected={correctedPosition}, replayed {unprocessedInputs.Count} inputs");
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