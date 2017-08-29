using System;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable 0168

public
class SettingsController : MonoBehaviour {
 private
  bool overrideToggle = false;

 public
  void Start() {
    overrideToggle = true;
    Toggle temp = GameObject.Find("Toggle Vignette").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.vignette;
    temp = GameObject.Find("Toggle DOF").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.dof;
    temp = GameObject.Find("Toggle Motion Blur").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.motionBlur;
    temp = GameObject.Find("Toggle Bloom and Flare").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.bloomAndFlares;
    temp = GameObject.Find("Toggle Fullscreen").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.fullscreen;
    temp = GameObject.Find("Toggle Sound Effects").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.soundEffects;
    temp = GameObject.Find("Toggle Music").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.music;
    temp = GameObject.Find("Toggle Camera Damping").GetComponent<Toggle>();
    if (temp != null) temp.isOn = GameData.cameraDamping;
    overrideToggle = false;
  }

 public
  void ToggleVignette() {
    if (overrideToggle) return;
    GameData.vignette = !GameData.vignette;
  }
 public
  void ToggleDOF() {
    if (overrideToggle) return;
    GameData.dof = !GameData.dof;
  }
 public
  void ToggleMotionBlur() {
    if (overrideToggle) return;
    GameData.motionBlur = !GameData.motionBlur;
  }
 public
  void ToggleBloomAndFlare() {
    if (overrideToggle) return;
    GameData.bloomAndFlares = !GameData.bloomAndFlares;
  }
 public
  void ToggleFullscreen() {
    if (overrideToggle) return;
    GameData.fullscreen = !GameData.fullscreen;
    Screen.fullScreen = GameData.fullscreen;
  }
 public
  void ToggleSoundEffects() {
    if (overrideToggle) return;
    GameData.soundEffects = !GameData.soundEffects;
  }
 public
  void ToggleMusic() {
    if (overrideToggle) return;
    GameData.music = !GameData.music;
  }
 public
  void ToggleCameraDamping() {
    if (overrideToggle) return;
    GameData.cameraDamping = !GameData.cameraDamping;
  }
 public void Quit() {
    GameData.quit();
 }
 public
  void Exit() {
    GameData.isPaused = false;
    GameData.showCursor = false;
    Destroy(gameObject);
  }
  void OnDestroy() { GameData.SaveSettings(); }
}

