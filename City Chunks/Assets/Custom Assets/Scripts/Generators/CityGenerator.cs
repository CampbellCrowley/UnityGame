// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

public class CityGenerator : SubGenerator {
  public const string version = "c1";
  // TODO: Generate roads
  // TODO: Remove grass where buildings and roads are.
  public Building[] buildingPrefabs;
  // Lower is higher resolition/more points per chunk.
  public float searchResolution = 10f;
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
  public bool debugFlat = false;
  public bool debugDisableTerrain = false;
  public bool debugThreshold = false;

  private float divisionWidth = 0f;

  private int numGroundFloors = 0;
  private int numMidFloors = 0;
  private int numRoofFloors = 0;
  private int numCompleteBuildings = 0;
  private RockGenerator rg;

  protected override void Initialized() {
    Debug.Log("City Generator Initialized");
    divisionWidth = (1f - cityHeight);
    rg = GetComponent<RockGenerator>();

    numGroundFloors = 0;
    numMidFloors = 0;
    numRoofFloors = 0;
    numCompleteBuildings = 0;
    for (int i = 0; i < buildingPrefabs.Length; i++) {
      switch (buildingPrefabs[i].floor) {
        case Building.Floor.GROUND:
          numGroundFloors++;
          break;
        case Building.Floor.MIDDLE:
          numMidFloors++;
          break;
        case Building.Floor.ROOF:
          numRoofFloors++;
          break;
        case Building.Floor.COMPLETE:
          numCompleteBuildings++;
          break;
        default:
          Debug.LogError("Unsupported building: " + i + ", " +
                         buildingPrefabs[i].floor);
          break;
      }
    }
  }

