using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;

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
    public TMP_InputField signUpName;

    [Header("Profile Info")]
    public TextMeshProUGUI profileUserName_Text;
    public TextMeshProUGUI profileUserEmail_Text;
    public TextMeshProUGUI errorText;

    private void Start()
    {
        // Subscribe to Firebase events
        FirebaseManager.Instance.OnFirebaseInitialized += HandleFirebaseInitialized;
        FirebaseManager.Instance.OnUserAuthenticated += HandleUserAuthenticated;
        FirebaseManager.Instance.OnUserRegistered += HandleUserRegistered;
        FirebaseManager.Instance.OnUserSignedOut += HandleUserSignedOut;

        // Show loading panel until Firebase is initialized
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // If Firebase is already initialized and user is signed in, show profile
        if (FirebaseManager.Instance.IsInitialized && FirebaseManager.Instance.IsUserSignedIn)
        {
            UpdateProfileInfo();
            OpenProfilePanel();
        }
        else
        {
            OpenLoginPanel();
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
        }
    }

    #region Event Handlers

    private void HandleFirebaseInitialized(bool success, string message)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (!success)
        {
            ShowErrorMessage(message);
        }
    }

    private void HandleUserAuthenticated(bool success, string message)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (success)
        {
            UpdateProfileInfo();
            OpenProfilePanel();
        }
        else
        {
            ShowErrorMessage(message);
        }
    }

    private void HandleUserRegistered(bool success, string message)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (success)
        {
            UpdateProfileInfo();
            OpenProfilePanel();
            ShowNotificationMessage("Success", message);
        }
        else
        {
            ShowErrorMessage(message);
        }
    }

    private void HandleUserSignedOut()
    {
        // Clear profile info
        if (profileUserName_Text != null)
            profileUserName_Text.text = "";
        if (profileUserEmail_Text != null)
            profileUserEmail_Text.text = "";

        OpenLoginPanel();
    }

    #endregion

    #region UI Methods

    public void OpenLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        loadingPanel?.SetActive(false);

        // Clear error message
        ClearErrorMessage();
    }

    public void OpenSignUpPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        profilePanel.SetActive(false);
        loadingPanel?.SetActive(false);

        // Clear error message
        ClearErrorMessage();
    }

    public void OpenProfilePanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(true);
        loadingPanel?.SetActive(false);
    }

    private void UpdateProfileInfo()
    {
        if (FirebaseManager.Instance.CurrentUser != null)
        {
            profileUserName_Text.text = FirebaseManager.Instance.CurrentUser.DisplayName;
            profileUserEmail_Text.text = FirebaseManager.Instance.CurrentUser.Email;
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

        // Show loading panel
        loadingPanel?.SetActive(true);

        // Call Firebase manager to handle login
        FirebaseManager.Instance.SignInWithEmailPassword(loginEmail.text, loginPassword.text,
            (success, message) => {
                // Feedback is handled by event handlers
            });
    }

    public void OnSignUpButtonClick()
    {
        // Validate input
        if (string.IsNullOrEmpty(signUpEmail.text) || string.IsNullOrEmpty(signUpPassword.text) || string.IsNullOrEmpty(signUpName.text))
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

        // Show loading panel
        loadingPanel?.SetActive(true);

        // Call Firebase manager to handle registration
        FirebaseManager.Instance.CreateUserWithEmailPassword(signUpEmail.text, signUpPassword.text, signUpName.text,
            (success, message) => {
                // Feedback is handled by event handlers
            });
    }

    public void OnLogOutButtonClick()
    {
        FirebaseManager.Instance.SignOut();
    }

    #endregion

    #region Helper Methods

    private void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
        UnityEngine.Debug.LogError(message);
    }

    private void ClearErrorMessage()
    {
        if (errorText != null)
        {
            errorText.text = "";
        }
    }

    private void ShowNotificationMessage(string title, string message)
    {
        UnityEngine.Debug.Log($"{title}: {message}");

        // Example of how to show a notification
        // UIManager.Instance.ShowNotification(title, message);

        // For now, just update the error text
        if (errorText != null)
        {
            errorText.text = $"{title}: {message}";
        }
    }

    #endregion
}