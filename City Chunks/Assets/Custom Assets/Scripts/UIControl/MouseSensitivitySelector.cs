using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MouseSensitivitySelector : MonoBehaviour {

  public const float min = 0.01f;
  public const float max = 10f;

  Slider slider;
  InputField textInput;

  void Start() {
    slider = GetComponentInChildren<Slider>();
    textInput = GetComponentInChildren<InputField>();

    if (slider == null || textInput == null) {
      this.enabled = false;
      return;
    }

    slider.minValue = min;
    slider.maxValue = max;
    slider.value = GameData.mouseSensitivity;
    if (slider.value < min) slider.value = min;
    if (slider.value > max) slider.value = max;

    textInput.text = (GameData.mouseSensitivity) + "";
    textInput.onValueChanged.AddListener(delegate { TextChange(); });
  }

  public void SliderUpdate(float position) {
    textInput.text = position + "";
  }

  public void TextUpdate(string distance) {
    float d;
    if (float.TryParse(distance, out d)) {
      if (d < min) d = min;
      if (d > max) d = max;
      slider.value = d;
    }
  }

  public void TextChange() { textInput.text = GetNumbers(textInput.text); }

  string GetNumbers(string input) {
     int index = input.IndexOf(".");
     string first, second;
     if (index > 0) {
       first = input.Substring(0, index);
       if (index + 2 < input.Length) {
         second = input.Substring(index + 1, input.Length - index - 1);
         if (second.Length > 2) {
           second = second.Substring(0, 2);
         }
       } else {
         second = "0";
       }
     } else {
       first = input;
       second = "0";
     }
     first = new string(first.Where(c => char.IsDigit(c)).ToArray());
     second = new string(second.Where(c => char.IsDigit(c)).ToArray());

     return first + "." + second;
  }

  void OnDestroy() { GameData.mouseSensitivity = slider.value; }
 }
