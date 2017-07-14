// using UnityEngine;
// using UnityEngine.UI;
// using ExitGames.Client.Photon.Chat;
// using System;
// using System.Collections.Generic;
// 
// public class ChatManager : MonoBehaviour, IChatClientListener {
// 
//   public InputField input;
// 	public ChatClient chatClient;
// 
//   void Start() {
//     bool _AppIdPresent =
//         string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.ChatAppID);
//     if (string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.ChatAppID)) {
//       Debug.LogError(
//           "You need to set the chat app ID in the PhotonServerSettings file " +
//           "in order to continue.");
//       return;
//     }
//   }
// 
//   void Update() {
//     if (this.chatClient != null) {
//       this.chatClient.Service();
//     }
//   }
// 
//   void Connect() {
//     chatClient = new ChatClient(this);
// #if !UNITY_WEBGL
//     chatClient.UseBackgroundWorkerForSending = true;
// #endif
//     // chatClient.Connect(PhotonNetwork.PhotonServerSettings.ChatAppID, "0.1");
// 
//     Debug.Log("Connecting as: " + GameData.username);
//   }
// 
//  public
//   void SendMessage(string message) {
//     chatClient.PublishMessage("Chat", message);
//     input.text = "";
//   }
// 
//   void OnDestroy() {
//     if (this.chatClient != null) {
//       chatClient.Disconnect();
//     }
//   }
// 	public void OnConnected() {
//     chatClient.Subscribe("Chat", 0);
//   }
//   void OnDisconnected() {}
//   void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message) {
//     if (level == ExitGames.Client.Photon.DebugLevel.ERROR) {
//       UnityEngine.Debug.LogError(message);
//     } else if (level == ExitGames.Client.Photon.DebugLevel.WARNING) {
//       UnityEngine.Debug.LogWarning(message);
//     } else {
//       UnityEngine.Debug.Log(message);
//     }
// 	}
// }
