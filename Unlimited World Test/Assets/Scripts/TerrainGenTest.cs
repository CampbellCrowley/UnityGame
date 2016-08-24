// #define DEBUG_ARRAY
// #define DEBUG_ATTRIBUTES
// #define DEBUG_BORDERS_1
// #define DEBUG_BORDERS_2
// #define DEBUG_BORDERS_3
// #define DEBUG_BORDERS_4
// #define DEBUG_HEIGHTS
// #define DEBUG_MISC
// #define DEBUG_POSITION
// #define DEBUG_SEED
// #define DEBUG_STEEPNESS
// #define DEBUG_UPDATES
#define DEBUG_HUD_POS
#define DEBUG_HUD_TIMES
#define DEBUG_HUD_LOADED
#pragma warning disable 0168

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
/// Acronym definitions:
/// P = Player
/// T = Terrain
/// F = First
/// L = Last
/// X = xpos
/// Y = ypos

// allows for the multi-dim List
public class MultiDimDictList<K, T> : Dictionary<K, List<T>> { }

[Serializable] public class GeneratorModes {
  // Number of pixels to update per chunk per frame
  [Range(0, 100000)] public int HeightmapSpeed = 1000;
  public bool DisplaceDivide = false;
  public bool Reach = false;
  public bool Cube = false;
  public bool Perlin = true;
  public bool Distort = false;
}
[Serializable] public class Times {
  public GUIText deltaNextUpdate;
  public GUIText deltaTimes;
  [Range(0.1f, 500f)] public float UpdateSpeed = 5;
  public float lastUpdate = 0;
  public float DeltaDivide = 0;
  public float DeltaFractal = 0;
  public float DeltaGenerate = 0;
  public float DeltaGenerateTerrain = 0;
  public float DeltaGenerateWater = 0;
  public float DeltaUpdate = 0;
  public float DeltaTotal = 0;
  public float[] DeltaTotalAverageArray = new float[1000];
  public float DeltaTotalAverage = 0;
  public int avgEnd = -1;
}
public class TerrainGenTest : MonoBehaviour {
  public static float EmptyPoint = -100;

  // List of terrain data for setting heights. Equivalent to
  // terrList[].GetComponent<Terrain>().terrainData
  private List<TerrainData> terrData = new List<TerrainData>();
  // List of terrains for instantiating
  private List<GameObject> terrList = new List<GameObject>();
  // List of terrain heightmap data points for setting heights over a period of
  // time.
  private List<float[, ]> terrPoints = new List<float[, ]>();
  // List of chunks to be updated with points in terrPoints. True if points need
  // to be flushed to terrainData.
  private List<bool> terrQueue = new List<bool>();
  // List of chunks. True if all points have been defined in terrPoints.
  // Used for determining adjacent chunk heightmaps
  private List<bool> terrReady = new List<bool>();
  // List of chunks. True if the chunk needs to be unloaded.
  private List<bool> terrToUnload = new List<bool>();

  // Water Tile to instantiate with the terrain when generating a new chunk
  [SerializeField] public GameObject waterTile;
  // Player for deciding when to load chunks based on position
  [SerializeField] public GameObject player;
  // Whether or not to use the pre-determined seed or use Unity's random seed
  [SerializeField] public bool useSeed = true;
  [SerializeField] public int Seed = 4;
  // Modifier to shift the perlin noise map in order to reduce chance of finding
  // the same patch of terrain again. This value is multiplied by the seed.
  [SerializeField] public float PerlinSeedModifier = 100000.12f;
  // The GUIText object that is used to display information on the HUD
  [SerializeField] public GUIText positionInfo;
  [SerializeField] public GUIText chunkListInfo;
  // Show list of available terrain generators
  [SerializeField] public GeneratorModes GenMode;
  // Distance from chunk for it to be loaded
  [SerializeField] public int loadDist = 50;  // chunk load distance
  // Roughness of terrain is modified by this value
  [SerializeField] public float roughness = 0.3f;
  // Vertical shift of values pre-rectification
  [SerializeField] public float yShift = 0.0f;
  // Number of terrain chunks to generate initially before the player spawns
  [SerializeField] public int maxX = 2;
  [SerializeField] public int maxZ = 2;
  // Array of textures to apply to the terrain
  // TODO(Campbell): Actually do something useful with this
  [SerializeField] public Texture2D[] TerrainTextures;
  // GUIText of debugging data to show on the HUD
  [SerializeField] public Times times;
  // Used to identify the corners of the loaded terrain when not generating in a
  // radius from the player
  private Terrain lastLoaded, firstLoaded;
  int terrWidth;  // Used to space the terrains when instantiating.
  int terrLength; // Size of the terrain chunk in normal units.
  int heightmapWidth;  // The size of an individual heightmap of each chunk.
  int heightmapHeight;
  int width;  // Total size of heightmaps combined
  int height;
  int logCount = 1;
  float lastUpdate;
  float PeakModifier = 1;
  int lastTerrUpdateLoc = 0;
  int lastTerrUpdated = 0;
  float[, ] TerrUpdatePoints;
  float lowest = 1.0f;
  float highest = 0.0f;
  String LoadedChunkList = "";

  void Start() {
    for(int i=0; i<times.DeltaTotalAverageArray.Length; i++) {
      times.DeltaTotalAverageArray[i] = -1;
    }

    times.lastUpdate = Time.time;
    if (GenMode.Perlin && useSeed) Seed = (int)(100 * UnityEngine.Random.value);
    if (Seed == 0) Seed++;
    GenerateTerrainChunk(0,0);
    FractalNewTerrains(0,0);
    terrData[0].SetHeights(0,0, terrPoints[0]);
    for(int x=0; x<maxX; x++) {
      for(int z=0; z<maxZ; z++) {
        GenerateTerrainChunk(x,z);
        FractalNewTerrains(x,z);
        int terrID = GetTerrainWithCoord(x,z);
        terrData[terrID].SetHeights(0, 0, terrPoints[terrID]);
        terrQueue[terrID] = false;
        terrToUnload[terrID] = false;
        terrReady[terrID] = true;
      }
    }

    /* times.DeltaGenerate =
       (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);
    iTime = Time.realtimeSinceStartup;
    FractalTerrains();
    times.DeltaFractal =
        (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);*/
    float playerX = maxX * firstLoaded.terrainData.size.x / 2f;
    float playerZ = maxZ * firstLoaded.terrainData.size.z / 2f;
    // float playerY = 700f;
    float playerY =
        terrList[GetTerrainWithCoord(maxX / 2, maxZ / 2)]
            .GetComponent<Terrain>()
            .SampleHeight(new Vector3(playerX, 0, playerZ));
            // .terrainData.GetHeight((int)(maxX / 2 / heightmapWidth),
            //                       (int)(maxZ / 2 / heightmapHeight));
    (player.GetComponent<InitPlayer>()).go(playerX, playerY, playerZ);
    TerrUpdatePoints = new float[ heightmapWidth, heightmapHeight ];
  }

