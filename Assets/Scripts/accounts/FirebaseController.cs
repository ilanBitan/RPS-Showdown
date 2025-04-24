using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using Firebase;
using Firebase.Auth;
using System;
using System.Threading.Tasks;
using Firebase.Extensions;

public class FirebaseController : MonoBehaviour
{
    public GameObject loginPanel, signupPanel, profilePanel, loadingPanel;
    public TMP_InputField loginEmail, loginPassword, signUpEmail, signUpPassword, signUpName;
    public TextMeshProUGUI profileUserName_Text, profileUserEmail_Text, errorText;

    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;

    bool isSignIn = false;
    private bool isFirebaseInitialized = false;

    public void OpenLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        loadingPanel?.SetActive(false);

        // Clear error message if exists
        if (errorText != null)
            errorText.text = "";
    }

    public void OpenSignUpPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        profilePanel.SetActive(false);
        loadingPanel?.SetActive(false);

        // Clear error message if exists
        if (errorText != null)
            errorText.text = "";
    }

    public void OpenProfilePanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(true);
        loadingPanel?.SetActive(false);
    }

    public void LoginUser()
    {
        // Check if Firebase is initialized before attempting login
        if (!isFirebaseInitialized)
        {
            showNotificationMessage("Error", "Firebase is still initializing. Please wait.");
            return;
        }

        UnityEngine.Debug.Log($"Attempting login with email: {loginEmail.text}");

        // Check if any field is empty
        if (string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            showNotificationMessage("Error", "Please enter email and password");
            return;
        }

        // Show loading panel if it exists
        loadingPanel?.SetActive(true);

        SignInUser(loginEmail.text, loginPassword.text);
    }

    public void SignUpUser()
    {
        // Check if Firebase is initialized before attempting signup
        if (!isFirebaseInitialized)
        {
            showNotificationMessage("Error", "Firebase is still initializing. Please wait.");
            return;
        }

        UnityEngine.Debug.Log($"Attempting signup with email: {signUpEmail.text}");

        // Check if any field is empty
        if (string.IsNullOrEmpty(signUpEmail.text) || string.IsNullOrEmpty(signUpPassword.text) || string.IsNullOrEmpty(signUpName.text))
        {
            showNotificationMessage("Error", "Please fill all fields");
            return;
        }

        // Check password length - Firebase requires minimum 6 characters
        if (signUpPassword.text.Length < 6)
        {
            showNotificationMessage("Error", "Password must be at least 6 characters");
            return;
        }

        // Show loading panel if it exists
        loadingPanel?.SetActive(true);

        CreateUser(signUpEmail.text, signUpPassword.text, signUpName.text);
    }

    public void LogOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            profileUserEmail_Text.text = "";
            profileUserName_Text.text = "";
            isSignIn = false;
            isSigned = false;
            OpenLoginPanel();
        }
    }

    void CreateUser(string email, string password, string Username)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            // Hide loading panel if it exists
            loadingPanel?.SetActive(false);

            if (task.IsCanceled)
            {
                UnityEngine.Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                showNotificationMessage("Error", "Request was canceled");
                return;
            }
            if (task.IsFaulted)
            {
                HandleFirebaseError(task.Exception, "Signup Failed");
                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            UnityEngine.Debug.LogFormat("User created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            UpdateUserProfile(Username);
        });
    }

    public void SignInUser(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            // Hide loading panel if it exists
            loadingPanel?.SetActive(false);

            if (task.IsCanceled)
            {
                UnityEngine.Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                showNotificationMessage("Error", "Request was canceled");
                return;
            }
            if (task.IsFaulted)
            {
                HandleFirebaseError(task.Exception, "Login Failed");
                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            UnityEngine.Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            profileUserName_Text.text = "" + result.User.DisplayName;
            profileUserEmail_Text.text = "" + result.User.Email;

            OpenProfilePanel();
        });
    }

    // Handle Firebase error messages with detailed logs
    void HandleFirebaseError(Exception exception, string defaultMessage)
    {
        string errorMessage = defaultMessage;

        if (exception != null)
        {
            // Check if this is an AggregateException (which has InnerExceptions)
            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    if (innerException is Firebase.FirebaseException firebaseEx)
                    {
                        UnityEngine.Debug.LogError($"Firebase error code: {firebaseEx.ErrorCode}, Message: {firebaseEx.Message}");

                        // Provide more meaningful errors based on common codes
                        switch (firebaseEx.ErrorCode)
                        {
                            case -6: // ERROR_WEAK_PASSWORD
                                errorMessage = "Password is too weak";
                                break;
                            case -5: // ERROR_ACCOUNT_EXISTS_WITH_DIFFERENT_CREDENTIAL
                                errorMessage = "An account already exists with this email";
                                break;
                            case -17: // ERROR_USER_NOT_FOUND
                                errorMessage = "User not found";
                                break;
                            case -16: // ERROR_INVALID_EMAIL
                                errorMessage = "Invalid email format";
                                break;
                            case -13: // ERROR_WRONG_PASSWORD
                                errorMessage = "Incorrect password";
                                break;
                            default:
                                errorMessage = $"{defaultMessage}: {firebaseEx.Message}";
                                break;
                        }
                    }
                }
            }
            // Check if this is a FirebaseException directly
            else if (exception is Firebase.FirebaseException firebaseEx)
            {
                UnityEngine.Debug.LogError($"Firebase error code: {firebaseEx.ErrorCode}, Message: {firebaseEx.Message}");

                // Same switch case as above
                switch (firebaseEx.ErrorCode)
                {
                    case -6: // ERROR_WEAK_PASSWORD
                        errorMessage = "Password is too weak";
                        break;
                    case -5: // ERROR_ACCOUNT_EXISTS_WITH_DIFFERENT_CREDENTIAL
                        errorMessage = "An account already exists with this email";
                        break;
                    case -17: // ERROR_USER_NOT_FOUND
                        errorMessage = "User not found";
                        break;
                    case -16: // ERROR_INVALID_EMAIL
                        errorMessage = "Invalid email format";
                        break;
                    case -13: // ERROR_WRONG_PASSWORD
                        errorMessage = "Incorrect password";
                        break;
                    default:
                        errorMessage = $"{defaultMessage}: {firebaseEx.Message}";
                        break;
                }
            }
            // Handle other exception types or just log the message
            else
            {
                errorMessage = $"{defaultMessage}: {exception.Message}";
            }
        }

        UnityEngine.Debug.LogError($"Firebase operation failed: {errorMessage}");
        showNotificationMessage("Error", errorMessage);

        // Update UI error text if available
        if (errorText != null)
        {
            errorText.text = errorMessage;
        }
    }

    void InitializeFirebase()
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
        isFirebaseInitialized = true;
        UnityEngine.Debug.Log("Firebase Auth initialized successfully");
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();

            if (!signedIn && user != null)
            {
                UnityEngine.Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                UnityEngine.Debug.Log("Signed in " + user.UserId);
                isSignIn = true;
            }
        }
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }

    void UpdateUserProfile(string UserName)
    {
        Firebase.Auth.FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
            {
                DisplayName = UserName,
                PhotoUrl = new System.Uri("https://placehold.co/600x400"),
            };

            user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task => {
                if (task.IsCanceled)
                {
                    UnityEngine.Debug.LogError("UpdateUserProfileAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
                    HandleFirebaseError(task.Exception, "Profile update failed");
                    return;
                }

                UnityEngine.Debug.Log("User profile updated successfully.");
                showNotificationMessage("Success", "Account Successfully created");

                // Update UI with new user info
                profileUserName_Text.text = user.DisplayName;
                profileUserEmail_Text.text = user.Email;

                // Show profile panel after successful account creation
                OpenProfilePanel();
            });
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Check for existing auth sessions first
        if (Firebase.Auth.FirebaseAuth.DefaultInstance != null &&
            Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            UnityEngine.Debug.Log("User already logged in");
            // Initialize anyway to set up event handlers
            InitializeFirebase();
        }

        // Initialize Firebase
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp
                InitializeFirebase();
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                showNotificationMessage("Error", "Failed to initialize Firebase. Please restart the app.");
            }
        });
    }

    bool isSigned = false;

    // Update is called once per frame
    void Update()
    {
        // Handle auto-login when user is signed in
        if (isSignIn)
        {
            if (!isSigned)
            {
                isSigned = true;
                if (user != null)
                {
                    profileUserName_Text.text = "" + user.DisplayName;
                    profileUserEmail_Text.text = "" + user.Email;
                    OpenProfilePanel();
                }
            }
        }
    }

    // Display notification message to user
    void showNotificationMessage(string title, string message)
    {
        // You can implement a proper UI notification system here
        // For now, just log to console
        UnityEngine.Debug.Log($"{title}: {message}");

        // TODO: Replace with your UI notification system, like:
        // UIManager.Instance.ShowNotification(title, message);
    }
}