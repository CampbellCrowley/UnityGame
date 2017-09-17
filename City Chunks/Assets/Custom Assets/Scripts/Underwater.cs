// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.PostProcessing.Utilities;

public class Underwater : MonoBehaviour {
  PostProcessingController controller;
  float chromaticAberrationIntensity = 0;

  void Awake() {
    controller = GameObject.FindObjectOfType<PostProcessingController>();
    controller.controlColorGrading = true;
    controller.enableColorGrading = true;
    controller.controlChromaticAberration = true;
    controller.enableChromaticAberration = true;
  }

  void Update() {
    if (controller.chromaticAberration.intensity !=
            chromaticAberrationIntensity &&
        controller.chromaticAberration.intensity != 0.5f) {
      chromaticAberrationIntensity = controller.chromaticAberration.intensity;
    }
  }

  void LateUpdate() {
    if (controller == null) return;
    if (GetComponent<Camera>().transform.position.y <
        TerrainGenerator.waterHeight) {
      controller.colorGrading.channelMixer.blue.z = 2;
      controller.colorGrading.channelMixer.green.y = 1.25f;
      controller.colorGrading.channelMixer.red.x = 0.5f;
      controller.chromaticAberration.intensity = 0.5f;
    } else {
      controller.colorGrading.channelMixer.blue.z = 1;
      controller.colorGrading.channelMixer.green.y = 1;
      controller.colorGrading.channelMixer.red.x = 1;
      controller.chromaticAberration.intensity = chromaticAberrationIntensity;
    }
  }
}
