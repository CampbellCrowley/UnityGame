#if ENABLE_UNET

namespace UnityEngine.Networking {
[AddComponentMenu("Network/NetworkManagerHUD")]
[RequireComponent(typeof(NetworkManager))]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]

public class MyNetworkManagerHUD : MonoBehaviour {

  [SerializeField]
  public bool showGUI = true;
  public NetworkManager manager;
  private string tempPort;

  void Awake() {
    manager = GetComponent<NetworkManager>();
    tempPort = manager.networkPort.ToString();
  }

  void Update() {
    if (!showGUI) return;

    if (GetComponent<NetworkMigrationManager>().disconnectedFromHost) {
      GameData.showCursor = true;
    }

    if (!NetworkClient.active && !NetworkServer.active &&
        manager.matchMaker == null) {
      if (Input.GetKeyDown(KeyCode.S)) {
        manager.StartServer();
      }
      if (Input.GetKeyDown(KeyCode.H)) {
        manager.StartHost();
      }
      if (Input.GetKeyDown(KeyCode.C)) {
        manager.StartClient();
      }
    }
    if (NetworkServer.active && NetworkClient.active) {
      if (GameData.isPaused && Input.GetKeyDown(KeyCode.X)) {
        GameData.isPaused = false;
        FindObjectOfType<TerrainGenerator>().SaveAllChunks();
        manager.StopHost();
      }
    }
  }

  public static void changeLevel(SceneManagement.Scene level) {
     Debug.Log("Changing Level to " + level.name);
     FindObjectOfType<UnityEngine.Networking.NetworkManager>().ServerChangeScene(
         level.name);
   }

  public static string getLevel() { return NetworkManager.networkSceneName; }

  public void OnGUI() {
    if (!showGUI) return;

    int xposCentered = Screen.width / 2 - 125;
    int yposCentered = Screen.height / 2 - 100;
    int xpos = 10;
    int ypos = 20;
    int spacing = 50;

    if (!NetworkClient.active && !NetworkServer.active &&
        manager.matchMaker == null) {
      GameData.username = GUI.TextField(
          new Rect(xposCentered + 25, yposCentered, 200, 30), GameData.username);
      GUI.contentColor = Color.black;
      GUI.Label(new Rect(xposCentered + 25 + 200 + 5, yposCentered - 7, 200, 45),
                "Leaving this as \"Username\" will assign a random name");
      GUI.contentColor = Color.white;

      yposCentered += spacing;

      if (GUI.Button(new Rect(xposCentered, yposCentered, 250, 30),
                     "Host Game / SinglePlayer")) {
        manager.StartHost();
      }

      yposCentered += spacing * 2;

      manager.networkAddress =
          GUI.TextField(new Rect(xposCentered + 25, yposCentered, 200, 30),
                        manager.networkAddress);
      tempPort = GUI.TextField(
          new Rect(xposCentered + 25 + 200 + 5, yposCentered, 50, 30), tempPort);
      GUI.contentColor = Color.black;
      GUI.skin.label.fontSize += 20;
      GUI.Label(new Rect(xposCentered + 25 + 200, yposCentered, 10, 30), ":");
      GUI.contentColor = Color.white;
      GUI.skin.label.fontSize -= 20;

      int output = 7777;
      if (int.TryParse(tempPort, out output)) {
        manager.networkPort = output;

        yposCentered += spacing / 2 + 5;

        if (GUI.Button(new Rect(xposCentered + 25, yposCentered, 200, 30),
                       "Connect to Game")) {
          manager.StartClient();
        }
      } else {
        yposCentered += spacing / 2 + 5;
        GUI.contentColor = Color.red;
        GUI.Label(new Rect(xposCentered + 25, yposCentered, 200, 45),
                  "Port may not be empty and must be an integer. Default: 7777");
        GUI.contentColor = Color.white;
      }

      /*if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Server Only(S)"))
      {
        manager.StartServer();
      }*/
      yposCentered += spacing;
    } else {
      if (NetworkServer.active) {
        int numConnected = 0;
        foreach (NetworkConnection con in NetworkServer.connections) {
          if (con != null) numConnected++;
        }
        GUI.contentColor = GameData.isPaused ? Color.white : Color.black;
        GUI.Label(new Rect(xpos, ypos, 300, 40),
                  "Server: Local= " + Network.player.ipAddress + " port=" +
                      manager.networkPort + "\nPlayers: " + numConnected);
        ypos += spacing;
      } else if (NetworkClient.active) {
        GUI.contentColor = GameData.isPaused ? Color.white : Color.black;
        GUI.Label(new Rect(xpos, ypos, 300, 20),
                  "Client: address=" + manager.networkAddress + " port=" +
                      manager.networkPort);
        ypos += spacing;
      }
    }

    if (NetworkClient.active && !ClientScene.ready) {
      GUI.contentColor = GameData.isPaused ? Color.white : Color.black;
      if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Client Ready")) {
        ClientScene.Ready(manager.client.connection);
        if (ClientScene.localPlayers.Count == 0) {
          ClientScene.AddPlayer(0);
        }
      }
      ypos += spacing;
    }

