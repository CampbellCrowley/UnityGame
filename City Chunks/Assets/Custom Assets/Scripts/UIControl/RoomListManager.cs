// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

class RoomListManager : MonoBehaviour {
  public Row rowTemplate;
  public RectTransform content;
  List<Row> rows = new List<Row>();
  bool updated = false;

  void Start() {
    if (rowTemplate == null || content == null) {
      Debug.LogError("Values may not be empty!");
      enabled = false;
    } else {
      Refresh();
    }
  }
  void Update() {
    if (!updated && NetworkManager.roomsList != null &&
        NetworkManager.roomsList.Length != 0) {
      updated = true;
      Refresh();
    }
  }
  public void Refresh() {
    for (int i = 0; i < rows.Count; i++) {
      Destroy(rows[i].gameObject);
      rows.RemoveAt(i);
      i--;
    }
    content.sizeDelta = new Vector2(content.sizeDelta.x, 0);
    if (NetworkManager.roomsList == null ||
        NetworkManager.roomsList.Length == 0) {
      for (int i = 0; i < 1; i++) {
        Row newRow =
            Instantiate(rowTemplate, rowTemplate.transform.parent, false);
        newRow.transform.name = "Room" + (i + 1);
        newRow.description = "No available rooms to join.";
        newRow.joinButton.interactable = false;
        newRow.GetComponent<RectTransform>().anchoredPosition +=
            Vector2.down * 50f * (i + 1);
        newRow.UpdateRow();
        content.sizeDelta += Vector2.up * 50f;
        rows.Add(newRow);
      }
    } else {
      for (int i = 0; i < NetworkManager.roomsList.Length; i++) {
        Row newRow =
            Instantiate(rowTemplate, rowTemplate.transform.parent, false);
        newRow.transform.name = "Room" + (i + 1);
        newRow.description =
            "Creator: " +
            NetworkManager.roomsList[i].Name.Substring(
                0, NetworkManager.roomsList[i].Name.IndexOf('`'));
        newRow.GetComponent<RectTransform>().anchoredPosition +=
            Vector2.down * 50f * (i + 1);
        newRow.numPlayersConnected = NetworkManager.roomsList[i].PlayerCount;
        newRow.numMaxPlayers = NetworkManager.roomsList[i].MaxPlayers;
        newRow.roomName = NetworkManager.roomsList[i].Name;
        newRow.joinButton.interactable = true;
        newRow.UpdateRow();
        content.sizeDelta += Vector2.up * 50f;
        rows.Add(newRow);
      }
    }
  }
}
