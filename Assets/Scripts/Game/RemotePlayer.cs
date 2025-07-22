using UnityEngine;

namespace SimpleMMO.Game
{
    /// <summary>
    /// Represents a remote player character controlled by another client.
    /// Handles position interpolation, animation, and visual representation for network-synced players.
    /// Provides smooth movement interpolation and performance optimizations for multiplayer gameplay.
    /// </summary>
    public class RemotePlayer : MonoBehaviour
    {
        [Header("Visual Components")]
        public SpriteRenderer spriteRenderer;
        public TMPro.TextMeshPro nameText;
        
        [Header("Animation")]
        public Sprite[] idleSprites;
        public Sprite[] movingSprites;
        public float animationSpeed = 0.15f;
        
        [Header("Interpolation Settings")]
        [SerializeField] private float interpolationSpeed = 15f;
        [SerializeField] private bool enableSmoothing = true;
        
        /// <summary>
        /// Unique identifier for this remote player across the game server.
        /// </summary>
        public ulong PlayerId { get; private set; }
        
        /// <summary>
        /// Display name of the remote player character.
        /// </summary>
        public string PlayerName { get; private set; }
        
        // Position interpolation
        private Vector3 targetPosition;
        private Vector3 targetVelocity;
        private bool hasTargetPosition = false;
        
        // Animation
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private bool isMoving = false;
        
        // Performance optimization
        private float lastPositionUpdate = 0f;
        private const float MIN_UPDATE_INTERVAL = 0.016f; // ~60 FPS

        /// <summary>
        /// Unity Awake callback that caches component references for performance.
        /// Automatically finds SpriteRenderer and TextMeshPro components if not assigned in inspector.
        /// </summary>
        void Awake()
        {
            // Cache components
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (nameText == null)
                nameText = GetComponentInChildren<TMPro.TextMeshPro>();
        }

        /// <summary>
        /// Unity Start callback that initializes the interpolation system.
        /// Sets the initial target position to prevent unwanted movement on spawn.
        /// </summary>
        void Start()
        {
            // Set initial target position
            targetPosition = transform.position;
        }

        /// <summary>
        /// Unity Update callback that handles per-frame interpolation and animation updates.
        /// Smoothly moves the player towards target positions and updates sprite animations.
        /// </summary>
        void Update()
        {
            if (enableSmoothing && hasTargetPosition)
            {
                InterpolateToTarget();
            }
            
            UpdateAnimation();
        }

        /// <summary>
        /// Initializes the remote player with server-provided data.
        /// Sets up player identification, name display, and validates required components.
        /// </summary>
        /// <param name="playerId">Unique server-assigned player identifier</param>
        /// <param name="playerName">Display name for the player</param>
        public void Initialize(ulong playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;

            // Set player name display
            if (nameText != null)
            {
                nameText.text = playerName;
                nameText.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"RemotePlayer {playerName}: Name text component not found");
            }

            // Set visual appearance (different from local player)
            // Note: Color is now set in prefab, no need to override here
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"RemotePlayer {playerName}: SpriteRenderer not found");
            }

            Debug.Log($"RemotePlayer initialized: {playerName} (ID: {playerId})");
        }

        /// <summary>
        /// Updates the player's target position with optional smooth interpolation.
        /// Includes rate limiting to prevent excessive update calls and performance issues.
        /// </summary>
        /// <param name="newPosition">The new target position from server data</param>
        public void UpdatePosition(Vector3 newPosition)
        {
            if (Time.time - lastPositionUpdate < MIN_UPDATE_INTERVAL)
            {
                return; // Rate limiting
            }

            if (enableSmoothing)
            {
                // Set target for smooth interpolation
                targetPosition = newPosition;
                hasTargetPosition = true;
            }
            else
            {
                // Immediate position update
                transform.position = newPosition;
            }

            lastPositionUpdate = Time.time;
        }

        /// <summary>
        /// Updates the player's movement velocity and facing direction.
        /// Calculates movement state for animation and rotates the player to face movement direction.
        /// </summary>
        /// <param name="velocity">The player's current movement velocity vector</param>
        public void UpdateVelocity(Vector3 velocity)
        {
            targetVelocity = velocity;
            isMoving = velocity.magnitude > 0.1f;

            // Use velocity for rotation (facing movement direction)
            if (isMoving)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        /// <summary>
        /// Smoothly interpolates the player's position towards the target position.
        /// Includes distance-based optimizations and teleportation for extreme lag compensation.
        /// </summary>
        private void InterpolateToTarget()
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // If too far, teleport (prevent lag artifacts)
            if (distance > 20f)
            {
                transform.position = targetPosition;
                return;
            }

            // Smooth interpolation
            float speed = interpolationSpeed;
            
            // Faster interpolation for larger distances
            if (distance > 5f)
            {
                speed *= 2f;
            }

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speed);

            // Stop interpolating when close enough
            if (distance < 0.01f)
            {
                transform.position = targetPosition;
                hasTargetPosition = false;
            }
        }
        
        /// <summary>
        /// Updates the player's sprite animation based on movement state.
        /// Cycles through appropriate sprite arrays (idle or moving) at the specified animation speed.
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
        /// Sets the interpolation speed for smooth position updates.
        /// Speed is clamped between 1 and 50 to prevent extreme values.
        /// </summary>
        /// <param name="speed">The new interpolation speed (1-50)</param>
        public void SetInterpolationSpeed(float speed)
        {
            interpolationSpeed = Mathf.Clamp(speed, 1f, 50f);
        }

        /// <summary>
        /// Enables or disables position smoothing/interpolation.
        /// When disabled, position updates are applied immediately without interpolation.
        /// </summary>
        /// <param name="enable">Whether to enable smooth position interpolation</param>
        public void EnableSmoothing(bool enable)
        {
            enableSmoothing = enable;
            if (!enable)
            {
                hasTargetPosition = false;
            }
        }

        /// <summary>
        /// Updates the visual appearance of the remote player.
        /// Allows dynamic changes to player color and display name.
        /// </summary>
        /// <param name="color">The new color for the player sprite</param>
        /// <param name="displayName">Optional new display name for the player</param>
        public void SetVisual(Color color, string displayName = null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }

            if (!string.IsNullOrEmpty(displayName) && nameText != null)
            {
                nameText.text = displayName;
            }
        }

        /// <summary>
        /// Prepares the remote player for destruction when going out of view or disconnecting.
        /// Stops interpolation and optionally applies visual effects before cleanup.
        /// </summary>
        public void PrepareForDestroy()
        {
            hasTargetPosition = false;
            
            // Optional: Add fade out effect
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0.5f);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Debug visualization
            if (hasTargetPosition)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
}