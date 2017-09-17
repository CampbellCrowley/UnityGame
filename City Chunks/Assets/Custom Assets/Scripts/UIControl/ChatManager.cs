// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon.Chat;
using System;
using System.Collections.Generic;

public class ChatManager : MonoBehaviour, IChatClientListener {

  public InputField input;
  public Text[] uiLines;
  [Range(0f, 30f)]
  public float uiLineLifespan = 5f;
  public float uiLineFadeOutTime = 1f;
  public Vector3 offScreenRelativePos = new Vector3(-100f, 0, 0);
  public string[] chatsToSubscribeTo = {"General"};

  public static ExitGames.Client.Photon.Chat.AuthenticationValues AuthVal;

  UiLinesData[] uiLinesData;
  public class UiLinesData {
    public UiLinesData(Vector3 startingPos) { this.startingPos = startingPos; }
    public string text = "";
    public float TTL = 0f;
    public Vector3 startingPos;
  }

  string[] history = new string[50];
  int lastIndex = -1;
  int firstIndex = -1;

  class User {
    public User(string userId, string username) {
       this.userId = userId;
       this.username = username;
    }
    public string userId = "";
    public string username = "";
  }
  List<User> knownUsers = new List<User>();

  float lastScrollTime = 0f;
  int currentIndex = 0;
  ChatClient chatClient;

  void Awake() {
    uiLinesData = new UiLinesData[uiLines.Length];
    currentIndex = history.Length;
    for (int i = 0; i < uiLinesData.Length; i++) {
      uiLinesData[i] =
          new UiLinesData(uiLines[i].rectTransform.anchoredPosition);
    }
  }

  void Update() {
    if (chatClient != null) {
      chatClient.Service();
    } else {
      Connect();
    }

    if (!GameData.isPaused) {
      foreach (UiLinesData l in uiLinesData) { l.TTL -= Time.deltaTime; }
    }

    if (GameData.isChatOpen) {
      if ((Input.GetButton("Scroll Up") &&
           Time.realtimeSinceStartup - lastScrollTime > 0.1) ||
          Input.GetAxis("Mouse ScrollWheel") > 0) {
        PushLinesDown();
        lastScrollTime = Time.realtimeSinceStartup;
      } else if ((Input.GetButton("Scroll Down") &&
                  Time.realtimeSinceStartup - lastScrollTime > 0.1) ||
                 Input.GetAxis("Mouse ScrollWheel") < 0) {
        PushLinesUp();
        lastScrollTime = Time.realtimeSinceStartup;
      }
      foreach (UiLinesData d in uiLinesData) {
        if (d.TTL < 0) d.TTL = 0;
      }
    }

    for (int i = 0; i < uiLines.Length; i++) {
      if (GameData.isChatOpen || uiLinesData[i].TTL >= 0) {
        uiLines[i].rectTransform.anchoredPosition = uiLinesData[i].startingPos;
      } else {
        uiLines[i].rectTransform.anchoredPosition =
            Vector3.Lerp(uiLinesData[i].startingPos,
                         uiLinesData[i].startingPos + offScreenRelativePos,
                         uiLinesData[i].TTL / -uiLineFadeOutTime);
      }
      uiLines[i].text = uiLinesData[i].text;
    }
    if(input.interactable != GameData.isChatOpen) {
      input.interactable = GameData.isChatOpen;
      if (GameData.isChatOpen) {
        OpenChat();
      } else {
        CloseChat();
      }
    }
  }

  public bool connected() {
    return chatClient != null && AuthVal != null &&
           chatClient.State == ChatState.ConnectedToFrontEnd;
  }

  void OpenChat() { input.ActivateInputField(); }

  void CloseChat() {
    input.DeactivateInputField();
    PushLinesUpAllTheWay();
  }

  void Connect() {
    if (AuthVal == null) return;

    chatClient = new ChatClient(this);
#if !UNITY_WEBGL
    chatClient.UseBackgroundWorkerForSending = true;
#endif
    if (!chatClient.Connect(PhotonNetwork.PhotonServerSettings.ChatAppID,
                            "0.1", AuthVal)) {
      Debug.LogWarning("Connecting to chat returned false!");
    } else {
      Debug.Log("Chat Connecting as: " + GameData.username);
      string[] message = {"Connecting to chat as: " + GameData.username};

      AddUsername("CityChunks", ref message[0]);
      OnGetMessages("Notice", new string[]{"CityChunks"}, message);
    }
  }

  void Subscribe() { chatClient.Subscribe(chatsToSubscribeTo); }

