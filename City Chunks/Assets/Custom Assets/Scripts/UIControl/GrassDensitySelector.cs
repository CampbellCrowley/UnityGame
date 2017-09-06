using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GrassDensitySelector : MonoBehaviour {

  Slider slider;
  InputField textInput;

  void Start() {
    slider = GetComponentInChildren<Slider>();
    textInput = GetComponentInChildren<InputField>();

    if (slider == null || textInput == null) {
      this.enabled = false;
      return;
    }

    slider.minValue = 0.0f;
    slider.maxValue = 1.0f;
    slider.value = GameData.GrassDensity;
    if (slider.value < 0) slider.value = 0f;
    if (slider.value > 1) slider.value = 1f;

    textInput.text = (GameData.GrassDensity * 100f) + "";
    textInput.onValueChanged.AddListener(delegate {TextChange(); });
  }

  public void SliderUpdate(float position) {
    textInput.text = (position * 100f) + "";
    UpdateValue();
  }

  public void TextUpdate(string distance) {
    float d;
    if (float.TryParse(distance, out d)) {
      d /= 100f;
      if (d < 0) d = 0f;
      if (d > 1) d = 1f;
      slider.value = d;
      UpdateValue();
    }
  }

  public void TextChange() {
    textInput.text = GetNumbers(textInput.text);
  }

  string GetNumbers(string input) {
    int index = input.IndexOf(".");
    if (index > 0) input = input.Substring(0, index);
    return new string(input.Where(c => char.IsDigit(c)).ToArray());
  }

  void UpdateValue() {
    GameData.GrassDensity = slider.value;
    if (GameData.getLevel() != 0) {
      TerrainGenerator tg = FindObjectOfType<TerrainGenerator>();
      if (tg != null) tg.ChangeGrassDensity(slider.value);
    }
  }

  void OnDestroy() { UpdateValue(); }
 }
