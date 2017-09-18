// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

[RequireComponent(typeof(TerrainGenerator))]
public abstract class SubGenerator : MonoBehaviour {
  public bool Enabled = true;
  private bool firstError = false;
  protected TerrainGenerator tg;
  public void Initialize(TerrainGenerator TG) {
    tg = TG;
    firstError = true;
    Initialized();
  }
  protected abstract void Initialized();
  public void Go(Terrains terrain) {
    if (tg == null && firstError && Enabled) {
      Debug.LogError("Go was called before Initialize! This is not allowed!");
      firstError = false;
    } else if (tg != null && Enabled) {
      Generate(terrain);
    }
  }
  protected abstract void Generate(Terrains terrain);
}