    if (NetworkServer.active || NetworkClient.active) {
      if ((GameData.isPaused || GameData.getLevel() == 5) &&
          GUI.Button(new Rect(xpos, ypos, 200, 20), "Return to Main Menu (X)")) {
        GameData.isPaused = false;
        FindObjectOfType<TerrainGenerator>().SaveAllChunks();
        manager.StopHost();
      }
      ypos += spacing;
    }

    /* if (!NetworkServer.active && !NetworkClient.active)
    {
      ypos += 10;

      if (manager.matchMaker == null)
      {
        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Enable Match Maker (M)"))
        {
          manager.StartMatchMaker();
        }
        ypos += spacing;
      }
      else
      {
        if (manager.matchInfo == null)
        {
          if (manager.matches == null)
          {
            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Create Internet
    Match"))
            {
              manager.matchMaker.CreateMatch(manager.matchName,
    manager.matchSize, true, "", manager.OnMatchCreate);
            }
            ypos += spacing;

            GUI.Label(new Rect(xpos, ypos, 100, 20), "Room Name:");
            manager.matchName = GUI.TextField(new Rect(xpos+100, ypos, 100, 20),
    manager.matchName);
            ypos += spacing;

            ypos += 10;

            if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Find Internet Match"))
            {
              manager.matchMaker.ListMatches(0,20, "", manager.OnMatchList);
            }
            ypos += spacing;
          }
          else
          {
            foreach (var match in manager.matches)
            {
              if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Join Match:" +
    match.name))
              {
                manager.matchName = match.name;
                manager.matchSize = (uint)match.currentSize;
                manager.matchMaker.JoinMatch(match.networkId, "",
    manager.OnMatchJoined);
              }
              ypos += spacing;
            }
          }
        }

        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Change MM server"))
        {
          showServer = !showServer;
        }
        if (showServer)
        {
          ypos += spacing;
          if (GUI.Button(new Rect(xpos, ypos, 100, 20), "Local"))
          {
            manager.SetMatchHost("localhost", 1337, false);
            showServer = false;
          }
          ypos += spacing;
          if (GUI.Button(new Rect(xpos, ypos, 100, 20), "Internet"))
          {
            manager.SetMatchHost("mm.unet.unity3d.com", 443, true);
            showServer = false;
          }
          ypos += spacing;
          if (GUI.Button(new Rect(xpos, ypos, 100, 20), "Staging"))
          {
            manager.SetMatchHost("staging-mm.unet.unity3d.com", 443, true);
            showServer = false;
          }
        }

        ypos += spacing;

        GUI.Label(new Rect(xpos, ypos, 300, 20), "MM Uri: " +
    manager.matchMaker.baseUri);
        ypos += spacing;

        if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Disable Match Maker"))
        {
          manager.StopMatchMaker();
        }
        ypos += spacing;
      }
    }*/
  }
}
}
#endif  // ENABLE_UNET
