// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;

public class VersionGetter : MonoBehaviour {
  Text text;

  public enum versions {
    SHORT,
    LONG
  }
  public versions version = versions.SHORT;

  void Start() {
    text = GetComponent<Text>();
    if (version == versions.SHORT) text.text = GameData.version;
    if (version == versions.LONG) text.text = GameData.longVersion;
  }
}
