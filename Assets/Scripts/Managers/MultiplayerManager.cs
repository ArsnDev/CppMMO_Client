using UnityEngine;
using SimpleMMO.Network;
using SimpleMMO.Game;
using CppMMO.Protocol;
using SimpleMMO.Protocol.Extensions;
using System.Collections.Generic;

namespace SimpleMMO.Managers
{
    /// <summary>
    /// Manages remote player spawning, tracking, and synchronization in the multiplayer game.
    /// Handles remote player lifecycle from spawn to despawn, and provides performance optimizations
    /// for visibility culling and player count management.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class MultiplayerManager : MonoBehaviour
    {
        [Header("Remote Player Settings")]
        [SerializeField] private GameObject remotePlayerPrefab;
        [SerializeField] private Transform remotePlayersParent;
        
        [Header("Performance Settings")]
        [SerializeField] private float maxRenderDistance = 100f;
        [SerializeField] private int maxVisiblePlayers = 50;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        /// <summary>
        /// Singleton instance for scene-specific multiplayer management.
        /// Handles remote player operations for the current gameplay scene.
        /// </summary>
        public static MultiplayerManager Instance { get; private set; }
        
        // Remote player tracking
        private Dictionary<ulong, RemotePlayer> activePlayers = new Dictionary<ulong, RemotePlayer>();
        private PlayerController localPlayer;
        
        // Performance optimization
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.1f; // Update visibility every 100ms
        
        // Event subscription tracking
        private bool isSubscribed = false;

        /// <summary>
        /// Unity Awake callback that initializes the singleton instance.
        /// Ensures only one MultiplayerManager exists per scene.
        /// </summary>
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("MultiplayerManager: Initialized");
            }
            else
            {
                Debug.LogWarning("MultiplayerManager: Multiple instances detected, destroying duplicate");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity Start callback that initializes the multiplayer manager.
        /// Creates parent object for remote players and subscribes to game server events.
        /// </summary>
        void Start()
        {
            // Create parent object if not assigned
            if (remotePlayersParent == null)
            {
                remotePlayersParent = new GameObject("RemotePlayers").transform;
            }
            
            // Debug prefab reference
            if (remotePlayerPrefab != null)
            {
                Debug.Log($"🎯 [MultiplayerManager] RemotePlayerPrefab assigned: {remotePlayerPrefab.name} (InstanceID: {remotePlayerPrefab.GetInstanceID()})");
            }
            else
            {
                Debug.LogError("🎯 [MultiplayerManager] ❌ RemotePlayerPrefab is NULL! Please assign it in Inspector.");
            }
            
            // Subscribe to game server events
            SubscribeToEvents();
        }

        /// <summary>
        /// Unity Update callback that handles periodic optimization tasks.
        /// Performs visibility culling and optimization at regular intervals.
        /// </summary>
        void Update()
        {
            // Periodic optimization updates
            if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
            {
                OptimizeVisibility();
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Unity OnDestroy callback that cleans up the singleton and unsubscribes from events.
        /// </summary>
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                UnsubscribeFromEvents();
            }
        }

        /// <summary>
        /// Subscribes to multiplayer-related game server events.
        /// Handles player join/leave events and tracks subscription status.
        /// </summary>
        private void SubscribeToEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient != null && !isSubscribed)
            {
                gameClient.OnPlayerJoined += OnPlayerJoined;
                gameClient.OnPlayerLeft += OnPlayerLeft;
                isSubscribed = true;
                LogDebug("Subscribed to multiplayer events");
            }
            else if (gameClient == null)
            {
                Debug.LogWarning("MultiplayerManager: GameServerClient not available");
            }
        }

        /// <summary>
        /// Unsubscribes from multiplayer-related game server events.
        /// Called during cleanup to prevent memory leaks and unwanted event callbacks.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient != null && isSubscribed)
            {
                gameClient.OnPlayerJoined -= OnPlayerJoined;
                gameClient.OnPlayerLeft -= OnPlayerLeft;
                isSubscribed = false;
                LogDebug("Unsubscribed from multiplayer events");
            }
        }

        /// <summary>
        /// Sets the reference to the local player controller.
        /// Used to differentiate between local and remote players during spawn operations.
        /// </summary>
        /// <param name="player">The local player controller instance</param>
        public void SetLocalPlayer(PlayerController player)
        {
            localPlayer = player;
            LogDebug($"Local player set: {player?.PlayerName}");
        }

        #region Event Handlers

        /// <summary>
        /// Event handler called when a new player joins the current zone.
        /// Validates player information and spawns the remote player if valid.
        /// </summary>
        /// <param name="playerJoinedData">Player joined data containing new player information</param>
        private void OnPlayerJoined(S_PlayerJoined playerJoinedData)
        {
            Debug.Log($"🔥 [MultiplayerManager] OnPlayerJoined called!");
            var playerInfo = playerJoinedData.PlayerInfo;
            if (playerInfo.HasValue)
            {
                Debug.Log($"🔥 [MultiplayerManager] Player info valid - PlayerId: {playerInfo.Value.PlayerId}, Name: {playerInfo.Value.Name}");
                SpawnRemotePlayer(playerInfo.Value);
            }
            else
            {
                Debug.LogError("🔥 [MultiplayerManager] PlayerInfo is null!");
            }
        }

        /// <summary>
        /// Event handler called when a player leaves the current zone.
        /// Automatically despawns the corresponding remote player.
        /// </summary>
        /// <param name="playerLeftData">Player left data containing departing player ID</param>
        private void OnPlayerLeft(S_PlayerLeft playerLeftData)
        {
            DespawnRemotePlayer(playerLeftData.PlayerId);
        }

        #endregion

        #region Remote Player Management

        /// <summary>
        /// Spawns a remote player in the game world using the provided player information.
        /// Performs validation checks to prevent duplicate spawns and enforce player limits.
        /// </summary>
        /// <param name="playerInfo">Player information containing ID, name, and spawn position</param>
        public void SpawnRemotePlayer(PlayerInfo playerInfo)
        {
            Debug.Log($"🚀 [SpawnRemotePlayer] Called for PlayerId: {playerInfo.PlayerId}, Name: {playerInfo.Name}");
            
            // Don't spawn if it's the local player
            if (localPlayer != null && playerInfo.PlayerId == localPlayer.PlayerId)
            {
                Debug.Log($"🚀 [SpawnRemotePlayer] Ignoring spawn for local player {playerInfo.PlayerId}");
                return;
            }

            // Don't spawn if already exists
            if (activePlayers.ContainsKey(playerInfo.PlayerId))
            {
                Debug.Log($"🚀 [SpawnRemotePlayer] Player {playerInfo.PlayerId} already exists, updating instead");
                UpdateRemotePlayer(playerInfo.PlayerId, playerInfo.Position?.ToUnityVector3() ?? Vector3.zero);
                return;
            }

            // Check player limit
            if (activePlayers.Count >= maxVisiblePlayers)
            {
                Debug.LogWarning($"🚀 [SpawnRemotePlayer] Max visible players ({maxVisiblePlayers}) reached, ignoring spawn");
                return;
            }

            // Create remote player object
            if (remotePlayerPrefab != null)
            {
                Debug.Log($"🚀 [SpawnRemotePlayer] Creating player object with prefab");
                Vector3 spawnPosition = playerInfo.Position?.ToUnityVector3() ?? Vector3.zero;
                GameObject playerObj = Instantiate(remotePlayerPrefab, spawnPosition, Quaternion.identity, remotePlayersParent);
                
                // Get RemotePlayer component
                RemotePlayer remotePlayer = playerObj.GetComponent<RemotePlayer>();
                if (remotePlayer == null)
                {
                    remotePlayer = playerObj.AddComponent<RemotePlayer>();
                }

                // Initialize remote player
                remotePlayer.Initialize(playerInfo.PlayerId, playerInfo.Name);
                
                // Track the player
                activePlayers[playerInfo.PlayerId] = remotePlayer;
                
                Debug.Log($"✅ [SpawnRemotePlayer] Successfully spawned: {playerInfo.Name} (ID: {playerInfo.PlayerId}) at {spawnPosition}");
            }
            else
            {
                Debug.LogError($"❌ [SpawnRemotePlayer] Remote player prefab not assigned! Cannot spawn player {playerInfo.PlayerId}");
            }
        }

        /// <summary>
        /// Removes a remote player from the game world and cleans up associated resources.
        /// Safely handles cases where the player doesn't exist or has already been removed.
        /// </summary>
        /// <param name="playerId">The unique ID of the player to despawn</param>
        public void DespawnRemotePlayer(ulong playerId)
        {
            if (activePlayers.TryGetValue(playerId, out RemotePlayer remotePlayer))
            {
                LogDebug($"Despawning remote player: {remotePlayer.PlayerName} (ID: {playerId})");
                
                activePlayers.Remove(playerId);
                
                if (remotePlayer != null)
                {
                    Destroy(remotePlayer.gameObject);
                }
            }
            else
            {
                LogDebug($"Attempted to despawn non-existent player: {playerId}");
            }
        }

        /// <summary>
        /// Updates the position, velocity, and rotation of an existing remote player.
        /// Only updates players that are currently active and spawned.
        /// </summary>
        /// <param name="playerId">The unique ID of the player to update</param>
        /// <param name="position">The new position for the player</param>
        /// <param name="velocity">Optional velocity information for smooth interpolation</param>
        /// <param name="rotation">Optional rotation information for player orientation</param>
        public void UpdateRemotePlayer(ulong playerId, Vector3 position, Vector3 velocity = default, Vector3 rotation = default)
        {
            if (activePlayers.TryGetValue(playerId, out RemotePlayer remotePlayer))
            {
                remotePlayer.UpdatePosition(position);
                if (velocity != default)
                {
                    remotePlayer.UpdateVelocity(velocity);
                }
                if (rotation != default)
                {
                    remotePlayer.UpdateRotation(rotation);
                }
            }
        }

        /// <summary>
        /// Updates a remote player using data from a world snapshot.
        /// Automatically excludes the local player from updates and converts protocol data to Unity types.
        /// </summary>
        /// <param name="playerState">Player state data from server world snapshot</param>
        public void UpdateRemotePlayerFromSnapshot(PlayerState playerState)
        {
            // Don't update local player here
            if (localPlayer != null && playerState.PlayerId == localPlayer.PlayerId)
            {
                return;
            }

            Vector3 position = playerState.Position?.ToUnityVector3() ?? Vector3.zero;
            Vector3 velocity = playerState.Velocity?.ToUnityVector3() ?? Vector3.zero;
            Vector3 rotation = playerState.Rotation?.ToUnityVector3() ?? Vector3.zero;
            
            UpdateRemotePlayer(playerState.PlayerId, position, velocity, rotation);
        }

        /// <summary>
        /// Spawns multiple players that were already present in the zone when the local player joined.
        /// Typically called when entering a zone that contains other players.
        /// </summary>
        /// <param name="existingPlayers">Array of player information for existing zone occupants</param>
        public void SpawnExistingPlayers(PlayerInfo[] existingPlayers)
        {
            if (existingPlayers == null) return;

            LogDebug($"Spawning {existingPlayers.Length} existing players");
            
            foreach (var playerInfo in existingPlayers)
            {
                SpawnRemotePlayer(playerInfo);
            }
        }

        #endregion

        #region Optimization

        private void OptimizeVisibility()
        {
            if (localPlayer == null) return;

            Vector3 localPos = localPlayer.transform.position;
            
            foreach (var kvp in activePlayers)
            {
                RemotePlayer remotePlayer = kvp.Value;
                if (remotePlayer == null) continue;

                float distance = Vector3.Distance(localPos, remotePlayer.transform.position);
                bool shouldBeVisible = distance <= maxRenderDistance;
                
                if (remotePlayer.gameObject.activeSelf != shouldBeVisible)
                {
                    remotePlayer.gameObject.SetActive(shouldBeVisible);
                    LogDebug($"Player {remotePlayer.PlayerName} visibility: {shouldBeVisible} (distance: {distance:F1})");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the current number of active remote players being tracked.
        /// </summary>
        /// <returns>The count of active remote players</returns>
        public int GetActivePlayerCount() => activePlayers.Count;
        
        /// <summary>
        /// Retrieves a specific remote player by their unique ID.
        /// </summary>
        /// <param name="playerId">The unique ID of the player to retrieve</param>
        /// <returns>The RemotePlayer instance, or null if not found</returns>
        public RemotePlayer GetRemotePlayer(ulong playerId)
        {
            activePlayers.TryGetValue(playerId, out RemotePlayer player);
            return player;
        }

        public RemotePlayer[] GetAllRemotePlayers()
        {
            var players = new RemotePlayer[activePlayers.Count];
            int index = 0;
            foreach (var player in activePlayers.Values)
            {
                players[index++] = player;
            }
            return players;
        }

        public void ClearAllRemotePlayers()
        {
            LogDebug("Clearing all remote players");
            
            foreach (var remotePlayer in activePlayers.Values)
            {
                if (remotePlayer != null)
                {
                    Destroy(remotePlayer.gameObject);
                }
            }
            
            activePlayers.Clear();
        }

        /// <summary>
        /// Retry subscription to GameServerClient events if it becomes available after Start()
        /// </summary>
        public void RetrySubscription()
        {
            if (GameServerClient.Instance != null && !isSubscribed)
            {
                SubscribeToEvents();
            }
        }

        public void SetMaxVisiblePlayers(int maxPlayers)
        {
            maxVisiblePlayers = Mathf.Clamp(maxPlayers, 1, 200);
        }

        public void SetMaxRenderDistance(float distance)
        {
            maxRenderDistance = Mathf.Clamp(distance, 10f, 500f);
        }

        #endregion

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MultiplayerManager] {message}");
            }
        }
    }
}