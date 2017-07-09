using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class NetworkManager : Photon.MonoBehaviour {
  string _gameVersion =
       GameData.TerrainGeneratorVersion + GameData.MultiplayerVersion;
  string playerNamePrefKey = "Username";

  const string roomName = "Game";
  RoomInfo[] roomsList;

  void Awake() {
    if (PhotonNetwork.connected) return;
    if (GameData.getLevel() != 0) GameData.MainMenu();
    PhotonNetwork.autoJoinLobby = true;
    PhotonNetwork.automaticallySyncScene = true;
  }
  void Start() {
    if (PhotonNetwork.connected) return;
    // if (PlayerPrefs.HasKey(playerNamePrefKey)) {
    //   GameData.username = PlayerPrefs.GetString(playerNamePrefKey);
    // }
    PhotonNetwork.ConnectUsingSettings(_gameVersion);
  }
  void OnGUI() {
    int xposCentered = Screen.width / 2 - 125;
    int yposCentered = Screen.height / 2 - 100;
    int xpos = 10;
    int ypos = 20;
    int spacing = 50;

    if (!PhotonNetwork.connected) {
      GUI.contentColor = Color.black;
      GUI.Label(new Rect(xposCentered, yposCentered, 250, 30),
                PhotonNetwork.connectionStateDetailed.ToString());
    } else if (PhotonNetwork.room == null) {
      GameData.username =
          GUI.TextField(new Rect(xposCentered + 25, yposCentered, 200, 30),
                        GameData.username);
      GUI.contentColor = Color.black;
      GUI.Label(
          new Rect(xposCentered + 25 + 200 + 5, yposCentered - 7, 200, 45),
          "Leaving this as \"Username\" will assign a random name");
      GUI.contentColor = Color.white;

      yposCentered += spacing;

      if (GUI.Button(new Rect(xposCentered, yposCentered, 250, 30),
                     "Create Room")) {
        if (GameData.username.ToLower() == "username" ||
            GameData.username == "") {
          GameData.username = NameList.GetName();
        }
        SetPlayerName(GameData.username);
        PhotonNetwork.CreateRoom(GameData.username + " " + roomName +
                                 Guid.NewGuid().ToString("N"));
      }

      yposCentered += spacing * 2;

      // Join Room
      if (roomsList != null && roomsList.Length > 0) {
        for (int i = 0; i < roomsList.Length; i++) {
          if (GUI.Button(
                  new Rect(xposCentered - 125, yposCentered + (30 * i), 500, 27),
                  "Join " + roomsList[i].Name)) {
            if (GameData.username.ToLower() == "username" ||
                GameData.username == "") {
              GameData.username = NameList.GetName();
            }
            SetPlayerName(GameData.username);
            PhotonNetwork.JoinRoom(roomsList[i].Name);
          }
        }
      } else {
        GUI.enabled = false;
        GUI.Button(new Rect(xposCentered + 25, yposCentered, 200, 27),
                   "No rooms available");
        GUI.enabled = true;
      }
    } else {
      GUI.contentColor = GameData.isPaused ? Color.white : Color.black;
      if (GameData.isPaused &&
          GUI.Button(new Rect(xpos, ypos, 200, 20), "Return to Main Menu")) {
        GameData.isPaused = false;
        FindObjectOfType<TerrainGenerator>().SaveAllChunks();
        LeaveRoom();
      } else {
        GUI.Label(new Rect(xpos, ypos, 300, 40),
                  "Players: " + PhotonNetwork.room.PlayerCount);
        if(PhotonNetwork.isMasterClient) {
          GUI.Label(new Rect(xpos, ypos + spacing, 300, 40),
                    "You are the master client");
        }
      }
    }
  }
  void SetPlayerName(string name) {
    PhotonNetwork.playerName = name;
    PlayerPrefs.SetString(playerNamePrefKey, name);
  }
  void OnReceivedRoomListUpdate() { roomsList = PhotonNetwork.GetRoomList(); }
  void OnJoinedRoom() { 
    Debug.Log("Connected to Room");
    if (PhotonNetwork.isMasterClient) PhotonNetwork.LoadLevel("Game");
  }
  void OnLeftRoom() {
    Debug.Log("Left Room");
    SceneManager.LoadScene("Menu");
  }

  public static void LeaveRoom() {
    PhotonNetwork.LeaveRoom();
  }
}