  void Update() {  // generates terrain based on player transform and generated
                   // terrain
// Determines when to load terrain by the corner terrain chunks of the
// currently loaded map and compare their X and Z positions to
// the player's position which is 1playerunit/2terrainunit (player x and z
// increment 1/2 as fast as the terrain x and z over the same distance).

// TODO:
// Terrain should be stored in an ArrayList/List (DONE)
// When loading, add and remove Terrain from an ArrayList/List
// Generate Terrain based on neighbor

#if DEBUG_POSITION
    Debug.Log ("P: ("
            + player.transform.position.x
            + ", " + player.transform.position.y
            + ", " + player.transform.position.z
            + "), TF: ("
            + firstLoaded.transform.position.x
            + ", " + firstLoaded.transform.position.y
            + ", " + firstLoaded.transform.position.z
            + "), TL: ("
            + lastLoaded.transform.position.x
            + ", " + lastLoaded.transform.position.y
            + ", " + lastLoaded.transform.position.z
            + ")");
#endif

    for (int i = 0; i < terrList.Count; i++) {
      if (!terrList[i]) {
        terrList.RemoveAt(i);
        terrData.RemoveAt(i);
        terrPoints.RemoveAt(i);
        terrReady.RemoveAt(i);
        terrQueue.RemoveAt(i);
        terrToUnload.RemoveAt(i);
        i--;
      }
    }

    // Make sure the player stays above the terrain
    /* if(player.transform.position.y < 0) {
      (player.GetComponent<InitPlayer>()).updatePosition(
          player.transform.position.x,
          602.0f,
          player.transform.position.z);
    } */
    int xCenter = Mathf.RoundToInt(
        (player.transform.position.x - terrWidth / 2) / terrWidth);
    int yCenter = Mathf.RoundToInt(
        (player.transform.position.z - terrLength / 2) / terrLength);
    int radius = Mathf.RoundToInt(loadDist / ((terrWidth + terrLength) / 2.0f));
    // float PlayerX = xCenter;
    // float PlayerZ = yCenter;
    // float PlayerX = player.transform.position.x;
    // float PlayerZ = player.transform.position.z;
    // int terrLoc = GetTerrainWithCoord((int)(PlayerX / terrWidth),
    // (int)(PlayerZ / terrLength));
    int terrLoc = GetTerrainWithCoord(xCenter,yCenter);
    if(terrLoc != -1) {
      float TerrainHeight =
          terrList[terrLoc].GetComponent<Terrain>().SampleHeight(
              player.transform.position /*new Vector3(PlayerX,0,PlayerZ)*/);

#if DEBUG_HUD_POS
      positionInfo.text =
          "Joystick(" + Input.GetAxis("Mouse X") + ", " +
          Input.GetAxis("Mouse Y") + ")(" + Input.GetAxis("Horizontal") + ", " +
          Input.GetAxis("Vertical") + "\n" + "Player" +
          player.transform.position + "\n" + "Coord(" + (int)(xCenter) + ", " +
          (int)(yCenter) + ")(" + terrLoc + ")\n" + "TerrainHeight: " +
          TerrainHeight + "\nHighest Point: " + highest + "\nLowest Point: " +
          lowest;
#endif
      if(player.transform.position.y < TerrainHeight - 10.0f) {
        Debug.Log("Player at " + player.transform.position +
              "\nCoord: (" + (int)(xCenter)
                + ", " + (int)(yCenter) + ")" +
              "\nPlayer(" + player.transform.position + ")" +
              "\nSampleHeight: " + TerrainHeight +
              "\n\n"
        );
        (player.GetComponent<InitPlayer>())
            .updatePosition(player.transform.position.x, TerrainHeight,
                            player.transform.position.z);
      }
    }



    float iTime = -1;
    bool done = false;
#if DEBUG_HUD_LOADED
    LoadedChunkList = "x: " + xCenter + ", y: " + yCenter + ", r: " + radius + "\n";
#endif

    for (int i = 0; i < terrToUnload.Count; i++) {
      terrToUnload[i] = true;
    }

    for (int x = xCenter - radius; x <= xCenter; x++) {
      for (int y = yCenter - radius; y <= yCenter; y++) {
        int xSym = xCenter - (x - xCenter);
        int ySym = yCenter - (y - yCenter);
        // don't have to take the square root, it's slow
        if ((x - xCenter) * (x - xCenter) + (y - yCenter) * (y - yCenter) <=
            radius * radius) {
          if (GetTerrainWithCoord(x, y) == -1) {
            if (iTime == -1) iTime = Time.realtimeSinceStartup;
            if (!done) {
              GenerateTerrainChunk(x, y);
              FractalNewTerrains(x, y);
              done = true;
            }
          } else {
#if DEBUG_HUD_LOADED
            LoadedChunkList += "+(" + x + ", " + y + ") ";
#endif
            terrToUnload[GetTerrainWithCoord(x, y)] = false;
          }
          if (!(x == xSym && y == ySym) &&
              GetTerrainWithCoord(xSym, ySym) == -1) {
            if (iTime == -1) iTime = Time.realtimeSinceStartup;
            if (!done) {
              GenerateTerrainChunk(xSym, ySym);
              FractalNewTerrains(xSym, ySym);
              done = true;
            }
          } else {
#if DEBUG_HUD_LOADED
            LoadedChunkList += "+(" + xSym + ", " + ySym + ") ";
#endif
            if (GetTerrainWithCoord(xSym, ySym) != -1) {
              try {
                terrToUnload[GetTerrainWithCoord(xSym, ySym)] = false;
              } catch (ArgumentOutOfRangeException e) {
                Debug.Log("(" + xSym + ", " + ySym + "): " +
                          GetTerrainWithCoord(xSym, ySym) + " Out of Range (" +
                          terrToUnload.Count + ")");
              }
            }
          }
          if (y != ySym && GetTerrainWithCoord(x, ySym) == -1) {
            if (iTime == -1) iTime = Time.realtimeSinceStartup;
            if (!done) {
              GenerateTerrainChunk(x, ySym);
              FractalNewTerrains(x, ySym);
              done = true;
            }
          } else {
#if DEBUG_HUD_LOADED
            LoadedChunkList += "+(" + x + ", " + ySym + ") ";
#endif
            if (GetTerrainWithCoord(x, ySym) != -1) {
              try {
                terrToUnload[GetTerrainWithCoord(x, ySym)] = false;
              } catch (ArgumentOutOfRangeException e) {
                Debug.Log("(" + xSym + ", " + ySym + "): " +
                          GetTerrainWithCoord(xSym, ySym) + " Out of Range (" +
                          terrToUnload.Count + ")");
              }
            }
          }
          if (x != xSym && GetTerrainWithCoord(xSym, y) == -1) {
            if (iTime == -1) iTime = Time.realtimeSinceStartup;
            if (!done) {
              GenerateTerrainChunk(xSym, y);
              FractalNewTerrains(xSym, y);
              done = true;
            }
          } else {
#if DEBUG_HUD_LOADED
            LoadedChunkList += "+(" + xSym + ", " + y + ") ";
#endif
            if (GetTerrainWithCoord(xSym, y) != -1) {
              try {
                terrToUnload[GetTerrainWithCoord(xSym, y)] = false;
              } catch (ArgumentOutOfRangeException e) {
                Debug.Log("(" + xSym + ", " + ySym + "): " +
                          GetTerrainWithCoord(xSym, ySym) + " Out of Range (" +
                          terrToUnload.Count + ")");
              }
            }
          }

          // (x, y), (x, ySym), (xSym , y), (xSym, ySym) are in the circle
        } else {
#if DEBUG_HUD_LOADED
          LoadedChunkList +=
              "-(" + Mathf.RoundToInt(x) + ", " + Mathf.RoundToInt(y) + ")\n";
#endif
        }
      }
      LoadedChunkList += "\n";
    }

    int tileCnt = -1;
    if (lastTerrUpdated <= 0 || terrList.Count <= lastTerrUpdated ||
        !terrQueue[lastTerrUpdated]) {
      for (int i = 0; i < terrQueue.Count; i++) {
        if (terrQueue[i]) {
          tileCnt = i;
          Debug.Log("Loading Chunk " + tileCnt + " (" + GetXCoord(tileCnt) +
                    ", " + GetZCoord(tileCnt) + ")");
          break;
        }
      }
    } else {
      tileCnt = lastTerrUpdated;
    }
    if (tileCnt > 0 && terrList.Count > tileCnt && terrList[tileCnt] &&
        terrQueue[tileCnt]) {
      int lastTerrUpdateLoc_ = lastTerrUpdateLoc;
      for (int i = lastTerrUpdateLoc;
           i < lastTerrUpdateLoc + GenMode.HeightmapSpeed; i++) {
        int z = i % heightmapHeight;
        int x = (int)Math.Floor((float)i / heightmapWidth);
#if DEBUG_HEIGHTS
        if(x < heightmapWidth)
          Debug.Log("Update Coord: (" + z + ", " + x + ")\nI: " + i +
                    "\nLastUpdateLoc: " + lastTerrUpdateLoc + "\nUpdateSpeed: "
                    + GenMode.HeightmapSpeed + "\nHeight: " +
                    terrPoints[tileCnt][ z, x ] + "\nLoc: " + tileCnt);
#endif
        if (x >= heightmapWidth) {
          terrQueue[tileCnt] = false;
          lastTerrUpdateLoc = 0;
          lastTerrUpdated = 0;
          break;
        }
        try {
          TerrUpdatePoints[ z, x ] = terrPoints[tileCnt][ z, x ];
        } catch (ArgumentOutOfRangeException e) {
          Debug.LogError("Failed to read terrPoints(err1) " + tileCnt + " x:" +
                         z + ", z:" + x + "\n\n" + e);
          break;
        } catch (NullReferenceException e) {
          Debug.LogError("Failed to read terrPoints(err2) " + tileCnt + "\n\n" +
                         e);
          break;
        } catch (IndexOutOfRangeException e) {
          Debug.LogError("Failed to read terrPoints(err3) " + tileCnt + " x:" +
                         z + ", z:" + x + "\n\n" + e);
        }
        lastTerrUpdateLoc_++;
      }
      terrData[tileCnt].SetHeights(0, 0, TerrUpdatePoints);
      terrList[tileCnt].GetComponent<Terrain>().Flush();
      // lastTerrUpdateLoc += GenMode.HeightmapSpeed - 1;
      lastTerrUpdateLoc = lastTerrUpdateLoc_;
      lastTerrUpdated = tileCnt;
      if (!terrQueue[tileCnt]) {
        TerrUpdatePoints = new float[ heightmapHeight, heightmapWidth ];
        lastTerrUpdateLoc = 0;
        lastTerrUpdated = 0;
      }
    }

    if (Time.time > times.lastUpdate + times.UpdateSpeed) {
      float iTime2 = Time.realtimeSinceStartup;
      UpdateTerrainNeighbors(
          (int)Math.Floor(player.transform.position.x / 500),
          (int)Math.Floor(player.transform.position.z / 500));
#if DEBUG_UPDATES
      Debug.Log("Updating Neighbors(" +
                (int)Math.Floor(player.transform.position.x / 500) + ", " +
                (int)Math.Floor(player.transform.position.z / 500) + ")");
#endif
      times.lastUpdate = Time.time;
      times.DeltaUpdate =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime2) * 1000);
    }

    LoadedChunkList += "\nUnloading: ";
    for (int i = 0; i < terrToUnload.Count; i++) {
      if (terrToUnload[i]) {
        LoadedChunkList += "(" + GetXCoord(i) + ", " + GetZCoord(i) + "), ";
        UnloadTerrainChunk(i);
      }
    }

    chunkListInfo.text = LoadedChunkList;

    if (iTime > -1) {
      times.DeltaTotal =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);
      times.avgEnd++;
      if(times.avgEnd >= times.DeltaTotalAverageArray.Length) times.avgEnd=0;
      times.DeltaTotalAverageArray[times.avgEnd] = times.DeltaTotal;
      times.DeltaTotalAverage = 0;
      int DeltaNum = 0;
      for(int i=0; i<times.DeltaTotalAverageArray.Length; i++) {
        if(times.DeltaTotalAverageArray[i] != -1) {
          times.DeltaTotalAverage += times.DeltaTotalAverageArray[i];
          DeltaNum++;
        }
      }
      times.DeltaTotalAverage/=(float)DeltaNum;
    }
