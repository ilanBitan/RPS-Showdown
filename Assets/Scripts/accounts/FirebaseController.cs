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
using Firebase.Database;

public class FirebaseController : MonoBehaviour
{
    public GameObject loginPanel, signupPanel, profilePanel, loadingPanel;
    public TMP_InputField loginEmail, loginPassword, signUpEmail, signUpPassword, signUpName;
    public TextMeshProUGUI profileUserName_Text, profileUserEmail_Text, errorText;

    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;

    bool isSignIn = false;
    private bool isFirebaseInitialized = false;
    private bool isInitializing = false;
    private DatabaseReference databaseReference;

    // Queue to store pending operations
    private Queue<Action> pendingOperations = new Queue<Action>();

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
        // Show loading panel immediately to give feedback
        loadingPanel?.SetActive(true);

        // Check if any field is empty
        if (string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            loadingPanel?.SetActive(false);
            showNotificationMessage("Error", "Please enter email and password");
            return;
        }

        UnityEngine.Debug.Log($"Attempting login with email: {loginEmail.text}");

        // Store login credentials for later use if needed
        string email = loginEmail.text;
        string password = loginPassword.text;

        // If Firebase is initialized, login directly
        if (isFirebaseInitialized)
        {
            SignInUser(email, password);
        }
        // If Firebase is being initialized, queue the login operation
        else if (isInitializing)
        {
            UnityEngine.Debug.Log("Firebase is initializing. Queuing login operation.");
            pendingOperations.Enqueue(() => SignInUser(email, password));
        }
        // If Firebase isn't initialized or initializing, start initialization and queue login
        else
        {
            UnityEngine.Debug.Log("Firebase not initialized. Starting initialization and queuing login.");
            pendingOperations.Enqueue(() => SignInUser(email, password));
            InitializeFirebaseWithRetry();
        }
    }

    public void SignUpUser()
    {
        // Check if Firebase is still initializing
        if (isInitializing)
        {
            showNotificationMessage("Please wait", "Firebase is still initializing...");
            return;
        }

        // Check if Firebase is initialized before attempting signup
        if (!isFirebaseInitialized)
        {
            showNotificationMessage("Error", "Firebase is not initialized. Trying to reconnect...");
            InitializeFirebaseWithRetry();
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
        if (!isFirebaseInitialized)
        {
            UnityEngine.Debug.LogError("Attempted to sign in before Firebase was initialized");
            loadingPanel?.SetActive(false);
            showNotificationMessage("Error", "Firebase is not ready yet. Please try again.");
            return;
        }

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

            user = result.User;
            isSignIn = true;
            isSigned = true;

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

    // Improved initialization with error handling
    void InitializeFirebase()
    {
        isInitializing = true;
        try
        {
            auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            if (auth != null)
            {
                auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);
                // Initialize Database reference
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseInitialized = true;
                isInitializing = false;
                UnityEngine.Debug.Log("Firebase Auth initialized successfully");

                // Process any pending operations that were queued while initializing
                ProcessPendingOperations();
            }
            else
            {
                UnityEngine.Debug.LogError("Firebase Auth instance is null");
                isInitializing = false;
                showNotificationMessage("Error", "Firebase authentication could not be initialized");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error initializing Firebase Auth: {ex.Message}");
            isInitializing = false;
            showNotificationMessage("Error", $"Firebase initialization error: {ex.Message}");
        }
    }

    // Process any operations that were queued while Firebase was initializing
    void ProcessPendingOperations()
    {
        UnityEngine.Debug.Log($"Processing {pendingOperations.Count} pending operations");
        while (pendingOperations.Count > 0)
        {
            try
            {
                Action operation = pendingOperations.Dequeue();
                operation.Invoke();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error processing pending operation: {ex.Message}");
            }
        }
    }

    // Retry mechanism for Firebase initialization
    void InitializeFirebaseWithRetry()
    {
        if (isInitializing) return;

        UnityEngine.Debug.Log("Attempting to initialize Firebase...");
        loadingPanel?.SetActive(true);
        isInitializing = true;

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                try
                {
                    auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
                    if (auth != null)
                    {
                        auth.StateChanged += AuthStateChanged;
                        AuthStateChanged(this, null);
                        isFirebaseInitialized = true;
                        UnityEngine.Debug.Log("Firebase Auth initialized successfully");

                        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

                        // Check for existing auth sessions
                        if (auth.CurrentUser != null && auth.CurrentUser.IsValid())
                        {
                            user = auth.CurrentUser;
                            isSignIn = true;
                            isSigned = true;

                            if (profileUserName_Text != null && profileUserEmail_Text != null)
                            {
                                profileUserName_Text.text = user.DisplayName;
                                profileUserEmail_Text.text = user.Email;
                                OpenProfilePanel();
                            }
                        }

                        // Process any pending operations
                        ProcessPendingOperations();
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Firebase Auth instance is null");
                        showNotificationMessage("Error", "Firebase authentication could not be initialized");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error initializing Firebase Auth: {ex.Message}");
                    showNotificationMessage("Error", $"Firebase initialization error: {ex.Message}");
                }
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                showNotificationMessage("Error", "Failed to initialize Firebase. Please restart the app.");
            }

            isInitializing = false;
            loadingPanel?.SetActive(false);
        });
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

                // הוסף את השורה הזו לאתחול הניקוד
                InitializeUserScore(user.UserId);

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
        // Show loading panel initially
        loadingPanel?.SetActive(true);
        // Initialize Firebase with a small delay to ensure Unity is fully ready
        Invoke("DelayedFirebaseInitialization", 0.5f);
    }

    void DelayedFirebaseInitialization()
    {
        // Initialize Firebase
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp
                InitializeFirebase();

                // Check for existing auth sessions
                if (auth != null && auth.CurrentUser != null && auth.CurrentUser.IsValid())
                {
                    user = auth.CurrentUser;
                    isSignIn = true;
                    isSigned = true;
                    profileUserName_Text.text = user.DisplayName;
                    profileUserEmail_Text.text = user.Email;
                    OpenProfilePanel();
                }
                else
                {
                    OpenLoginPanel();
                }
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                showNotificationMessage("Error", "Failed to initialize Firebase. Please restart the app.");
                OpenLoginPanel();
            }

            // Hide loading panel
            loadingPanel?.SetActive(false);
        });
    }

    bool isSigned = false;

    // Update is called once per frame
    void Update()
    {
        // Handle auto-login when user is signed in
        if (isSignIn && !isSigned && isFirebaseInitialized)
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

    // Display notification message to user
    void showNotificationMessage(string title, string message)
    {
        // You can implement a proper UI notification system here
        // For now, just log to console
        UnityEngine.Debug.Log($"{title}: {message}");

        // Update UI error text if available
        if (errorText != null)
        {
            errorText.text = $"{title}: {message}";
        }

        // TODO: Replace with your UI notification system, like:
        // UIManager.Instance.ShowNotification(title, message);
    }

    private void InitializeUserScore(string userId)
    {
        if (databaseReference != null)
        {
            // יצירת אובייקט המייצג את נתוני המשתמש
            UserData userData = new UserData
            {
                score = 0,
                displayName = auth.CurrentUser.DisplayName,
                lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string json = JsonUtility.ToJson(userData);

            // שמירת מידע המשתמש בדאטאבייס
            Task task = databaseReference.Child("users").Child(userId).SetRawJsonValueAsync(json);
            task.ContinueWith(t => {
                if (t.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Failed to initialize user score: {t.Exception}");
                    // כדי להריץ קוד UI על ה-main thread:
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        showNotificationMessage("Warning", "Failed to initialize game score");
                    });
                }
                else if (t.IsCompleted)
                {
                    UnityEngine.Debug.Log($"User score initialized successfully for user {userId}");
                }
            });
        }
        else
        {
            UnityEngine.Debug.LogError("Database reference is null. Cannot initialize user score.");
        }
    }
    // עדכון פונקציות נוספות ללא שימוש ב-ContinueWithOnMainThread
    public void UpdateUserScore(int newScore)
    {
        if (auth.CurrentUser != null && databaseReference != null)
        {
            string userId = auth.CurrentUser.UserId;
            Task task = databaseReference.Child("users").Child(userId).Child("score").SetValueAsync(newScore);
            task.ContinueWith(t => {
                if (t.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Score update failed: {t.Exception}");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        showNotificationMessage("Error", "Failed to update score");
                    });
                }
                else if (t.IsCompleted)
                {
                    UnityEngine.Debug.Log($"Score updated to {newScore} for user {userId}");
                }
            });
        }
    }

    public void GetCurrentUserScore(System.Action<int> callback)
    {
        if (auth.CurrentUser != null && databaseReference != null)
        {
            string userId = auth.CurrentUser.UserId;
            databaseReference.Child("users").Child(userId).Child("score").GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Failed to get user score: {task.Exception}");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        callback(-1); // קוד שגיאה
                    });
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        int score = Convert.ToInt32(snapshot.Value);
                        UnityEngine.Debug.Log($"Current score for user {userId}: {score}");
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            callback(score);
                        });
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"No score found for user {userId}");
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            callback(0); // ניקוד ברירת מחדל
                        });
                    }
                }
            });
        }
        else
        {
            callback(-1); // קוד שגיאה
        }
    }

    [System.Serializable]
    public class UserData
    {
        public int score;
        public string displayName;
        public string lastLogin;
    }
}