using UnityEngine.SceneManagement;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    private static GameFlowManager _instance;
    private static readonly object _lock = new object();
    
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

    public static void Initialize()
    {
        if (_instance != null)
        {
            Debug.LogWarning("GameFlowManager: Already initialized");
            return;
        }
        
        lock (_lock)
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameFlowManager");
                _instance = go.AddComponent<GameFlowManager>();
                DontDestroyOnLoad(go);
                Debug.Log("GameFlowManager: Initialized successfully");
            }
        }
    }
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

    public void LoadLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }

    public void LoadCharacterSelectScene()
    {
        SceneManager.LoadScene("CharacterSelectScene");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
