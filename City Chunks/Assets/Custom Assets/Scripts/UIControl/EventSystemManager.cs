// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventSystem))]
class EventSystemManager : MonoBehaviour {
  private GameObject previousSelectedGameObject;

  void Start() {
    if (EventSystem.current == null) GetComponent<EventSystem>().enabled = true;
    previousSelectedGameObject = EventSystem.current.currentSelectedGameObject;
    EventSystem.current.SetSelectedGameObject(
        GetComponent<EventSystem>().firstSelectedGameObject);
  }
  void OnDestroy() {
    if (previousSelectedGameObject != null)
      EventSystem.current.SetSelectedGameObject(previousSelectedGameObject);
    previousSelectedGameObject = null;
  }
}
