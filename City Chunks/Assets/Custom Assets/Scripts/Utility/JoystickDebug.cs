// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

public class JoystickDebug : MonoBehaviour {
  GUIText text;

  void Start() {
    text = GetComponent<GUIText>();
  }
  void Update() {
    string output = "Move: (" + Input.GetAxisRaw("Horizontal") + ", " +
                    Input.GetAxisRaw("Vertical") + ")\nLook(" +
                    Input.GetAxisRaw("Joystick X") + ", " +
                    Input.GetAxisRaw("Joystick Y") + ")";
    float lookHorizontal =
        Input.GetAxis("Mouse X") + Input.GetAxis("Joystick X");
    float lookVertical = Input.GetAxis("Mouse Y") + Input.GetAxis("Joystick Y");
    output += "\nMove: (" + Input.GetAxis("Horizontal") + ", " +
              Input.GetAxis("Vertical") + ")\nLook(" + lookVertical + ", " +
              lookHorizontal + ")";
    text.text = output;
  }
}
