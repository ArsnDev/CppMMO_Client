using UnityEngine;
using SimpleMMO.Network;
using SimpleMMO.Game;
using CppMMO.Protocol;
using SimpleMMO.Protocol.Extensions;

namespace SimpleMMO.Managers
{
    /// <summary>
    /// Manages the in-game state and coordinates between the game server connection,
    /// player spawning, and various game systems once the gameplay scene is loaded.
    /// Handles the complete game initialization sequence from server connection to player spawn.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class InGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform playerSpawnPoint;
        
        [Header("Zone Settings")]
        [SerializeField] private int defaultZoneId = 1;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // Game State
        private PlayerController localPlayer;
        private bool isGameStarted = false;
        private bool isConnecting = false;
        
        /// <summary>
        /// Singleton instance for scene-specific management.
        /// Unlike persistent managers, this instance is recreated for each gameplay scene.
        /// </summary>
        public static InGameManager Instance { get; private set; }

        /// <summary>
        /// Unity Awake callback that initializes the singleton instance for the current scene.
        /// Ensures only one InGameManager exists per gameplay scene.
        /// </summary>
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("InGameManager: Initialized for GameScene");
            }
            else
            {
                Debug.LogWarning("InGameManager: Multiple instances detected, destroying duplicate");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity Start callback that begins the game initialization sequence.
        /// </summary>
        void Start()
        {
            StartGameSequence();
        }

        /// <summary>
        /// Unity OnDestroy callback that cleans up the singleton reference and unsubscribes from events.
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
        /// Initiates the complete game startup sequence including validation, event subscription, and server connection.
        /// Performs pre-checks to ensure all required managers and data are available before proceeding.
        /// </summary>
        private void StartGameSequence()
        {
            if (isConnecting)
            {
                Debug.LogWarning("InGameManager: Already connecting to game server");
                return;
            }

            if (!GameInitializer.AreManagersInitialized())
            {
                Debug.LogError("InGameManager: Core managers not initialized");
                return;
            }

            if (!ValidateGameData())
            {
                Debug.LogError("InGameManager: Invalid game data, cannot start game");
                return;
            }

            Debug.Log("InGameManager: Starting game sequence...");
            isConnecting = true;

            SubscribeToGameServerEvents();

            ConnectToGameServer();
        }

        /// <summary>
        /// Validates that all required game data is available before starting the game.
        /// Checks for valid session ticket and selected character.
        /// </summary>
        /// <returns>True if all validation checks pass, false otherwise</returns>
        private bool ValidateGameData()
        {
            string sessionTicket = SessionManager.Instance?.SessionTicket;
            if (string.IsNullOrEmpty(sessionTicket))
            {
                Debug.LogError("InGameManager: No valid session ticket");
                return false;
            }

            var selectedCharacter = PlayerDataManager.Instance?.GetSelectedCharacter();
            if (selectedCharacter == null)
            {
                Debug.LogError("InGameManager: No character selected");
                return false;
            }

            LogDebug($"Session: [REDACTED], Character: {selectedCharacter.name} (ID: {selectedCharacter.playerId})");
            return true;
        }

        /// <summary>
        /// Subscribes to all necessary game server events for handling gameplay flow.
        /// Sets up event handlers for connection, authentication, zone management, and multiplayer events.
        /// </summary>
        private void SubscribeToGameServerEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient == null)
            {
                Debug.LogError("InGameManager: GameServerClient not available");
                return;
            }

            gameClient.OnConnected += OnGameServerConnected;
            gameClient.OnDisconnected += OnGameServerDisconnected;

            gameClient.OnLoginSuccess += OnLoginSuccess;
            gameClient.OnLoginFailure += OnLoginFailure;

            gameClient.OnZoneEntered += OnZoneEntered;
            gameClient.OnWorldSnapshot += OnWorldSnapshot;
            gameClient.OnPlayerJoined += OnPlayerJoined;
            gameClient.OnPlayerLeft += OnPlayerLeft;

            LogDebug("Subscribed to GameServer events");
        }

        /// <summary>
        /// Unsubscribes from all game server events to prevent memory leaks and unwanted callbacks.
        /// Called during cleanup when the InGameManager is being destroyed.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient == null) return;

            gameClient.OnConnected -= OnGameServerConnected;
            gameClient.OnDisconnected -= OnGameServerDisconnected;
            gameClient.OnLoginSuccess -= OnLoginSuccess;
            gameClient.OnLoginFailure -= OnLoginFailure;
            gameClient.OnZoneEntered -= OnZoneEntered;
            gameClient.OnWorldSnapshot -= OnWorldSnapshot;
            gameClient.OnPlayerJoined -= OnPlayerJoined;
            gameClient.OnPlayerLeft -= OnPlayerLeft;

            LogDebug("Unsubscribed from GameServer events");
        }

        /// <summary>
        /// Initiates the connection to the game server.
        /// Sets connecting flag to false if GameServerClient is not available.
        /// </summary>
        private void ConnectToGameServer()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient == null)
            {
                Debug.LogError("InGameManager: GameServerClient not available");
                isConnecting = false;
                return;
            }

            LogDebug("Connecting to game server...");
            gameClient.Connect();
        }

        #region GameServer Event Handlers

        /// <summary>
        /// Event handler called when connection to the game server is established.
        /// Automatically sends login credentials using session ticket and selected character.
        /// </summary>
        private void OnGameServerConnected()
        {
            LogDebug("Connected to game server, sending login...");

            var sessionTicket = SessionManager.Instance?.SessionTicket;
            var selectedCharacter = PlayerDataManager.Instance?.GetSelectedCharacter();

            if (!string.IsNullOrEmpty(sessionTicket) && selectedCharacter != null)
            {
                GameServerClient.Instance?.SendLogin(sessionTicket, selectedCharacter.playerId);
            }
            else
            {
                Debug.LogError("InGameManager: Missing session or character data for login");
                isConnecting = false;
            }
        }

        /// <summary>
        /// Event handler called when connection to the game server is lost.
        /// Resets connection and game state flags.
        /// </summary>
        private void OnGameServerDisconnected()
        {
            Debug.LogWarning("InGameManager: Disconnected from game server");
            isConnecting = false;
            isGameStarted = false;
        }

        /// <summary>
        /// Event handler called when login to the game server succeeds.
        /// Automatically sends zone entry request and starts timeout monitoring.
        /// </summary>
        /// <param name="loginData">Login success data from the server</param>
        private void OnLoginSuccess(S_LoginSuccess loginData)
        {
            LogDebug($"Login successful! Entering zone {defaultZoneId}...");

            // Enter zone with configured zone ID
            GameServerClient.Instance?.SendEnterZone(defaultZoneId);
            
            // Start timeout monitoring for server response
            StartCoroutine(WaitForZoneResponse());
        }
        
        /// <summary>
        /// Coroutine that waits for zone entry response from the server.
        /// Implements timeout handling if S_ZoneEntered packet is not received within 5 seconds.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution</returns>
        private System.Collections.IEnumerator WaitForZoneResponse()
        {
            yield return new WaitForSeconds(5f); // Wait for server response
            
            // Timeout if S_ZoneEntered is not received after 5 seconds
            if (localPlayer == null && !isGameStarted)
            {
                LogDebug("Zone entry timeout - this should not happen if server is working properly");
                // Handle timeout without retry to avoid connection issues
                isConnecting = false;
            }
        }
        
        /// <summary>
        /// Coroutine that waits for server response and manually spawns player if needed.
        /// Fallback mechanism when S_ZoneEntered packet is not received from server.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution</returns>
        private System.Collections.IEnumerator WaitAndSpawnPlayer()
        {
            // Wait 2 seconds for server response
            yield return new WaitForSeconds(2f);
            
            // Manually spawn if player hasn't been spawned yet
            if (localPlayer == null)
            {
                LogDebug("Server didn't send S_ZoneEntered, manually spawning player...");
                
                var selectedCharacter = PlayerDataManager.Instance?.GetSelectedCharacter();
                if (selectedCharacter != null)
                {
                    // Direct spawn without FlatBuffers data
                    if (playerPrefab != null)
                    {
                        Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
                        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                        localPlayer = playerObj.GetComponent<PlayerController>();

                        if (localPlayer != null)
                        {
                            localPlayer.Initialize(selectedCharacter.playerId, selectedCharacter.name, isLocal: true);
                            LogDebug($"Manual local player spawned: {selectedCharacter.name} at {spawnPosition}");
                            LogDebug($"Player object: {playerObj.name}, Active: {playerObj.activeInHierarchy}");
                            LogDebug($"Player scale: {playerObj.transform.localScale}");
                            LogDebug($"Camera position: {Camera.main.transform.position}");

                            // Register with managers
                            if (WorldSyncManager.Instance != null)
                            {
                                WorldSyncManager.Instance.SetLocalPlayer(localPlayer);
                                WorldSyncManager.Instance.RetrySubscription();
                            }

                            if (MultiplayerManager.Instance != null)
                            {
                                MultiplayerManager.Instance.SetLocalPlayer(localPlayer);
                                MultiplayerManager.Instance.RetrySubscription();
                            }

                            SetupCameraFollow(playerObj.transform);
                        }
                    }
                    
                    isConnecting = false;
                    isGameStarted = true;
                    LogDebug("Manual player spawn completed!");
                }
            }
        }

        /// <summary>
        /// Event handler called when login to the game server fails.
        /// Logs error message and resets connection state.
        /// </summary>
        /// <param name="loginFailure">Login failure data containing error information</param>
        private void OnLoginFailure(S_LoginFailure loginFailure)
        {
            Debug.LogError($"InGameManager: Login failed - {loginFailure.ErrorMessage}");
            isConnecting = false;
        }

        /// <summary>
        /// Event handler called when successfully entering a game zone.
        /// Spawns the local player and any existing players already in the zone.
        /// </summary>
        /// <param name="zoneData">Zone entry data containing player information and zone details</param>
        private void OnZoneEntered(S_ZoneEntered zoneData)
        {
            LogDebug($"Entered zone {zoneData.ZoneId}");

            SpawnLocalPlayer(zoneData.MyPlayer);

            // Spawn existing players in the zone
            if (zoneData.OtherPlayersLength > 0)
            {
                LogDebug($"Found {zoneData.OtherPlayersLength} other players in zone");
                
                // Collect existing players
                var existingPlayers = new PlayerInfo[zoneData.OtherPlayersLength];
                for (int i = 0; i < zoneData.OtherPlayersLength; i++)
                {
                    var otherPlayer = zoneData.OtherPlayers(i);
                    if (otherPlayer.HasValue)
                    {
                        existingPlayers[i] = otherPlayer.Value;
                        LogDebug($"Other player: {otherPlayer.Value.Name} at {otherPlayer.Value.Position}");
                    }
                }

                // Spawn them through MultiplayerManager
                if (MultiplayerManager.Instance != null)
                {
                    MultiplayerManager.Instance.SpawnExistingPlayers(existingPlayers);
                }
            }

            isConnecting = false;
            isGameStarted = true;
            LogDebug("Game started successfully!");
        }

        /// <summary>
        /// Event handler for world snapshot updates from the server.
        /// Functionality delegated to WorldSyncManager which directly subscribes to this event.
        /// </summary>
        /// <param name="snapshot">World snapshot data from the server</param>
        private void OnWorldSnapshot(S_WorldSnapshot snapshot)
        {
            // Delegated to WorldSyncManager - this method is no longer needed
            // WorldSyncManager directly subscribes to OnWorldSnapshot event
        }

        /// <summary>
        /// Event handler called when a new player joins the current zone.
        /// Functionality delegated to MultiplayerManager via event subscription.
        /// </summary>
        /// <param name="playerJoined">Player joined data containing new player information</param>
        private void OnPlayerJoined(S_PlayerJoined playerJoined)
        {
            LogDebug($"Player joined: {playerJoined.PlayerInfo?.Name}");
            // Delegated to MultiplayerManager via event subscription
        }

        /// <summary>
        /// Event handler called when a player leaves the current zone.
        /// Functionality delegated to MultiplayerManager via event subscription.
        /// </summary>
        /// <param name="playerLeft">Player left data containing departing player information</param>
        private void OnPlayerLeft(S_PlayerLeft playerLeft)
        {
            LogDebug($"Player left: ID {playerLeft.PlayerId}");
            // Delegated to MultiplayerManager via event subscription
        }

        #endregion

        /// <summary>
        /// Spawns the local player character in the game world using server-provided player information.
        /// Registers the player with all relevant managers and sets up camera following.
        /// </summary>
        /// <param name="playerInfo">Optional player information from the server containing spawn data</param>
        private void SpawnLocalPlayer(PlayerInfo? playerInfo)
        {
            if (!playerInfo.HasValue)
            {
                Debug.LogError("InGameManager: Invalid player info for local player");
                return;
            }

            var info = playerInfo.Value;
            Vector3 spawnPosition = info.Position?.ToUnityVector3() ?? Vector3.zero;

            if (playerPrefab != null)
            {
                GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                localPlayer = playerObj.GetComponent<PlayerController>();

                if (localPlayer != null)
                {
                    localPlayer.Initialize(info.PlayerId, info.Name, isLocal: true);
                    LogDebug($"Local player spawned: {info.Name} at {spawnPosition}");

                    // Register local player with WorldSyncManager
                    if (WorldSyncManager.Instance != null)
                    {
                        WorldSyncManager.Instance.SetLocalPlayer(localPlayer);
                        // Ensure WorldSyncManager is properly subscribed to events
                        WorldSyncManager.Instance.RetrySubscription();
                    }

                    // Register local player with MultiplayerManager
                    if (MultiplayerManager.Instance != null)
                    {
                        MultiplayerManager.Instance.SetLocalPlayer(localPlayer);
                        // Ensure MultiplayerManager is properly subscribed to events
                        MultiplayerManager.Instance.RetrySubscription();
                    }

                    SetupCameraFollow(playerObj.transform);
                }
                else
                {
                    Debug.LogError("InGameManager: PlayerController component not found on player prefab");
                }
            }
            else
            {
                Debug.LogError("InGameManager: Player prefab not assigned");
            }
        }

        /// <summary>
        /// Sets up camera to follow the local player.
        /// Currently implements basic camera positioning; will be enhanced with CameraController.
        /// </summary>
        /// <param name="playerTransform">The transform of the player to follow</param>
        private void SetupCameraFollow(Transform playerTransform)
        {
            // TODO: Apply proper CameraController implementation
            Camera.main.transform.position = new Vector3(
                playerTransform.position.x, 
                playerTransform.position.y, 
                -10f
            );
        }

        /// <summary>
        /// Logs debug messages with InGameManager prefix if debug logging is enabled.
        /// </summary>
        /// <param name="message">The message to log</param>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[InGameManager] {message}");
            }
        }

        #region Public API

        /// <summary>
        /// Gets whether the game has been successfully started and the player is spawned.
        /// </summary>
        public bool IsGameStarted => isGameStarted;
        
        /// <summary>
        /// Gets whether the manager is currently in the process of connecting to the game server.
        /// </summary>
        public bool IsConnecting => isConnecting;
        
        /// <summary>
        /// Gets the local player controller instance, or null if not yet spawned.
        /// </summary>
        /// <returns>The local player controller, or null if not available</returns>
        public PlayerController GetLocalPlayer() => localPlayer;

        /// <summary>
        /// Disconnects from the game server and resets the game state.
        /// Can be called to gracefully leave the game session.
        /// </summary>
        public void DisconnectFromGame()
        {
            if (GameServerClient.Instance != null)
            {
                GameServerClient.Instance.Disconnect();
            }
            
            isGameStarted = false;
            isConnecting = false;
        }

        #endregion
    }
}
