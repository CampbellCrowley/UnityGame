using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunController : MonoBehaviour {

  [Tooltip("Length of a day in minutes.")]
  public float lengthOfDay = 5f;
  [Tooltip("Number of real world minutes since game midnight.")]
  public float timeNow = 1f;
  [Tooltip("Distance the GameObject will be positioned from the player")]
  public float sunDistance = 500f;
  [Tooltip("Amount of realtime between each time the sun actually moves in seconds.")]
  public float deltaUpdate = 10f;
  [Tooltip("Color of iluminated light during sunset.")]
  public Color sunsetColor = Color.yellow;

  private float lastUpdate = 0f;
  private float maxIntensity = 1f;
  private float minIntensity = 0.2f;
  private Light thisLight;
  private Color daylightColor;

  void Start() {
    thisLight = GetComponent<Light>();
    maxIntensity = thisLight.intensity;
    daylightColor = thisLight.color;
  }

  void Update() {
                timeNow += Time.deltaTime / 60f;
    if(timeNow > lengthOfDay) timeNow = timeNow % lengthOfDay;

    if (Camera.main != null && Time.time - lastUpdate > deltaUpdate) {
      lastUpdate = Time.time;
      transform.position =
          Camera.main.transform.position +
          new Vector3(
              sunDistance * Mathf.Sin(timeNow / lengthOfDay * 2f * Mathf.PI),
              sunDistance * -Mathf.Cos(timeNow / lengthOfDay * 2f * Mathf.PI),
              0f);
      transform.LookAt(Camera.main.transform.position);
      thisLight.intensity =
          Mathf.Lerp(minIntensity, maxIntensity,
                     -Mathf.Cos(timeNow / lengthOfDay * 2f * Mathf.PI));
      thisLight.color =
          Color.Lerp(sunsetColor, daylightColor,
                     -Mathf.Cos(timeNow / lengthOfDay * 2f * Mathf.PI));
    }
  }
}
