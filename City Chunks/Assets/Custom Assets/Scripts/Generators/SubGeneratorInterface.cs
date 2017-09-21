// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

[RequireComponent(typeof(TerrainGenerator))]
public abstract class SubGenerator : MonoBehaviour {
  public new bool enabled = true;
  [Tooltip("SubGenerators will be run in the order of highest priority to lowest.")]
  [Range(0, 100)]
  public int priority = 50;
  private string myName;
  private int id = -1;
  private bool firstError = false;
  protected TerrainGenerator tg;
  public void Initialize(TerrainGenerator TG, int id) {
    tg = TG;
    this.id = id;
    myName = this.GetType().Name;
    firstError = true;
    Initialized();
    Debug.Log(Name + " initialized!");
  }
  protected abstract void Initialized();
  public bool Go(Terrains terrain) {
    if (tg == null && firstError && enabled) {
      Debug.LogError("Go was called before Initialize! This is not allowed!");
      firstError = false;
    } else if (tg != null && enabled) {
      Generate(terrain);
      return true;
    }
    return false;
  }
  protected abstract void Generate(Terrains terrain);
  public string Name { get { return myName; } }
  public int ID { get { return id; } }
}
