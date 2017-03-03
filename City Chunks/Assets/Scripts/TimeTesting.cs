using UnityEngine;
using System;

public class TimeTesting : MonoBehaviour {
  private float startTime;
  private float endTime;
  public void Start() {
    int output = 1;
    float input = 1.2f;
    startTime = Time.realtimeSinceStartup;
    for (int i = 0; i < 87381 * 14; i++) {
      output = (int)input;
    }
    endTime = Time.realtimeSinceStartup;
    Debug.Log("Cast: " + (endTime - startTime) * 1000 + "ms");

    startTime = Time.realtimeSinceStartup;
    for (int i = 0; i < 87381 * 14; i++) {
      output = (int)Math.Floor(input);
    }
    endTime = Time.realtimeSinceStartup;
    Debug.Log("Floor: " + (endTime - startTime) * 1000 + "ms");

    startTime = Time.realtimeSinceStartup;
    for (int i = 0; i < 87381 * 14; i++) {
      input = UnityEngine.Random.value;
    }
    endTime = Time.realtimeSinceStartup;
    Debug.Log("Cast: " + (endTime - startTime) * 1000 + "ms");
    output=output+0;
   }
}
