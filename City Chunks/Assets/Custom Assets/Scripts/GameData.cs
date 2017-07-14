using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameData : MonoBehaviour {

  public const string TerrainGeneratorVersion = "v0.0.6";
  public const string MultiplayerVersion = "m_4";
 
  public static GameData Instance;

  public AudioSource MusicPlayer;
  public AudioClip QueuedMusic;
  public GameObject PauseMenu;
  private GameObject PauseMenu_;
 
  void Awake() {
    if (Instance == null) {
      MusicPlayer = GetComponent<AudioSource>();
      DontDestroyOnLoad(gameObject);
      Instance = this;
    } else if (Instance != this) {
      if (GetComponent<AudioSource>().clip != null) {
        Instance.QueuedMusic = GetComponent<AudioSource>().clip;
      } else if (QueuedMusic != null) {
        Instance.QueuedMusic = QueuedMusic;
      }
      Destroy(gameObject);
    }
  }
  void Start() {
    LoadSettings();
    if (MusicPlayer != null && !music) {
      MusicPlayer.volume = 0.0f;
    }
  }
  public static int health = 100;
  public static int tries = 3;
  public static int collectedCollectibles = 10000;
  public static bool showCursor = true;
  public static bool isChatOpen = false;
  public static bool isPaused = false;
  public static VehicleController Vehicle;
  public static string username = "Username";
  public static int numEnemies = 0;
  public static int numVehicles = 0;
  public static bool loading = false;
  public static float loadingPercent = 1f;
  public static string previousLoadingMessage = "Readying the pigeons.";
  public static string loadingMessage = "Readying the pigeons.";
  public static bool LoadingScreenExists = false;
 
  private static float loadEndTime = -1;
 
  public static int getLevel() {
    return SceneManager.GetActiveScene().buildIndex;
  }

  public static void AddLoadingScreen() {
    if (LoadingScreenExists) return;
    Debug.Log("Additively loading loading screen scene.");
    loading = true;
    SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
    LoadingScreenExists = true;
  }

  public static void RemoveLoadingScreen() {
    Debug.Log("Unloading loading screen scene.");
    GameObject[] toUnload = GameObject.FindGameObjectsWithTag("LoadingScene");
    loadEndTime = Time.time;
    foreach (GameObject g in toUnload) { Destroy(g, 0.1f); }
    LoadingScreenExists = false;
  }

  public static void PlayGame() {
    Debug.Log("Play Game!");
  }
  public static void MainMenu() {
    Debug.Log("Menu!");
    GameData.Vehicle = null;
    GameData.isPaused = false;
    GameData.isChatOpen = false;
    if (FindObjectOfType<TerrainGenerator>() != null)
      FindObjectOfType<TerrainGenerator>().SaveAllChunks();
    if (PhotonNetwork.room != null) NetworkManager.LeaveRoom();
    else SceneManager.LoadScene("Menu");
    GameData.health = 100;
    RemoveLoadingScreen();
  }

  public static void quit() {
    Debug.Log("Exiting Game");
    Application.Quit();
  }
 
  void Update() {
 #if UNITY_EDITOR || UNITY_STANDALONE
    if (getLevel() == 0 || isPaused) {
      Application.targetFrameRate = 30;
    } else {
      Application.targetFrameRate = -1;
    }
 #endif
    if (Time.time - loadEndTime > 1 &&
        Time.time - loadEndTime - Time.deltaTime < 1 && loading &&
        loadEndTime != -1) {
      loading = false;
    }
    if (Input.GetButtonDown("Pause") && getLevel() != 0 &&
        (TerrainGenerator.doneLoadingSpawn || !TerrainGenerator.Enabled)) {
      if (isChatOpen) {
        isChatOpen = false;
        GameData.showCursor = false;
      } else {
        GameData.isPaused = !GameData.isPaused;
        GameData.showCursor = isPaused;
        if (GameData.isPaused) {
          PauseMenu_ = Instantiate(PauseMenu);
          // PauseMenu_.GetComponent<Canvas>().worldCamera = Camera.main;
        } else {
          Destroy(PauseMenu_);
        }
      }
    } else if (Input.GetButtonDown("OpenChat") && getLevel() != 0 &&
               !isPaused) {
      isChatOpen = true;
      GameData.showCursor = true;
    } else if (Input.GetButtonDown("Menu") && getLevel() != 0) {
      MainMenu();
    }
    Cursor.visible = showCursor;
    Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;

    if (MusicPlayer != null) {
      float goalVol = music ? 0.5f : 0.0f;
      if(QueuedMusic != null && QueuedMusic != MusicPlayer.clip) {
        goalVol = 0.0f;
        if(MusicPlayer.volume <= 0.001f) {
          Debug.Log("Playing Music: " + QueuedMusic.name);
          MusicPlayer.clip = QueuedMusic;
          MusicPlayer.Play();
        }
      }
      MusicPlayer.volume = Mathf.Lerp(MusicPlayer.volume, goalVol, 0.1f);
    }
  }
 
  public static void LoadSettings() {
    string debug = "Settings Loaded: [\n";
    if (PlayerPrefs.HasKey("Vignette")) {
      vignette = PlayerPrefs.GetInt("Vignette") == 1;
      debug += "Vignette: " + vignette + ",\n";
    }
 
    if (PlayerPrefs.HasKey("DOF")) {
      dof = PlayerPrefs.GetInt("DOF") == 1;
      debug += "DOF: " + dof + ",\n";
    }
 
    if (PlayerPrefs.HasKey("Motion Blur")) {
      motionBlur = PlayerPrefs.GetInt("Motion Blur") == 1;
      debug += "Motion Blur: " + motionBlur + ",\n";
    }
 
    if (PlayerPrefs.HasKey("Bloom and Flare")) {
      bloomAndFlares =
          PlayerPrefs.GetInt("Bloom and Flare") == 1;
      debug += "Bloom and Flare: " + bloomAndFlares + ",\n";
    }
 
    if (PlayerPrefs.HasKey("Fullscreen")) {
      fullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
      debug += "Fullscreen: " + fullscreen + ",\n";
    }
 
    if (PlayerPrefs.HasKey("Sound Effects")) {
      soundEffects = PlayerPrefs.GetInt("Sound Effects") == 1;
      debug += "Sound Effects: " + soundEffects + ",\n";
    }
 
    if (PlayerPrefs.HasKey("Music")) {
      music = PlayerPrefs.GetInt("Music") == 1;
      debug += "Music: " + music + ",\n";
    }
 
    if (PlayerPrefs.HasKey("Camera Damping")) {
      cameraDamping = PlayerPrefs.GetInt("Camera Damping") == 1;
      debug += "Camera Damping: " + cameraDamping + ",\n";
    }
 
    debug += "]";
    Debug.Log(debug);
 
    Screen.fullScreen = fullscreen;
  }
 
  public static void SaveSettings() {
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
 
  public static bool vignette = true;
  public static bool dof = true;
  public static bool motionBlur = true;
  public static bool bloomAndFlares = false;
  public static bool fullscreen = true;
  public static bool soundEffects = true;
  public static bool music = true;
  public static bool cameraDamping = true;
}
