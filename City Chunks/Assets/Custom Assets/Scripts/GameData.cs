using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum QuitReason {
   NORMAL,
   UNEXPECTED,
   RANDMISMATCH,
   GENUINECHECKFAIL,
   VERSIONMISMATCH
};

public class GameData : MonoBehaviour {
  public const string version = TerrainGenerator.version + NetworkManager.version +
                          CityGenerator.version;

  public static GameData Instance;

  public AudioSource MusicPlayer;
  public AudioClip QueuedMusic;
  public GameObject PauseMenu;

  private static GameObject PauseMenu_;
  private static ChatManager chatManager;

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
    chatManager = FindObjectOfType<ChatManager>();
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
  public static QuitReason quitReason = QuitReason.UNEXPECTED;

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

  public static void OpenChat() {
    if (isChatOpen) return;
    if (chatManager != null && !chatManager.connected()) return;
    isChatOpen = true;
    GameData.showCursor = true;
  }

  public static void CloseChat() {
    if (!isChatOpen) return;
    isChatOpen = false;
    GameData.showCursor = false;
  }

  public static void TogglePaused(bool force = false, bool paused = false) {
    if (force) {
      if (paused == GameData.isPaused) return;
      GameData.isPaused = paused;
    } else {
      GameData.isPaused = !GameData.isPaused;
    }
    GameData.showCursor = isPaused;
    if (GameData.isPaused) {
      PauseMenu_ = Instantiate(Instance.PauseMenu);
    } else {
      Destroy(PauseMenu_);
    }
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

  public static bool isUsernameDefault() {
    return GameData.username.ToLower() == "username" ||
           GameData.username == "";
  }
  public static bool isUsernameValid() {
    if (GameData.username.IndexOf('`') != -1) return false;
    return GameData.username.ToLower() != "citychunks";
  }

  public static void quit(QuitReason reason = QuitReason.NORMAL) {
    Debug.LogWarning("Exiting Game (" + reason + ")");
    quitReason = reason;
    if (quitReason != QuitReason.NORMAL) {
      CustomDebug.isEnabled = true;
      CustomDebug.pauseExecutionEnabled = true;
      CustomDebug.Assert(false, quitReason + ": See log for more info.");
    }
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
  }

  void OnApplicationQuit() { Debug.Log("Application Quitting"); }

  void Update() {
#if UNITY_EDITOR || UNITY_STANDALONE
    if (getLevel() == 0 || isPaused) {
      Application.targetFrameRate = 30;
    } else {
      Application.targetFrameRate = -1;
    }
#endif
    if (Time.time - loadEndTime > 1 &&
        Time.time - loadEndTime - Time.deltaTime < 1 && loadEndTime != -1) {
      loading = false;
    }
    if (loadingMessage == "Readying the pigeons.") {
      loadingPercent = Mathf.PingPong(Time.time / 2, 1);
    }
    if (chatManager == null) chatManager = FindObjectOfType<ChatManager>();
    if (Input.GetButtonDown("Pause")) {
      if (isChatOpen) {
        CloseChat();
      } else if (!TerrainGenerator.loadingSpawn) {
        TogglePaused();
      }
    } else if (Input.GetButtonDown("OpenChat") && getLevel() != 0 &&
               !isPaused) {
      OpenChat();
    } else if (isPaused && Input.GetButtonDown("Menu") && getLevel() != 0) {
      MainMenu();
    }
    Cursor.visible = showCursor || isPaused || getLevel() == 0;
    Cursor.lockState =
        Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

    if (MusicPlayer != null) {
      float goalVol = music ? 0.5f : 0.0f;
      if (QueuedMusic != null && QueuedMusic != MusicPlayer.clip) {
        goalVol = 0.0f;
        if (MusicPlayer.volume <= 0.001f) {
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

    if (PlayerPrefs.HasKey("Color Grading")) {
      colorGrading = PlayerPrefs.GetInt("Color Grading") == 1;
      debug += "Color Grading: " + colorGrading + ",\n";
    }

    fullscreen = Screen.fullScreen;
    debug += "Fullscreen: " + fullscreen + ",\n";
    // if (PlayerPrefs.HasKey("Fullscreen")) {
    //   fullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
    //   debug += "Fullscreen: " + fullscreen + ",\n";
    // }

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

    if (PlayerPrefs.HasKey("Load Distance")) {
      LoadDistance = PlayerPrefs.GetFloat("Load Distance");
      if (LoadDistance < LoadDistanceSelector.minDistance) {
        LoadDistance = LoadDistanceSelector.minDistance;
      } else if (LoadDistance > LoadDistanceSelector.maxDistance) {
        LoadDistance = LoadDistanceSelector.maxDistance;
      }
      debug += "Load Distance: " + LoadDistance + ",\n";
    }

    if (PlayerPrefs.HasKey("Grass Density")) {
      GrassDensity = PlayerPrefs.GetFloat("Grass Density");
      if (GrassDensity < 0) {
        GrassDensity = 0f;
      } else if (GrassDensity > 1) {
        GrassDensity = 1f;
      }
      debug += "Grass Density: " + GrassDensity + ",\n";
    }

    if (PlayerPrefs.HasKey("Mouse Sensitivity")) {
      mouseSensitivity = PlayerPrefs.GetFloat("Mouse Sensitivity");
      if (mouseSensitivity < 0) {
        mouseSensitivity = 0f;
      } else if (mouseSensitivity > 10) {
        mouseSensitivity = 10f;
      }
      debug += "Mouse Sensitivity: " + mouseSensitivity + ",\n";
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
    PlayerPrefs.SetInt("Color Grading", colorGrading ? 1 : 0);
    PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
    PlayerPrefs.SetInt("Sound Effects", soundEffects ? 1 : 0);
    PlayerPrefs.SetInt("Music", music ? 1 : 0);
    PlayerPrefs.SetInt("Camera Damping", cameraDamping ? 1 : 0);
    PlayerPrefs.SetFloat("Load Distance", LoadDistance);
    PlayerPrefs.SetFloat("Grass Density", GrassDensity);
    PlayerPrefs.SetFloat("Mouse Sensitivity", mouseSensitivity);

    PlayerPrefs.Save();
  }

  public static bool vignette = true;
  public static bool dof = true;
  public static bool motionBlur = true;
  public static bool bloomAndFlares = true;
  public static bool colorGrading = true;
  public static bool fullscreen = true;
  public static bool soundEffects = true;
  public static bool music = true;
  public static bool cameraDamping = false;
  public static float LoadDistance = 1500.0f;
  public static float GrassDensity = 1.0f;
  public static float mouseSensitivity = 1.0f;
}
