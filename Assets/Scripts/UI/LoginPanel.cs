using CppMMO.Protocol;
using SimpleMMO.Managers;
using SimpleMMO.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleMMO.UI
{
    public class LoginPanel : MonoBehaviour
    {
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingIndicator;

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
        }
        private void InitializeUI()
        {
            SetLoadingState(false);
            SetStatusText("Enter your credentials to login or register.", Color.white);

            if (usernameInput != null) usernameInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
            
            Debug.Log("LoginPanel: UI initialized.");
        }

        private void SetupEventListeners()
        {
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClicked);
            else
                Debug.LogError("LoginPanel: Login button is not assigned.");
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterButtonClicked);
            else
                Debug.LogError("LoginPanel: Register button is not assigned.");
            if (passwordInput != null)
                passwordInput.onSubmit.AddListener(OnPasswordSubmit);
            else
                Debug.LogError("LoginPanel: Password input field is not assigned.");
        }

        private void OnPasswordSubmit(string value)
        {
            OnLoginButtonClicked();
        }

        private void OnLoginButtonClicked()
        {
            string username = usernameInput?.text?.Trim();
            string password = passwordInput?.text;
            
            if (!ValidateLoginInput(username, password))
            {
                SetStatusText("Please enter a valid username and password.", Color.red);
                return;
            }
            StartLogin(username, password);
        }

        private void OnRegisterButtonClicked()
        {
            string username = usernameInput?.text?.Trim();
            string password = passwordInput?.text;

            if(!ValidateRegisterInput(username, password))
            {
                SetStatusText("Please enter a valid username and password.", Color.red);
                return;
            }
            StartRegister(username, password);
        }

        private bool ValidateLoginInput(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                SetStatusText("Username cannot be empty.", Color.red);
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                SetStatusText("Password cannot be empty.", Color.red);
                return false;
            }

            if (username.Length < 3)
            {
                SetStatusText("Username must be at least 3 characters long.", Color.red);
                return false;
            }
            return true;
        }

        private bool ValidateRegisterInput(string username, string password)
        {
            if (!ValidateLoginInput(username, password))
                return false;
            
            if (password.Length < 6)
            {
                SetStatusText("Password must be at least 6 characters long.", Color.red);
                return false;
            }
            return true;
        }

        private void StartLogin(string username, string password)
        {
            SetLoadingState(true);
            SetStatusText("Logging in...", Color.white);
            Debug.Log($"LoginPanel: Attempting login for user: {username}");
            AuthApiClient.Instance.Login(username, password, OnLoginSuccess, OnLoginFailure);
        }

        private void StartRegister(string username, string password)
        {
            SetLoadingState(true);
            SetStatusText("Creating account...", Color.white);
            Debug.Log($"LoginPanel: Attempting registration for user: {username}");
            AuthApiClient.Instance.Register(username, password, OnRegisterSuccess, OnRegisterFailure);
        }

        private void OnLoginSuccess(LoginResponse response)
        {
            SetLoadingState(false);
            if (response.success)
            {
                SetStatusText("Login successful!", Color.green);
                SessionManager.Instance.SaveSession(response.sessionTicket);
                Debug.Log("LoginPanel: Login successful, session saved");
                Invoke(nameof(LoadCharacterSelectScene), 1.0f);
            }
            else
            {
                SetStatusText($"Login failed: {response.message}", Color.red);
            }
        }

        private void OnLoginFailure(string error)
        {
            SetLoadingState(false);
            SetStatusText($"Login failed: {error}", Color.red);
            Debug.LogError($"LoginPanel: Login failed with error: {error}");
        }

        private void OnRegisterSuccess(LoginResponse response)
        {
            SetLoadingState(false);
            if (response.success)
            {
                SetStatusText("Registration successful! You can now log in.", Color.green);
                Debug.Log("LoginPanel: Registration successful");
            }
            else
            {
                SetStatusText($"Registration failed: {response.message}", Color.red);
            }
        }

        private void OnRegisterFailure(string error)
        {
            SetLoadingState(false);
            SetStatusText($"Registration failed: {error}", Color.red);
            Debug.LogError($"LoginPanel: Registration failed with error: {error}");
        }

        private void LoadCharacterSelectScene()
        {
            Debug.Log("LoginPanel: Loading Character Select scene...");
            GameFlowManager.Instance.LoadCharacterSelectScene();
        }

        private void SetLoadingState(bool isLoading)
        {
            if (loginButton != null)
                loginButton.interactable = !isLoading;
            if (registerButton != null)
                registerButton.interactable = !isLoading;
            if (usernameInput != null)
                usernameInput.interactable = !isLoading;
            if (passwordInput != null)
                passwordInput.interactable = !isLoading;
            if (loadingIndicator != null)
                loadingIndicator.SetActive(isLoading);
        }

        private void SetStatusText(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            else
            {
                Debug.LogError("LoginPanel: Status text is not assigned.");
            }
            Debug.Log($"LoginPanel: Status : '{message}'");
        }

        private void OnDestroy()
        {
            if (loginButton != null)
                loginButton.onClick.RemoveListener(OnLoginButtonClicked);
            if (registerButton != null)
                registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
            if (passwordInput != null)
                passwordInput.onSubmit.RemoveListener(OnPasswordSubmit);
            Debug.Log("LoginPanel: Event listeners removed.");
        }
    }
}