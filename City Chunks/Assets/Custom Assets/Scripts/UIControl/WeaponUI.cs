// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Canvas))]
class WeaponUI : MonoBehaviour {

  public WeaponUIOption templateOption;
  List<WeaponUIOption> options = new List<WeaponUIOption>();
  Canvas canvas;
  EventSystemManager esm;

  void Awake() {
    canvas = GetComponent<Canvas>();
    Hide();
    esm = GetComponentInChildren<EventSystemManager>();
  }

  public void Create(Weapon[] allWeapons, Weapon currentWeapon = null) {
    if (templateOption == null) {
      Debug.LogError("Template for weapon options is not defined!");
      return;
    }
    for (int i = 0; i < allWeapons.Length; i++) {
      WeaponUIOption newOption = Instantiate(templateOption, transform);
      RectTransform rectTransform = newOption.GetComponent<RectTransform>();
      rectTransform.anchoredPosition += Vector2.right * 266f * i;
      rectTransform.anchoredPosition += Vector2.down * 266f * (int)(i / 5f);
      newOption.transform.name = allWeapons[i].ToString();
      newOption.gameObject.SetActive(true);
      newOption.setWeapon(allWeapons[i]);
      options.Add(newOption);
    }
  }

  public void Show() {
    if (esm == null) esm = GetComponentInChildren<EventSystemManager>();
    if (esm != null) esm.Trigger();
    canvas.enabled = true;
  }
  public Weapon Hide() {
    if (esm == null) esm = GetComponentInChildren<EventSystemManager>();
    if (esm != null) esm.Reset();
    canvas.enabled = false;
    for (int i = 0; i < options.Count; i++) {
      if (options[i].isHighlighted) {
        return options[i].getWeapon();
      }
    }
    return null;
  }
}
