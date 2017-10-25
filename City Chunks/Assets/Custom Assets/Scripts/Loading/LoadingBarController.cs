// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

class LoadingBarController : MonoBehaviour {
  Text thisText;

  void Start() {
    thisText = GetComponent<Text>();
  }
  void Update() {
    string output = Mathf.RoundToInt(GameData.loadingPercent * 100f) + "%";
    thisText.text = output;
  }
}
