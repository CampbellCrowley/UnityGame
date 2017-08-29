using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class LoadDistanceSelector : MonoBehaviour {

  public const float minDistance = 600.0f;
  public const float maxDistance = 3000.0f;

  Slider slider;
  InputField textInput;

  void Start() {
    slider = GetComponentInChildren<Slider>();
    textInput = GetComponentInChildren<InputField>();

    if (slider == null || textInput == null) {
      this.enabled = false;
      return;
    }

    slider.minValue = minDistance;
    slider.maxValue = maxDistance;
    slider.value = GameData.LoadDistance;

    textInput.text = GameData.LoadDistance + "";
    textInput.onValueChanged.AddListener(delegate {TextChange(); });
  }

  public void SliderUpdate(float position) {
    textInput.text = position + "";
  }

  public void TextUpdate(string distance) {
    float d;
    if (float.TryParse(distance, out d)) {
      if (d < minDistance) d = minDistance;
      if (d > maxDistance) d = maxDistance;
      slider.value = d;
    }
  }

  public void TextChange() {
    textInput.text = GetNumbers(textInput.text);
  }

  private string GetNumbers(string input) {
    int index = input.IndexOf(".");
    if (index > 0) input = input.Substring(0, index);
    return new string(input.Where(c => char.IsDigit(c)).ToArray());
  }

  public void ForceUnloadAll() {
    OnDestroy();
    FindObjectOfType<TerrainGenerator>().ForceUnloadAll();
  }

  void OnDestroy() { GameData.LoadDistance = slider.value; }
 }
