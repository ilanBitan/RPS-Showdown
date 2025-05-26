using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class AuthUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject signupPanel;
    public GameObject profilePanel;
    public GameObject loadingPanel;
    public GameObject resetPasswordPanel;  // New panel for password reset

    [Header("Login Fields")]
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;
    public Button forgotPasswordButton;  // New button for forgot password

    [Header("Signup Fields")]
    public TMP_InputField signUpEmail;
    public TMP_InputField signUpPassword;
    public TMP_InputField signUpConfirmPassword;
    public TMP_InputField signUpName;

    [Header("Reset Password Fields")]
    public TMP_InputField resetPasswordEmail;  // New field for reset password email
    public Button resetPasswordSubmitButton;   // Button to submit reset request
    public Button resetPasswordBackButton;     // Button to go back to login
    public TextMeshProUGUI resetPasswordErrorText;  // New error text field for reset password panel

    [Header("Profile Info")]
    public TextMeshProUGUI profileUserName_Text;
    public TextMeshProUGUI profileUserEmail_Text;
    public TextMeshProUGUI profileScore_Text;
    public TextMeshProUGUI profileWins_Text;
    public TextMeshProUGUI profileLosses_Text;
    public TextMeshProUGUI errorText;

    [Header("Buttons")]
    public Button refreshStatsButton;
    public Button mainMenuButton;

    // Services
    private FirebaseAuthService authService;
    private FirebaseDatabaseService dbService;

    // Ensure Firebase events only get handled once by tracking state
    private bool isProcessingAuthentication = false;
    private bool forceLoginScreen = false;

    private void Start()
    {
        // Initialize services
        authService = new FirebaseAuthService(FirebaseManager.Instance);
        dbService = new FirebaseDatabaseService(FirebaseManager.Instance);

        // Subscribe to events
        FirebaseManager.Instance.OnFirebaseInitialized += HandleFirebaseInitialized;
        authService.OnUserAuthenticated += HandleUserAuthenticated;
        authService.OnUserRegistered += HandleUserRegistered;
        authService.OnUserSignedOut += HandleUserSignedOut;
        dbService.OnUserDataUpdated += HandleUserDataUpdated;
        FirebaseManager.Instance.OnSceneChangeRequested += HandleSceneChangeRequested;

        // Show loading panel until Firebase is initialized
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Hide all other panels initially
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (signupPanel != null)
            signupPanel.SetActive(false);
        if (profilePanel != null)
            profilePanel.SetActive(false);
        if (resetPasswordPanel != null)
            resetPasswordPanel.SetActive(false);

        // Check if Firebase is already initialized
        if (FirebaseManager.Instance.IsInitialized)
        {
            UnityEngine.Debug.Log("Firebase already initialized on start");

            if (forceLoginScreen)
            {
                UnityEngine.Debug.Log("Forcing login screen display");
                OpenLoginPanel();
            }
            else if (FirebaseManager.Instance.IsUserSignedIn)
            {
                UnityEngine.Debug.Log("Start: User already signed in, showing profile");
                OpenProfilePanel();
            }
            else
            {
                UnityEngine.Debug.Log("Start: Firebase initialized but no user, showing login");
                OpenLoginPanel();
            }
        }
        else
        {
            UnityEngine.Debug.Log("Start: Waiting for Firebase initialization...");
        }

        if (refreshStatsButton != null)
        {
            refreshStatsButton.onClick.AddListener(OnRefreshStatsButtonClick);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClick);
        }

        if (forgotPasswordButton != null)
        {
            forgotPasswordButton.onClick.AddListener(OnForgotPasswordButtonClick);
        }

        if (resetPasswordSubmitButton != null)
        {
            resetPasswordSubmitButton.onClick.AddListener(OnResetPasswordButtonClick);
        }

        if (resetPasswordBackButton != null)
        {
            resetPasswordBackButton.onClick.AddListener(OpenLoginPanel);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized -= HandleFirebaseInitialized;
            FirebaseManager.Instance.OnSceneChangeRequested -= HandleSceneChangeRequested;
        }

        if (authService != null)
        {
            authService.OnUserAuthenticated -= HandleUserAuthenticated;
            authService.OnUserRegistered -= HandleUserRegistered;
            authService.OnUserSignedOut -= HandleUserSignedOut;
        }

        if (dbService != null)
        {
            dbService.OnUserDataUpdated -= HandleUserDataUpdated;
        }

        if (refreshStatsButton != null)
        {
            refreshStatsButton.onClick.RemoveListener(OnRefreshStatsButtonClick);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClick);
        }

        if (forgotPasswordButton != null)
        {
            forgotPasswordButton.onClick.RemoveListener(OnForgotPasswordButtonClick);
        }

        if (resetPasswordSubmitButton != null)
        {
            resetPasswordSubmitButton.onClick.RemoveListener(OnResetPasswordButtonClick);
        }

        if (resetPasswordBackButton != null)
        {
            resetPasswordBackButton.onClick.RemoveListener(OpenLoginPanel);
        }
    }

    #region Event Handlers

    private void HandleFirebaseInitialized(bool success, string message)
    {
        UnityEngine.Debug.Log($"Firebase initialized: {success}, Message: {message}");
        HideLoadingPanel();

        if (!success)
        {
            ShowErrorMessage("Firebase initialization failed: " + message);
            OpenLoginPanel();
        }
        else
        {
            if (forceLoginScreen)
            {
                OpenLoginPanel();
            }
            else if (FirebaseManager.Instance.IsUserSignedIn)
            {
                OpenProfilePanel();
            }
            else
            {
                OpenLoginPanel();
            }
        }
    }

    private void HandleUserAuthenticated(bool success, string message)
    {
        UnityEngine.Debug.Log($"Authentication event: {success}, Message: {message}");
        HideLoadingPanel();

        if (success)
        {
            ShowNotificationMessage("Success", "Login successful!");
            // Request scene change to MainMenuScene after successful login
            FirebaseManager.Instance.RequestSceneChange("MainMenuScene");
        }
        else
        {
            // Display specific error message based on error type
            string errorMessage = message;
            if (message.Contains("Invalid email"))
            {
                errorMessage = "Invalid email address";
            }
            else if (message.Contains("Invalid password"))
            {
                errorMessage = "Incorrect password";
            }
            else if (message.Contains("User not found"))
            {
                errorMessage = "User not found in the system";
            }
            else if (message.Contains("Too many attempts"))
            {
                errorMessage = "Too many login attempts. Please try again later";
            }

            ShowErrorMessage(errorMessage);
        }
    }

    private void HandleSceneChangeRequested(string sceneName)
    {
        UnityEngine.Debug.Log($"Scene change requested to: {sceneName}");
        ShowLoadingPanel();
        StartCoroutine(LoadSceneWithDelay(sceneName, 0.5f));
    }

    private void HandleUserRegistered(bool success, string message)
    {
        UnityEngine.Debug.Log($"Registration event: {success}, Message: {message}");
        HideLoadingPanel();

        if (success)
        {
            ShowNotificationMessage("Success", "Account created successfully!");
            OpenLoginPanel();
        }
        else
        {
            ShowErrorMessage(message);
        }
    }

    private void HandleUserSignedOut()
    {
        UnityEngine.Debug.Log("User signed out event received");
        ClearProfileInfo();
        OpenLoginPanel();
    }

    private void HandleUserDataUpdated(UserData userData)
    {
        if (userData != null)
        {
            UpdateProfileInfo(userData);
        }
    }

    #endregion

    #region UI Methods

    public void OpenResetPasswordPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        resetPasswordPanel.SetActive(true);
        loadingPanel?.SetActive(false);
        ClearErrorMessage();
        if (resetPasswordErrorText != null)
        {
            resetPasswordErrorText.text = "";
            resetPasswordErrorText.gameObject.SetActive(false);
        }
    }

    public void OpenLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        resetPasswordPanel.SetActive(false);
        loadingPanel?.SetActive(false);
        ClearErrorMessage();
    }

    public void OpenSignUpPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        profilePanel.SetActive(false);
        loadingPanel?.SetActive(false);
        ClearErrorMessage();
    }

    public void OpenProfilePanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(true);
        loadingPanel?.SetActive(false);
        UpdateProfileInfo();
    }

    private void UpdateProfileInfo(UserData userData = null)
    {
        if (userData != null)
        {
            UnityEngine.Debug.Log($"Updating profile info for user: {userData.displayName}, Email: {userData.email}, Score: {userData.score}, Wins: {userData.wins}, Losses: {userData.losses}");

            if (profileUserName_Text != null)
                profileUserName_Text.text = userData.displayName;
            if (profileUserEmail_Text != null)
                profileUserEmail_Text.text = userData.email;
            if (profileScore_Text != null)
                profileScore_Text.text = userData.score.ToString();
            if (profileWins_Text != null)
                profileWins_Text.text = userData.wins.ToString();
            if (profileLosses_Text != null)
                profileLosses_Text.text = userData.losses.ToString();
        }
        else
        {
            UnityEngine.Debug.Log("No user data provided, fetching from database...");
            dbService.GetUserStats((data) => {
                if (data == null)
                {
                    UnityEngine.Debug.LogError("Failed to get user stats from database");
                    return;
                }
                UpdateProfileInfo(data);
            });
        }
    }

    #endregion

    #region Button Handlers

    public void OnMainMenuButtonClick()
    {
        FirebaseManager.Instance.RequestSceneChange("MainMenuScene");
    }

    public void OnLoginButtonClick()
    {
        if (isProcessingAuthentication) return;

        if (string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            ShowErrorMessage("Please enter email and password");
            return;
        }

        isProcessingAuthentication = true;
        ShowLoadingPanel();

        authService.SignInWithEmailPassword(loginEmail.text, loginPassword.text, (success, message) => {
            isProcessingAuthentication = false;
            if (success)
            {
                // Request scene change to MainMenuScene after successful login
                FirebaseManager.Instance.RequestSceneChange("MainMenuScene");

                // OpenProfilePanel();
            }
        });
    }

    public void OnSignUpButtonClick()
    {
        if (isProcessingAuthentication) return;

        if (string.IsNullOrEmpty(signUpEmail.text) || string.IsNullOrEmpty(signUpPassword.text) ||
            string.IsNullOrEmpty(signUpName.text))
        {
            ShowErrorMessage("Please fill all fields");
            return;
        }

        if (signUpPassword.text != signUpConfirmPassword.text)
        {
            ShowErrorMessage("Passwords do not match");
            return;
        }

        if (signUpPassword.text.Length < 6)
        {
            ShowErrorMessage("Password must be at least 6 characters");
            return;
        }

        isProcessingAuthentication = true;
        ShowLoadingPanel();

        authService.CreateUserWithEmailPassword(signUpEmail.text, signUpPassword.text, signUpName.text,
            (success, message) => {
                isProcessingAuthentication = false;
                if (success)
                {
                    // Initialize user data in database
                    dbService.InitializeUserData(FirebaseManager.Instance.CurrentUser.UserId, signUpName.text);

                    // Wait a moment for the data to be saved
                    StartCoroutine(WaitForUserData());
                }
                else
                {
                    ShowErrorMessage(message);
                }
            });
    }

    private IEnumerator WaitForUserData()
    {
        yield return new WaitForSeconds(1f); // Wait for 1 second
        OpenLoginPanel();
    }

    public void OnLogOutButtonClick()
    {
        authService.SignOut();
    }

    public void OnRefreshStatsButtonClick()
    {
        UpdateProfileInfo();
    }

    public void OnForgotPasswordButtonClick()
    {
        OpenResetPasswordPanel();
    }

    public void OnResetPasswordButtonClick()
    {
        if (string.IsNullOrEmpty(resetPasswordEmail.text))
        {
            ShowErrorMessage("Please enter your email address");
            return;
        }

        ShowLoadingPanel();
        authService.ResetPassword(resetPasswordEmail.text, (success, message) => {
            if (success)
            {
                HideLoadingPanel();
                ShowErrorMessage("Check your mail inbox for password reset instructions");
                StartCoroutine(WaitAndReturnToLogin(2f));
            }
            else
            {
                HideLoadingPanel();
                if (message.Contains("No account found"))
                {
                    ShowErrorMessage("This email is not registered in our system");
                }
                else
                {
                    ShowErrorMessage(message);
                }
            }
        });
    }

    private IEnumerator WaitAndReturnToLogin(float delay)
    {
        yield return new WaitForSeconds(delay);
        OpenLoginPanel();
    }

    #endregion

    #region Helper Methods

    private void ClearProfileInfo()
    {
        if (profileUserName_Text != null)
            profileUserName_Text.text = "";
        if (profileUserEmail_Text != null)
            profileUserEmail_Text.text = "";
        if (profileScore_Text != null)
            profileScore_Text.text = "0";
        if (profileWins_Text != null)
            profileWins_Text.text = "0";
        if (profileLosses_Text != null)
            profileLosses_Text.text = "0";
        ClearErrorMessage();
    }

    private void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        if (resetPasswordErrorText != null && resetPasswordPanel.activeSelf)
        {
            resetPasswordErrorText.text = message;
            resetPasswordErrorText.gameObject.SetActive(true);
        }
    }

    private void ClearErrorMessage()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
        if (resetPasswordErrorText != null)
        {
            resetPasswordErrorText.text = "";
            resetPasswordErrorText.gameObject.SetActive(false);
        }
    }

    private void ShowNotificationMessage(string title, string message)
    {
        // Implement your notification system here
        UnityEngine.Debug.Log($"{title}: {message}");
    }

    private void ShowLoadingPanel()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    #endregion
}