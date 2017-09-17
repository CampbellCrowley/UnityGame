// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

public class MyCameraController : MonoBehaviour {

  public GameObject cam;
  public bool CameraObjectAvoidance = true;
  public bool firstPerson = true;
  public bool distanceSnap = false;
  public float MaxCameraDistance = 3f;
  public float maxXAngle = 90f;
  public GUIText debug;

  [HideInInspector] public bool isMaster = true;
  [HideInInspector] public bool userInput = true;
  [HideInInspector] public bool rotateWithCamera = false;

  Transform target;
  float CurrentCameraDistance = 0f;
  float intendedCameraDistance = 0f;
  bool initialized = false;

  public void Initialize() {
    if (initialized) {
      Debug.LogWarning(
          "Initialize() was called more than once. This is not allowed!");
      return;
    }
    if (cam == null) {
      Debug.LogError("There is no Camera to instantiate!");
      initialized = false;
      return;
    }
    cam = Instantiate(cam);
    cam.transform.parent = null;
    cam.GetComponent<Camera>().enabled = true;
    cam.GetComponent<AudioListener>().enabled = true;
    cam.name = "CameraFor" + GameData.username;
  }

  public void UpdateTarget(Transform target_) { target = target_; }

  public void UpdateTransform(float dt) {
    if (!isMaster) return;
    if (target == null) return;

    if (firstPerson) {
      intendedCameraDistance = 0f;
      rotateWithCamera = true;
    } else {
      intendedCameraDistance = MaxCameraDistance;
      rotateWithCamera = false;
    }

    if (debug != null) {
      debug.text = "IntendedCameraDistance: " + intendedCameraDistance +
                   "\nMaxCameraDistance: " + MaxCameraDistance +
                   "\nCurrentCameraDistance: " + CurrentCameraDistance;
    }

    float lookHorizontal =
        Input.GetAxis("Mouse X") + Input.GetAxis("Joystick X") * 3f;
    float lookVertical =
        Input.GetAxis("Mouse Y") + Input.GetAxis("Joystick Y") * 3f;

    if (!userInput || GameData.isPaused || GameData.isChatOpen) {
      lookHorizontal = 0f;
      lookVertical = 0f;
    }

    float midpoint = (maxXAngle + (360f - maxXAngle)) / 2f;
    float intendedAngle = cam.transform.eulerAngles.x -
                          lookVertical * GameData.mouseSensitivity;
    if (intendedAngle > maxXAngle && intendedAngle < midpoint) {
      cam.transform.rotation =
          Quaternion.Euler(maxXAngle, cam.transform.eulerAngles.y, 0f);
    } else if (intendedAngle < 360f - maxXAngle && intendedAngle > midpoint) {
      cam.transform.rotation =
          Quaternion.Euler(-maxXAngle, cam.transform.eulerAngles.y, 0f);
    } else {
      cam.transform.rotation =
          Quaternion.Euler(cam.transform.eulerAngles.x -
                               lookVertical * GameData.mouseSensitivity,
                           cam.transform.eulerAngles.y +
                               lookHorizontal * GameData.mouseSensitivity,
                           0);
    }

    if (CameraObjectAvoidance) {
      RaycastHit hit;
      Physics.Linecast(target.position, cam.transform.position, out hit,
                       LayerMask.GetMask("Ground"));

      if (hit.transform != cam.transform && hit.transform != transform &&
          hit.transform != null) {
        CurrentCameraDistance = hit.distance;
      } else {
        CurrentCameraDistance += 5.0f * dt;

      }
    }

    if (CurrentCameraDistance > intendedCameraDistance || distanceSnap) {
      CurrentCameraDistance = intendedCameraDistance;
    }

    Vector3 newCameraPos =
        Vector3.ClampMagnitude(
            (Vector3.left *
                 (Mathf.Sin(cam.transform.eulerAngles.y / 180f * Mathf.PI) -
                  Mathf.Sin(cam.transform.eulerAngles.y / 180f * Mathf.PI) *
                      Mathf.Sin((-45f + cam.transform.eulerAngles.x) / 90f *
                                Mathf.PI)) +
             Vector3.back *
                 (Mathf.Cos(cam.transform.eulerAngles.y / 180f * Mathf.PI) -
                  Mathf.Cos(cam.transform.eulerAngles.y / 180f * Mathf.PI) *
                      Mathf.Sin((-45f + cam.transform.eulerAngles.x) / 90f *
                                Mathf.PI)) +
             Vector3.up *
                 Mathf.Sin(cam.transform.eulerAngles.x / 180f * Mathf.PI)),
            1.0f) *
        CurrentCameraDistance;

    newCameraPos += target.position;

    if (GameData.cameraDamping && !firstPerson) {
      Vector3 velocity = Vector3.zero;
      newCameraPos = Vector3.SmoothDamp(cam.transform.position, newCameraPos,
                                        ref velocity, 0.05f);
    }

    if (newCameraPos.y <= TerrainGenerator.waterHeight + 0.4f) {
      newCameraPos += Vector3.down * 0.8f;
    }

    cam.transform.position = newCameraPos;
  }

  public void ToggleThirdPerson() { firstPerson = !firstPerson; }
  public bool Initialized() { return initialized; }
}
