using UnityEngine;

public class CityGenerator : SubGenerator {
  public const string version = "c1";
  // TODO: Make buildings modular vertically. Meaning they can stack on top of
  // each other. Or use different models for different heights.
  // TODO: Generate roads
  // TODO: Remove grass, trees, and rocks where buildings and roads are.
  public GameObject[] buildingPrefabs;
  // Meters
  public float buildingWidth = 30f;
  public float floorHeight = 4f;
  public int maxNumFloors = 100;
  public float roadWidth = 12.857f;
  [Range(0f,1f)]
  public float noiseRoughness = 0.2f;
  [Range(0f,1f)]
  public float citySize = 0.05f;
  [Range(0f,1f)]
  public float cityHeight = 0.04f;
  [Header("Debug Options")]
  public bool debugAll = false;
  public bool debugPerlin = false;
  public bool debugDisableTerrain = false;
  public bool debugThreshold = false;

  private float divisionWidth = 0f;

  protected override void Initialized() {
    Debug.Log("City Generator Initialized");
    divisionWidth = (1f - cityHeight);
  }

  protected override void Generate(Terrains terrain) {
    int PWidth =
        Mathf.FloorToInt(tg.GetTerrainWidth() / (buildingWidth + roadWidth));
    float chunkXPos = terrain.x * tg.GetTerrainWidth();
    float chunkZPos = terrain.z * tg.GetTerrainLength();

    float[, ] pointMap = new float[PWidth, PWidth];

    tg.PerlinDivide(ref pointMap, terrain.x, terrain.z, PWidth, PWidth, -1,
                    noiseRoughness);

    float threshold = 1f - (citySize * (1f - Mathf.Pow(terrain.biome, 2f)));
    if (debugThreshold) {
      Debug.DrawLine(
          new Vector3(chunkXPos, threshold * 1000f, chunkZPos),
          new Vector3(chunkXPos + tg.GetTerrainWidth(), threshold * 1000f,
                      chunkZPos + tg.GetTerrainLength()),
          Color.red, 10000f);
      Debug.DrawLine(new Vector3(chunkXPos, 1000f, chunkZPos),
                     new Vector3(chunkXPos + tg.GetTerrainWidth(), 1000f,
                                 chunkZPos + tg.GetTerrainLength()),
                     Color.green, 10000f);
    }

    for (int x = 0; x < pointMap.GetLength(0); x++) {
      for (int z = 0; z < pointMap.GetLength(1); z++) {
        if (debugAll || pointMap[x, z] > threshold) {
          // TODO: The larger the value of pointMap, the larger the building.

          float buildingX = chunkXPos + (roadWidth / 2f) +
                            (buildingWidth / 2f) +
                            z * (buildingWidth + roadWidth);
          float buildingZ = chunkZPos + (roadWidth / 2f) +
                            (buildingWidth / 2f) +
                            x * (buildingWidth + roadWidth);

          float buildingDoorX = buildingX - (buildingWidth / 2f);
          float buildingDoorZ = buildingZ - (buildingWidth / 2f);
          float buildingY =
              TerrainGenerator.GetTerrainHeight(buildingDoorX, buildingDoorZ);

          int numFloors =
              Mathf.CeilToInt((pointMap[x, z] - threshold) / divisionWidth);
          for (int i=0; i<numFloors; i++) {
            GameObject last = Instantiate(
                buildingPrefabs[Random.Range(0, buildingPrefabs.Length)],
                new Vector3(buildingX,
                            debugPerlin ? pointMap[x, z] * 1000f
                                        : buildingY + floorHeight * i,
                            buildingZ),
                Quaternion.identity);

            last.transform.name =
                "BuildingFloor" + i + "(" + x + ", " + z + ")";
            last.transform.parent = terrain.gameObject.transform;
          }

          if (!debugDisableTerrain) {
            int heightmapX = Mathf.FloorToInt(
                (buildingX - chunkXPos - buildingWidth / 2f) /
                tg.GetTerrainWidth() * (float)terrain.terrData.heightmapWidth);
            float heightmapY =
                (float)buildingY / (float)tg.GetTerrainMaxHeight();
            int heightmapZ =
                Mathf.FloorToInt((buildingZ - chunkZPos - buildingWidth / 2f) /
                                 tg.GetTerrainLength() *
                                 (float)terrain.terrData.heightmapHeight);
            int heightmapW =
                Mathf.CeilToInt((float)buildingWidth *
                                ((float)terrain.terrData.heightmapWidth /
                                 (float)tg.GetTerrainWidth())) +
                1;
            int heightmapH =
                Mathf.CeilToInt((float)buildingWidth *
                                ((float)terrain.terrData.heightmapHeight /
                                 (float)tg.GetTerrainLength())) +
                1;
            float[, ] points = new float[heightmapW, heightmapH];
            for (int i = 0; i < points.GetLength(0); i++) {
              for (int j = 0; j < points.GetLength(1); j++) {
                points[i, j] = heightmapY;
              }
            }
            // Flipped because Unity is dumb
            terrain.terrData.SetHeights(heightmapX, heightmapZ, points);
          }
        }
      }
    }
    terrain.cityQueue = false;
  }
}
