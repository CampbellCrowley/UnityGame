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

  void Start() { Connect(); }

  void Update() {
    if (chatClient != null) {
      chatClient.Service();
    } else {
      Start();
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

  public
   void OpenChat() { input.ActivateInputField(); }

  public
   void CloseChat() {
     input.DeactivateInputField();
     PushLinesUpAllTheWay();
   }

   void Connect() {
     chatClient = new ChatClient(this);
#if !UNITY_WEBGL
    chatClient.UseBackgroundWorkerForSending = true;
#endif
    chatClient.Connect(PhotonNetwork.PhotonServerSettings.ChatAppID, "0.1",
                       new ExitGames.Client.Photon.Chat.AuthenticationValues(
                           GameData.username));

    Debug.Log("Connecting as: " + GameData.username);
  }

  void Subscribe() { chatClient.Subscribe(chatsToSubscribeTo); }

 public
  void SendPublicMessage(string message) {
    if(string.IsNullOrEmpty(message)) return;
    chatClient.PublishMessage("General", message);
    input.text = "";
    GameData.CloseChat();
  }
 public
  bool SendPrivateMessage(string target, string message) {
    return chatClient.SendPrivateMessage(target, message);
 }

  void OnDestroy() {
    if (chatClient != null) {
      chatClient.Disconnect();
    }
  }
	public void OnConnected() {
    Subscribe();
  }

  public void OnSubscribed(string[] channels, bool[] results) {
    string[] outputs = new string[channels.Length];
    string[] sender = new string[channels.Length];
    for (int i = 0; i < channels.Length; i++) {
      sender[i] = "Server";
      outputs[i] = string.Format("Connecting to {0} {1}.", channels[i],
                                 results[i] ? "succeeded" : "failed");
    }
    OnGetMessages("Info", sender, outputs);
  }
  public void OnUnsubscribed(string[] channels) {
    string[] outputs = new string[channels.Length];
    string[] sender = new string[channels.Length];
    for (int i = 0; i < channels.Length; i++) {
      sender[i] = "Server";
      outputs[i] = string.Format("Disconnected from {0}.", channels[i]);
    }
    OnGetMessages("Info", sender, outputs);
  }

  public void OnGetMessages(string channelName, string[] senders,
                            object[] messages) {
    for (int i = 0; i < senders.Length; i++) {
      PutInHistory(string.Format("[{0}]<{1}>: {2}", channelName, senders[i],
                                 messages[i]));
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


  // Overrides
  public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string text) {}
  public void OnDisconnected() {}
  public void OnChatStateChange(ExitGames.Client.Photon.Chat.ChatState state) {}
  public void OnStatusUpdate(string text, int num, bool tf, object obj) {}
}
