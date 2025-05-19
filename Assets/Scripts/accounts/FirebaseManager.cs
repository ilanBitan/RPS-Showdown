using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using Firebase.Extensions;

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
    private FirebaseDatabaseService databaseService;

    // State tracking
    private bool isFirebaseInitialized = false;
    private bool isInitializing = false;
    private Queue<Action> pendingOperations = new Queue<Action>();

    // Events
    public event Action<bool, string> OnFirebaseInitialized;
    public event Action<string> OnSceneChangeRequested;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        databaseService = new FirebaseDatabaseService(this);
    }

    private void Start()
    {
        // התנתקות ממשתמש קיים אם יש כזה
        if (auth != null && auth.CurrentUser != null)
        {
            UnityEngine.Debug.Log("Signing out existing user at game start");
            auth.SignOut();
        }

        // Initialize Firebase with a small delay to ensure Unity is fully ready
        Invoke("InitializeFirebaseWithRetry", 0.5f);
    }

    private void OnApplicationQuit()
    {
        // התנתקות אוטומטית כשהמשחק נסגר
        if (auth != null && auth.CurrentUser != null)
        {
            UnityEngine.Debug.Log("Signing out user before application quit");
            auth.SignOut();
        }
    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }

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

            currentUser = auth.CurrentUser;

            if (signedIn)
            {
                UnityEngine.Debug.Log("Signed in " + currentUser.UserId);
            }
        }
    }

    public void RequestSceneChange(string sceneName)
    {
        UnityEngine.Debug.Log($"Requesting scene change to: {sceneName}");
        OnSceneChangeRequested?.Invoke(sceneName);
    }

    // Public properties
    public bool IsInitialized => isFirebaseInitialized;
    public bool IsInitializing => isInitializing;
    public bool IsUserSignedIn => currentUser != null && currentUser.IsValid();
    public FirebaseUser CurrentUser => currentUser;
    public DatabaseReference DatabaseReference => databaseReference;
    public FirebaseAuth Auth => auth;
    public FirebaseDatabaseService DatabaseService => databaseService;
}