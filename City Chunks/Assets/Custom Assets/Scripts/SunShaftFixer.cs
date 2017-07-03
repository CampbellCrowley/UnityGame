using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

class SunShaftFixer : MonoBehaviour {
  SunShafts sunShafts;
  GameObject sun;

  void Awake() {
    sunShafts = GetComponent<SunShafts>();
    sun = GameObject.FindGameObjectWithTag("Sun");
  }

  void LateUpdate() {
    if (Camera.main == null) return;
    if (sunShafts == null) {
      sunShafts = GetComponent<SunShafts>();
      return;
    }
    if (sun == null) {
      sun = GameObject.FindGameObjectWithTag("Sun");
      return;
    }

    sunShafts.sunTransform = sun.transform;
  }
}
