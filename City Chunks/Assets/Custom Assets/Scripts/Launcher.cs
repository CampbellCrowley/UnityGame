using UnityEngine;

public class Launcher : MonoBehaviour {

  void Awake() {
    PhotonNetwork.autoJoinLobby = false;
    PhotonNetwork.automaticallySyncScene = true;
  }

  void Start() {
    Connect();
  }

  public void Connect() {
    if(PhotonNetwork.connected) {
    } else {

    }
  }
}