#if DEBUG_HUD_TIMES
    times.deltaNextUpdate.text =
        (times.UpdateSpeed - (int)Math.Ceiling(Time.time - times.lastUpdate))
            .ToString() +
        "s";
    times.deltaTimes.text =
        "Delta Times:\n" + "Generate(" + times.DeltaGenerate + "ms)<--" + "T(" +
        times.DeltaGenerateTerrain + "ms)<--" + "W(" +
        times.DeltaGenerateWater + "ms),\n" + "Fractal(" + times.DeltaFractal +
        "ms)<--" + "Divide(" + times.DeltaDivide + "ms),\n" + "Last Total(" +
        times.DeltaTotal + "ms) Avg: " + times.DeltaTotalAverage + ",\n" +
        "Update Neighbors(" + times.DeltaUpdate + "ms)";
#endif
  }

  void UpdateTerrainNeighbors(int X, int Z, int count = 2) {
    if (count > 0) {
      // Debug.Log("Updating (" + X + ", " + Z + ") (" + count + ")");
      Terrain LeftTerr = null, TopTerr = null, RightTerr = null,
              BottomTerr = null;
      try {
        LeftTerr =
            terrList[GetTerrainWithCoord(X - 1, Z)].GetComponent<Terrain>();
        UpdateTerrainNeighbors(X - 1, Z, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        TopTerr =
            terrList[GetTerrainWithCoord(X, Z + 1)].GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z + 1, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        RightTerr =
            terrList[GetTerrainWithCoord(X + 1, Z)].GetComponent<Terrain>();
        UpdateTerrainNeighbors(X + 1, Z, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        BottomTerr =
            terrList[GetTerrainWithCoord(X, Z - 1)].GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z - 1, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        Terrain MidTerr =
            terrList[GetTerrainWithCoord(X, Z)].GetComponent<Terrain>();
        MidTerr.SetNeighbors(LeftTerr, TopTerr, RightTerr, BottomTerr);
      } catch (ArgumentOutOfRangeException e) {
      }
    }
  }

  void GenerateTerrainChunk(int x, int z) {
    if(GetTerrainWithCoord(x,z) != -1) return;
    float iTime = Time.realtimeSinceStartup;
    int cntX = x;
    int cntZ = z;
    if (cntZ == 0 && cntX == 0) {
      lastLoaded = this.GetComponent<Terrain>();
      firstLoaded = this.GetComponent<Terrain>();
      terrWidth = (int)this.GetComponent<Terrain>().terrainData.size.x;
      terrLength = (int)this.GetComponent<Terrain>().terrainData.size.z;
      heightmapWidth =
          this.GetComponent<Terrain>().terrainData.heightmapWidth;
      heightmapHeight =
          this.GetComponent<Terrain>().terrainData.heightmapHeight;

      terrData.Add(GetComponent<Terrain>().terrainData);
      terrList.Add(this.gameObject);
      terrPoints.Add(new float[ terrWidth, terrLength ]);
      terrQueue.Add(false);
      terrToUnload.Add(false);
      terrReady.Add(false);
      UpdateTexture(terrData[terrList.Count-1]);
      terrList[terrList.Count-1].name = "Terrain(" + cntX + "," + cntZ + ")";
#if DEBUG_MISC
      Debug.Log("Added Terrain (0,0){" + terrList.Count-1 + "}");
#endif
      // lastX = cntX;
      // lastZ = cntZ;
      gBigSize = terrWidth + terrLength;
    } else {
      float iTime2 = Time.realtimeSinceStartup;
      terrData.Add(new TerrainData() as TerrainData);
      terrData[terrList.Count-1].heightmapResolution = terrData[0].heightmapResolution;
      terrData[terrList.Count-1].size = terrData[0].size;
      UpdateTexture(terrData[terrList.Count-1]);

      terrList.Add(Terrain.CreateTerrainGameObject(terrData[terrList.Count-1]));
      terrList[terrList.Count-1].name = "Terrain(" + cntX + "," + cntZ + ")";
      terrList[terrList.Count - 1].transform.Translate(cntX * terrWidth, 0f,
                                                       cntZ * terrLength);
      times.DeltaGenerateTerrain =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime2) * 1000);
      // Add Water
      iTime2 = Time.realtimeSinceStartup;
      Vector3 terrVector3 =
          terrList[terrList.Count-1].GetComponent<Terrain>().transform.position;
      Vector3 waterVector3 = terrVector3;
      waterVector3.y += 150;
      waterVector3.x += terrWidth/2;
      waterVector3.z += terrLength/2;
      Instantiate(waterTile, waterVector3, Quaternion.identity,
                  terrList[terrList.Count - 1].transform);
      times.DeltaGenerateWater =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime2) * 1000);

      int thisLoc = GetTerrainWithCoord(cntX, cntZ);
      int lastLoadedLoc = GetTerrainWithData(lastLoaded);
      int firstLoadedLoc = GetTerrainWithData(firstLoaded);

