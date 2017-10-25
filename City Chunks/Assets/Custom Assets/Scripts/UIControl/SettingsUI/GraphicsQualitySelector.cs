// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;

public class GraphicsQualitySelector : MonoBehaviour {
  Text text;

  void Start() {
    text = GetComponentInChildren<Text>();

    if (text == null) {
      this.enabled = false;
      return;
    }

    text.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
  }

  public void DecreaseQuality() {
    QualitySettings.DecreaseLevel(true);
    text.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
  }

  public void IncreaseQuality() {
    QualitySettings.IncreaseLevel(true);
    text.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
  }
}
