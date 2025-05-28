using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Firebase.Database;
using System.Collections.Generic;
using System.Diagnostics;

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

    private void CreateRoom()
    {
        // Generate a unique room ID
        currentRoomId = Guid.NewGuid().ToString();
        isHost = true;

        UnityEngine.Debug.Log($"Creating room with ID: {currentRoomId}");

        // Create room in Firebase
        Dictionary<string, object> roomData = new Dictionary<string, object>
        {
            { "hostId", FirebaseManager.Instance.CurrentUser.UserId },
            { "guestId", "" },
            { "status", "waiting" },
            { "createdAt", ServerValue.Timestamp }
        };

        roomsRef.Child(currentRoomId).SetValueAsync(roomData).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                UnityEngine.Debug.LogError("Failed to create room: " + task.Exception);
                return;
            }

            // Show room ID to host
            UnityEngine.Debug.Log($"Room created successfully. Setting text to: Room ID: {currentRoomId}");
            roomIdText.text = "Room ID: " + currentRoomId;
            roomIdText.gameObject.SetActive(true);
            statusText.text = "Waiting for opponent...";

            // Start listening for guest joining
            StartListeningForGuest();
        });
    }

    private void JoinRoom()
    {
        string roomId = roomIdInput.text.Trim();
        if (string.IsNullOrEmpty(roomId))
        {
            statusText.text = "Please enter a room ID";
            return;
        }

        // Check if room exists and has space
        roomsRef.Child(roomId).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                UnityEngine.Debug.LogError("Failed to check room: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                statusText.text = "Room not found";
                return;
            }

            string guestId = snapshot.Child("guestId").Value?.ToString();
            if (!string.IsNullOrEmpty(guestId))
            {
                statusText.text = "Room is full";
                return;
            }

            // Join room
            currentRoomId = roomId;
            isHost = false;

            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "guestId", FirebaseManager.Instance.CurrentUser.UserId },
                { "status", "ready" }
            };

            roomsRef.Child(roomId).UpdateChildrenAsync(updates).ContinueWith(updateTask =>
            {
                if (updateTask.IsFaulted)
                {
                    UnityEngine.Debug.LogError("Failed to join room: " + updateTask.Exception);
                    return;
                }

                statusText.text = "Joined room! Waiting for host...";
                StartListeningForGameStart();
            });
        });
    }

    private void StartListeningForGuest()
    {
        roomsRef.Child(currentRoomId).ValueChanged += HandleRoomValueChanged;
    }

    private void StartListeningForGameStart()
    {
        roomsRef.Child(currentRoomId).ValueChanged += HandleRoomValueChanged;
    }

    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            UnityEngine.Debug.LogError("Database error: " + args.DatabaseError);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        if (!snapshot.Exists) return;

        string status = snapshot.Child("status").Value?.ToString();
        string guestId = snapshot.Child("guestId").Value?.ToString();

        if (isHost && !string.IsNullOrEmpty(guestId))
        {
            // Guest has joined, update status
            statusText.text = "Opponent joined! Starting game...";
            StartGame();
        }
        else if (!isHost && status == "ready")
        {
            // Host is ready, start game
            statusText.text = "Host is ready! Starting game...";
            StartGame();
        }
    }

    private void StartGame()
    {
        // Stop listening to room changes
        roomsRef.Child(currentRoomId).ValueChanged -= HandleRoomValueChanged;

        // Load game scene
        GameModeManager.Instance.SelectedMode = GameMode.PvP;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (roomsRef != null && !string.IsNullOrEmpty(currentRoomId))
        {
            roomsRef.Child(currentRoomId).ValueChanged -= HandleRoomValueChanged;
        }
    }
}