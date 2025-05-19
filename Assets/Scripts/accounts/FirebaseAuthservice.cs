using UnityEngine;
using Firebase.Auth;
using System;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase;

public class FirebaseAuthService
{
    // Events
    public event Action<bool, string> OnUserAuthenticated;
    public event Action<bool, string> OnUserRegistered;
    public event Action OnUserSignedOut;

    private readonly FirebaseManager firebaseManager;

    public FirebaseAuthService(FirebaseManager manager)
    {
        firebaseManager = manager;
    }

    public void SignInWithEmailPassword(string email, string password, Action<bool, string> callback)
    {
        if (!firebaseManager.IsInitialized)
        {
            callback?.Invoke(false, "Firebase is not initialized");
            return;
        }

        UnityEngine.Debug.Log($"Attempting login with email: {email}");

        firebaseManager.Auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
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

            callback?.Invoke(true, "Login successful");
            OnUserAuthenticated?.Invoke(true, "Login successful");
        });
    }

    public void CreateUserWithEmailPassword(string email, string password, string displayName, Action<bool, string> callback)
    {
        if (!firebaseManager.IsInitialized)
        {
            callback?.Invoke(false, "Firebase is not initialized");
            return;
        }

        firebaseManager.Auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                UnityEngine.Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                callback?.Invoke(false, "Request was canceled");
                OnUserRegistered?.Invoke(false, "Request was canceled");
                return;
            }
            if (task.IsFaulted)
            {
                string errorMessage = HandleFirebaseError(task.Exception, "Registration Failed");
                callback?.Invoke(false, errorMessage);
                OnUserRegistered?.Invoke(false, errorMessage);
                return;
            }

            AuthResult result = task.Result;
            UnityEngine.Debug.LogFormat("User created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            UpdateUserProfile(displayName, (success, message) => {
                callback?.Invoke(success, message);
                OnUserRegistered?.Invoke(success, message);
            });
        });
    }

    private void UpdateUserProfile(string displayName, Action<bool, string> callback)
    {
        if (firebaseManager.CurrentUser == null)
        {
            callback?.Invoke(false, "No user is currently signed in");
            return;
        }

        UserProfile profile = new UserProfile
        {
            DisplayName = displayName
        };

        firebaseManager.CurrentUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                UnityEngine.Debug.LogError("UpdateUserProfileAsync was canceled.");
                callback?.Invoke(false, "Request was canceled");
                return;
            }
            if (task.IsFaulted)
            {
                string errorMessage = HandleFirebaseError(task.Exception, "Profile Update Failed");
                callback?.Invoke(false, errorMessage);
                return;
            }

            UnityEngine.Debug.Log("User profile updated successfully.");
            callback?.Invoke(true, "Profile updated successfully");
        });
    }

    public void SignOut()
    {
        if (firebaseManager.Auth != null)
        {
            firebaseManager.Auth.SignOut();
            OnUserSignedOut?.Invoke();
        }
    }

    private string HandleFirebaseError(Exception exception, string defaultMessage)
    {
        string errorMessage = defaultMessage;

        if (exception != null)
        {
            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    if (innerException is FirebaseException firebaseEx)
                    {
                        UnityEngine.Debug.LogError($"Firebase error code: {firebaseEx.ErrorCode}, Message: {firebaseEx.Message}");

                        // Check for specific error messages in the exception message
                        if (firebaseEx.Message.Contains("INVALID_LOGIN_CREDENTIALS"))
                        {
                            errorMessage = "Invalid email or password";
                        }
                        else if (firebaseEx.Message.Contains("USER_NOT_FOUND"))
                        {
                            errorMessage = "User not found";
                        }
                        else if (firebaseEx.Message.Contains("INVALID_EMAIL"))
                        {
                            errorMessage = "Invalid email format";
                        }
                        else if (firebaseEx.Message.Contains("WEAK_PASSWORD"))
                        {
                            errorMessage = "Password is too weak";
                        }
                        else if (firebaseEx.Message.Contains("EMAIL_ALREADY_IN_USE"))
                        {
                            errorMessage = "Email is already in use";
                        }
                        else if (firebaseEx.Message.Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
                        {
                            errorMessage = "Too many login attempts. Please try again later";
                        }
                        else
                        {
                            errorMessage = "Authentication failed. Please check your credentials and try again";
                        }
                        break;
                    }
                }
            }
            else
            {
                errorMessage = "Authentication failed. Please try again";
            }
        }

        return errorMessage;
    }
}