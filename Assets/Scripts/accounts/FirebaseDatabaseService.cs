using UnityEngine;
using Firebase.Database;
using System;
using System.Threading.Tasks;
using Firebase.Extensions;

public class FirebaseDatabaseService
{
    // Events
    public event Action<UserData> OnUserDataUpdated;

    private readonly FirebaseManager firebaseManager;
    private DatabaseReference userDataRef;
    private EventHandler<ValueChangedEventArgs> userDataListener;

    public FirebaseDatabaseService(FirebaseManager manager)
    {
        firebaseManager = manager;
    }

    public Task InitializeUserData(string userId, string displayName)
    {
        if (!firebaseManager.IsInitialized)
        {
            UnityEngine.Debug.LogError("Firebase is not initialized");
            return Task.FromException(new Exception("Firebase is not initialized"));
        }

        if (firebaseManager.CurrentUser == null)
        {
            UnityEngine.Debug.LogError("No user is currently signed in");
            return Task.FromException(new Exception("No user is currently signed in"));
        }

        UserData initialData = new UserData
        {
            displayName = displayName,
            email = firebaseManager.CurrentUser.Email,
            score = 0,
            wins = 0,
            losses = 0,
            lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        return firebaseManager.DatabaseReference.Child("users").Child(userId).SetRawJsonValueAsync(
            JsonUtility.ToJson(initialData)
        ).ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                UnityEngine.Debug.LogError($"Error initializing user data: {task.Exception}");
                throw task.Exception;
            }
            else
            {
                UnityEngine.Debug.Log("User data initialized successfully");
            }
        });
    }

    public void SetupUserDataListener(string userId)
    {
        if (userDataListener != null)
        {
            RemoveUserDataListener();
        }

        userDataRef = firebaseManager.DatabaseReference.Child("users").Child(userId);
        userDataListener = (object sender, ValueChangedEventArgs args) => {
            if (args.DatabaseError != null)
            {
                UnityEngine.Debug.LogError($"Error listening to user data: {args.DatabaseError.Message}");
                return;
            }

            if (args.Snapshot != null && args.Snapshot.Exists)
            {
                string json = args.Snapshot.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        UserData userData = JsonUtility.FromJson<UserData>(json);
                        OnUserDataUpdated?.Invoke(userData);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Error parsing user data: {ex.Message}");
                    }
                }
            }
        };

        userDataRef.ValueChanged += userDataListener;
    }

    public void RemoveUserDataListener()
    {
        if (userDataRef != null && userDataListener != null)
        {
            userDataRef.ValueChanged -= userDataListener;
            userDataListener = null;
            userDataRef = null;
        }
    }

    public void UpdateUserScore(int newScore)
    {
        if (!firebaseManager.IsInitialized || firebaseManager.CurrentUser == null)
        {
            UnityEngine.Debug.LogError("Firebase is not initialized or no user is signed in");
            return;
        }

        firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId)
            .Child("score").SetValueAsync(newScore).ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Error updating score: {task.Exception}");
                }
            });
    }

    public void UpdateUserWins(int newWins)
    {
        if (!firebaseManager.IsInitialized || firebaseManager.CurrentUser == null)
        {
            UnityEngine.Debug.LogError("Firebase is not initialized or no user is signed in");
            return;
        }

        firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId)
            .Child("wins").SetValueAsync(newWins).ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Error updating wins: {task.Exception}");
                }
            });
    }

    public void UpdateUserLosses(int newLosses)
    {
        if (!firebaseManager.IsInitialized || firebaseManager.CurrentUser == null)
        {
            UnityEngine.Debug.LogError("Firebase is not initialized or no user is signed in");
            return;
        }

        firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId)
            .Child("losses").SetValueAsync(newLosses).ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Error updating losses: {task.Exception}");
                }
            });
    }

    public void UpdateRPSChoice(RPSUnit.RPSKind choice)
    {
        if (!firebaseManager.IsInitialized || firebaseManager.CurrentUser == null)
        {
            UnityEngine.Debug.LogError("Firebase is not initialized or no user is signed in");
            return;
        }

        string fieldName = choice switch
        {
            RPSUnit.RPSKind.Rock => "rockChoices",
            RPSUnit.RPSKind.Paper => "paperChoices",
            RPSUnit.RPSKind.Scissors => "scissorsChoices",
            _ => throw new System.ArgumentException("Invalid RPS choice")
        };

        // ���� ���� �� ���� ������
        firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId)
            .Child(fieldName).GetValueAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Error getting current {fieldName}: {task.Exception}");
                    return;
                }

                int currentValue = 0;
                if (task.Result.Exists)
                {
                    currentValue = int.Parse(task.Result.Value.ToString());
                }

                // ����� �� ���� ����
                firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId)
                    .Child(fieldName).SetValueAsync(currentValue + 1).ContinueWithOnMainThread(updateTask => {
                        if (updateTask.IsFaulted)
                        {
                            UnityEngine.Debug.LogError($"Error updating {fieldName}: {updateTask.Exception}");
                        }
                    });
            });
    }

    public void GetUserStats(Action<UserData> callback)
    {
        if (!firebaseManager.IsInitialized || firebaseManager.CurrentUser == null)
        {
            callback?.Invoke(null);
            return;
        }

        firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId)
            .GetValueAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Error getting user stats: {task.Exception}");
                    callback?.Invoke(null);
                    return;
                }

                if (task.Result.Exists)
                {
                    string json = task.Result.GetRawJsonValue();
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            UserData userData = JsonUtility.FromJson<UserData>(json);

                            // Check if we need to update the user data
                            if (string.IsNullOrEmpty(userData.email) || string.IsNullOrEmpty(userData.displayName))
                            {
                                UpdateExistingUserData(userData);
                            }

                            callback?.Invoke(userData);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Error parsing user data: {ex.Message}");
                            callback?.Invoke(null);
                        }
                    }
                    else
                    {
                        // If no data exists, create it
                        InitializeUserData(firebaseManager.CurrentUser.UserId, firebaseManager.CurrentUser.DisplayName ?? "User");
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    // If no data exists, create it
                    InitializeUserData(firebaseManager.CurrentUser.UserId, firebaseManager.CurrentUser.DisplayName ?? "User");
                    callback?.Invoke(null);
                }
            });
    }

    private void UpdateExistingUserData(UserData existingData)
    {
        if (!firebaseManager.IsInitialized || firebaseManager.CurrentUser == null)
        {
            UnityEngine.Debug.LogError("Firebase is not initialized or no user is signed in");
            return;
        }

        // Create updated data
        UserData updatedData = new UserData
        {
            displayName = string.IsNullOrEmpty(existingData.displayName) ?
                firebaseManager.CurrentUser.DisplayName ?? "User" : existingData.displayName,
            email = string.IsNullOrEmpty(existingData.email) ?
                firebaseManager.CurrentUser.Email : existingData.email,
            score = existingData.score,
            wins = existingData.wins,
            losses = existingData.losses,
            lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Update the data in Firebase
        firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId)
            .SetRawJsonValueAsync(JsonUtility.ToJson(updatedData)).ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Error updating existing user data: {task.Exception}");
                }
                else
                {
                    UnityEngine.Debug.Log("Existing user data updated successfully");
                }
            });
    }

    public void IncrementUserLossesAsync()
    {
        if (!firebaseManager.IsInitialized || firebaseManager.CurrentUser == null)
        {
            UnityEngine.Debug.LogError("Firebase is not initialized or no user is signed in");
            return;
        }

        DatabaseReference lossesRef = firebaseManager.DatabaseReference.Child("users").Child(firebaseManager.CurrentUser.UserId).Child("losses");

        lossesRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                UnityEngine.Debug.LogError($"Error getting current losses: {task.Exception}");
                return;
            }

            long currentLosses = 0;
            if (task.Result.Exists)
            {
                currentLosses = (long)task.Result.Value;
            }

            lossesRef.SetValueAsync(currentLosses + 1).ContinueWithOnMainThread(updateTask =>
            {
                if (updateTask.IsFaulted)
                {
                    UnityEngine.Debug.LogError($"Error updating losses: {updateTask.Exception}");
                }
                else
                {
                    UnityEngine.Debug.Log("User losses incremented successfully.");
                }
            });
        });
    }
}