  protected override void Generate(Terrains terrain) {
    int PWidth = Mathf.FloorToInt(tg.GetTerrainWidth() / searchResolution);
    float chunkXPos = terrain.gameObject.transform.position.x;
    float chunkZPos = terrain.gameObject.transform.position.z;

    float[, ] pointMap = new float[PWidth, PWidth];

    tg.PerlinDivide(ref pointMap, terrain.x, terrain.z, PWidth, PWidth, -1,
                    noiseRoughness);

    float threshold = 1f - (citySize * (1f - Mathf.Pow(terrain.biome, 3f)));
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

    if (tg.useSeed) {
      UnityEngine.Random.InitState(
          (int)(tg.Seed +
                tg.PerfectlyHashThem((short)(terrain.x * 3 - 3),
                                     (short)(terrain.z * 3 - 2))));
    }

    for (int x = 0; x < pointMap.GetLength(0); x++) {
      for (int z = 0; z < pointMap.GetLength(1); z++) {
        if (debugAll || pointMap[x, z] > threshold) {
          int buildingNum = Random.Range(1, numGroundFloors);
          int buildingID = -1;
          int count = 0;
          for (int i = 0; i < buildingPrefabs.Length; i++) {
            if (buildingPrefabs[i].floor == Building.Floor.GROUND) {
              count++;
              if (count == buildingNum) {
                buildingID = i;
                break;
              }
            }
          }

          if (buildingID <= -1 || buildingID >= buildingPrefabs.Length) {
            Debug.LogError("Invalid building ID: " + buildingID +
                           "\nNumGroundFloors: " + numGroundFloors +
                           ", buildingNum: " + buildingNum);
            return;
          }

          // TODO: Get previous building widths instead of assuming all are the
          // same size. Also, stop assuming buildings are square.
          float buildingWidth = buildingPrefabs[buildingID].dimensions.x;

          float buildingX =
              chunkXPos + (roadWidth / 2f) + (buildingWidth / 2f) +
              (float)z / (float)pointMap.GetLength(1) * tg.GetTerrainWidth();
          float buildingZ =
              chunkZPos + (roadWidth / 2f) + (buildingWidth / 2f) +
              (float)x / (float)pointMap.GetLength(0) * tg.GetTerrainWidth();

          if (debugThreshold) {
            Debug.DrawLine(
                new Vector3(buildingX, pointMap[x, z] * 1000f, buildingZ),
                new Vector3(buildingX + buildingWidth, pointMap[x, z] * 1000f,
                            buildingZ + buildingWidth),
                Color.yellow, 10000f);
          }

          float buildingDoorX =
              buildingX + buildingPrefabs[buildingID].doorPosition.x;
          float buildingDoorZ =
              buildingZ + buildingPrefabs[buildingID].doorPosition.z;
          float buildingY =
              TerrainGenerator.GetTerrainHeight(buildingDoorX, buildingDoorZ) +
              buildingPrefabs[buildingID].doorPosition.y;
          if (buildingY == 0f - buildingPrefabs[buildingID].doorPosition.y &&
              !(debugFlat || debugAll)) {
            continue;
          }
          if (debugFlat) buildingY = 0f;

          int numFloors =
              Mathf.CeilToInt((pointMap[x, z] - threshold) / divisionWidth);
          if (debugAll && numFloors <= 0) numFloors = 1;

          Vector3 checkPos = new Vector3(buildingX, buildingY, buildingZ) +
                             buildingPrefabs[buildingID].centerOffset;
          Vector3 checkDim =
              new Vector3(buildingPrefabs[buildingID].dimensions.x / 2f,
                          buildingPrefabs[buildingID].dimensions.y / 2f,
                          buildingPrefabs[buildingID].dimensions.z / 2f);

          int mask = 1 << buildingPrefabs[buildingID].gameObject.layer;
          bool overlapping = Physics.CheckBox(
              checkPos, checkDim + Vector3.up * 50, Quaternion.identity, mask);
          if (overlapping) continue;

          if (rg != null && rg.SpawnableRocks.Length > 0) {
            mask = 1 << rg.SpawnableRocks[0].layer;
            Collider[] hits =
                Physics.OverlapBox(checkPos, checkDim + Vector3.up * 50,
                                   Quaternion.identity, mask);
            for (int i = 0; i < hits.Length; i++) {
              Destroy(hits[i].transform.gameObject);
            }
          }

          float floorHeight = 0f;

          GameObject ground = null;
          for (int i = 0; i < numFloors; i++) {
            Building.Floor goalFloor;
            int floorID = 0;
            if (i == 0 && i == numFloors - 1) {
              // goalFloor = Building.Floor.COMPLETE;
              goalFloor = Building.Floor.GROUND;
            } else if (i == 0) {
              goalFloor = Building.Floor.GROUND;
            } else if (i == numFloors - 1) {
              goalFloor = Building.Floor.ROOF;
            } else {
              goalFloor = Building.Floor.MIDDLE;
            }
            for (int j = 0; j < buildingPrefabs.Length; j++) {
              if (buildingPrefabs[i].ID == buildingPrefabs[j].ID &&
                  goalFloor == buildingPrefabs[j].floor) {
                floorID = j;
                break;
              }
            }
            GameObject last = Instantiate(
                buildingPrefabs[floorID].gameObject,
                new Vector3(
                    buildingX - buildingPrefabs[floorID].centerOffset.x,
                    (debugPerlin ? pointMap[x, z] * 1000f : buildingY) +
                        floorHeight - buildingPrefabs[floorID].centerOffset.y,
                    buildingZ - buildingPrefabs[floorID].centerOffset.z),
                Quaternion.identity);
            floorHeight += buildingPrefabs[floorID].dimensions.y;

            last.transform.name =
                "BuildingFloor" + (i + 1) + "(" + x + ", " + z + ")";
            if (i == 0) {
              last.transform.parent = terrain.gameObject.transform;
              ground = last;
            } else {
              last.transform.parent = ground.transform;
            }
            terrain.BuildingInstances.Add(last.GetComponent<Building>());
          }

          if (!debugDisableTerrain) {
            int heightmapX = Mathf.FloorToInt(
                (buildingX - chunkXPos - buildingWidth / 2f) /
                tg.GetTerrainWidth() * (float)terrain.terrData.heightmapWidth);
            float heightmapY = buildingY / (float)tg.GetTerrainMaxHeight();
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
            if (heightmapW + heightmapX >= terrain.terrData.heightmapWidth) {
              heightmapW -= (heightmapW + heightmapX) -
                            terrain.terrData.heightmapWidth + 1;
            }
            if (heightmapH + heightmapY >= terrain.terrData.heightmapHeight) {
              heightmapH -= (heightmapH + heightmapZ) -
                            terrain.terrData.heightmapHeight + 1;
            }
            float[, ] points = new float[heightmapW, heightmapH];
            for (int i = 0; i < points.GetLength(0); i++) {
              for (int j = 0; j < points.GetLength(1); j++) {
                points[i, j] = heightmapY;
              }
            }
            terrain.terrData.SetHeights(heightmapX, heightmapZ, points);
          }
        }
      }
    }
    terrain.cityQueue = false;
  }
}