#if DEBUG_ARRAY
      Debug.Log("This: " + thisLoc + ", First: " + firstLoadedLoc + ", Last: " +
                lastLoadedLoc);
#endif

      if (thisLoc != -1) {
        if (terrList[thisLoc].GetComponent<Terrain>().transform.position.x >=
                terrList[lastLoadedLoc]
                    .GetComponent<Terrain>()
                    .transform.position.x &&
            terrList[thisLoc].GetComponent<Terrain>().transform.position.z >=
                terrList[lastLoadedLoc]
                    .GetComponent<Terrain>()
                    .transform.position.z &&
            lastLoadedLoc != -1) {
          lastLoaded = terrList[thisLoc].GetComponent<Terrain>();
        }
        if (terrList[thisLoc].GetComponent<Terrain>().transform.position.x <=
                terrList[firstLoadedLoc]
                    .GetComponent<Terrain>()
                    .transform.position.x &&
            terrList[thisLoc].GetComponent<Terrain>().transform.position.z <=
                terrList[firstLoadedLoc]
                    .GetComponent<Terrain>()
                    .transform.position.z &&
            firstLoadedLoc != -1) {
          firstLoaded = terrList[thisLoc].GetComponent<Terrain>();
        }
      }
      terrPoints.Add(new float[ terrWidth, terrLength ]);
      terrQueue.Add(false);
      terrToUnload.Add(false);
      terrReady.Add(false);

      times.DeltaGenerate =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);
    }
  }

  void FractalNewTerrains(int changeX, int changeZ) {
    float iTime = Time.realtimeSinceStartup;

    int tileCnt;
    if (changeX == 0 && changeZ == 0) {
      tileCnt = 0;
    } else {
      tileCnt = GetTerrainWithCoord(changeX, changeZ);
    }
#if DEBUG_ARRAY
    Debug.Log("TileCount: " + tileCnt + ", X: " + changeX + " Z: " + changeZ +
              "\nterrData.Count: " + terrData.Count + ", terrData[tileCnt]: " +
              terrData[tileCnt] + "\nTerrain Name: " + terrList[tileCnt].name);
#endif
    try {
      terrPoints[tileCnt] = GenerateNew(changeX, changeZ, roughness);
      terrQueue[tileCnt] = true;
      terrReady[tileCnt] = true;
#if DEBUG_HEIGHTS
      Debug.Log(
          "Top Right = " +
          terrPoints[tileCnt][ heightmapWidth - 1, heightmapHeight - 1 ] +
          "\nBottom Right = " + terrPoints[tileCnt][ heightmapWidth - 1, 0 ] +
          "\nBottom Left = " + terrPoints[tileCnt][ 0, 0 ] + "\nTop Left = " +
          terrPoints[tileCnt][ 0, heightmapHeight - 1 ]);
#endif
#if DEBUG_STEEPNESS
      Debug.Log(
          "Terrain Steepness(0,0): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              0, 0) +
          "\nTerrain Steepness(513,0): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              1, 0) +
          "\nTerrain Steepness(0,513): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              0, 1) +
          "\nTerrain Steepness(513,513): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              1, 1) +
          "\nTerrain Height(0,0): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(0,
                                                                          0) +
          "\nTerrain Height(513,0): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(1,
                                                                          0) +
          "\nTerrain Height(0,513): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(0,
                                                                          1) +
          "\nTerrain Height(513,513): " +
          terrList[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(1,
                                                                          1));
