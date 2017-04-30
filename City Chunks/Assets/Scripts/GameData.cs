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
  static int collectedCollectibles = 0;
 public
  static bool showCursor = true;
 public
  static bool isPaused = false;

 public
  static string username = "Username";

 public
  static bool levelComplete() { return false; }
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
    Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
    /*if (Input.GetAxis("Skip") > 0.5f) {
      nextLevel();
    }*/
    if (MusicPlayer != null) MusicPlayer.volume = music ? 0.5f : 0.0f;
  }

 public
  static void LoadSettings() {
    string debug = "Settings Loaded: [\n";
    if (PlayerPrefs.HasKey("Vignette")) {
      vignette = PlayerPrefs.GetInt("Vignette") == 1 ? true : false;
      debug += "Vignette: " + vignette + ",\n";
    }

    if (PlayerPrefs.HasKey("DOF")) {
      dof = PlayerPrefs.GetInt("DOF") == 1 ? true : false;
      debug += "DOF: " + dof + ",\n";
    }

    if (PlayerPrefs.HasKey("Motion Blur")) {
      motionBlur = PlayerPrefs.GetInt("Motion Blur") == 1 ? true : false;
      debug += "Motion Blur: " + motionBlur + ",\n";
    }

    if (PlayerPrefs.HasKey("Bloom and Flare")) {
      bloomAndFlares =
          PlayerPrefs.GetInt("Bloom and Flare") == 1 ? true : false;
      debug += "Bloom and Flare: " + bloomAndFlares + ",\n";
    }

    if (PlayerPrefs.HasKey("Fullscreen")) {
      fullscreen = PlayerPrefs.GetInt("Fullscreen") == 1 ? true : false;
      debug += "Fullscreen: " + fullscreen + ",\n";
    }

    if (PlayerPrefs.HasKey("Sound Effects")) {
      soundEffects = PlayerPrefs.GetInt("Sound Effects") == 1 ? true : false;
      debug += "Sound Effects: " + soundEffects + ",\n";
    }

    if (PlayerPrefs.HasKey("Music")) {
      music = PlayerPrefs.GetInt("Music") == 1 ? true : false;
      debug += "Music: " + music + ",\n";
    }

    if (PlayerPrefs.HasKey("Camera Damping")) {
      cameraDamping = PlayerPrefs.GetInt("Camera Damping") == 1 ? true : false;
      debug += "Camera Damping: " + cameraDamping + ",\n";
    }

    debug += "]";
    Debug.Log(debug);

    Screen.fullScreen = fullscreen;
  }

 public
  static void SaveSettings() {
    PlayerPrefs.SetInt("Vignette", vignette ? 1 : 0);
    PlayerPrefs.SetInt("DOF", dof ? 1 : 0);
    PlayerPrefs.SetInt("Motion Blur", motionBlur ? 1 : 0);
    PlayerPrefs.SetInt("Bloom and Flare", bloomAndFlares ? 1 : 0);
    PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
    PlayerPrefs.SetInt("Sound Effects", soundEffects ? 1 : 0);
    PlayerPrefs.SetInt("Music", music ? 1 : 0);
    PlayerPrefs.SetInt("Camera Damping", cameraDamping ? 1 : 0);

    PlayerPrefs.Save();
  }

 public
  static bool vignette = true;
 public
  static bool dof = true;
 public
  static bool motionBlur = true;
 public
  static bool bloomAndFlares = false;
 public
  static bool fullscreen = true;
 public
  static bool soundEffects = true;
 public
  static bool music = true;
 public
  static bool cameraDamping = true;
}
