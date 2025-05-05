using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using System.Diagnostics;

public class FirebaseManager : MonoBehaviour
{
    // Singleton pattern
    private static FirebaseManager _instance;
    public static FirebaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("FirebaseManager");
                _instance = obj.AddComponent<FirebaseManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    // Firebase components
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private DatabaseReference databaseReference;

    // State tracking
    private bool isFirebaseInitialized = false;
    private bool isInitializing = false;
    private Queue<Action> pendingOperations = new Queue<Action>();

    // Events
    public event Action<bool, string> OnFirebaseInitialized;
    public event Action<bool, string> OnUserAuthenticated;
    public event Action<bool, string> OnUserRegistered;
    public event Action OnUserSignedOut;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Initialize Firebase with a small delay to ensure Unity is fully ready
        Invoke("InitializeFirebaseWithRetry", 0.5f);
    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }

    #region Firebase Initialization

    public void InitializeFirebaseWithRetry()
    {
        if (isInitializing) return;

        UnityEngine.Debug.Log("Attempting to initialize Firebase...");
        isInitializing = true;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                try
                {
                    auth = FirebaseAuth.DefaultInstance;
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
                            currentUser = auth.CurrentUser;
                            OnUserAuthenticated?.Invoke(true, "User already signed in");
                        }

                        // Process any pending operations
                        ProcessPendingOperations();
                        OnFirebaseInitialized?.Invoke(true, "Firebase initialized successfully");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Firebase Auth instance is null");
                        OnFirebaseInitialized?.Invoke(false, "Firebase authentication could not be initialized");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error initializing Firebase Auth: {ex.Message}");
                    OnFirebaseInitialized?.Invoke(false, $"Firebase initialization error: {ex.Message}");
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                OnFirebaseInitialized?.Invoke(false, "Failed to initialize Firebase. Please restart the app.");
            }

            isInitializing = false;
        });
    }

    private void ProcessPendingOperations()
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

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != currentUser)
        {
            bool signedIn = currentUser != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();

            if (!signedIn && currentUser != null)
            {
                UnityEngine.Debug.Log("Signed out " + currentUser.UserId);
                OnUserSignedOut?.Invoke();
            }

            currentUser = auth.CurrentUser;

            if (signedIn)
            {
                UnityEngine.Debug.Log("Signed in " + currentUser.UserId);
            }
        }
    }

    public bool IsInitialized => isFirebaseInitialized;
    public bool IsInitializing => isInitializing;
    public bool IsUserSignedIn => currentUser != null && currentUser.IsValid();
    public FirebaseUser CurrentUser => currentUser;
    public DatabaseReference DatabaseReference => databaseReference;

    #endregion

    #region Authentication Methods

    public void SignInWithEmailPassword(string email, string password, Action<bool, string> callback)
    {
        // If Firebase is not ready, queue this operation
        if (!isFirebaseInitialized && !isInitializing)
        {
            pendingOperations.Enqueue(() => SignInWithEmailPassword(email, password, callback));
            InitializeFirebaseWithRetry();
            return;
        }
        else if (isInitializing)
        {
            pendingOperations.Enqueue(() => SignInWithEmailPassword(email, password, callback));
            return;
        }

        UnityEngine.Debug.Log($"Attempting login with email: {email}");

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                UnityEngine.Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                callback?.Invoke(false, "Request was canceled");
                OnUserAuthenticated?.Invoke(false, "Request was canceled");
                return;
            }
            if (task.IsFaulted)
            {
                string errorMessage = HandleFirebaseError(task.Exception, "Login Failed");
                callback?.Invoke(false, errorMessage);
                OnUserAuthenticated?.Invoke(false, errorMessage);
                return;
            }

            AuthResult result = task.Result;
            UnityEngine.Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            currentUser = result.User;
            callback?.Invoke(true, "Login successful");
            OnUserAuthenticated?.Invoke(true, "Login successful");
        });
    }

    public void CreateUserWithEmailPassword(string email, string password, string displayName, Action<bool, string> callback)
    {
        if (!isFirebaseInitialized)
        {
            callback?.Invoke(false, "Firebase not initialized");
            return;
        }

        UnityEngine.Debug.Log($"Attempting signup with email: {email}");

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                UnityEngine.Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                callback?.Invoke(false, "Request was canceled");
                return;
            }
            if (task.IsFaulted)
            {
                string errorMessage = HandleFirebaseError(task.Exception, "Signup Failed");
                callback?.Invoke(false, errorMessage);
                return;
            }

            AuthResult result = task.Result;
            UnityEngine.Debug.LogFormat("User created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            UpdateUserProfile(displayName, callback);
        });
    }

    private void UpdateUserProfile(string displayName, Action<bool, string> callback)
    {
        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            UserProfile profile = new UserProfile
            {
                DisplayName = displayName,
                PhotoUrl = new Uri("https://placehold.co/600x400"),
            };

            user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    string errorMessage = HandleFirebaseError(task.Exception, "Profile update failed");
                    callback?.Invoke(false, errorMessage);
                    return;
                }

                UnityEngine.Debug.Log("User profile updated successfully.");

                // Initialize user data in database
                InitializeUserData(user.UserId, displayName);

                currentUser = user;
                callback?.Invoke(true, "Account successfully created");
                OnUserRegistered?.Invoke(true, "Account successfully created");
            });
        }
    }

    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            OnUserSignedOut?.Invoke();
        }
    }

    private string HandleFirebaseError(Exception exception, string defaultMessage)
    {
        string errorMessage = defaultMessage;

        if (exception != null)
        {
            // Check if this is an AggregateException (which has InnerExceptions)
            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    if (innerException is FirebaseException firebaseEx)
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
            else if (exception is FirebaseException firebaseEx)
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
        return errorMessage;
    }

    #endregion

    #region Database Methods

    public void InitializeUserData(string userId, string displayName)
    {
        if (databaseReference != null)
        {
            // Create user data object
            UserData userData = new UserData
            {
                score = 0,
                displayName = displayName,
                lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string json = JsonUtility.ToJson(userData);

            // Save user data to database
            Task task = databaseReference.Child("users").Child(userId).SetRawJsonValueAsync(json);
            task.ContinueWith(t => {
                if (t.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Failed to initialize user data: {t.Exception}");
                }
                else if (t.IsCompleted)
                {
                    UnityEngine.Debug.Log($"User data initialized successfully for user {userId}");
                }
            });
        }
        else
        {
            UnityEngine.Debug.LogError("Database reference is null. Cannot initialize user data.");
        }
    }

    public void UpdateUserScore(int newScore)
    {
        if (currentUser != null && databaseReference != null)
        {
            string userId = currentUser.UserId;
            Task task = databaseReference.Child("users").Child(userId).Child("score").SetValueAsync(newScore);
            task.ContinueWith(t => {
                if (t.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Score update failed: {t.Exception}");
                }
                else if (t.IsCompleted)
                {
                    UnityEngine.Debug.Log($"Score updated to {newScore} for user {userId}");
                }
            });
        }
    }

    public void GetUserScore(Action<int> callback)
    {
        if (currentUser != null && databaseReference != null)
        {
            string userId = currentUser.UserId;
            databaseReference.Child("users").Child(userId).Child("score").GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Failed to get user score: {task.Exception}");
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        callback(-1); // Error code
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
                            callback(0); // Default score
                        });
                    }
                }
            });
        }
        else
        {
            callback(-1); // Error code
        }
    }

    #endregion
}