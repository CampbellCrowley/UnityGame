// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

public class LevelController : MonoBehaviour {
  [Tooltip("The prefab to use for representing the player")]
  public GameObject playerPrefab;
  public static GameObject LocalPlayerInstance;

  void Start() {
    if (playerPrefab == null) {
      Debug.LogError(
          "<Color=Red>Missing</Color> playerPrefab Reference. Please set it " +
              "up in GameObject 'Game Manager'",
          this);
    } else {
      if(LocalPlayerInstance == null) {
        PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, 0, 0),
                                  Quaternion.identity, 0);
        Debug.Log("Instantiated Player " + playerPrefab.name);
      }
    }
  }
}
