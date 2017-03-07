using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public
class GameData : MonoBehaviour {
 public
  static GameData Instance;
 public
  static AudioSource MusicPlayer;
 public
  void Awake() {
    if (Instance == null) {
      DontDestroyOnLoad(gameObject);
      Instance = this;
    } else if (Instance != this) {
      Destroy(gameObject);
    }
    MusicPlayer = GetComponent<AudioSource>();
  }
 public
  static int health = 5;
 public
  static bool showCursor = false;
 public
  static bool isPaused = false;

 public
  static int getLevel() { return SceneManager.GetActiveScene().buildIndex; }
 public
  static void nextLevel() {
    Debug.Log("Next Level!");
    SceneManager.LoadScene(getLevel() + 1);
  }
 public
  static void restartLevel() {
    Debug.Log("Restarting Level!");
    SceneManager.LoadScene(getLevel());
  }
 public
  static void MainMenu() {
    Debug.Log("Menu!");
    SceneManager.LoadScene(0);
  }
 public
  static void quit() {
    Debug.Log("Exiting Game");
    Application.Quit();
  }

 public
  void Update() {
    Cursor.visible = showCursor;
    if (Input.GetAxis("Skip") > 0.5f) {
      nextLevel();
    }
    if (MusicPlayer != null) MusicPlayer.volume = music ? 0.5f : 0.0f;
  }

  public static bool vignette = true;
  public static bool dof = true;
  public static bool motionBlur = true;
  public static bool bloomAndFlares = false;
  public static bool fullscreen = true;
  public static bool soundEffects = true;
  public static bool music = true;
  public static bool cameraDamping = true;
}
