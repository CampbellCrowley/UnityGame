using UnityEngine;
using System.Collections;

[System.Serializable]
public class Point {
  public Vector3 Position = Vector3.zero;
  public Vector3 Rotation = Vector3.zero;
  public float moveTime = 0;
}
public class Cinematic : MonoBehaviour {

  public bool isDone = false;
  public Point[] points;

  private float startTime = 0;
  private int destinationIndex = 0;
  private Vector3 linVelocity;

  public void Start() {
    isDone = false;
    linVelocity = Vector3.zero;
    destinationIndex = 0;
    startTime = Time.time;
  }

  public void Update() {
    if (isDone) return;
    GameObject[] cams = GameObject.FindGameObjectsWithTag("MainCamera");
    if (cams.Length == 0) return;
    if (Input.GetKeyDown("enter")) {
      isDone = true;
      return;
    }
    Vector3 destinationPos = points[0].Position;
    Vector3 previousPos = cams[0].transform.position;
    Vector3 destinationRot = points[0].Rotation;
    Vector3 previousRot = points[0].Rotation;
    float timeLeft = 0f;
    if (destinationIndex >= 0 && destinationIndex < points.Length) {
      destinationPos = points[destinationIndex].Position;
      destinationRot = points[destinationIndex].Rotation;
      if(destinationIndex > 0) {
        previousRot = points[destinationIndex - 1].Rotation;
      }
      timeLeft = points[destinationIndex].moveTime + (startTime - Time.time);
      if (timeLeft < 0) {
        if(destinationIndex == 0) {
          foreach(GameObject cam in cams) {
            cam.transform.position = points[0].Position;
            cam.transform.eulerAngles = points[0].Rotation;
          }
        }
        destinationIndex++;
        startTime = Time.time;
        return;
      }
    } else if(destinationIndex >= points.Length) {
      isDone = true;
      linVelocity = Vector3.zero;
      return;
    }

    Vector3 thisPosition = Vector3.SmoothDamp(previousPos, destinationPos,
                                              ref linVelocity, timeLeft);
    Vector3 thisRotation =
        Vector3.Slerp(previousRot, destinationRot,
                      1f - timeLeft / points[destinationIndex].moveTime);
    RenderSettings.fogStartDistance = 300;
    RenderSettings.fogEndDistance = 500;
    RenderSettings.fogColor = Color.red;

    foreach (GameObject cam in cams) {
      cam.transform.position = thisPosition;
      cam.transform.eulerAngles = thisRotation;
    }
  }
}
