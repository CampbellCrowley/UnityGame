using UnityEngine;
using UnityEngine.PostProcessing.Utilities;

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
  }
  void Update() {
    if (controller == null) {
      controller = GameObject.FindObjectOfType<PostProcessingController>();
      if (controller == null) return;
      controller.controlVignette = true;
      controller.controlDepthOfField = true;
      controller.controlMotionBlur = true;
      controller.controlBloom = true;
      controller.controlColorGrading = true;
    }
    controller.enableVignette = GameData.vignette;
    controller.enableDepthOfField = GameData.dof;
    controller.enableMotionBlur = GameData.motionBlur;
    controller.enableBloom = GameData.bloomAndFlares;
    controller.enableColorGrading = GameData.colorGrading;
  }
}
