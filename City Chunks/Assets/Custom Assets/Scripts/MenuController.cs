using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {
 public void Start() { GameData.showCursor = true; }

 public void MainMenu() { GameData.MainMenu(); }
 public void PlayGame() { GameData.PlayGame(); }
 public void OpenSettings() { GameData.TogglePaused(true, true); }
 public void CloseSettings() { GameData.TogglePaused(true, false); }
 public void ToggleSettings() { GameData.TogglePaused(); }
 public void quitGame() { GameData.quit(); }
}
