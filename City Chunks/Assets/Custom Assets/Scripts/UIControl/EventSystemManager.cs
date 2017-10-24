// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventSystem))]
class EventSystemManager : MonoBehaviour {
  public enum Mode { Auto, Triggered };
  public Mode mode = Mode.Auto;

  private GameObject previousSelectedGameObject;
  EventSystem eventSystem;

  void Awake() { (eventSystem = GetComponent<EventSystem>()).enabled = false; }

  void Start() {
    if (mode == Mode.Auto) ForceSelected();
  }
  public void Trigger() {
    if (mode == Mode.Triggered) ForceSelected();
    else Debug.LogWarning("EventSystemManager set to " + mode.ToString() +
                       ", but something attempted to trigger it!");
  }
  void ForceSelected() {
    if (EventSystem.current == null) eventSystem.enabled = true;
    previousSelectedGameObject = EventSystem.current.currentSelectedGameObject;
    EventSystem.current.SetSelectedGameObject(
        GetComponent<EventSystem>().firstSelectedGameObject);
  }
  public void Reset() {
    if (mode == Mode.Triggered) ResetSelected();
    else Debug.LogWarning("EventSystemManager set to " + mode.ToString() +
                       ", but something attempted to trigger it!");
  }
  void ResetSelected() {
    if (previousSelectedGameObject != null)
      EventSystem.current.SetSelectedGameObject(previousSelectedGameObject);
    previousSelectedGameObject = null;
  }
  void OnDestroy() { ResetSelected(); }
}
