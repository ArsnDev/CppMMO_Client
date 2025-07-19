using UnityEngine;
using SimpleMMO.Network;
using SimpleMMO.Game;
using CppMMO.Protocol;
using System.Collections.Generic;

namespace SimpleMMO.Test
{
    public class LoginTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private string testUsername = "testuser";
        [SerializeField] private string testPassword = "testpass";
        [SerializeField] private string testCharacterName = "TestChar";
        [SerializeField] private ulong testPlayerId = 12345;
        
        [Header("UI")]
        [SerializeField] private bool showGUI = true;
        
        private string statusMessage = "Ready to test";
        private string sessionTicket = "";
        private List<PlayerInfoDto> characters = new List<PlayerInfoDto>();
        private Vector2 scrollPosition;
        
        void Start()
        {
            // 서버 설정 확인
            Debug.Log($"Auth Server: {ServerConfig.AuthServerUrl}");
            Debug.Log($"Game Server: {ServerConfig.GameServerHost}:{ServerConfig.GameServerPort}");
            
            // 기존 이벤트 구독 해제 (중복 방지)
            GameServerClient.Instance.OnConnected -= OnGameServerConnected;
            GameServerClient.Instance.OnDisconnected -= OnGameServerDisconnected;
            GameServerClient.Instance.OnLoginSuccess -= OnGameLoginSuccess;
            GameServerClient.Instance.OnLoginFailure -= OnGameLoginFailure;
            GameServerClient.Instance.OnZoneEntered -= OnZoneEntered;
            
            // GameServerClient 이벤트 구독
            GameServerClient.Instance.OnConnected += OnGameServerConnected;
            GameServerClient.Instance.OnDisconnected += OnGameServerDisconnected;
            GameServerClient.Instance.OnLoginSuccess += OnGameLoginSuccess;
            GameServerClient.Instance.OnLoginFailure += OnGameLoginFailure;
            GameServerClient.Instance.OnZoneEntered += OnZoneEntered;
        }
        
        void OnGUI()
        {
            if (!showGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 500, Screen.height - 20));
            GUILayout.Label("SimpleMMO Login Test", new GUIStyle() { fontSize = 16 });
            
            GUILayout.Space(10);
            GUILayout.Label($"Status: {statusMessage}");
            
            GUILayout.Space(5);
            if (!string.IsNullOrEmpty(sessionTicket))
            {
                GUILayout.Label($"Session: {sessionTicket.Substring(0, 8)}...");
            }
            if (characters.Count > 0)
            {
                GUILayout.Label($"Characters: {characters.Count}, Using ID: {testPlayerId}");
            }
            
            GUILayout.Space(10);
            testUsername = GUILayout.TextField(testUsername);
            testPassword = GUILayout.TextField(testPassword);
            testCharacterName = GUILayout.TextField(testCharacterName);
            
            GUILayout.Space(10);
            
            // 스크롤 가능한 버튼 영역
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            if (GUILayout.Button("1. Register"))
            {
                TestAuthRegister();
            }
            
            if (GUILayout.Button("2. Auth Server Login"))
            {
                TestAuthLogin();
            }
            
            if (GUILayout.Button("3. Create Character"))
            {
                TestCreateCharacter();
            }
            
            if (GUILayout.Button("4. Get Characters"))
            {
                TestGetCharacters();
            }
            
            if (GUILayout.Button("5. Connect to Game Server"))
            {
                TestGameServerConnect();
            }
            
            if (GUILayout.Button("6. Game Server Login"))
            {
                TestGameServerLogin();
            }
            
