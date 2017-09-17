// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;

public class VersionGetter : MonoBehaviour {
  Text text;

  void Start() {
    text = GetComponent<Text>();
    text.text = GameData.version;
  }
}
