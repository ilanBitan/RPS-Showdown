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

    [Header("Login Fields")]
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;

    [Header("Signup Fields")]
    public TMP_InputField signUpEmail;
    public TMP_InputField signUpPassword;
    public TMP_InputField signUpConfirmPassword;
    public TMP_InputField signUpName;

    [Header("Profile Info")]
    public TextMeshProUGUI profileUserName_Text;
    public TextMeshProUGUI profileUserEmail_Text;
    public TextMeshProUGUI profileScore_Text;
    public TextMeshProUGUI profileWins_Text;
    public TextMeshProUGUI profileLosses_Text;
    public TextMeshProUGUI errorText;

    [Header("Buttons")]
    public Button refreshStatsButton;

    // Ensure Firebase events only get handled once by tracking state
    private bool isProcessingAuthentication = false;

    // הוסף משתנה להצגת מסך ההתחברות בהתחלה
    private bool forceLoginScreen = true;

    private void Start()
    {
        // Subscribe to Firebase events
        FirebaseManager.Instance.OnFirebaseInitialized += HandleFirebaseInitialized;
        FirebaseManager.Instance.OnUserAuthenticated += HandleUserAuthenticated;
        FirebaseManager.Instance.OnUserRegistered += HandleUserRegistered;
        FirebaseManager.Instance.OnUserSignedOut += HandleUserSignedOut;
        FirebaseManager.Instance.OnUserDataUpdated += HandleUserDataUpdated;

        // הוספת הרשמה לאירוע מעבר סצנה
        if (FirebaseManager.Instance != null)
        {
            // נרשמים לאירוע מעבר סצנה
            FirebaseManager.Instance.OnSceneChangeRequested += HandleSceneChangeRequested;
        }

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

        // Check if Firebase is already initialized
        if (FirebaseManager.Instance.IsInitialized)
        {
            UnityEngine.Debug.Log("Firebase already initialized on start");

            // גם אם המשתמש מחובר, אם הדגל מופעל אנחנו נראה את מסך ההתחברות
            if (forceLoginScreen)
            {
                UnityEngine.Debug.Log("Forcing login screen display");
                OpenLoginPanel();
            }
            // אחרת, אם המשתמש מחובר אנחנו נציג את מסך הפרופיל
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
            // Keep loading panel active until Firebase initializes
        }

        if (refreshStatsButton != null)
        {
            refreshStatsButton.onClick.AddListener(OnRefreshStatsButtonClick);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnFirebaseInitialized -= HandleFirebaseInitialized;
            FirebaseManager.Instance.OnUserAuthenticated -= HandleUserAuthenticated;
            FirebaseManager.Instance.OnUserRegistered -= HandleUserRegistered;
            FirebaseManager.Instance.OnUserSignedOut -= HandleUserSignedOut;
            FirebaseManager.Instance.OnUserDataUpdated -= HandleUserDataUpdated;

            // ביטול הרשמה לאירוע מעבר סצנה
            FirebaseManager.Instance.OnSceneChangeRequested -= HandleSceneChangeRequested;
        }

        if (refreshStatsButton != null)
        {
            refreshStatsButton.onClick.RemoveListener(OnRefreshStatsButtonClick);
        }
    }

    #region Event Handlers

    private void HandleFirebaseInitialized(bool success, string message)
    {
        UnityEngine.Debug.Log($"Firebase initialized: {success}, Message: {message}");

        // Always hide loading panel when Firebase initialization is complete
        HideLoadingPanel();

        if (!success)
        {
            ShowErrorMessage("Firebase initialization failed: " + message);
            OpenLoginPanel(); // Always return to login panel on failure
        }
        else
        {
            // גם אם המשתמש מחובר, אם הדגל מופעל אנחנו נראה את מסך ההתחברות
            if (forceLoginScreen)
            {
                UnityEngine.Debug.Log("Firebase Init: Forcing login screen display");
                OpenLoginPanel();
            }
            // אחרת, אם המשתמש מחובר אנחנו נציג את מסך הפרופיל
            else if (FirebaseManager.Instance.IsUserSignedIn)
            {
                UnityEngine.Debug.Log("Firebase Init: User already signed in, showing profile");
                OpenProfilePanel();
            }
            else
            {
                UnityEngine.Debug.Log("Firebase Init: No user signed in, showing login");
                OpenLoginPanel();
            }
        }
    }

    private void HandleUserAuthenticated(bool success, string message)
    {
        UnityEngine.Debug.Log($"Authentication event: {success}, Message: {message}");

        // תמיד להסתיר את מסך הטעינה אם נקבל תשובה מהשרת (הצלחה או כישלון)
        HideLoadingPanel();

        if (success)
        {
            UnityEngine.Debug.Log("Authentication successful, updating profile");

            // Force all other panels to close before opening profile panel
            if (loginPanel != null)
                loginPanel.SetActive(false);
            if (signupPanel != null)
                signupPanel.SetActive(false);

            // הקוד הבא לא צריך להתבצע כאן כי השרת יבקש מעבר לסצנה אחרת
            // OpenProfilePanel();
            ShowNotificationMessage("Success", "Login successful!");

            // Firebase Manager כבר מטפל במעבר הסצנה
        }
        else
        {
            ShowErrorMessage(message);
        }
    }

    // טיפול באירוע מעבר סצנה
    private void HandleSceneChangeRequested(string sceneName)
    {
        UnityEngine.Debug.Log($"Scene change requested to: {sceneName}");

        // הפעלת מסך הטעינה לפני מעבר לסצנה אחרת
        ShowLoadingPanel();

        // מעבר לסצנה המבוקשת עם השהייה קצרה
        StartCoroutine(LoadSceneWithDelay(sceneName, 0.5f));
    }

    private void HandleUserRegistered(bool success, string message)
    {
        UnityEngine.Debug.Log($"Registration event: {success}, Message: {message}");

        // Always hide loading panel
        HideLoadingPanel();

        if (success)
        {
            UnityEngine.Debug.Log("Registration successful, updating profile");

            // Force all other panels to close before opening profile panel
            if (loginPanel != null)
                loginPanel.SetActive(false);
            if (signupPanel != null)
                signupPanel.SetActive(false);

            // Ensure profile panel is activated
            OpenProfilePanel();
            ShowNotificationMessage("Success", "Account created successfully!");
        }
        else
        {
            ShowErrorMessage(message);
        }
    }

    private void HandleUserSignedOut()
    {
        UnityEngine.Debug.Log("User signed out event received");

        // Clear profile info
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

        // Force to show login panel
        OpenLoginPanel();

        UnityEngine.Debug.Log("Login panel should now be visible after logout");
    }

    private void HandleUserDataUpdated(UserData userData)
    {
        UnityEngine.Debug.Log($"Real-time user data updated: Score={userData.score}, Wins={userData.wins}, Losses={userData.losses}");

        if (profileScore_Text != null)
            profileScore_Text.text = userData.score.ToString();

        if (profileWins_Text != null)
            profileWins_Text.text = userData.wins.ToString();

        if (profileLosses_Text != null)
            profileLosses_Text.text = userData.losses.ToString();

        if (!string.IsNullOrEmpty(userData.displayName) && profileUserName_Text != null)
            profileUserName_Text.text = userData.displayName;
    }

    #endregion

    #region UI Methods

    public void OpenLoginPanel()
    {
        UnityEngine.Debug.Log("Opening login panel");

        // First hide all panels
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (signupPanel != null)
            signupPanel.SetActive(false);
        if (profilePanel != null)
            profilePanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Then show only login panel
        if (loginPanel != null)
            loginPanel.SetActive(true);

        // Clear error message
        ClearErrorMessage();
    }

    public void OpenSignUpPanel()
    {
        UnityEngine.Debug.Log("Opening signup panel");

        // First hide all panels
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (signupPanel != null)
            signupPanel.SetActive(false);
        if (profilePanel != null)
            profilePanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Then show only signup panel
        if (signupPanel != null)
            signupPanel.SetActive(true);

        // Clear error message
        ClearErrorMessage();
    }

    public void OpenProfilePanel()
    {
        UnityEngine.Debug.Log("Opening profile panel");

        // First hide all panels
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (signupPanel != null)
            signupPanel.SetActive(false);
        if (profilePanel != null)
            profilePanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Then show only profile panel
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
            UnityEngine.Debug.Log("Profile panel activated");

            UpdateProfileInfo();
        }
        else
        {
            UnityEngine.Debug.LogError("Profile panel reference is null!");
        }
    }

    private void UpdateProfileInfo()
    {
        UnityEngine.Debug.Log("Updating profile information");

        if (FirebaseManager.Instance.IsUserSignedIn)
        {
            // עדכון מידע בסיסי מ-auth
            if (FirebaseManager.Instance.CurrentUser != null)
            {
                // עדכון שם תצוגה ואימייל
                if (profileUserName_Text != null)
                {
                    string displayName = FirebaseManager.Instance.CurrentUser.DisplayName;
                    profileUserName_Text.text = !string.IsNullOrEmpty(displayName) ? displayName : "User";
                    UnityEngine.Debug.Log($"Display name updated: {displayName}");
                }

                if (profileUserEmail_Text != null)
                {
                    string email = FirebaseManager.Instance.CurrentUser.Email;
                    profileUserEmail_Text.text = !string.IsNullOrEmpty(email) ? email : "No email provided";
                    UnityEngine.Debug.Log($"Email updated: {email}");
                }
            }

            // כפיית רענון נתונים מהשרת
            FirebaseManager.Instance.ForceRefreshUserStats();
        }
        else
        {
            UnityEngine.Debug.LogError("Cannot update profile - User not signed in");
        }
    }

    public void OnLoginButtonClick()
    {
        // Validate input
        if (string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            ShowErrorMessage("Please enter email and password");
            return;
        }

        // Clear any previous error messages
        ClearErrorMessage();

        // Show loading panel
        ShowLoadingPanel();

        UnityEngine.Debug.Log("Attempting login with email: " + loginEmail.text);

        // Call Firebase manager to handle login with explicit callback to handle navigation immediately
        FirebaseManager.Instance.SignInWithEmailPassword(loginEmail.text, loginPassword.text,
            (success, message) => {
                if (success)
                {
                    UnityEngine.Debug.Log("Direct callback: Login successful");
                    // מסך הטעינה יישאר מוצג עד למעבר לסצנה הבאה
                    // FirebaseManager יטפל במעבר לסצנה הבאה
                }
                else
                {
                    UnityEngine.Debug.LogError("Direct callback: Login failed - " + message);
                    HideLoadingPanel();
                    ShowErrorMessage(message);
                }
            });
    }

    public void OnSignUpButtonClick()
    {
        // Validate input
        if (string.IsNullOrEmpty(signUpEmail.text) || string.IsNullOrEmpty(signUpPassword.text) ||
            string.IsNullOrEmpty(signUpName.text) || string.IsNullOrEmpty(signUpConfirmPassword.text))
        {
            ShowErrorMessage("Please fill all fields");
            return;
        }

        // Check password length
        if (signUpPassword.text.Length < 6)
        {
            ShowErrorMessage("Password must be at least 6 characters");
            return;
        }

        // בדיקה שהסיסמאות זהות
        if (signUpPassword.text != signUpConfirmPassword.text)
        {
            ShowErrorMessage("Passwords do not match");
            return;
        }

        // Clear any previous error messages
        ClearErrorMessage();

        // Show loading panel
        ShowLoadingPanel();

        UnityEngine.Debug.Log("Attempting registration with email: " + signUpEmail.text + " and name: " + signUpName.text);

        // Call Firebase manager to handle registration with explicit callback to handle navigation immediately
        FirebaseManager.Instance.CreateUserWithEmailPassword(signUpEmail.text, signUpPassword.text, signUpName.text,
            (success, message) => {
                if (success)
                {
                    UnityEngine.Debug.Log("Direct callback: Registration successful");

                    // In case the event system fails, also handle UI changes here
                    HideLoadingPanel();
                    OpenProfilePanel();
                }
                else
                {
                    UnityEngine.Debug.LogError("Direct callback: Registration failed - " + message);
                    HideLoadingPanel();
                    ShowErrorMessage(message);
                }
            });
    }

    public void OnLogOutButtonClick()
    {
        UnityEngine.Debug.Log("Logging out user");

        // Directly handle UI changes for logout here
        // Clear profile info
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

        // Call Firebase signout
        FirebaseManager.Instance.SignOut();

        // Force UI to show login screen immediately
        UnityEngine.Debug.Log("Forcing login panel after logout");
        OpenLoginPanel();
    }

    public void OnRefreshStatsButtonClick()
    {
        UnityEngine.Debug.Log("Manual refresh of user stats requested");
        if (FirebaseManager.Instance.IsUserSignedIn)
        {
            ShowLoadingPanel();
            FirebaseManager.Instance.ForceRefreshUserStats();
            StartCoroutine(HideLoadingAfterDelay(0.5f));
        }
    }

    public void UpdatePlayerScore(int newScore)
    {
        if (FirebaseManager.Instance.IsUserSignedIn)
        {
            FirebaseManager.Instance.UpdateUserScore(newScore);
        }
    }

    public void IncrementPlayerScore(int amount = 1)
    {
        if (FirebaseManager.Instance.IsUserSignedIn && profileScore_Text != null)
        {
            int currentScore = int.Parse(profileScore_Text.text);
            int newScore = currentScore + amount;
            FirebaseManager.Instance.UpdateUserScore(newScore);
        }
    }

    public void AddPlayerWin()
    {
        if (FirebaseManager.Instance.IsUserSignedIn)
        {
            FirebaseManager.Instance.IncrementUserWins();
        }
    }

    public void AddPlayerLoss()
    {
        if (FirebaseManager.Instance.IsUserSignedIn)
        {
            FirebaseManager.Instance.IncrementUserLosses();
        }
    }

    #endregion

    #region Helper Methods

    private void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        UnityEngine.Debug.LogError(message);
    }

    private void ClearErrorMessage()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }

    private void ShowNotificationMessage(string title, string message)
    {
        UnityEngine.Debug.Log($"{title}: {message}");

        if (errorText != null)
        {
            errorText.text = $"{title}: {message}";
            errorText.gameObject.SetActive(true);
        }
    }

    private void ShowLoadingPanel()
    {
        if (loadingPanel != null)
        {
            UnityEngine.Debug.Log("Showing loading panel");
            loadingPanel.SetActive(true);
        }
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
        {
            UnityEngine.Debug.Log("Hiding loading panel");
            loadingPanel.SetActive(false);
        }
    }

    private IEnumerator HideLoadingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoadingPanel();
    }

    // קורוטינה חדשה למעבר לסצנה עם השהייה ומסך טעינה
    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        // הפעלת מסך הטעינה
        ShowLoadingPanel();

        // המתנה לפרק זמן מוגדר
        yield return new WaitForSeconds(delay);

        // הפעלת מעבר לסצנה החדשה
        SceneManager.LoadScene(sceneName);
    }

    #endregion
}