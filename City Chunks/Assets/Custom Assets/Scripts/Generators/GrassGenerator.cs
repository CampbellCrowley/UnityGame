using UnityEngine;

class GrassGenerator : SubGenerator {

  [TextArea]
  public string Notes = "Grass Generator uses the details defined in the " +
    "Terrain options. There are no settings to configure.";

  private DetailPrototype[] TerrainGrassPrototypes;

  protected override void Initialized() {}
  protected override void Generate(Terrains terrain) {
    UpdateDetailPrototypes(terrain);
    UpdateGrassDetail(terrain);
  }

  void UpdateDetailPrototypes(Terrains terrain) {
    if (terrain.x != 0 || terrain.z != 0) {
      terrain.terrData.detailPrototypes = TerrainGrassPrototypes;
      terrain.terrData.RefreshPrototypes();
    } else {
      TerrainGrassPrototypes = terrain.terrData.detailPrototypes;
    }
  }

  void UpdateGrassDetail(Terrains terrain) {
    int[, ] map =
        new int[terrain.terrData.detailWidth, terrain.terrData.detailHeight];
    float max = 16f * (1f - terrain.biome);

    for (int x = 0; x < terrain.terrData.detailWidth; x++) {
      for (int z = 0; z < terrain.terrData.detailHeight; z++) {
        float height = terrain.terrData.GetInterpolatedHeight(
            (float)x / (float)(terrain.terrData.detailWidth - 1),
            (float)z / (float)(terrain.terrData.detailHeight - 1));
        if (height <= TerrainGenerator.waterHeight ||
            height >= TerrainGenerator.snowHeight) {
          map[ z, x ] = 0;
        } else {
          map[z, x] =
              (int)((1 - (terrain.terrData.GetSteepness(
                              (float)x / (float)terrain.terrData.detailWidth,
                              (float)z / (float)terrain.terrData.detailHeight) /
                          60f)) *
                    max);
        }
      }
    }

    for (int i = 0; i < terrain.terrData.detailPrototypes.Length; i++) {
      terrain.terrData.SetDetailLayer(0, 0, i, map);
    }
    terrain.gameObject.GetComponent<Terrain>().Flush();
  }
}
