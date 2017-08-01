using UnityEngine;
using System;
using System.Collections.Generic;

public class RockGenerator : MonoBehaviour {
  public GameObject[] SpawnableRocks;
  public float density = 0.5f;
  [Range(0f, 10f)]
  public float scaleMultiplier = 1.0f;
  public bool randomRotations = true;
  public float UpdateInterval = 15f;

  private float lastUpdate = 0f;
  private TerrainGenerator tg;

  private List<GameObject> SpawnedRocks = new List<GameObject>();

  void Start() { tg = FindObjectOfType<TerrainGenerator>(); }
  void Update() {
    if (Time.time - lastUpdate > UpdateInterval) {
      UpdateInterval = Time.time;

      // Spawn rocks here
    }
  }
}
