// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using System.Collections;

class LoadingLightController : MonoBehaviour {

  void Update() {
    GetComponent<Light>().intensity = Mathf.Lerp(0, 1, GameData.loadingPercent);
  }
}
