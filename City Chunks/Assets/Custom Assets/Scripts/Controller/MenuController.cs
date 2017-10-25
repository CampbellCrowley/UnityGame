// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {
  public InputField UsernameInput;
  void Start() { GameData.showCursor = true; }

  public void UpdateUsername() { GameData.username = UsernameInput.text; }
  public void MainMenu() { GameData.MainMenu(); }
  public void CreateRoom() { NetworkManager.CreateRoom("Game"); }
  public void JoinRoom(string name) { NetworkManager.CreateRoom(name); }
  public void OpenSettings() { GameData.TogglePaused(true, true); }
  public void CloseSettings() { GameData.TogglePaused(true, false); }
  public void ToggleSettings() { GameData.TogglePaused(); }
  public void quitGame() { GameData.quit(); }
}
