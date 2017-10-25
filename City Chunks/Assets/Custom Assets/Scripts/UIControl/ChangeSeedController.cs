using UnityEngine;
using UnityEngine.UI;

class ChangeSeedController : MonoBehaviour {

  InputField field;
  void Start() { field = GetComponent<InputField>(); }
  void Update() {
    field.interactable = GameData.getLevel() == 0;
    if (!field.interactable) field.text = "Menu Only";
  }
  public void ChangeSeed() { ChangeSeed(System.Int32.Parse(field.text)); }
  public void ChangeSeed(int newSeed) { GameData.Seed = newSeed; }
}
