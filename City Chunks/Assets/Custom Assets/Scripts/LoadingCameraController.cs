using UnityEngine;
using System.Collections;

class LoadingCameraController : MonoBehaviour {
  Color startColor;

  void Start () {
    startColor= GetComponent<Camera>().backgroundColor;
  }
  void Update() {
    GetComponent<Camera>().backgroundColor =
        Color.Lerp(Color.black, startColor, GameData.loadingPercent);
  }
}
