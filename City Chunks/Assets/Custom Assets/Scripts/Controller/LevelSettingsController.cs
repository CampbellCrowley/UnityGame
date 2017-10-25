// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.PostProcessing.Utilities;
using UnityEngine.PostProcessing;

public class LevelSettingsController : MonoBehaviour {

  PostProcessingController controller;

  void Awake() {
    Debug.Log("Vignette: " + GameData.vignette + ", DOF: " + GameData.dof +
              ", Blur: " + GameData.motionBlur + ", Bloom: " +
              GameData.bloomAndFlares + ", Color Grading: " +
              GameData.colorGrading);

    controller = GameObject.FindObjectOfType<PostProcessingController>();
    if (controller == null) return;

    controller.controlVignette = true;
    controller.controlDepthOfField = true;
    controller.controlMotionBlur = true;
    controller.controlBloom = true;
    controller.controlColorGrading = true;
    controller.enableVignette = GameData.vignette;
    controller.enableDepthOfField = GameData.dof;
    controller.enableMotionBlur = GameData.motionBlur;
    controller.enableBloom = GameData.bloomAndFlares;
    controller.enableColorGrading = GameData.colorGrading;
#if !UNITY_2017_2_0_OR_NEWER
    if (Application.platform == RuntimePlatform.Android) {
      Debug.LogWarning(
          "Disabling post processing on Android device since this is known " +
          "to cause crashes.");
      PostProcessingBehaviour[] behaviours =
          GameObject.FindObjectsOfType<PostProcessingBehaviour>();
      foreach (PostProcessingBehaviour ppb in behaviours) {
        ppb.enabled = false;
      }
    }
#endif
  }
  void LateUpdate() {
    if (controller == null) {
      controller = GameObject.FindObjectOfType<PostProcessingController>();
      if (controller == null) return;
      controller.controlVignette = true;
      controller.controlDepthOfField = true;
      controller.controlMotionBlur = true;
      controller.controlBloom = true;
      controller.controlColorGrading = true;
#if !UNITY_2017_2_0_OR_NEWER
      if (Application.platform == RuntimePlatform.Android) {
        Debug.LogWarning(
            "Disabling post processing on Android device since this is known " +
            "to cause crashes.");
        PostProcessingBehaviour[] behaviours =
            GameObject.FindObjectsOfType<PostProcessingBehaviour>();
        foreach (PostProcessingBehaviour ppb in behaviours) {
          ppb.enabled = false;
        }
      }
#endif
    }
    controller.enableVignette = GameData.vignette;
    controller.enableDepthOfField = GameData.dof;
    controller.enableMotionBlur = GameData.motionBlur;
    controller.enableBloom = GameData.bloomAndFlares;
    controller.enableColorGrading = GameData.colorGrading;
#if !UNITY_2017_2_0_OR_NEWER
    if (Application.platform == RuntimePlatform.Android) {
      controller.enableEyeAdaptation = false;
    }
#endif
  }
}
