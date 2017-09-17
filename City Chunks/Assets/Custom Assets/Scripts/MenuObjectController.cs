using UnityEngine;
using System.Collections;

class MenuObjectController : MonoBehaviour {
  Vector3 startPosition;
  public float radius = 5;
  public float speed = 1;
  public float angle = 0f;

  private float startAngle = 0f;

  void Start() {
    startPosition = transform.position;
    startAngle = angle;
  }

  void Update() {
    angle = Time.time * speed + startAngle;

    transform.position = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    transform.position *= radius * GameData.loadingPercent;
    transform.position += startPosition;

    transform.rotation =
        Quaternion.Euler(0, 0, -radius * GameData.loadingPercent /
                                   transform.localScale.x * 2f * Mathf.Rad2Deg);
    transform.rotation =
        Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * transform.rotation;

    /* transform.rotation = Quaternion.Euler(
        Mathf.Sin(angle) * radius * GameData.loadingPercent /
            transform.localScale.x * 2f * Mathf.Rad2Deg,
        0, -Mathf.Cos(angle) * radius * GameData.loadingPercent /
               transform.localScale.x * 2f * Mathf.Rad2Deg); */
  }
}
