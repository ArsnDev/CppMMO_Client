using UnityEngine;
using SimpleMMO.Network;
using SimpleMMO.Game;
using CppMMO.Protocol;
using SimpleMMO.Protocol.Extensions;
using System.Collections.Generic;

namespace SimpleMMO.Managers
{
    public class WorldSyncManager : MonoBehaviour
    {
        [Header("Sync Settings")]
        [SerializeField] private float interpolationSpeed = 10f;
        [SerializeField] private bool enablePositionInterpolation = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        public static WorldSyncManager Instance { get; private set; }
        
        private PlayerController localPlayer;
        
        private ulong lastTickNumber = 0;
        private float lastSyncTime = 0f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("WorldSyncManager: Initialized");
            }
            else
            {
                Debug.LogWarning("WorldSyncManager: Multiple instances detected, destroying duplicate");
                Destroy(gameObject);
            }
        }

        void Start()
        {
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                UnsubscribeFromEvents();
            }
        }

        private void SubscribeToEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient != null)
            {
                gameClient.OnWorldSnapshot += OnWorldSnapshot;
                LogDebug("Subscribed to WorldSnapshot events");
            }
            else
            {
                Debug.LogWarning("WorldSyncManager: GameServerClient not available");
            }
        }

        private void UnsubscribeFromEvents()
        {
            var gameClient = GameServerClient.Instance;
            if (gameClient != null)
            {
                gameClient.OnWorldSnapshot -= OnWorldSnapshot;
                LogDebug("Unsubscribed from WorldSnapshot events");
            }
        }

        public void SetLocalPlayer(PlayerController player)
        {
            localPlayer = player;
            LogDebug($"Local player set: {player?.PlayerName}");
        }

        private void OnWorldSnapshot(S_WorldSnapshot snapshot)
        {
            if (snapshot.TickNumber <= lastTickNumber)
            {
                return;
            }

            lastTickNumber = snapshot.TickNumber;
            lastSyncTime = Time.time;

            LogDebug($"WorldSnapshot received: Tick {snapshot.TickNumber}, {snapshot.PlayerStatesLength} players");

            SyncPlayerStates(snapshot);

            ProcessGameEvents(snapshot);
        }

        private void SyncPlayerStates(S_WorldSnapshot snapshot)
        {
            if (localPlayer == null)
            {
                LogDebug("Local player not set, skipping sync");
                return;
            }

            for (int i = 0; i < snapshot.PlayerStatesLength; i++)
            {
                var playerState = snapshot.PlayerStates(i);
                if (playerState.HasValue)
                {
                    SyncPlayerState(playerState.Value);
                }
            }
        }

        private void SyncPlayerState(PlayerState playerState)
        {
            if (localPlayer != null && playerState.PlayerId == localPlayer.PlayerId)
            {
                SyncLocalPlayer(playerState);
            }
            else
            {
                // 다른 플레이어 동기화 (TODO: MultiplayerManager로 이관)
                SyncRemotePlayer(playerState);
            }
        }

        private void SyncLocalPlayer(PlayerState playerState)
        {
            Vector3 serverPosition = playerState.Position?.ToUnityVector3() ?? Vector3.zero;
            Vector3 serverVelocity = playerState.Velocity?.ToUnityVector3() ?? Vector3.zero;

            if (enablePositionInterpolation)
            {
                StartCoroutine(InterpolateToPosition(localPlayer, serverPosition));
            }
            else
            {
                localPlayer.UpdatePosition(serverPosition);
            }

            localPlayer.UpdateVelocity(serverVelocity);

            LogDebug($"Local player synced: {serverPosition}, velocity: {serverVelocity}");
        }

        private void SyncRemotePlayer(PlayerState playerState)
        {
            // TODO: MultiplayerManager와 연동하여 다른 플레이어 동기화
            LogDebug($"Remote player {playerState.PlayerId} at {playerState.Position?.ToUnityVector3()}");
        }

        private System.Collections.IEnumerator InterpolateToPosition(PlayerController player, Vector3 targetPosition)
        {
            Vector3 startPosition = player.transform.position;
            float journey = 0f;
            float distance = Vector3.Distance(startPosition, targetPosition);

            if (distance > 10f)
            {
                player.UpdatePosition(targetPosition);
                yield break;
            }

            while (journey <= 1f)
            {
                journey += Time.deltaTime * interpolationSpeed;
                Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, journey);
                player.UpdatePosition(currentPosition);
                yield return null;
            }

            player.UpdatePosition(targetPosition);
        }

        private void ProcessGameEvents(S_WorldSnapshot snapshot)
        {
            if (snapshot.EventsLength > 0)
            {
                LogDebug($"Processing {snapshot.EventsLength} game events");

                for (int i = 0; i < snapshot.EventsLength; i++)
                {
                    var gameEvent = snapshot.Events(i);
                    if (gameEvent.HasValue)
                    {
                        ProcessGameEvent(gameEvent.Value);
                    }
                }
            }
        }

        private void ProcessGameEvent(GameEvent gameEvent)
        {
            switch (gameEvent.EventType)
            {
                case CppMMO.Protocol.EventType.PLAYER_DAMAGE:
                    LogDebug($"Player damage event: Source={gameEvent.SourcePlayerId}, Target={gameEvent.TargetPlayerId}, Value={gameEvent.Value}");
                    // TODO: 데미지 이펙트 처리
                    break;

                case CppMMO.Protocol.EventType.PLAYER_HEAL:
                    LogDebug($"Player heal event: Source={gameEvent.SourcePlayerId}, Target={gameEvent.TargetPlayerId}, Value={gameEvent.Value}");
                    // TODO: 힐 이펙트 처리
                    break;

                case CppMMO.Protocol.EventType.PLAYER_DEATH:
                    LogDebug($"Player death event: Source={gameEvent.SourcePlayerId}, Target={gameEvent.TargetPlayerId}");
                    // TODO: 사망 이펙트 처리
                    break;

                case CppMMO.Protocol.EventType.PLAYER_RESPAWN:
                    LogDebug($"Player respawn event: PlayerId={gameEvent.SourcePlayerId}, Position={gameEvent.Position?.ToUnityVector3()}");
                    // TODO: 리스폰 이펙트 처리
                    break;

                case CppMMO.Protocol.EventType.NONE:
                default:
                    LogDebug($"Event type: {gameEvent.EventType}, Source={gameEvent.SourcePlayerId}, Target={gameEvent.TargetPlayerId}");
                    break;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[WorldSyncManager] {message}");
            }
        }

        #region Public API

        public bool IsReceivingUpdates => Time.time - lastSyncTime < 1f; // 1초 이내 업데이트 수신 확인
        public ulong LastTickNumber => lastTickNumber;
        public float LastSyncTime => lastSyncTime;

        public void EnableDebugLogs(bool enable)
        {
            enableDebugLogs = enable;
        }

        public void SetInterpolationSpeed(float speed)
        {
            interpolationSpeed = Mathf.Clamp(speed, 1f, 50f);
        }

        #endregion
    }
}