using UnityEngine;

class MiniMapCameraPositionController : MonoBehaviour {
  public Camera MiniMapCamera;
  public Vector3 miniMapRelativePosition = new Vector3(0, 2001, 0);
  public bool miniMapRelativeY = false;

  void Start() {
    if (MiniMapCamera != null) {
      MiniMapCamera = Instantiate(MiniMapCamera);
    }
  }
  void LateUpdate() {
    if (MiniMapCamera != null) {
      Vector3 mapPos = transform.position + miniMapRelativePosition;
      if (!miniMapRelativeY) mapPos.y = miniMapRelativePosition.y;
      MiniMapCamera.transform.position = mapPos;
    }
  }
}
