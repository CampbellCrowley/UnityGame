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
    text.text = output;
  }
}
