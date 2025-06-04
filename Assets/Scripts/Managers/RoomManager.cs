using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Firebase.Database;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public class RoomManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField roomIdInput;
    [SerializeField] private TextMeshProUGUI roomIdText;
    [SerializeField] private TextMeshProUGUI statusText;

    private DatabaseReference roomsRef;
    private string currentRoomId;
    private bool isHost = false;
    private bool isListening = false;

    private void Awake()
    {
        // Initialize Firebase reference
        roomsRef = FirebaseManager.Instance.DatabaseReference.Child("rooms");
    }

    private void Start()
    {
        // Initialize UI
        roomPanel.SetActive(false);

        // Validate TextMeshProUGUI component
        if (roomIdText == null)
        {
            UnityEngine.Debug.LogError("RoomIdText is not assigned in the inspector!");
        }
        else
        {
            roomIdText.gameObject.SetActive(false);
            UnityEngine.Debug.Log("RoomIdText is properly assigned");
        }

        statusText.text = "";

        // Add button listeners
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
    }

    public void ShowRoomPanel()
    {
        roomPanel.SetActive(true);
    }

    private async void CreateRoom()
    {
        // Generate a unique room ID
        currentRoomId = Guid.NewGuid().ToString();
        isHost = true;

        UnityEngine.Debug.Log($"Creating room with ID: {currentRoomId}");

        // Create room in Firebase with extended setup tracking
        Dictionary<string, object> roomData = new Dictionary<string, object>
        {
            { "hostId", FirebaseManager.Instance.CurrentUser.UserId },
            { "guestId", "" },
            { "status", "waiting" }, // waiting -> host_setup -> guest_setup -> playing
            { "createdAt", ServerValue.Timestamp },
            { "currentSetupPhase", "waiting" }, // waiting -> host -> guest -> complete
            { "hostSetupComplete", false },
            { "guestSetupComplete", false },
            { "hostFlagPosition", "" }, // "col_row" format
            { "hostTrapPosition", "" },
            { "guestFlagPosition", "" },
            { "guestTrapPosition", "" }
        };

        try
        {
            await roomsRef.Child(currentRoomId).SetValueAsync(roomData);

            // Show room ID to host on main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UnityEngine.Debug.Log($"Room created successfully. Setting text to: Room ID: {currentRoomId}");
                roomIdText.text = "Room ID: " + currentRoomId;
                roomIdText.gameObject.SetActive(true);
                statusText.text = "Waiting for opponent...";

                // Start listening for guest joining
                StartListeningForGuest();
            });
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to create room: " + ex.Message);
        }
    }

    private async void JoinRoom()
    {
        string roomId = roomIdInput.text.Trim();
        if (string.IsNullOrEmpty(roomId))
        {
            statusText.text = "Please enter a room ID";
            return;
        }

        try
        {
            // Check if room exists and has space
            DataSnapshot snapshot = await roomsRef.Child(roomId).GetValueAsync();

            if (!snapshot.Exists)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    statusText.text = "Room not found";
                });
                return;
            }

            string guestId = snapshot.Child("guestId").Value?.ToString();
            if (!string.IsNullOrEmpty(guestId))
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    statusText.text = "Room is full";
                });
                return;
            }

            // Join room
            currentRoomId = roomId;
            isHost = false;

            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "guestId", FirebaseManager.Instance.CurrentUser.UserId },
                { "status", "waiting" } // Keep status as waiting, let host change it to ready
            };

            await roomsRef.Child(roomId).UpdateChildrenAsync(updates);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                statusText.text = "Joined room! Waiting for host...";
                StartListeningForGameStart();
            });
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to join room: " + ex.Message);
        }
    }

    private void StartListeningForGuest()
    {
        if (!isListening)
        {
            UnityEngine.Debug.Log("Starting to listen for guest...");
            roomsRef.Child(currentRoomId).ValueChanged += HandleRoomValueChanged;
            isListening = true;
        }
    }

    private void StartListeningForGameStart()
    {
        if (!isListening)
        {
            UnityEngine.Debug.Log("Starting to listen for game start...");
            roomsRef.Child(currentRoomId).ValueChanged += HandleRoomValueChanged;
            isListening = true;
        }
    }

    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs args)
    {
        UnityEngine.Debug.Log("HandleRoomValueChanged called!");

        if (args.DatabaseError != null)
        {
            UnityEngine.Debug.LogError("Database error: " + args.DatabaseError);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        if (!snapshot.Exists)
        {
            UnityEngine.Debug.Log("Snapshot doesn't exist");
            return;
        }

        string status = snapshot.Child("status").Value?.ToString();
        string guestId = snapshot.Child("guestId").Value?.ToString();
        string hostId = snapshot.Child("hostId").Value?.ToString();
        string currentSetupPhase = snapshot.Child("currentSetupPhase").Value?.ToString();

        // Ignore updates during setup phase
        if (!string.IsNullOrEmpty(currentSetupPhase) && currentSetupPhase != "waiting")
        {
            UnityEngine.Debug.Log($"[RoomManager] Ignoring room update during setup phase: {currentSetupPhase}");
            return;
        }

        UnityEngine.Debug.Log($"Room state changed - Status: {status}, GuestId: {guestId}, HostId: {hostId}, IsHost: {isHost}");

        // Use main thread dispatcher to update UI and start game
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (status == "ready" && !string.IsNullOrEmpty(guestId) && guestId != "")
            {
                // Both players are ready, start game
                UnityEngine.Debug.Log($"Both players ready - starting game. IsHost: {isHost}");
                statusText.text = "Both players ready! Starting game...";
                StartGame();
            }
            else if (isHost && !string.IsNullOrEmpty(guestId) && guestId != "" && status == "waiting")
            {
                // Guest just joined, update status to ready for both
                UnityEngine.Debug.Log("Guest joined - updating status to ready");
                statusText.text = "Opponent joined! Preparing game...";

                // Update room status to ready
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "status", "ready" }
                };
                roomsRef.Child(currentRoomId).UpdateChildrenAsync(updates);
            }
        });
    }

    private void StartGame()
    {
        UnityEngine.Debug.Log($"[RoomManager] Starting game... IsHost: {isHost}, RoomId: {currentRoomId}");

        // Stop listening to room changes
        if (isListening)
        {
            roomsRef.Child(currentRoomId).ValueChanged -= HandleRoomValueChanged;
            isListening = false;
        }

        // Load game scene
        GameModeManager.Instance.SelectedMode = GameMode.PvP;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");

        // After scene loads, we need to pass the room info to GameSetupManager
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "GameScene")
            {
                UnityEngine.Debug.Log($"[RoomManager] Game scene loaded. Passing room info - RoomId: {currentRoomId}, IsHost: {isHost}");
                var unitPlacer = FindObjectOfType<UnitPlacer>();
                if (unitPlacer != null)
                {
                    unitPlacer.InitializeWithRoomInfo(currentRoomId, isHost);
                }
                else
                {
                    UnityEngine.Debug.LogError("[RoomManager] UnitPlacer not found in scene!");
                }
            }
        };
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (roomsRef != null && !string.IsNullOrEmpty(currentRoomId) && isListening)
        {
            roomsRef.Child(currentRoomId).ValueChanged -= HandleRoomValueChanged;
            isListening = false;
        }
    }
}