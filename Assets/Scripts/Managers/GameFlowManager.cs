using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>
/// Manages the overall game flow and scene transitions for the MMO client.
/// Implements singleton pattern with thread-safe initialization.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    private static GameFlowManager _instance;
    private static readonly object _lock = new object();
    
    /// <summary>
    /// Gets the singleton instance of GameFlowManager.
    /// Thread-safe property that ensures only one instance exists.
    /// </summary>
    public static GameFlowManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    Debug.LogError("GameFlowManager: Instance not initialized. Call Initialize() from main thread first.");
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// Initializes the GameFlowManager singleton instance.
    /// Creates a persistent GameObject that survives scene changes.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_instance != null)
            {
                Debug.LogWarning("GameFlowManager: Already initialized");
                return;
            }
            
            if (_instance == null)
            {
                GameObject go = new GameObject("GameFlowManager");
                _instance = go.AddComponent<GameFlowManager>();
                DontDestroyOnLoad(go);
                Debug.Log("GameFlowManager: Initialized successfully");
            }
        }
    }
    /// <summary>
    /// Unity Awake callback that ensures singleton pattern integrity.
    /// Prevents duplicate instances by destroying any additional GameFlowManager objects.
    /// </summary>
    void Awake()
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning("GameFlowManager: Duplicate instance detected, destroying");
                Destroy(gameObject);
                return;
            }
        }
    }

    /// <summary>
    /// Loads the login scene where players authenticate with the auth server.
    /// </summary>
    public void LoadLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }

    /// <summary>
    /// Loads the character selection scene where players choose their character.
    /// </summary>
    public void LoadCharacterSelectScene()
    {
        SceneManager.LoadScene("CharacterSelectScene");
    }

    /// <summary>
    /// Loads the main gameplay scene.
    /// InGameManager will handle the game server connection and authentication.
    /// </summary>
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GamePlayScene");
    }

    /// <summary>
    /// Terminates the application.
    /// In editor, this will stop play mode; in builds, this will close the application.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}
