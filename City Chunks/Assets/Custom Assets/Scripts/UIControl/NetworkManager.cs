// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class NetworkManager : Photon.MonoBehaviour {
  public const string version = "m4";

  private const string playerNamePrefKey = "Username";

  private const string roomName = "Game";
  public static RoomInfo[] roomsList;

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
    PhotonNetwork.ConnectUsingSettings(GameData.longVersion);
  }
  void OnGUI() {
    if (GameData.isPaused) return;
    // int xposCentered = Screen.width / 2 - 125;
    // int yposCentered = Screen.height / 2 - 100;
    int xpos = 10;
    int ypos = 20;
    // int spacing = 50;

    if (!PhotonNetwork.connected) {
      // GUI.contentColor = Color.black;
      // GUI.Label(new Rect(xposCentered, yposCentered, 250, 30),
      //           PhotonNetwork.connectionStateDetailed.ToString());
      if (PhotonNetwork.connectionStateDetailed.ToString() == "PeerCreated") {
        PhotonNetwork.ConnectUsingSettings(GameData.longVersion);
      }

      GUI.contentColor = Color.black;
      if (!PhotonNetwork.offlineMode) {
        if (GUI.Button(new Rect(xpos, ypos, 250, 150), "Offline Mode")) {
          PhotonNetwork.offlineMode = true;
        }
        if (GUI.Button(new Rect(xpos, ypos + 155, 250, 75), "Retry Connecting")) {
          PhotonNetwork.ConnectUsingSettings(GameData.longVersion);
        }
      }
      if (PhotonNetwork.offlineMode &&
          GUI.Button(new Rect(xpos, ypos, 250, 150), "Go back Online"))
        PhotonNetwork.offlineMode = false;
    } else if (PhotonNetwork.room == null) {
      GUI.contentColor = Color.black;
      if (PhotonNetwork.offlineMode &&
          GUI.Button(new Rect(xpos, ypos, 250, 150), "Go back Online"))
        PhotonNetwork.offlineMode = false;
    } else {
      GUI.contentColor = GameData.isPaused ? Color.white : Color.black;
      if (!GameData.isPaused) {
        GUI.Label(new Rect(xpos, ypos - 20, 300, 20), PhotonNetwork.room.Name);
        if (PhotonNetwork.isMasterClient) {
          GUI.Label(new Rect(xpos, ypos, 300, 20), "You are the master client");
        }
        string playerList = "";
        foreach (PhotonPlayer players in PhotonNetwork.playerList) {
          playerList += "\n" + players.NickName;
        }
        GUI.Label(
            new Rect(xpos, ypos + 20, 300, 300),
            "Players in room: " + PhotonNetwork.room.PlayerCount + playerList);
      }
    }
  }
  private static void SetPlayerName(string name) {
    name = name.Replace('`', '\'');
    PhotonNetwork.playerName = name;
    if (ChatManager.AuthVal != null) {
      ChatManager.AuthVal.UserId = name;
    }
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
    GameData.Seed = UnityEngine.Random.Range(1, 100000);
    PhotonNetwork.CreateRoom(GameData.username + "`" + GameData.Seed + "`" +
                             name + Guid.NewGuid().ToString("N"));
  }
  public static void JoinRoom(string name) {
    if (GameData.isUsernameDefault() || !GameData.isUsernameValid()) {
      GameData.username = NameList.GetName();
    }
    string seed = name.Split('`')[1];
    int Seed;
    if (System.Int32.TryParse(seed, out Seed)) {
      GameData.Seed = Seed;
    } else {
      Debug.LogError("Failed to parse room seed!");
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
