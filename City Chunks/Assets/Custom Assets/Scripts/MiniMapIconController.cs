using UnityEngine;
using System.Collections;

public class MiniMapIconController : MonoBehaviour {
 public Vector3 Rotation = new Vector3(0, 0, 0);
 public bool handleRotation = true;
 public float height = 50f;

 public void Update() {
    if (handleRotation) {
      transform.eulerAngles = new Vector3(
          Rotation.x, Rotation.y + transform.parent.rotation.eulerAngles.y,
          Rotation.z);
    }
    transform.position = transform.parent.position + Vector3.up * height;
  }
}