            if (GUILayout.Button("7. Enter Zone"))
            {
                TestEnterZone();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Full Test"))
            {
                StartCoroutine(FullLoginTest());
            }
            
            if (GUILayout.Button("Disconnect"))
            {
                GameServerClient.Instance.Disconnect();
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        void TestAuthRegister()
        {
            statusMessage = "Testing Auth Server register...";
            
            AuthApiClient.Instance.Register(testUsername, testPassword, 
                onSuccess: (response) =>
                {
                    if (response.success)
                    {
                        sessionTicket = response.sessionTicket;
                        statusMessage = $"✅ Register success! SessionTicket: {sessionTicket}";
                        Debug.Log($"Register successful: {response.message}");
                    }
                    else
                    {
                        statusMessage = $"❌ Register failed: {response.message}";
                        Debug.LogError($"Register failed: {response.message}");
                    }
                },
                onError: (error) =>
                {
                    statusMessage = $"❌ Register error: {error}";
                    Debug.LogError($"Register error: {error}");
                });
        }
        
        void TestAuthLogin()
        {
            statusMessage = "Testing Auth Server login...";
            
            AuthApiClient.Instance.Login(testUsername, testPassword, 
                onSuccess: (response) =>
                {
                    if (response.success)
                    {
                        sessionTicket = response.sessionTicket;
                        statusMessage = $"✅ Auth login success! SessionTicket: {sessionTicket}";
                        Debug.Log($"Auth login successful: {response.message}");
                    }
                    else
                    {
                        statusMessage = $"❌ Auth login failed: {response.message}";
                        Debug.LogError($"Auth login failed: {response.message}");
                    }
                },
                onError: (error) =>
                {
                    statusMessage = $"❌ Auth login error: {error}";
                    Debug.LogError($"Auth login error: {error}");
                });
        }
        
        void TestCreateCharacter()
        {
            if (string.IsNullOrEmpty(sessionTicket))
            {
                statusMessage = "❌ Error: No session ticket. Login first.";
                return;
            }
            
            statusMessage = "Creating character...";
            
            AuthApiClient.Instance.CreateCharacter(sessionTicket, testCharacterName,
                onSuccess: (response) =>
                {
                    if (response.success)
                    {
                        statusMessage = $"✅ Character created! Name: {response.character.name}, ID: {response.character.playerId}";
                        testPlayerId = response.character.playerId; // 실제 playerId 저장
                        Debug.Log($"Character created: {response.character.name} (ID: {response.character.playerId})");
                    }
                    else
                    {
                        statusMessage = $"❌ Character creation failed: {response.message}";
                        Debug.LogError($"Character creation failed: {response.message}");
                    }
                },
                onError: (error) =>
                {
                    statusMessage = $"❌ Character creation error: {error}";
                    Debug.LogError($"Character creation error: {error}");
                });
        }
        
        void TestGetCharacters()
        {
            if (string.IsNullOrEmpty(sessionTicket))
            {
                statusMessage = "❌ Error: No session ticket. Login first.";
                return;
            }
            
            statusMessage = "Getting characters...";
            
            AuthApiClient.Instance.GetCharacters(sessionTicket,
                onSuccess: (response) =>
                {
                    if (response.success)
                    {
                        characters = response.characters;
                        statusMessage = $"✅ Characters loaded: {characters.Count} found";
                        Debug.Log($"Characters loaded: {characters.Count}");
                        
                        if (characters.Count > 0)
                        {
                            var firstChar = characters[0];
                            testPlayerId = firstChar.playerId; // 첫 번째 캐릭터 ID 사용
                            Debug.Log($"Using character: {firstChar.name} (ID: {firstChar.playerId})");
                        }
                        
                        foreach (var character in characters)
                        {
                            Debug.Log($"Character: {character.name} (ID: {character.playerId}) at ({character.posX}, {character.posY})");
                        }
                    }
                    else
                    {
                        statusMessage = $"❌ Get characters failed: {response.message}";
                        Debug.LogError($"Get characters failed: {response.message}");
                    }
                },
                onError: (error) =>
                {
                    statusMessage = $"❌ Get characters error: {error}";
                    Debug.LogError($"Get characters error: {error}");
                });
        }
        
        void TestGameServerConnect()
        {
            statusMessage = "Connecting to game server...";
            GameServerClient.Instance.Connect();
        }
        
        void TestGameServerLogin()
        {
            if (string.IsNullOrEmpty(sessionTicket))
            {
                statusMessage = "Error: No session ticket. Run auth login first.";
                return;
            }
            
            if (!GameServerClient.Instance.IsConnected)
            {
                statusMessage = "Error: Not connected to game server.";
                return;
            }
            
            statusMessage = "Sending game server login...";
            GameServerClient.Instance.SendLogin(sessionTicket, testPlayerId);
        }
        
        void TestEnterZone()
        {
            if (!GameServerClient.Instance.IsConnected)
            {
                statusMessage = "Error: Not connected to game server.";
                return;
            }
            
            statusMessage = "Entering zone...";
            GameServerClient.Instance.SendEnterZone(1);
        }
        
        System.Collections.IEnumerator FullLoginTest()
        {
            statusMessage = "Starting full login test...";
            
            // 1. Auth Register (if needed)
            TestAuthRegister();
            yield return new WaitForSeconds(2);
            
            // 2. Auth Login
            TestAuthLogin();
            yield return new WaitForSeconds(2);
            
            // 3. Create Character (if needed)
            TestCreateCharacter();
            yield return new WaitForSeconds(2);
            
            // 4. Get Characters
            TestGetCharacters();
            yield return new WaitForSeconds(2);
            
            // 5. Game Server Connect
            TestGameServerConnect();
            yield return new WaitForSeconds(2);
            
            // 6. Game Server Login
            if (GameServerClient.Instance.IsConnected)
            {
                TestGameServerLogin();
                yield return new WaitForSeconds(2);
            }
            
            // 7. Enter Zone
            if (GameServerClient.Instance.IsConnected)
            {
                TestEnterZone();
            }
        }
        
        // GameServerClient 이벤트 핸들러들
        void OnGameServerConnected()
        {
            statusMessage = "✅ Connected to game server!";
            Debug.Log("Game server connected successfully");
        }
        
        void OnGameServerDisconnected()
        {
            statusMessage = "❌ Disconnected from game server";
            Debug.Log("Game server disconnected");
        }
        
        void OnGameLoginSuccess(S_LoginSuccess loginSuccess)
        {
            if (loginSuccess.PlayerInfo.HasValue)
            {
                var playerInfo = loginSuccess.PlayerInfo.Value;
                statusMessage = $"✅ Game login success! Player ID: {playerInfo.PlayerId}";
                Debug.Log($"Game login successful: PlayerId={playerInfo.PlayerId}, Name={playerInfo.Name}");
            }
            else
            {
                statusMessage = "✅ Game login success! (No player info)";
                Debug.Log("Game login successful but no player info received");
            }
        }
        
        void OnGameLoginFailure(S_LoginFailure loginFailure)
        {
            statusMessage = $"❌ Game login failed: {loginFailure.ErrorMessage}";
            Debug.LogError($"Game login failed: Code={loginFailure.ErrorCode}, Message={loginFailure.ErrorMessage}");
        }
        
        void OnZoneEntered(S_ZoneEntered zoneEntered)
        {
            statusMessage = $"✅ Zone entered! Zone ID: {zoneEntered.ZoneId}, Other Players: {zoneEntered.OtherPlayersLength}";
            Debug.Log($"Zone entered: {zoneEntered.ZoneId}, Other players in zone: {zoneEntered.OtherPlayersLength}");
            
            // 내 플레이어 정보
            if (zoneEntered.MyPlayer.HasValue)
            {
                var myPlayer = zoneEntered.MyPlayer.Value;
                Debug.Log($"My Player: {myPlayer.PlayerId} at ({myPlayer.Position?.X}, {myPlayer.Position?.Y})");
            }
            
            // 존에 있는 다른 플레이어들 로그
            for (int i = 0; i < zoneEntered.OtherPlayersLength; i++)
            {
                var player = zoneEntered.OtherPlayers(i);
                if (player.HasValue)
                {
                    var p = player.Value;
                    Debug.Log($"Other Player in zone: {p.PlayerId} at ({p.Position?.X}, {p.Position?.Y})");
                }
            }
        }
        
        void OnDestroy()
        {
            if (GameServerClient.Instance != null)
            {
                GameServerClient.Instance.OnConnected -= OnGameServerConnected;
                GameServerClient.Instance.OnDisconnected -= OnGameServerDisconnected;
                GameServerClient.Instance.OnLoginSuccess -= OnGameLoginSuccess;
                GameServerClient.Instance.OnLoginFailure -= OnGameLoginFailure;
                GameServerClient.Instance.OnZoneEntered -= OnZoneEntered;
            }
        }
    }
}