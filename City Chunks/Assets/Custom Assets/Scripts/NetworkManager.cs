using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class NetworkManager : Photon.MonoBehaviour {
  string _gameVersion =
       GameData.TerrainGeneratorVersion + GameData.MultiplayerVersion;
  const string playerNamePrefKey = "Username";

  const string roomName = "Game";
  RoomInfo[] roomsList;

  public static NetworkManager NMInstance;

  void Awake() {
    if (NMInstance == null) {
      DontDestroyOnLoad(gameObject);
      NMInstance = this;
    } else if (NMInstance != this) {
      Destroy(gameObject);
      return;
    }

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
    if (GameData.isPaused) return;
    int xposCentered = Screen.width / 2 - 125;
    int yposCentered = Screen.height / 2 - 100;
    int xpos = 10;
    int ypos = 20;
    int spacing = 50;

    if (!PhotonNetwork.connected) {
      GUI.contentColor = Color.black;
      GUI.Label(new Rect(xposCentered, yposCentered, 250, 30),
                PhotonNetwork.connectionStateDetailed.ToString());

      GUI.contentColor = Color.black;
      if (!PhotonNetwork.offlineMode) {
        if (GUI.Button(new Rect(xpos, ypos, 250, 150), "Offline Mode")) {
          PhotonNetwork.offlineMode = true;
        }
        if (GUI.Button(new Rect(xpos, ypos + 155, 250, 75), "Retry Connecting")) {
          PhotonNetwork.ConnectUsingSettings(_gameVersion);
        }
      }
      if (PhotonNetwork.offlineMode &&
          GUI.Button(new Rect(xpos, ypos, 250, 150), "Go back Online"))
        PhotonNetwork.offlineMode = false;
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
        CreateRoom(roomName);
      }

      yposCentered += spacing * 2;

      // Join Room
      if (roomsList != null && roomsList.Length > 0) {
        for (int i = 0; i < roomsList.Length; i++) {
          if (GUI.Button(
                  new Rect(xposCentered - 125, yposCentered + (30 * i), 500, 27),
                  "Join " + roomsList[i].Name)) {
            JoinRoom(roomsList[i].Name);
          }
        }
      } else {
        GUI.enabled = false;
        GUI.Button(new Rect(xposCentered + 25, yposCentered, 200, 27),
                   "No rooms available");
        GUI.enabled = true;
      }

      GUI.contentColor = Color.black;
      if (PhotonNetwork.offlineMode &&
          GUI.Button(new Rect(xpos, ypos, 250, 150), "Go back Online"))
        PhotonNetwork.offlineMode = false;
    } else {
      GUI.contentColor = GameData.isPaused ? Color.white : Color.black;
      if (GameData.isPaused &&
          GUI.Button(new Rect(xposCentered + 25, Screen.height - ypos - 150, 200, 40),
                     "Return to Main Menu")) {
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
  private static void SetPlayerName(string name) {
    name = name.Replace('`', '\'');
    PhotonNetwork.playerName = name;
    ChatManager.AuthVal.UserId = name;
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
  public static void CreateRoom(string name) {
    if (GameData.isUsernameDefault() || !GameData.isUsernameValid()) {
      GameData.username = NameList.GetName();
    }
    SetPlayerName(GameData.username);
    PhotonNetwork.CreateRoom(GameData.username + " " + name +
                             Guid.NewGuid().ToString("N"));
  }
  public static void JoinRoom(string name) {
    if (GameData.isUsernameDefault() || !GameData.isUsernameValid()) {
      GameData.username = NameList.GetName();
    }
    SetPlayerName(GameData.username);
    PhotonNetwork.JoinRoom(name);
  }
  public static void JoinRandomRoom() {
    if (GameData.isUsernameDefault() || !GameData.isUsernameValid()) {
      GameData.username = NameList.GetName();
    }
    SetPlayerName(GameData.username);
    PhotonNetwork.JoinRandomRoom();
  }
}
