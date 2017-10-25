// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;

public class ResolutionSelector : MonoBehaviour {
  Text text;

  void Start() {
    text = GetComponentInChildren<Text>();

    if (text == null) {
      this.enabled = false;
      return;
    }

    text.text = Screen.currentResolution.ToString();
  }

  void Update() { text.text = Screen.currentResolution.ToString(); }

  public void DecreaseResolution() {
    Resolution res = new Resolution();
    bool newres = false;
    for (int i = 1; i < Screen.resolutions.Length; i++) {
      if (Screen.resolutions[i].ToString() ==
          Screen.currentResolution.ToString()) {
        res = Screen.resolutions[i - 1];
        newres = true;
        break;
      }
    }
    if (!newres) return;
    Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    text.text = res.ToString();
  }

  public void IncreaseResolution() {
    Resolution res = new Resolution();
    bool newres = false;
    for (int i = 0; i < Screen.resolutions.Length - 1; i++) {
      if (Screen.resolutions[i].ToString() ==
          Screen.currentResolution.ToString()) {
        res = Screen.resolutions[i + 1];
        newres = true;
        break;
      }
    }
    if (!newres) return;
    Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    text.text = res.ToString();
  }
}
