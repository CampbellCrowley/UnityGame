// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;

class Row : MonoBehaviour {
  [HideInInspector] public string description = "";
  [HideInInspector] public int numPlayersConnected = 0;
  [HideInInspector] public int numMaxPlayers = 0;
  [HideInInspector] public string roomName = "Oopsies";
  public Text descriptionTextBox;
  public Text playersTextBox;
  public Button joinButton;

  void Start() {
    UpdateRow();
    if (joinButton != null) joinButton.onClick.AddListener(OnClick);
  }

  public void UpdateRow() {
    if (descriptionTextBox != null) descriptionTextBox.text = description;
    if (playersTextBox != null)
      playersTextBox.text = numPlayersConnected + "/" + numMaxPlayers;
  }

  void OnClick() { NetworkManager.JoinRoom(roomName); }
}
