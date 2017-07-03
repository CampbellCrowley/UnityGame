using UnityEngine;
using System.Collections;

class MenuObjectController : MonoBehaviour {
  Vector3 startPosition;
  public float radius = 5;
  public float speed = 1;
  public float angle = 0f;

  void Start() { startPosition = transform.position; }

  void Update() {
    angle += Time.deltaTime * speed;

    transform.position = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    transform.position *= radius * GameData.loadingPercent;
    transform.position += startPosition;
  }
}
