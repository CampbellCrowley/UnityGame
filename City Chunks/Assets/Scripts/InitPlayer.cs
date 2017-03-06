using UnityEngine;
using System.Collections;

public
class InitPlayer : MonoBehaviour {
  [SerializeField] public float spawnHeight =
      2;  // Height off the ground to spawn
 private
  bool spawned = false;
 public
  void go(float x, float y, float z) {
    if (!spawned) {
      transform.position = new Vector3(x, y + spawnHeight, z);
      Debug.Log("Player Spawned\n" + transform.position);
      spawned = true;
    }
  }
 public
  void updatePosition(float x, float y, float z) {
    transform.position = new Vector3(x, y, z);
    Debug.Log("Player moved to " + transform.position);
  }
}
