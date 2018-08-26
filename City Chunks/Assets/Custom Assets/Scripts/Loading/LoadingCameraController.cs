// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using System.Collections;

class LoadingCameraController : MonoBehaviour {
  Color startColor;

  void Start () {
    startColor = GetComponent<Camera>().backgroundColor;
  }
  void Update() {
    GetComponent<Camera>().backgroundColor =
        Color.Lerp(Color.black, startColor, GameData.loadingPercent);
  }
}
