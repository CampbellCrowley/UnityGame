// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;
using System.Collections.Generic;

class TreeGenerator : SubGenerator {
  public const string version  = "tr2";
  [Tooltip("Maximum number of trees per chunk to be generated.")]
  public int maxNumTrees = 500;
  [Tooltip("Are trees allowed to be generated using the terrain instances?")]
  public bool useTerrainTrees = true;
  [Tooltip("Tree prefabs to place on the terrain.")]
  [SerializeField]
  public GameObject[] TerrainTrees;

  private List<TreePrototype> prototypeBuffer = new List<TreePrototype>();

  protected override void Initialized() {
    prototypeBuffer.Clear();
    if (TerrainTrees.Length == 0) enabled = false;
    for (int i = 0; i < TerrainTrees.Length; i++) {
      if (TerrainTrees[i].GetComponent<LODGroup>() == null) {
        TreePrototype newTreePrototype = new TreePrototype();
        newTreePrototype.prefab = TerrainTrees[i];
        prototypeBuffer.Add(newTreePrototype);
      }
    }
  }

  protected override void Generate(Terrains terrain) {
    if (useTerrainTrees && (terrain.x != 0 || terrain.z != 0)) {
      UpdateTreePrototypes(terrain.terrData);
    }
    UpdateTrees(terrain);
  }

  void UpdateTreePrototypes(TerrainData terrainData) {
    terrainData.treePrototypes = prototypeBuffer.ToArray();
    terrainData.RefreshPrototypes();
  }

  void UpdateTrees(Terrains terrain) {
    int numberOfTrees =
        (int)(Mathf.Pow((tg.PeakMultiplier - terrain.biome) /
                            (tg.PeakMultiplier == 0 ? 1f : tg.PeakMultiplier),
                        2f) *
              maxNumTrees);
    int CGID = tg.getSGID("CityGenerator");
    foreach (GameObject obj in terrain.ObjectInstances[ID]) Destroy(obj);
    terrain.ObjectInstances[ID].Clear();
    List<TreeInstance> newTrees =
        new List<TreeInstance>(terrain.terrData.treeInstances);
    if (tg.useSeed) {
      UnityEngine.Random.InitState(
          (int)(tg.Seed +
                tg.PerfectlyHashThem((short)(terrain.x * 3 - 1),
                                     (short)(terrain.z * 3 - 2))));
    }
    for (int i = 0; i < numberOfTrees; i++) {
      float X = UnityEngine.Random.value;
      float Z = UnityEngine.Random.value;
      float Y;

      // If trees have a LOD group, there is no reason to add it to the terrain,
      // so we just instantiate it as a game object.
      int TreeID = UnityEngine.Random.Range(0, TerrainTrees.Length);

      bool overlaps = false;
      if (TerrainTrees[TreeID].GetComponent<LODGroup>() == null &&
          useTerrainTrees) {
        Y = terrain.terrData.GetInterpolatedHeight(X, Z);
        if (Y <= TerrainGenerator.waterHeight) continue;
        if (terrain.terrData.GetSteepness(X, Z) > 30f) continue;
        Vector3 treePos =
            new Vector3((X + terrain.x) * tg.GetTerrainWidth(), Y + 0.5f,
                        (Z + terrain.z) * tg.GetTerrainLength());
        if (CGID != -1) {
          for (int j = 0; j < terrain.ObjectInstances[CGID].Count; j++) {
            if (terrain.ObjectInstances[CGID]
                                       [j].GetComponent<Collider>()
                                           .bounds.Contains(treePos)) {
              overlaps = true;
              break;
            }
          }
        }
        if (overlaps) continue;
        int index = 0;
        for (int j = 0; j < TreeID; j++) {
          if (TerrainTrees[j].GetComponent<LODGroup>() == null) index++;
        }
        TreeInstance newTreeInstance = new TreeInstance();
        newTreeInstance.prototypeIndex = index;
        newTreeInstance.color = new Color(1, 1, 1);
        newTreeInstance.lightmapColor = new Color(1, 1, 1);
        newTreeInstance.heightScale = 1.0f;
        newTreeInstance.widthScale = 1.0f;
        newTreeInstance.position =
            new Vector3(X, (Y - 2f) / tg.GetTerrainMaxHeight(), Z);
        newTrees.Add(newTreeInstance);
      } else {
        Y = terrain.terrData.GetHeight(
            (int)(X * terrain.terrData.heightmapHeight),
            (int)(Z * terrain.terrData.heightmapWidth));
        if (Y <= TerrainGenerator.waterHeight) continue;
        if (terrain.terrData.GetSteepness(X, Z) > 30f) continue;
        Vector3 treePos = new Vector3(X, Y, Z);
        if (CGID != -1) {
          for (int j = 0; j < terrain.ObjectInstances[CGID].Count; j++) {
            if (terrain.ObjectInstances[CGID]
                                       [j].GetComponent<Collider>()
                                           .bounds.Contains(treePos)) {
              overlaps = true;
              break;
            }
          }
        }
        if (overlaps) continue;
        terrain.ObjectInstances[ID].Add(Instantiate(
            TerrainTrees[TreeID],
            new Vector3(
                X * tg.GetTerrainWidth() + tg.GetTerrainWidth() * terrain.x, Y,
                Z * tg.GetTerrainLength() + tg.GetTerrainLength() * terrain.z),
            Quaternion.identity, terrain.gameObject.transform));
      }
      terrain.terrData.treeInstances = newTrees.ToArray();
      Terrain t = terrain.gameObject.GetComponent<Terrain>();
      t.Flush();
      // Refresh the collider since unity doesn't update this unless
      // SetHeights() is called or toggling the collider off and on.
      t.GetComponent<Collider>().enabled = false;
      t.GetComponent<Collider>().enabled = true;
    }
  }
}