#endif
    } catch (ArgumentOutOfRangeException e) {
      Debug.LogError("Invalid tileCnt: " + tileCnt +
                     "\nTried to find Terrain(" + changeX + "," + changeZ +
                     ")\n\n" + e);
    }
    times.DeltaFractal =
        (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);
  }

  public void UnloadTerrainChunk(int X, int Z) {
    UnloadTerrainChunk(GetTerrainWithCoord(X, Z));
  }
  public void UnloadTerrainChunk(int loc) {
    if( loc == 0 ) return;
    Destroy ( terrList[loc] );
    Debug.Log("Flagging Chunk for unload " + loc + " (" + GetXCoord(loc) + ", " +
              GetZCoord(loc) + ")");
    // terrList.RemoveAt(loc);
    // terrList[loc].name = "DESTROY " + terrList[loc].name;
    // terrData.RemoveAt(loc);
    terrReady[loc] = false;
    terrQueue[loc] = false;
    // terrToUnload.RemoveAt(loc);
    if (lastTerrUpdated == loc) {
      lastTerrUpdateLoc = -1;
      lastTerrUpdated = -1;
    }
  }

  float gRoughness;
  float gBigSize;

  public float[, ] GenerateNew(int changeX, int changeZ, float iRoughness) {
    float iTime = Time.realtimeSinceStartup;
    float iHeight = heightmapHeight;
    float iWidth = heightmapWidth;
    float[, ] points = new float[ (int)iWidth, (int)iHeight ];

    for (int r = 0; r < iHeight; r++) {
      for (int c = 0; c < iHeight; c++) {
        points[ r, c ] = EmptyPoint;
      }
    }

    // Generate heightmap of points by averaging all surrounding points then
    // displacing.
    if (useSeed) {
      UnityEngine.Random.seed =
          (int)(Seed + PerfectlyHashThem((short)changeX, (short)changeZ));
      // UnityEngine.Random.seed = Seed;
    }
#if DEBUG_SEED
    Debug.Log("Seed: (" + changeX + ", " + changeZ + ") = " +
              UnityEngine.Random.seed);
#endif
#if DEBUG_HEIGHTS || DEBUG_ARRAY
    Debug.Log(terrList[GetTerrainWithCoord(changeX, changeZ)]
                  .GetComponent<Terrain>()
                  .name +
              ",(0,0): " + points[ 0, 0 ]);
    Debug.Log(terrList[GetTerrainWithCoord(changeX, changeZ)]
                  .GetComponent<Terrain>()
                  .name +
              ",(" + iWidth + "," + iHeight + "): " +
              points[ (int)iWidth - 1, (int)iHeight - 1 ]);
#endif

    gRoughness = iRoughness;

    logCount = 11;
    if(!(changeX == 0 && changeZ == 0)) {
      MatchEdges(iWidth, iHeight, changeX, changeZ, ref points);
    } else if(true) {
      for (int r = 0; r < 4; r++) {
        for (int c = 0; c < iHeight; c++) {
          int i,j;
          switch(r){
            case 0:
              i = 0;
              j = c;
              break;
            case 1:
              i = c;
              j = (int)iHeight-1;
              break;
            case 2:
              i = (int)iWidth-1;
              j = c;
              break;
            case 3:
              i = c;
              j = 0;
              break;
            default:
              i = 0;
              j = 0;
              break;
          }
          points[ i, j ] = 0.5f;
        }
      }
    }

    float iTime2 = Time.realtimeSinceStartup;

    PeakModifier = UnityEngine.Random.value / 4 + 0.5f;
    if(GenMode.Perlin) {
      PerlinDivide(ref points, changeX, changeZ, iWidth, iHeight);
    } else if (GenMode.DisplaceDivide) {
      DivideNewGrid(ref points, 0, 0, iWidth, iHeight, points[ 0, 0 ],
                    points[ 0, (int)iHeight - 1 ],
                    points[ (int)iWidth - 1, (int)iHeight - 1 ],
                    points[ (int)iWidth - 1, 0 ]);
      MatchEdges(iWidth, iHeight, changeX, changeZ, ref points);
    }

    times.DeltaDivide =
        (int)Math.Ceiling((Time.realtimeSinceStartup - iTime2) * 1000);


#if DEBUG_HEIGHTS
    Debug.Log("Fractal:\n"
        + "C1: (0, 0):      " + points[ 0, 0 ] + ",\n"
        + "C2: (0, " + (int)(iHeight - 1) + "):      " +
          points[ 0, (int)iHeight - 1 ] + ",\n"
        + "C3: (" + (int)(iWidth - 1) + ", " + (int)(iHeight - 1) + "):      " +
          points[ (int)iWidth - 1, (int)iHeight - 1 ] + ",\n"
        + "C4: (" + (int)(iWidth - 1) + ", 0):      " +
          points[ (int)iWidth - 1, 0 ] + ",\n"

        + "Edge1: (0, " + (int)Math.Floor((iHeight) / 2) + "):      " +
          points[ 0, (int)Math.Floor((iHeight) / 2) ] + ",\n"
        + "Edge2: (" + (int)Math.Floor((iWidth) / 2) + ", " +
          (int)(iHeight - 1) + "):      " +
          points[ (int)Math.Floor((iWidth) / 2), (int)iHeight - 1 ] + ",\n"
        + "Edge3: (" + (int)(iWidth - 1) + ", " +
          (int)Math.Floor((iHeight - 1) / 2) + "):      " +
          points[ (int)iWidth - 1, (int)Math.Floor((iHeight) / 2) ] + ",\n"
        + "Edge4: (" + (int)Math.Floor((iWidth) / 2) + ", 0):      " +
          points[ (int)Math.Floor((iWidth) / 2), 0 ] + ",\n"

        + "Middle: (" + (int)Math.Floor((iWidth - 1) / 2) + ", " +
          (int)Math.Floor((iHeight - 1) / 2) + "):      " +
          points[
            (int)Math.Floor((iWidth - 1) / 2),
            (int)Math.Floor((iHeight - 1) / 2)
          ]
        + ",\n"
        + "\n"
        + "changeX: " + changeX + ", changeZ: " + changeZ + ", iWidth: " +
        iWidth + ", iHeight: " + iHeight);
#endif

    // float[, ] flippedPoints = new float[ (int)iHeight, (int)iWidth ];
    float[, ] flippedPoints = points;
    if(!GenMode.Perlin) {
      for (int r = 0; r < iWidth; r++) {
        for (int c = 0; c < iHeight; c++) {
          flippedPoints[ c, r ] = points[ r, c ];
          if (flippedPoints[ c, r ] <= 0) {
            float p1 = EmptyPoint, p2 = EmptyPoint, p3 = EmptyPoint,
                  p4 = EmptyPoint;
            if (c > 0) p1 = flippedPoints[ c - 1, r ];
            if (p1 <= 0) p1 = EmptyPoint;
            if (r > 0) p2 = flippedPoints[ c, r - 1 ];
            if (p2 <= 0) p2 = EmptyPoint;
            if (c < iHeight - 1) p3 = flippedPoints[ c + 1, r ];
            if (p3 <= 0) p3 = EmptyPoint;
            if (r < iWidth - 1) p4 = flippedPoints[ c, r + 1 ];
            if (p4 <= 0) p4 = EmptyPoint;
            float p = AverageCorners(p1, p2, p3, p4);
            if (p == EmptyPoint)
              p = Displace(iWidth + iHeight);
            else
              Displace(0);
            flippedPoints[ c, r ] = p;
          } else {
            Displace(0);
          }
        }
      }
    }
    // SmoothEdges(iWidth, iHeight, ref flippedPoints);

    times.DeltaFractal =
        (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);

    return flippedPoints;
    // return points;
  }

  public void MatchEdges(float iWidth, float iHeight, int changeX, int changeZ,
                  ref float[, ] points, bool flipped = true) {
// Set the edge of the new chunk to the same values as the bordering chunks.
// This is to create uniformity between chunks.
#if DEBUG_HEIGHTS
    Debug.Log(
        "(0,0) InterpolatedHeight = " +
        (terrList[0].GetComponent<Terrain>().terrainData.GetInterpolatedHeight(
             0, 0) /
         terrList[0].GetComponent<Terrain>().terrainData.size.y));
#endif
    int b1 = GetTerrainWithCoord(changeX - 1, changeZ);  // Left
    int b2 = GetTerrainWithCoord(changeX, changeZ + 1);  // Top
    int b3 = GetTerrainWithCoord(changeX + 1, changeZ);  // Right
    int b4 = GetTerrainWithCoord(changeX, changeZ - 1);  // Bottom
    float[, ] newpoints = points;
    if(b1 >= 0 && terrReady[b1]) {
#if DEBUG_HEIGHTS
      Debug.Log("Border1(0,0): " + terrPoints[b1][ 0, 0 ]);
#endif
      for (int i = 0; i < iHeight; i++) {
        if(!flipped)
          newpoints[ 0, i ] = terrPoints[b1][ (int)iWidth - 1, i ];
        else
          newpoints[ 0, i ] = terrPoints[b1][ i, (int)iWidth - 1 ];
      }
    }
    if (b2 >= 0 && terrReady[b2]) {  // top
#if DEBUG_HEIGHTS
      Debug.Log("Border2(0,0): " + terrPoints[b2][ 0, 0 ]);
#endif
      for (int i = 0; i < iWidth; i++) {
        if(!flipped)
          newpoints[ i, (int)iHeight - 1 ] = terrPoints[b2][ i, 0 ];
        else
          newpoints[ i, (int)iHeight - 1 ] = terrPoints[b2][ 0, i ];
      }
    }
    if (b3 >= 0 && terrReady[b3]) {  // right
#if DEBUG_HEIGHTS
      Debug.Log("Border3(0,0): " + terrPoints[b3][ 0, 0 ]);
#endif
      for (int i = 0; i < iHeight; i++) {
        if(!flipped)
          newpoints[ (int)iWidth - 1, i ] = terrPoints[b3][ 0, i ];
        else
          newpoints[ (int)iWidth - 1, i ] = terrPoints[b3][ i, 0 ];
      }
    }
    if (b4 >= 0 && terrReady[b4]) {  // bottom
#if DEBUG_HEIGHTS
      Debug.Log("Border4(0,0): " + terrPoints[b4][ 0, 0 ]);
#endif
      for (int i = 0; i < iWidth; i++) {
        if(!flipped)
          newpoints[ i, 0 ] = terrPoints[b4][ i, (int)iHeight - 1 ];
        else
          newpoints[ i, 0 ] = terrPoints[b4][ (int)iHeight - 1, i ];
      }
    }
    /* for (int i = 0; i < iWidth * iHeight; i++) {
      int x = i % (int)iWidth;
      int z = (int)Math.Floor((float)i / iHeight);
      points[ x, z ] = newpoints[ x, z ];
      // points[x,z] = 0.5f;
    } */
    points = newpoints;
  }

  public void SmoothEdges(float iWidth, float iHeight, ref float[, ] points) {
    // Left
    for (int r = 1; r < iHeight - 1; r++) {
      for (int c = 1; c < iWidth / 2; c++) {
        // float mod = 1f-((float)Math.Pow(c,2))/((float)Math.Pow(c,2)+1f);
        float mod = c / (iWidth / 2);
        points[ c, r ] = (points[ 0, r ] * mod) + (points[ c, r ] * (1 - mod));
      }
    }
    // Top
    for (int c = 1; c < iWidth - 1; c++) {
      for (int r = (int)iHeight - 2; r > iHeight / 2; r--) {
        // float mod = 1f-((float)Math.Pow(r,2))/((float)Math.Pow(r,2)+1f);
        float mod = r / (iHeight / 2);
        points[ c, r ] = (points[ c, (int)iHeight - 1 ] * mod) +
                         (points[ c, r ] * (1 - mod));
      }
    }
    // Right
    for (int r = 1; r < iHeight - 1; r++) {
      for (int c = (int)iWidth - 2; c > iWidth / 2; c--) {
        // float mod = 1f-((float)Math.Pow(c,2))/((float)Math.Pow(c,2)+1f);
        float mod = c / (iWidth / 2);
        points[ c, r ] =
            (points[ (int)iWidth - 1, r ] * mod) + (points[ c, r ] * (1 - mod));
      }
    }
    // Bottom
    for (int c = 1; c < iWidth - 1; c++) {
      for (int r = 1; r < iHeight / 2; r++) {
        // float mod = 1f-((float)Math.Pow(r,2))/((float)Math.Pow(r,2)+1f);
        float mod = r / (iHeight / 2);
        points[ c, r ] = (points[ 0, r ] * mod) + (points[ c, r ] * (1 - mod));
      }
    }
  }

  public void DivideGrid(ref float[, ] points, float dX, float dY, float dwidth,
                  float dheight, float c1, float c2, float c3, float c4) {
    if (logCount > 0) {
      Debug.Log("DX: " + dX + ", DY: " + dY + ", dwidth: " + dwidth +
                ", dheight: " + dheight);
      logCount--;
    }
    float Edge1, Edge2, Edge3, Edge4, Middle;
    float newWidth = (float)Math.Floor(dwidth / 2);
    float newHeight = (float)Math.Floor(dheight / 2);
    if (dwidth > 1 || dheight > 1) {
      Middle =
          ((c1 + c2 + c3 + c4) / 4) +
          Displace(newWidth + newHeight);  // Randomly displace the midpoint!
      Edge1 = ((c1 + c2) / 2);
      Edge2 = ((c2 + c3) / 2);
      Edge3 = ((c3 + c4) / 2);
      Edge4 = ((c4 + c1) / 2);

      // Make sure that the midpoint doesn't accidentally "randomly displaced"
      // past the boundaries!
      Middle = Rectify(Middle);
      Edge1 = Rectify(Edge1);
      Edge2 = Rectify(Edge2);
      Edge3 = Rectify(Edge3);
      Edge4 = Rectify(Edge4);

      // Do the operation over again for each of the four new grids.
      DivideGrid(ref points, dX, dY, newWidth, newHeight, c1, Edge1, Middle,
                 Edge4);
      DivideGrid(ref points, dX + newWidth, dY, dwidth - newWidth, newHeight,
                 Edge1, c2, Edge2, Middle);
      DivideGrid(ref points, dX + newWidth, dY + newHeight, dwidth - newWidth,
                 dheight - newHeight, Middle, Edge2, c3, Edge3);
      DivideGrid(ref points, dX, dY + newHeight, newWidth, dheight - newHeight,
                 Edge4, Middle, Edge3, c4);
    } else {
      // The four corners of the grid piece will be averaged and drawn as a
      // single pixel.
      Displace(0);
      float c = (c1 + c2 + c3 + c4) / 4;
      points[ (int)(dX), (int)(dY) ] = c;
      if (dwidth == 2) {
        points[ (int)(dX + 1), (int)(dY) ] = c;
      }
      if (dheight == 2) {
        points[ (int)(dX), (int)(dY + 1) ] = c;
      }
      if ((dwidth == 2) && (dheight == 2)) {
        points[ (int)(dX + 1), (int)(dY + 1) ] = c;
      }
    }
  }

  public void DivideNewGrid(ref float[, ] points, float dX, float dY, float dwidth,
                     float dheight, float c1, float c2, float c3, float c4) {
    if (logCount > -1 && dwidth != dheight) {
      Debug.Log("Width-Height Mismatch: Expected square grid.\nDX: " + dX +
                ", DY: " + dY + ", dwidth: " + dwidth + ", dheight: " +
                dheight);
      logCount--;
    }
#if DEBUG_ATTRIBUTES
    else if (logCount > 10) {
      Debug.Log("DX: " + dX + ", DY: " + dY + ", dwidth: " + dwidth +
                ", dheight: " + dheight);
      logCount--;
    }
#endif
    // float c1, c2, c3, c4;
    float Edge1, Edge2, Edge3, Edge4, Middle;
    float newWidth = (float)Math.Floor(dwidth / 2);
    float newHeight = (float)Math.Floor(dheight / 2);
    if (dwidth > 1 || dheight > 1) {
      if (GenMode.Reach) {
        try {
          c1 = points[ (int)dX - 1, (int)dY ];
        } catch (IndexOutOfRangeException e) {
          c1 = points[ (int)dX, (int)dY ];
          /*if(logCount>0) {
Debug.Log("C1 Out");
}*/
        }
        try {
          c2 = points[ (int)dX, (int)dY + (int)dheight ];
        } catch (IndexOutOfRangeException e) {
          c2 = points[ (int)dX, (int)dY + (int)dheight - 1 ];
          /*if(logCount>0) {
Debug.Log("C2 Out");
}*/
        }
        try {
          c3 = points[ (int)dX + (int)dwidth, (int)dY + (int)dheight ];
        } catch (IndexOutOfRangeException e) {
          c3 = points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
          /*if(logCount>0) {
Debug.Log("C3 Out");
}*/
        }
        try {
          c4 = points[ (int)dX + (int)dwidth - 1, (int)dY - 1 ];
        } catch (IndexOutOfRangeException e) {
          c4 = points[ (int)dX + (int)dwidth - 1, (int)dY ];
          /*if(logCount>0) {
Debug.Log("C4 Out");
}*/
        }
      } else if (GenMode.Cube) {
        c1 = points[ (int)dX, (int)dY ];
        c2 = points[ (int)dX, (int)dY + (int)dheight - 1 ];
        c3 = points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        c4 = points[ (int)dX + (int)dwidth - 1, (int)dY ];
      }  // else: use passed in values
      Middle = points[
        (int)Math.Floor((dX + dX + dwidth) / 2),
        (int)Math.Floor((dY + dY + dheight) / 2)
      ];
      Edge1 = points[ (int)dX, (int)Math.Floor((dY + dY + dheight) / 2) ];
      Edge2 = points
          [ (int)Math.Floor((dX + dX + dwidth) / 2), (int)(dY + dheight - 1) ];
      Edge3 = points
          [ (int)(dX + dwidth - 1), (int)Math.Floor((dY + dY + dheight) / 2) ];
      Edge4 = points[ (int)Math.Floor((dX + dX + dwidth) / 2), (int)dY ];

      if (c1 < 0) {
        // c1 = UnityEngine.Random.value;
        // c1 = 0.5f + yShift;
        c1 = points[ (int)dX, (int)dY ];
        if (c1 <= EmptyPoint)
          c1 = AverageCorners(EmptyPoint, c2, c3, c4) +
               Displace(newWidth + newHeight);
        else
          Displace(0);
        if (c1 < -1)
          c1 = Displace(dwidth + dheight);
        else
          Displace(0);
      } else {
        Displace(0);
        Displace(0);  // In order to maintain consistency with the random values
                      // returned
      }
      if (c2 < 0) {
        // c2 = UnityEngine.Random.value;
        // c2 = 0.5f + yShift;
        c2 = points[ (int)dX, (int)dY + (int)dheight - 1 ];
        if (c2 <= EmptyPoint)
          c2 = AverageCorners(c1, EmptyPoint, c3, c4) +
               Displace(newWidth + newHeight);
        else
          Displace(0);
        if (c2 < -1)
          c2 = Displace(dwidth + dheight);
        else
          Displace(0);
      } else {
        Displace(0);
        Displace(0);  // In order to maintain consistency with the random values
                      // returned
      }
      if (c3 < 0) {
        // c3 = UnityEngine.Random.value;
        // c3 = 0.5f + yShift;
        c3 = points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        if (c3 <= EmptyPoint)
          c3 = AverageCorners(c1, c2, EmptyPoint, c4) +
               Displace(newWidth + newHeight);
        else
          Displace(0);
        if (c3 < -1)
          c3 = Displace(dwidth + dheight);
        else
          Displace(0);
      } else {
        Displace(0);
        Displace(0);  // In order to maintain consistency with the random values
                      // returned
      }
      if (c4 < 0) {
        // c4 = UnityEngine.Random.value;
        // c4 = 0.5f + yShift;
        c4 = points[ (int)dX + (int)dwidth - 1, (int)dY ];
        if (c4 <= EmptyPoint)
          c4 = AverageCorners(c1, c2, c3, EmptyPoint) +
               Displace(newWidth + newHeight);
        else
          Displace(0);
        if (c4 < -1)
          c4 = Displace(dwidth + dheight);
        else
          Displace(0);
      } else {
        Displace(0);
        Displace(0);  // In order to maintain consistency with the random values
                      // returned
      }
      if (Middle < 0) {
        Middle = ((c1 + c2 + c3 + c4) / 4) +
                 Displace((dwidth + dheight) *
                          PeakModifier);  // Randomly displace the midpoint!
      } else {
        Displace(0);  // In order to maintain consistency with the random values
                      // returned
      }
      if (Edge1 < 0) {
        Edge1 = ((c1 + c2) / 2);
      }
      if (Edge2 < 0) {
        Edge2 = ((c2 + c3) / 2);
      }
      if (Edge3 < 0) {
        Edge3 = ((c3 + c4) / 2);
      }
      if (Edge4 < 0) {
        Edge4 = ((c4 + c1) / 2);
      }

      bool ShowWarning = false;
      if (logCount > 0 &&
          (Edge1 < 0 || Edge2 < 0 || Edge3 < 0 || Edge4 < 0 || c1 < 0 ||
           c2 < 0 || c3 < 0 || c4 < 0)) {
        ShowWarning = true;
        Debug.LogWarning ("Divide(Pre-Rectify):\n"
					+ "C1: (0, 0):      " + points [0, 0] + "/" + c1 + "\n"
					+ "C2: (0, " + (int)(dheight - 1) + "):      " +
			  		points [0, (int)dheight - 1] + "/" + c2 + "\n"
					+ "C3: (" + (int)(dwidth - 1) + ", " + (int)(dheight - 1) + "):      " +
			  		points [(int)dwidth - 1, (int)dheight - 1] + "/" + c3 + "\n"
					+ "C4: (" + (int)(dwidth - 1) + ", 0):      " +
			  		points [(int)dwidth - 1, 0] + "/" + c4 + "\n"

					+ "Edge1: (0, " + (int)Math.Floor ((dheight) / 2) + "):      " +
			  		points [0, (int)Math.Floor ((dheight) / 2)] + "/" + Edge1 + "\n"
					+ "Edge2: (" + (int)Math.Floor ((dwidth) / 2) + ", " +
					(int)(dheight - 1) + "):      " +
			  		points [(int)Math.Floor ((dwidth) / 2), (int)dheight - 1] + "/" +
                                                                    Edge2 + "\n"
					+ "Edge3: (" + (int)(dwidth - 1) + ", " +
			  		(int)Math.Floor ((dheight - 1) / 2) + "):      " +
			  		points [(int)dwidth - 1, (int)Math.Floor ((dheight) / 2)] + "/" +
                                                                    Edge3 + "\n"
					+ "Edge4: (" + (int)Math.Floor ((dwidth) / 2) + ", 0):      " +
					  points [(int)Math.Floor ((dwidth) / 2), 0] + "/" + Edge4 + "\n"

					+ "Middle: (" + (int)Math.Floor ((dwidth - 1) / 2) + ", " +
				  	(int)Math.Floor ((dheight - 1) / 2) + "):      " +
				  	points [
                (int)Math.Floor ((dwidth - 1) / 2),
                (int)Math.Floor ((dheight - 1) / 2)
				    ]
					+ "/" + Middle + "\n"
					+ "\n"
					+ "dX: " + dX + ", dY: " + dY + ", dwidth: " +
					  dwidth + ", dheight: " + dheight);
				logCount--;
			}

      // Make sure that the midpoint doesn't accidentally randomly displace past
      // the boundaries.
      Middle = Rectify(Middle);
      Edge1 = Rectify(Edge1);
      Edge2 = Rectify(Edge2);
      Edge3 = Rectify(Edge3);
      Edge4 = Rectify(Edge4);

      points[
        (int)Math.Floor((dX + dX + dwidth) / 2),
        (int)Math.Floor((dY + dY + dheight) / 2)
      ] = Middle;

      points[ (int)dX, (int)Math.Floor((dY + dY + dheight) / 2) ] = Edge1;
      points[
        (int)Math.Floor((dX + dX + dwidth) / 2),
        (int)(dY + dheight - 1)
      ] = Edge2;
      points[
        (int)(dX + dwidth - 1), (int)Math.Floor((dY + dY + dheight) / 2)
      ] = Edge3;
      points[ (int)Math.Floor((dX + dX + dwidth) / 2), (int)dY ] = Edge4;

      // Save points to array
      c1 = Rectify(c1);
      c2 = Rectify(c2);
      c3 = Rectify(c3);
      c4 = Rectify(c4);
      points[ (int)dX, (int)dY ] = c1;
      points[ (int)dX, (int)dY + (int)dheight - 1 ] = c2;
      points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ] = c3;
      points[ (int)dX + (int)dwidth - 1, (int)dY ] = c4;

      if (ShowWarning) {
        Debug.LogWarning ("Divide(Post-Rectify):\n"
					+ "C1: (0, 0):      " + points [0, 0] + "/" + c1 + "\n"
					+ "C2: (0, " + (int)(dheight - 1) + "):      " +
			  		points [0, (int)dheight - 1] + "/" + c2 + "\n"
					+ "C3: (" + (int)(dwidth - 1) + ", " + (int)(dheight - 1) + "):      " +
			  		points [(int)dwidth - 1, (int)dheight - 1] + "/" + c3 + "\n"
					+ "C4: (" + (int)(dwidth - 1) + ", 0):      " +
			  		points [(int)dwidth - 1, 0] + "/" + c4 + "\n"

					+ "Edge1: (0, " + (int)Math.Floor ((dheight) / 2) + "):      " +
			  		points [0, (int)Math.Floor ((dheight) / 2)] + "/" + Edge1 + "\n"
					+ "Edge2: (" + (int)Math.Floor ((dwidth) / 2) + ", " +
					(int)(dheight - 1) + "):      " +
			  		points [(int)Math.Floor ((dwidth) / 2), (int)dheight - 1] + "/" +
                                                                    Edge2 + "\n"
					+ "Edge3: (" + (int)(dwidth - 1) + ", " +
			  		(int)Math.Floor ((dheight - 1) / 2) + "):      " +
			  		points [(int)dwidth - 1, (int)Math.Floor ((dheight) / 2)] + "/" +
                                                                    Edge3 + "\n"
					+ "Edge4: (" + (int)Math.Floor ((dwidth) / 2) + ", 0):      " +
					  points [(int)Math.Floor ((dwidth) / 2), 0] + "/" + Edge4 + "\n"

					+ "Middle: (" + (int)Math.Floor ((dwidth - 1) / 2) + ", " +
				  	(int)Math.Floor ((dheight - 1) / 2) + "):      " +
				  	points [
                (int)Math.Floor ((dwidth - 1) / 2),
                (int)Math.Floor ((dheight - 1) / 2)
				    ]
					+ "/" + Middle + "\n"
					+ "\n"
					+ "dX: " + dX + ", dY: " + dY + ", dwidth: " +
					  dwidth + ", dheight: " + dheight);
			}

      // Do the operation over again for each of the four new
      // grids.
      DivideNewGrid(ref points, dX, dY, newWidth, newHeight, c1, Edge1, Middle, Edge4);
      DivideNewGrid(ref points, dX + newWidth, dY, newWidth, newHeight, Edge4, Middle, Edge3, c4);
      DivideNewGrid(ref points, dX + newWidth, dY + newHeight, newWidth, newHeight, Middle, Edge2, c3, Edge3);
      DivideNewGrid(ref points, dX, dY + newHeight, newWidth, newHeight, Edge1, c2, Edge2, Middle);

    } else {
      if (dheight < 1) dheight = 1;
      if (dwidth < 1) dwidth = 1;
      if (GenMode.Cube || GenMode.Reach) {
        c1 = points[ (int)dX, (int)dY ];
        c2 = points[ (int)dX, (int)dY + (int)dheight - 1 ];
        c3 = points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        c4 = points[ (int)dX + (int)dwidth - 1, (int)dY ];
      }  // else: use passed in values
      // The four corners of the grid piece will be averaged and drawn as a
      // single pixel.
      float c = (c1 + c2 + c3 + c4) / 4;
      points[ (int)(dX), (int)(dY) ] = c;
      if (dwidth == 2) {
        points[ (int)(dX + 1), (int)(dY) ] = c;
      }
      if (dheight == 2) {
        points[ (int)(dX), (int)(dY + 1) ] = c;
      }
      if ((dwidth == 2) && (dheight == 2)) {
        points[ (int)(dX + 1), (int)(dY + 1) ] = c;
      }
    }
  }

 private
  void PerlinDivide(ref float[, ] points, float x, float y, float w, float h) {
    float xShifted = (x * (w - 1)) + (Seed * PerlinSeedModifier);
    float yShifted = (y * (h - 1)) + (Seed * PerlinSeedModifier);
    // Debug.Log("xShifted: " + xShifted + "(" + (xShifted/(w-1)) +
    // ")\nyShifted: " + yShifted + "(" + (yShifted/(h-1)) + ")");
    for (int r = 0; r < h; r++) {
      for (int c = 0; c < w; c++) {
        if (GenMode.Distort) {
          float noise =
              Mathf.PerlinNoise(roughness * (xShifted + c) / (w - 1f),
                                roughness * (yShifted + r) / (h - 1f));
          // float noise = (r + c) / (w + h);
          float f1 = Mathf.Log(1 - noise) * -roughness * 0.3f;
          float f2 = -1 / (1 + Mathf.Pow(Math.E, 10 * (noise - 0.90f))) + 1;
          // e approx 2.718
          float blendStart = 0.9f;
          float blendEnd = 1.0f;
          if (noise > 0 && noise <= blendStart)
            points[ r, c ] = f1 + yShift;
          else if (noise < blendEnd && noise > blendStart)
            points[ r, c ] =
                ((f1 * ((blendEnd - blendStart) - (noise - blendStart))) +
                 (f2 * (noise - blendStart))) /
                    (blendEnd - blendStart) +
                yShift;
          else
            points[ r, c ] = f2 + yShift;
        } else {
          float noise =
              3.0f * roughness *
                  Mathf.PerlinNoise(
                      Mathf.Pow(roughness, 1.2f) * (xShifted + r) / (w - 1f),
                      Mathf.Pow(roughness, 1.2f) * (yShifted + c) / (h - 1f)) +
              yShift;

          points[ r, c ] = noise;
        }
        if (points[ r, c ] < lowest) lowest = points[ r, c ];
        if (points[ r, c ] > highest) highest = points[ r, c ];
        // points[r,c] = Mathf.Log(1 - noise) * -roughness * 0.5f;
        // points[r,c] = -1/(1+Mathf.Pow(2.718f, 20 * (noise - 0.85f))) + 1;
      }
    }
  }

  private float Rectify(float iNum) {
    if (iNum < 0 + yShift) {
      iNum = 0 + yShift;
    } else if (iNum > 1.0 + yShift) {
      iNum = 1.0f + yShift;
    }
    return iNum;
  }

  private float Displace(float SmallSize) {
    float Max = SmallSize / gBigSize * gRoughness;
    return (float)(UnityEngine.Random.value - 0.5) * Max;
  }

  private void UpdateTexture(TerrainData terrainData, int num = 0) {
    // TerrainData terrainData = terrain.GetComponent<TerrainData>();
    SplatPrototype[] tex = new SplatPrototype[TerrainTextures.Length];
    for (int i = 0; i < TerrainTextures.Length; i++) {
      tex[i] = new SplatPrototype();
      tex[i].texture = TerrainTextures[i];  // Sets the texture
      tex[i].tileSize = new Vector2(1, 1);  // Sets the size of the texture
    }
    terrainData.splatPrototypes = tex;
    // terrainData = Terrain.CreateTerrainGameObject
    // (terrainData).GetComponent<TerrainData> ();
  }

  private int GetTerrainWithCoord(int x, int z) {

    for (int i = 0; i < terrList.Count; i++) {
#if DEBUG_ARRAY
      Debug.Log(terrList[i].name + "==Terrain(" + x + "," + z + ")" + " [" +
                terrList[i].name.Equals("Terrain(" + x + "," + z + ")") + "]{" +
                i + "}");
#endif
      if (terrList[i].name.Equals("Terrain(" + x + "," + z + ")")) {
        return i;
      }
    }
    // Debug.LogError ("Could not find terrain with coord (" + x + "," + z +
    // ")");
    return -1;
  }

  private int GetTerrainWithData(TerrainData terr) {
    for (int i = 0; i < terrList.Count; i++) {
#if DEBUG_ARRAY
      Debug.Log(terrData[i] + "==" + terr + " [" + (terrData[i] == terr) +
                " (" + i + ")]");
#endif
      if (terrData[i] == terr) {
        return i;
      }
    }
    return -1;
  }

  private int GetTerrainWithData(Terrain terr) {
    for (int i = 0; i < terrList.Count; i++) {
#if DEBUG_ARRAY
      Debug.Log(terrList[i].name + "==" + terr.name + " [" +
                (terrList[i].name == terr.name) + " (" + i + ")]");
#endif
      if (terrList[i].name == terr.name) {
        return i;
      }
    }
    return -1;
  }

  private int GetXCoord(int index) {
    string name = terrList[index].name;
    int start = -1, end = -1;
    for (int i = 0; i < name.Length; i++) {
      if (name[i].Equals('(')) {
        start = i + 1;
      } else if (name[i].Equals(',')) {
        end = i;
        break;
      }
    }
    string coord = "";
    for (int i = start; i < end; i++) {
      coord += name[i];
    }
    int output;
    if (!Int32.TryParse(coord, out output)) {
      Debug.LogError("Failed to parse X Coordinate(" + index + "): " + coord +
                     "\n" + name + "," + terrList[index].name);
      return -1;
    } else {
      return output;
    }
  }

  private int GetZCoord(int index) {
    string name = terrList[index].name;
    int start = -1, end = -1;
    for (int i = 0; i < name.Length; i++) {
      if (name[i].Equals(',')) {
        start = i + 1;
      } else if (name[i].Equals(')')) {
        end = i;
        break;
      }
    }
    string coord = "";
    for (int i = start; i < end; i++) {
      coord += name[i];
    }
    int output;
    if (!Int32.TryParse(coord, out output)) {
      Debug.LogError("Failed to parse Z Coordinate(" + index + "): " + coord +
                     "\n" + name + "," + terrList[index].name);
      return -1;
    } else {
      return output;
    }
  }

  private float AverageCorners(float c1, float c2, float c3, float c4) {
    int numAvg = 0;
    float Avg = 0;
    if (c1 != EmptyPoint) {
      Avg += c1;
      numAvg++;
    }
    if (c2 != EmptyPoint) {
      Avg += c2;
      numAvg++;
    }
    if (c3 != EmptyPoint) {
      Avg += c3;
      numAvg++;
    }
    if (c4 != EmptyPoint) {
      Avg += c4;
      numAvg++;
    }
    if (numAvg == 0) {
      return EmptyPoint;
    }
    return Avg / numAvg;
  }
  public int PerfectlyHashThem(short a, short b)
  {
      var A = (uint)(a >= 0 ? 2 * a : -2 * a - 1);
      var B = (uint)(b >= 0 ? 2 * b : -2 * b - 1);
      var C = (int)((A >= B ? A * A + A + B : A + B * B) / 2);
      return a < 0 && b < 0 || a >= 0 && b >= 0 ? C : -C - 1;
  }
}