  public void SendPublicMessage(string message) {
    input.text = "";
    if (string.IsNullOrEmpty(message)) return;
    GameData.CloseChat();
    AddUsername(GameData.username, ref message);
    chatClient.PublishMessage("General", message);
  }
  public bool SendPrivateMessage(string target, string message) {
    Debug.LogError(
        "Sending private messages is not implemented and will not work unless " +
        "you know the specific userId of the person you are trying to send a " +
        "message to.");
    input.text = "";
    if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(target))
      return false;
    GameData.CloseChat();
    return chatClient.SendPrivateMessage(target, message);
  }

  void OnDestroy() {
    if (chatClient != null) {
      chatClient.Disconnect();
    }
  }
  public void OnConnected() {
    Debug.Log("Chat Connected!");
    Subscribe();
  }

  public void OnSubscribed(string[] channels, bool[] results) {
    string[] outputs = new string[channels.Length];
    string[] sender = new string[channels.Length];
    bool GeneralChat = false;
    for (int i = 0; i < channels.Length; i++) {
      sender[i] = "CityChunks";
      outputs[i] =
          string.Format("Connecting to channel: \"{0}\" {1}.", channels[i],
                        results[i] ? "succeeded" : "failed");
      if (channels[i] == "General") GeneralChat = true;
      AddUsername("CityChunks", ref outputs[i]);
      Debug.Log(outputs[i]);
    }
    OnGetMessages("Notice", sender, outputs);
    if (GeneralChat) {
      string[] message = {
          "General chat is public and anyone in any room can see your " +
          "messages"};

      AddUsername("CityChunks", ref message[0]);
      OnGetMessages("Notice", new string[]{"CityChunks"}, message);
    }
  }
  public void OnUnsubscribed(string[] channels) {
    string[] outputs = new string[channels.Length];
    string[] sender = new string[channels.Length];
    for (int i = 0; i < channels.Length; i++) {
      sender[i] = "CityChunks";
      outputs[i] = string.Format("Disconnected from {0}.", channels[i]);
    }
    OnGetMessages("Notice", sender, outputs);
  }

  public void OnGetMessages(string channelName, string[] senders,
                            object[] messages) {
    for (int i = 0; i < senders.Length; i++) {
      string[] messageParts = messages[i].ToString().Split('`');
      PutInHistory(string.Format("[{0}]<{1}>: {2}", channelName,
                                 messageParts[0], messageParts[1]));
      checkUser(senders[i], messageParts[0]);
    }
    PushLinesUp(false);
  }
  public void OnPrivateMessage(string sender, object message,
                               string channelName) {
    PutInHistory(
        string.Format("[{0}-Private]<{1}>:{2}", channelName, sender, message));
    PushLinesUp(false);
  }

  void PutInHistory(string input) {
    currentIndex++;
    if (firstIndex == history.Length - 1) firstIndex = -1;
    history[++firstIndex] = input;
    if (lastIndex == firstIndex || lastIndex == -1) lastIndex++;
    if (lastIndex == history.Length) lastIndex = 0;

    string debug = "";
    foreach (string s in history) { debug += s + "\n"; }
  }

  void GetFromHistory(ref string[] outs, int startIndex = 0) {
    for (int i = 0; i < outs.Length; i++) {
      int histIndex =
          (firstIndex + ((i + 1) * (history.Length - 1)) - startIndex + 1) %
          history.Length;
      if (!((histIndex <= lastIndex && histIndex > firstIndex) ||
            i > history.Length)) {
        outs[i] = history[histIndex];
      }
    }
  }

  public void PushLinesUpAllTheWay() {
    while (PushLinesUp()) {
    }
  }

  public bool PushLinesDown() {
    currentIndex++;
    if (currentIndex + uiLinesData.Length > history.Length ||
        getHistoryLength() - currentIndex < uiLinesData.Length) {
      currentIndex--;
      return false;
    }
    string[] currentLines = new string[uiLinesData.Length];
    GetFromHistory(ref currentLines, currentIndex);
    for (int i = 0; i < uiLines.Length; i++) {
      uiLinesData[i].TTL = uiLineLifespan;
      uiLinesData[i].text = currentLines[i];
    }
    return true;
  }
  public bool PushLinesUp(bool activateAll = true) {
    currentIndex--;
    if (currentIndex < 0) {
      currentIndex = 0;
      return false;
    }
    string[] currentLines = new string[uiLinesData.Length];
    GetFromHistory(ref currentLines, currentIndex);
    for (int i = uiLinesData.Length - 1; i >= 0; i--) {
      if (activateAll || i == 0) uiLinesData[i].TTL = uiLineLifespan;
      else uiLinesData[i].TTL = uiLinesData[i - 1].TTL;
      uiLinesData[i].text = currentLines[i];
    }
    return true;
  }

  int getHistoryLength() {
    if (firstIndex >= lastIndex) return firstIndex + 1;
    else return history.Length;
  }

  void AddUsername(string username, ref string message) {
    message = message.Replace('`', '\'');
    message = username + "`" + message;
  }

  void checkUser(string userId, string username) {
    bool knownUser = false;
    for (int i = 0; i < knownUsers.Count; i++) {
      if (knownUsers[i].userId == userId) {
        knownUser = true;
        if (knownUsers[i].username != username) {
          Debug.Log("User: " + userId + ", Changed username from " +
                    knownUsers[i].username + " to " + username);
          knownUsers[i].username = username;
        }
      }
    }
    if (!knownUser) {
      knownUsers.Add(new User(userId, username));
    }
  }

  // Overrides
  public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string text) {}
  public void OnDisconnected() {}
  public void OnChatStateChange(ExitGames.Client.Photon.Chat.ChatState state) {}
  public void OnStatusUpdate(string text, int num, bool tf, object obj) {}
}
