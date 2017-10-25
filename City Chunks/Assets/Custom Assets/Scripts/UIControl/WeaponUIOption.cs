// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
class WeaponUIOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
  public Text description = null;
  public Text myName = null;
  public Image image = null;
  public Color normalColor = Color.white;
  public Color highlightedColor = Color.blue;

  [HideInInspector] public bool isHighlighted = false;

  Weapon weapon;
  Button button;

  void Awake() { button = GetComponent<Button>(); }

  public void setWeapon(Weapon newWeapon) {
    weapon = newWeapon;
    if (description != null) description.text = weapon.ToString();
    if (myName != null) myName.text = weapon.gunType.ToString();
  }
  public Weapon getWeapon() { return weapon; }

  public void OnPointerExit(PointerEventData eventData) {
    isHighlighted = false;
    if (button != null) {
      ColorBlock colors = button.colors;
      colors.normalColor = normalColor;
      button.colors = colors;
    }
  }
  public void OnPointerEnter(PointerEventData eventData) {
    isHighlighted = true;
    if (button != null) {
      ColorBlock colors = button.colors;
      colors.normalColor = highlightedColor;
      button.colors = colors;
    }
  }
}
