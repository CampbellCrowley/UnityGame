// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

public class RockGenerator : SubGenerator {
  [Tooltip("Array of rocks to spawn.")]
  public GameObject[] SpawnableRocks;
  [Tooltip("Maximum number of rocks per chunk.")]
  public int maxNumRocks = 10;
  [Tooltip("Minimum amount the scale can be multiplied by before spawning.")]
  public Vector3 minScaleMultiplier = Vector3.one;
  [Tooltip("Maximum amount the scale can be multiplied by before spawning.")]
  public Vector3 maxScaleMultiplier = Vector3.one;
  [Tooltip("May rocks be rotated randomly?")]
  public bool randomRotations = true;

  protected override void Initialized() { Debug.Log("Rock Generator Initialized!"); }

  protected override void Generate(Terrains terrain) {
    float[, ] modifier = new float[2, 2];
    tg.PerlinDivide(ref modifier, terrain.x, terrain.z, 1, 1);
    int numRocks = Mathf.RoundToInt(modifier[0, 0] * maxNumRocks);

    UnityEngine.Random.InitState(
        (int)(tg.Seed +
              tg.PerfectlyHashThem((short)terrain.x, (short)terrain.z)));

    float tWidth = tg.GetTerrainWidth();
    float tLength = tg.GetTerrainLength();

    for (int i = 0; i < numRocks; i++) {
      Vector3 spawnPosition = TerrainGenerator.GetPointOnTerrain(
          new Vector3(Random.Range(0, tWidth), 0, Random.Range(0, tLength)) +
          terrain.gameObject.transform.position);

      Quaternion spawnRotation =
          randomRotations ? Random.rotation : Quaternion.identity;

      GameObject ri =
          Instantiate(SpawnableRocks[Random.Range(0, SpawnableRocks.Length)],
                      spawnPosition, spawnRotation);

      ri.transform.localScale = new Vector3(
          ri.transform.localScale.x *
              Random.Range(minScaleMultiplier.x, maxScaleMultiplier.x),
          ri.transform.localScale.y *
              Random.Range(minScaleMultiplier.y, maxScaleMultiplier.y),
          ri.transform.localScale.z *
              Random.Range(minScaleMultiplier.z, maxScaleMultiplier.z));

      terrain.RockInstances.Add(ri);

      ri.transform.parent = terrain.gameObject.transform;
    }
    terrain.rockQueue = false;
  }
}
