////////////////////////////////////////////////////////////////////
// WARNING: MANY DEBUG SETTINGS MAY CAUSE IMMENSE AMOUNTS OF LAG! //
//                      USE WITH CAUTION!                         //
////////////////////////////////////////////////////////////////////
// #define DEBUG_ARRAY
// #define DEBUG_ATTRIBUTES
// #define DEBUG_BORDERS_1 // LEFT -x
// #define DEBUG_BORDERS_2 // TOP +z
// #define DEBUG_BORDERS_3 // RIGHT +x
// #define DEBUG_BORDERS_4 // BOTTOM -z
// #define DEBUG_CHUNK_LOADING
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
  [Header("Displace Divide")]
  // Enables generator mode that displaces points randomly within ranges that
  // vary by the distance to the center of the chunk and averages surrounding
  // points.
  public bool DisplaceDivide = false;
  // A modifier to Displace Divide that causes terrain to be smoother.
  public bool Reach = false;
  // A modifier to Displace Divide that causes the terrain to look cube-ish.
  public bool Cube = false;

  [Header("Perlin Noise")]
  // Uses Perlin noise to generate terrain.
  public bool Perlin = true;
  // A modifier to Perlin Noise that exaggerates heights increasingly the higher
  // they get.
  public bool Distort = false;

  [Header("Settings")]
  // Used in a Lerp function between DisplaceDivide and Perlin.
  [Range(0.0f, 1.0f)]public float mixtureAmount = 0.5f;
  // Number of pixels to update per chunk per frame
  [Range(0, 129*129)] public int HeightmapSpeed = 1000;
}
[Serializable] public class Times {
  // Shows a countdown until neighbors get updated again.
  public GUIText deltaNextUpdate;
  // Shows timing for all other measured events.
  public GUIText deltaTimes;
  // Time between neighbor updates in seconds.
  [Range(0.1f, 500f)] public float UpdateSpeed = 5;
  // Previous amount of time updating neighbors took in milliseconds.
  public float lastUpdate = 0;
  // Previous amount of time calculating heightmap data took in milliseconds.
  public float DeltaDivide = 0;
  // Previous amount of time calculating heightmap data and initializing arrays
  // took in milliseconds. (Theoretically should be similar to DeltaDivide?)
  public float DeltaFractal = 0;
  // Previous amount of time instantiating a new chunk took in milliseconds.
  public float DeltaGenerate = 0;
  // Previous amount of time instantiating a new terrain component of a chunk
  // took in milliseconds.
  public float DeltaGenerateTerrain = 0;
  // Previous amount of time instantiating a new water component of a chunk took
  // in milliseconds.
  public float DeltaGenerateWater = 0;
  // Same as DeltaDivide but includes time it took to flip points and possible
  // additional logging.
  public float DeltaGenerateHeightmap = 0;
  // Amount of time it took to update the previous chunk textures. This is
  // a part of DeltaGenerate.
  public float DeltaTextureUpdate = 0;
  // Previous amount of time updating neighbors took in milliseconds.
  public float DeltaUpdate = 0;
  // Previous amount of time it took for all steps to occur during one frame if
  // something significant happened, otherwise it does not change until a
  // significant event occurs.
  public float DeltaTotal = 0;
  // Previous 1000 values of DeltaTotal for use in calculating the average total
  // amount of time this script takes during a frame while doing everything.
  public float[] DeltaTotalAverageArray = new float[1000];
  // Average total amount of time this script takes in one frame while doing
  // everything necessary.
  public float DeltaTotalAverage = 0;
  // Placeholder for next location to store DeltaTotal in DeltaTotalAverageArray
  // which is especially important when the array is full and we loop back from
  // the beginning and overwrite old data.
  public int avgEnd = -1;
}
public class Terrains {
  // List of terrain data for setting heights. Equivalent to
  // terrList[].GetComponent<Terrain>().terrainData
  public TerrainData terrData;
  // List of terrains for instantiating
  public GameObject terrList;
  // List of terrain heightmap data points for setting heights over a period of
  // time.
  public float[, ] terrPoints;
  public float[, ] terrPerlinPoints;
  public bool texQueue = false;
  // List of chunks to be updated with points in terrPoints. True if points need
  // to be flushed to terrainData.
  public bool terrQueue = false;
  // List of chunks. True if all points have been defined in terrPoints.
  // Used for determining adjacent chunk heightmaps
  public bool terrReady = false;
  // List of chunks. True if the chunk needs to be unloaded.
  public bool terrToUnload = false;

}
public class TerrainGenerator : MonoBehaviour {
  public static float EmptyPoint = -100;

  public List<Terrains> terrains = new List<Terrains>();

  [Header("Game Objects")]
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
  [Header("HUD Text for Debug")]
  // The GUIText object that is used to display information on the HUD
  [SerializeField] public GUIText positionInfo;
  [SerializeField] public GUIText chunkListInfo;
  // GUIText of debugging data to show on the HUD
  [SerializeField] public Times times;
  [Header("Generator Settings")]
  // Show list of available terrain generators
  [SerializeField] public GeneratorModes GenMode;
  // Distance from chunk for it to be loaded
  [SerializeField] public int loadDist = 50;  // chunk load distance
  // Roughness of terrain is modified by this value
  [SerializeField] public float roughness = 1.0f;
  [SerializeField] public float PerlinRoughness = 0.2f;
  // Maximum height of Perlin Generator in percentage.
  [SerializeField] public float PerlinHeight = 0.8f;
  // Vertical shift of values pre-rectification
  [SerializeField] public float yShift = 0.0f;
  // Number of terrain chunks to generate initially before the player spawns
  private int maxX = 1;
  private int maxZ = 1;
  [Header("Visuals")]
  // Array of textures to apply to the terrain
  [SerializeField] public Texture2D[] TerrainTextures;
  int terrWidth;  // Used to space the terrains when instantiating.
  int terrLength; // Size of the terrain chunk in normal units.
  int heightmapWidth;  // The size of an individual heightmap of each chunk.
  int heightmapHeight;
  // Used to identify the corners of the loaded terrain when not generating in a
  // radius from the player
  int width;  // Total size of heightmaps combined
  int height;
  // Remaining number of messages to send to the console. Setting a limit
  // greatly improves performance since the maximum messages is limited, and it
  // makes reading the output easier since there are fewer lines to look at.
  int logCount = 1;
  float lastUpdate;
  float PeakModifier = 1;
  // Previous terrain index whose heightmap was being applied.
  int lastTerrUpdateLoc = 0;
  // Chunk whose heightmap is being applied.
  Terrain lastTerrUpdated = new Terrain();
  // Array of points currently applied to the chunk.
  float[, ] TerrUpdatePoints;
  // Array of points to be applied to the chunk.
  float[, ] TerrTemplatePoints;
  // Lowest and highest points of loaded terrain.
  float lowest = 1.0f;
  float highest = 0.0f;
  // List of chunks loaded as a list of coordinates.
  String LoadedChunkList = "";

  void Start() {
    for (int i = 0; i < times.DeltaTotalAverageArray.Length; i++) {
      times.DeltaTotalAverageArray[i] = -1;
    }

    times.lastUpdate = Time.time;
    if (GenMode.Perlin && !useSeed)
      Seed = (int)(500 * UnityEngine.Random.value);
    if (Seed == 0) Seed = 0;
    if (GenMode.Perlin)
      Debug.Log("Seed*PerlinSeedModifier=" + Seed * PerlinSeedModifier);
    if (GenMode.Perlin && GenMode.DisplaceDivide) {
      Debug.Log("Adjusting Roughness");
      roughness *= 2f;
    }

    Debug.Log("Generating spawn chunk");
    // Initialize variables based off of values defining the terrain and add
    // the spawn chunk to arrays for later reference.
    GenerateTerrainChunk(0, 0);
    Debug.Log("Creating spawn chunk fractal");
    // Generate height map.
    FractalNewTerrains(0, 0);
    Debug.Log("Applying spawn chunk height map");
    terrains[0].terrData.SetHeights(0, 0, MixHeights(0));
    // Load chunks before player spawns to hide chunk loading. (Not really used)
    for (int x = 0; x < maxX; x++) {
      for (int z = 0; z < maxZ; z++) {
        Debug.Log("Repeating for chunk (" + x + ", " + z + ")");
        GenerateTerrainChunk(x, z);
        FractalNewTerrains(x, z);
        int terrID = GetTerrainWithCoord(x, z);
        terrains[terrID].terrData.SetHeights(0, 0, MixHeights(terrID));
        terrains[terrID].terrQueue = false;
        terrains[terrID].terrToUnload = false;
        terrains[terrID].terrReady = true;
      }
    }

    // Choose player spawn location based off of the center of all pre-loaded
    // chunks.
    float playerX = maxX * terrains[0].terrData.size.x / 2f;
    float playerZ = maxZ * terrains[0].terrData.size.z / 2f;
    // Get the player spawn height from the heightmap height at the coordinates
    // where the player will spawn.
    float playerY = terrains[GetTerrainWithCoord(maxX / 2, maxZ / 2)]
                        .terrList.GetComponent<Terrain>()
                        .SampleHeight(new Vector3(playerX, 0, playerZ));

    // TODO: Remove the try-catch because they are ugly.
    try {
      // Tell the player where to spawn.
      (player.GetComponent<InitPlayer>()).go(playerX, playerY, playerZ);
    } catch (NullReferenceException e) {
      Debug.LogError("Invalid Player or Player does not have InitPlayer");
    }
    TerrUpdatePoints = new float[ heightmapWidth, heightmapHeight ];
    TerrTemplatePoints = new float[ heightmapWidth, heightmapHeight ];
    Debug.Log("Initialization done!");
  }

  void Update() {
// Generates terrain based on player transform and generated terrain.
// Loads chunks in a circle centered on the player and unloads all other chunks
// that are not within this circle. The spawn chunk is exempt because it may not
// be unloaded.

#if DEBUG_POSITION
    Debug.Log ("P: ("
            + player.transform.position.x
            + ", " + player.transform.position.y
            + ", " + player.transform.position.z
            + ")");
#endif

    // Remove all undefined chunks from the array because they have been unloaded.
    for (int i = 0; i < terrains.Count; i++) {
      if (!terrains[i].terrList) {
        terrains.RemoveAt(i);
        i--;
      }
    }

    // Make sure the player stays above the terrain
    int xCenter = Mathf.RoundToInt(
        (player.transform.position.x - terrWidth / 2) / terrWidth);
    int yCenter = Mathf.RoundToInt(
        (player.transform.position.z - terrLength / 2) / terrLength);
    int radius = Mathf.RoundToInt(loadDist / ((terrWidth + terrLength) / 2.0f));
    int terrLoc = GetTerrainWithCoord(xCenter, yCenter);
    if (terrLoc != -1) {
      float TerrainHeight =
          terrains[terrLoc].terrList.GetComponent<Terrain>().SampleHeight(
              player.transform.position);

#if DEBUG_HUD_POS
      positionInfo.text =
          "Joystick(" + Input.GetAxis("Mouse X") + ", " +
          Input.GetAxis("Mouse Y") + ")(" + Input.GetAxis("Horizontal") + ", " +
          Input.GetAxis("Vertical") + "\n" + "Player" +
          player.transform.position + "\n" + "Coord(" + (int)(xCenter) + ", " +
          (int)(yCenter) + ")(" + terrLoc + ")\n" + "TerrainHeight: " +
          TerrainHeight + "\nHighest Point: " + highest + "\nLowest Point: " +
          lowest;
#else
      if (positionInfo != null) positionInfo.text = "";
#endif
      if (player.transform.position.y < TerrainHeight - 10.0f) {
        Debug.Log("Player at " + player.transform.position + "\nCoord: (" +
                  (int)(xCenter) + ", " + (int)(yCenter) + ")" + "\nPlayer(" +
                  player.transform.position + ")" + "\nTerrain Height: " +
                  TerrainHeight + "\n\n");
        // TODO: Remove try catch because it is ugly.
        try {
          (player.GetComponent<InitPlayer>())
              .updatePosition(player.transform.position.x, TerrainHeight,
                              player.transform.position.z);
        } catch (NullReferenceException e) {
          Debug.LogError("Invalid Player");
        }
      }
    }

    float iTime = -1;
    bool done = false;
#if DEBUG_HUD_LOADED
    LoadedChunkList =
        "x: " + xCenter + ", y: " + yCenter + ", r: " + radius + "\n";
#endif

    // Flag all chunks to be unloaded. If they should not be unloaded, they will
    // be flagged to stay loaded while finding all chunks to load before any
    // chunks are actually unloaded.
    for (int i = 0; i < terrains.Count; i++) {
      terrains[i].terrToUnload = true;
    }

    // Load chunks within radius, but only one per frame to help with
    // performance.
    for (int x = xCenter; x > xCenter - radius; x--) {
      for (int y = yCenter; y > yCenter - radius; y--) {
        int xSym = xCenter - (x - xCenter);
        int ySym = yCenter - (y - yCenter);
        // don't have to take the square root, it's slow
        if ((x - xCenter) * (x - xCenter) + (y - yCenter) * (y - yCenter) <=
            radius * radius) {
          // If the chunk has not been loaded yet, create it. Otherwise, make
          // sure the chunk doesn't get unloaded.
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
            // TODO: Remove try-catch because it's ugly. Also, I do not think
            // this one is necessary, this should never fail.
            try {
              terrains[GetTerrainWithCoord(x, y)].terrToUnload = false;
            } catch (ArgumentOutOfRangeException e) {
              Debug.Log("(" + x + ", " + y + "): " + GetTerrainWithCoord(x, y) +
                        " Out of Range (" + terrains.Count + ")");
            }
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
                terrains[GetTerrainWithCoord(xSym, ySym)].terrToUnload = false;
              } catch (ArgumentOutOfRangeException e) {
                Debug.Log("(" + xSym + ", " + ySym + "): " +
                          GetTerrainWithCoord(xSym, ySym) + " Out of Range (" +
                          terrains.Count + ")");
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
                terrains[GetTerrainWithCoord(x, ySym)].terrToUnload = false;
              } catch (ArgumentOutOfRangeException e) {
                Debug.Log("(" + xSym + ", " + ySym + "): " +
                          GetTerrainWithCoord(xSym, ySym) + " Out of Range (" +
                          terrains.Count + ")");
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
                terrains[GetTerrainWithCoord(xSym, y)].terrToUnload = false;
              } catch (ArgumentOutOfRangeException e) {
                Debug.Log("(" + xSym + ", " + ySym + "): " +
                          GetTerrainWithCoord(xSym, ySym) + " Out of Range (" +
                          terrains.Count + ")");
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
#if DEBUG_HUD_LOADED
      LoadedChunkList += "\n";
#endif
    }

    // Delay applying textures until later if a chunk was loaded this frame to
    // help with performance.
    bool skipTextureApplication = done;
    bool textureUpdated = false;
    if (!skipTextureApplication) {
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].texQueue) {
          UpdateTexture(terrains[i].terrData);
          terrains[i].texQueue = false;
          textureUpdated = true;
          break;
        }
      }
    }

    // Delay applying a heightmap if a chunk was loaded or textures were updated
    // on a chunk to help with performance.
    bool skipHeightmapApplication = done || textureUpdated;
    if (!skipHeightmapApplication) {
      // Find next chunk that needs heightmap to be applied or continue the last
      // chunk if it was not finished.
      int tileCnt = GetTerrainWithData(lastTerrUpdated);
      if (tileCnt <= 0 || !terrains[tileCnt].terrQueue) {
        for (int i = 0; i < terrains.Count; i++) {
          if (terrains[i].terrQueue) {
            tileCnt = i;
            lastTerrUpdateLoc = 0;
            TerrTemplatePoints = MixHeights(tileCnt);
            break;
          }
        }
      }
      // Apply heightmap to chunk GenMode.HeightmapSpeed points at a time.
      if (tileCnt > 0 && terrains[tileCnt].terrQueue) {
        int lastTerrUpdateLoc_ = lastTerrUpdateLoc;
        for (int i = lastTerrUpdateLoc_;
             i < lastTerrUpdateLoc_ + GenMode.HeightmapSpeed; i++) {
          int z = i % heightmapHeight;
          int x = (int)Math.Floor((float)i / heightmapWidth);
#if DEBUG_CHUNK_LOADING
          if (x < heightmapWidth)
            Debug.Log("Update Coord: (" + z + ", " + x + ")\nI: " + i +
                      "\nLastUpdateLoc: " + lastTerrUpdateLoc +
                      "\nUpdateSpeed: " + GenMode.HeightmapSpeed +
                      "\nHeight: " + terrains[tileCnt].terrPoints[ z, x ] +
                      "\nLoc: " + tileCnt);
#endif
          // Heightmap is done being applied, remove it from the queue and flag
          // it for texturing.
          if (x >= heightmapWidth) {
            terrains[tileCnt].terrQueue = false;
            terrains[tileCnt].texQueue = true;
            break;
          }

          // TODO: Remove try-catches because they are ugly.
          try {
            TerrUpdatePoints[ z, x ] = TerrTemplatePoints[ z, x ];
          } catch (ArgumentOutOfRangeException e) {
            Debug.LogError("Failed to read terrPoints(err1) " + tileCnt +
                           " x:" + z + ", z:" + x + "\n\n" + e);
            break;
          } catch (NullReferenceException e) {
            Debug.LogError("Failed to read terrPoints(err2) " + tileCnt +
                           "\n\n" + e);
            break;
          } catch (IndexOutOfRangeException e) {
            Debug.LogError("Failed to read terrPoints(err3) " + tileCnt +
                           " x:" + z + ", z:" + x + "\n\n" + e);
            break;
          }

          lastTerrUpdateLoc++;
        }

        // TODO: Remove try-catch.
        try {
          // Set the terrain heightmap to the defined points.
          terrains[tileCnt].terrData.SetHeights(0, 0, TerrUpdatePoints);
        } catch (ArgumentException e) {
          Debug.LogWarning(
              "TerrUpdatePoints is incorrect size " + heightmapHeight + "x" +
              heightmapWidth + " instead of " +
              terrains[tileCnt].terrData.heightmapWidth + "x" +
              terrains[tileCnt].terrData.heightmapHeight + "\n" + e);
        }

        // Push all changes to the terrain.
        terrains[tileCnt].terrList.GetComponent<Terrain>().Flush();
        // Make sure this chunk will continue being updated next frame.
        lastTerrUpdated = terrains[tileCnt].terrList.GetComponent<Terrain>();
        // The terrain has been removed from the queue so we should allow for
        // the another chunk to be loaded.
        if (!terrains[tileCnt].terrQueue) {
          TerrUpdatePoints = new float[ heightmapHeight, heightmapWidth ];
          lastTerrUpdateLoc = 0;
          lastTerrUpdated = new Terrain();
        }
      }
    }

    // Update terrain neighbors every times.UpdateSpeed seconds.
    // TODO: Is this even necessary? I don't really know what this does.
    if (Time.time > times.lastUpdate + times.UpdateSpeed) {
      float iTime2 = Time.realtimeSinceStartup;
      UpdateTerrainNeighbors(
          (int)Math.Floor(player.transform.position.x / terrWidth),
          (int)Math.Floor(player.transform.position.z / terrWidth));
#if DEBUG_UPDATES
      Debug.Log("Updating Neighbors(" +
                (int)Math.Floor(player.transform.position.x / terrWidth) +
                ", " +
                (int)Math.Floor(player.transform.position.z / terrWidth) + ")");
#endif
      times.lastUpdate = Time.time;
      times.DeltaUpdate =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime2) * 1000);
    }

#if DEBUG_HUD_LOADED
    LoadedChunkList += "\nUnloading: ";
#endif
    // Unload all chunks flagged for unloading.
    for (int i = 0; i < terrains.Count; i++) {
      if (terrains[i].terrToUnload) {
#if DEBUG_HUD_LOADED
        LoadedChunkList += "(" + GetXCoord(i) + ", " + GetZCoord(i) + "), ";
#endif
        UnloadTerrainChunk(i);
      }
    }

    chunkListInfo.text = LoadedChunkList;

    // Figure out timings and averages.
    if (iTime > -1) {
      times.DeltaTotal =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);
      times.avgEnd++;
      if (times.avgEnd >= times.DeltaTotalAverageArray.Length) times.avgEnd = 0;
      times.DeltaTotalAverageArray[times.avgEnd] = times.DeltaTotal;
      times.DeltaTotalAverage = 0;
      int DeltaNum = 0;
      for (int i = 0; i < times.DeltaTotalAverageArray.Length; i++) {
        if (times.DeltaTotalAverageArray[i] != -1) {
          times.DeltaTotalAverage += times.DeltaTotalAverageArray[i];
          DeltaNum++;
        }
      }
      times.DeltaTotalAverage /= (float)DeltaNum;
    }
#if DEBUG_HUD_TIMES
    times.deltaNextUpdate.text =
        (times.UpdateSpeed - (int)Math.Ceiling(Time.time - times.lastUpdate))
            .ToString() +
        "s";
    times.deltaTimes.text =
        "Delta Times:\n" +
        "Generate(" + times.DeltaGenerate + "ms)<--" +
          "T(" + times.DeltaGenerateTerrain + "ms)<--" +
          "W(" + times.DeltaGenerateWater + "ms)<--" +
          "Tex("+ times.DeltaTextureUpdate + "ms),\n" +
        "Heightmap(" + times.DeltaGenerateHeightmap + "ms)\n" +
        "Fractal(" + times.DeltaFractal + "ms)<--" +
          "Divide(" + times.DeltaDivide + "ms),\n" +
        "Last Total(" + times.DeltaTotal + "ms) " +
          "Avg: " + times.DeltaTotalAverage + ",\n" +
        "Update Neighbors(" + times.DeltaUpdate + "ms)";
#else
    if (times.deltaTimes != null) times.deltaTimes.text = "";
#endif
  }

  void UpdateTerrainNeighbors(int X, int Z, int count = 2) {
    if (count > 0) {
      Terrain LeftTerr = null, TopTerr = null, RightTerr = null,
              BottomTerr = null;
      try {
        LeftTerr = terrains[GetTerrainWithCoord(X - 1, Z)]
                       .terrList.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X - 1, Z, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        TopTerr = terrains[GetTerrainWithCoord(X, Z + 1)]
                      .terrList.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z + 1, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        RightTerr = terrains[GetTerrainWithCoord(X + 1, Z)]
                        .terrList.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X + 1, Z, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        BottomTerr = terrains[GetTerrainWithCoord(X, Z - 1)]
                         .terrList.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z - 1, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        Terrain MidTerr = terrains[GetTerrainWithCoord(X, Z)]
                              .terrList.GetComponent<Terrain>();
        MidTerr.SetNeighbors(LeftTerr, TopTerr, RightTerr, BottomTerr);
      } catch (ArgumentOutOfRangeException e) {
      }
    }
  }

  // Instantiate a new chunk and define its properties based off of the spawn
  // chunk.
  void GenerateTerrainChunk(int x, int z) {
    if (GetTerrainWithCoord(x, z) != -1) return;
    float iTime = Time.realtimeSinceStartup;
    int cntX = x;
    int cntZ = z;
    if (cntZ == 0 && cntX == 0) {
      terrWidth = (int)this.GetComponent<Terrain>().terrainData.size.x;
      terrLength = (int)this.GetComponent<Terrain>().terrainData.size.z;
      heightmapWidth = this.GetComponent<Terrain>().terrainData.heightmapWidth;
      heightmapHeight =
          this.GetComponent<Terrain>().terrainData.heightmapHeight;

      // Adjust heightmap by it's resolution so it appears the same no matter
      // how high resolution it is.
      roughness *= 65f / heightmapWidth;

      terrains.Add(new Terrains());
      terrains[terrains.Count - 1].terrData =
          GetComponent<Terrain>().terrainData;
      terrains[terrains.Count - 1].terrList = this.gameObject;
      terrains[terrains.Count - 1].terrPoints =
          new float[ terrWidth, terrLength ];
      terrains[terrains.Count - 1].terrPerlinPoints =
          new float[ terrWidth, terrLength ];
      UpdateTexture(terrains[terrains.Count - 1].terrData);
      terrains[terrains.Count - 1].terrList.name =
          "Terrain(" + cntX + "," + cntZ + ")";
#if DEBUG_MISC
      Debug.Log("Added Terrain (0,0){" + terrains.Count - 1 + "}");
#endif
      gBigSize = terrWidth + terrLength;
    } else {
      float iTime2 = Time.realtimeSinceStartup;

      terrains.Add(new Terrains());
      terrains[terrains.Count - 1].terrData = new TerrainData() as TerrainData;
      terrains[terrains.Count - 1].terrData.heightmapResolution =
          terrains[0].terrData.heightmapResolution;
      terrains[terrains.Count - 1].terrData.size = terrains[0].terrData.size;
      // UpdateTexture(terrains[terrains.Count - 1].terrData);

      terrains[terrains.Count - 1].terrList = Terrain.CreateTerrainGameObject(
          terrains[terrains.Count - 1].terrData);
      terrains[terrains.Count - 1].terrList.name =
          "Terrain(" + cntX + "," + cntZ + ")";
      terrains[terrains.Count - 1].terrList.transform.Translate(
          cntX * terrWidth, 0f, cntZ * terrLength);
      times.DeltaGenerateTerrain =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime2) * 1000);

      // Add Water
      iTime2 = Time.realtimeSinceStartup;
      Vector3 terrVector3 = terrains[terrains.Count - 1]
                                .terrList.GetComponent<Terrain>()
                                .transform.position;
      Vector3 waterVector3 = terrVector3;
      waterVector3.y += 150;
      waterVector3.x += terrWidth / 2;
      waterVector3.z += terrLength / 2;
      Instantiate(waterTile, waterVector3, Quaternion.identity,
                  terrains[terrains.Count - 1].terrList.transform);
      times.DeltaGenerateWater =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime2) * 1000);

      terrains[terrains.Count - 1].terrPoints =
          new float[ terrWidth, terrLength ];
      terrains[terrains.Count - 1].terrPerlinPoints =
          new float[ terrWidth, terrLength ];

      times.DeltaGenerate =
          (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);
    }
  }

  // Generate a heightmap for a chunk.
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
              "\nterrains.Count: " + terrains.Count + ", terrains[tileCnt]: " +
              terrains[tileCnt].terrData + "\nTerrain Name: " +
              terrains[tileCnt].terrList.name);
#endif
    // TODO: Remove try-catch.
    try {
      GenerateNew(changeX, changeZ, roughness);
      terrains[tileCnt].terrQueue = true;
      terrains[tileCnt].terrReady = true;
#if DEBUG_HEIGHTS
      Debug.Log("Top Right = " +
                terrains[tileCnt]
                    .terrPoints[ heightmapWidth - 1, heightmapHeight - 1 ] +
                "\nBottom Right = " +
                terrains[tileCnt].terrPoints[ heightmapWidth - 1, 0 ] +
                "\nBottom Left = " + terrains[tileCnt].terrPoints[ 0, 0 ] +
                "\nTop Left = " +
                terrains[tileCnt].terrPoints[ 0, heightmapHeight - 1 ]);
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

  // Unload a chunk by destroying its GameObject then removing it from the array
  // of all chunks. Also make sure that if it was previously applying its
  // heightmap, that it does not continue to try and apply this heightmap now
  // that the chunk is gone.
  void UnloadTerrainChunk(int X, int Z) {
    UnloadTerrainChunk(GetTerrainWithCoord(X, Z));
  }
  void UnloadTerrainChunk(int loc) {
    if (loc == 0) return;
    Destroy(terrains[loc].terrList);
    terrains.RemoveAt(loc);
    if (GetTerrainWithData(lastTerrUpdated) == loc) {
      lastTerrUpdateLoc = -1;
      lastTerrUpdated = new Terrain();
    }
  }

  float gRoughness;
  float gBigSize;

  // Called by FractalNewTerrains. Creates a iWidth x iHeight matrix of
  // interpolated heightmap points.
  void GenerateNew(int changeX, int changeZ, float iRoughness) {
    float iTime = Time.realtimeSinceStartup;
    float iHeight = heightmapHeight;
    float iWidth = heightmapWidth;
    float[, ] points = new float[ (int)iWidth, (int)iHeight ];
    float[, ] perlinPoints = new float[ (int)iWidth, (int)iHeight ];

    // Ensure the matrices begin with something other than 0 since 0 is a valid
    // point and we want to be able to find errors.
    for (int r = 0; r < iHeight; r++) {
      for (int c = 0; c < iHeight; c++) {
        points[ r, c ] = EmptyPoint;
        perlinPoints[ r, c ] = EmptyPoint;
      }
    }

    // Generate heightmap of points by averaging all surrounding points then
    // displacing.
    if (GenMode.DisplaceDivide || GenMode.Reach || GenMode.Cube ||
        GenMode.Distort) {
      // Give each chunk its own unique number which is offset by the seed so
      // that it will always generate the same way.
      if (useSeed) {
        UnityEngine.Random.InitState(
            (int)(Seed + PerfectlyHashThem((short)changeX, (short)changeZ)));
      }
#if DEBUG_SEED
      Debug.Log("Seed: (" + changeX + ", " + changeZ + ") = " +
                UnityEngine.Random.seed);
#endif
#if DEBUG_HEIGHTS || DEBUG_ARRAY
      Debug.Log(terrains[GetTerrainWithCoord(changeX, changeZ)]
                    .terrList.GetComponent<Terrain>()
                    .name +
                ",(0,0): " + points[ 0, 0 ]);
      Debug.Log(terrains[GetTerrainWithCoord(changeX, changeZ)]
                    .terrList.GetComponent<Terrain>()
                    .name +
                ",(" + iWidth + "," + iHeight + "): " +
                points[ (int)iWidth - 1, (int)iHeight - 1 ]);
#endif

      gRoughness = iRoughness;

      logCount = 11;
      // Set all borders to the middle of valid points to cause the spawn chunk
      // to be around the middle of the possible heights and has a lower chance
      // of clipping extreme heights.
      if (changeX == 0 && changeZ == 0) {
        for (int r = 0; r < 4; r++) {
          for (int c = 0; c < iHeight; c++) {
            int i, j;
            switch (r) {
              case 0:
                i = 0;
                j = c;
                break;
              case 1:
                i = c;
                j = (int)iHeight - 1;
                break;
              case 2:
                i = (int)iWidth - 1;
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
            points[ i, j ] = 0.5f + yShift;
          }
        }
      } else {
        // Make sure each chunk aligns with the one next to it before we
        // generate the heightmap so we don't get any gaps and it meshes
        // smoother.
        MatchEdges(iWidth, iHeight, changeX, changeZ, ref points);
      }
    }

    float iTime2 = Time.realtimeSinceStartup;

    // If the Perlin Noise generator is being used, create a heightmap with it.
    if (GenMode.Perlin) {
      PerlinDivide(ref perlinPoints, changeX, changeZ, iWidth, iHeight);
    }
    // If the Displace Divide generator is being used, create a heightmap with
    // this generator.
    if (GenMode.DisplaceDivide) {
      // Divide chunk into 4 sections and displace the center thus creating 4
      // more sections per section until every pixel is defined.
      PeakModifier = UnityEngine.Random.value / 4 + 0.5f;
      DivideNewGrid(ref points, 0, 0, iWidth, iHeight, points[ 0, 0 ],
                    points[ 0, (int)iHeight - 1 ],
                    points[ (int)iWidth - 1, (int)iHeight - 1 ],
                    points[ (int)iWidth - 1, 0 ]);
    }
    // If neither generator is used, then use some debugging pattern.
    if (!GenMode.DisplaceDivide && !GenMode.Perlin) {
      for (float r = 0; r < heightmapHeight; r++) {
        for (float c = 0; c < heightmapWidth; c++) {
          // points[ (int)r, (int)c ] =
          //     (Mathf.Sin(((r / heightmapHeight) + (c / heightmapWidth)) *
          //                Mathf.PI) +
          //      1f) /
          //     2f;

          points[ (int)r, (int)c ] =
              Mathf.Sqrt(changeX * changeX + changeZ * changeZ) / 10f;
        }
      }
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

    // Flip points across the line y=x because Unity is dumb and likes
    // heightmaps
    // given as (z,x).
    // This doesn't flip points anymore since this was moved to where the
    // heightmap
    // is being applied since this is easier for matching edges. This does,
    // however,
    // still check every point and find and fix any undefined points.
    float[, ] flippedPoints = points;
    for (int r = 0; r < iWidth; r++) {
      for (int c = 0; c < iHeight; c++) {
        // flippedPoints[ c, r ] = points[ r, c ];

        // If point is undefined, average surrounding points or
        // just choose a random location. Should only happen if something is
        // broken.
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
          if (p1 == EmptyPoint || p2 == EmptyPoint || p3 == EmptyPoint ||
              p4 == EmptyPoint) {
            // Warn if a point was undefined. This was happening a lot so I
            // commented this out to disable it temporarily since everything
            // still seems to be working even though not everything is being
            // defined...

            /* Debug.LogWarning("Flipping points found undefined point. (" +
                             changeX + ", " + changeZ + "),(" + c + ", " + r +
                             ")");*/
          }
          float p = AverageCorners(p1, p2, p3, p4);
          if (p == EmptyPoint) {
            p = Displace(iWidth + iHeight);
            // This is really bad. If a point is undefined and surrounded by
            // undefined points then the terrain may appear broken. This also
            // means that there is something very wrong with the generator.
            Debug.LogWarning("Flipping points found undefined area! (" + c +
                             ", " + r + ")");
          } else {
            Displace(0);
          }
          flippedPoints[ c, r ] = p;
        } else {
          Displace(0);
        }
      }
    }

    // Smooth the edge of chunks where they meet in order to hide seams better.
    // SmoothEdges(iWidth, iHeight, ref flippedPoints);

    // Double check that all the edges match up.
    MatchEdges(iWidth, iHeight, changeX, changeZ, ref flippedPoints);

    times.DeltaGenerateHeightmap =
        (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);

    int terrLoc = GetTerrainWithCoord(changeX, changeZ);
    // Save each heightmap to the array to be applied later.
    terrains[terrLoc].terrPoints = flippedPoints;
    terrains[terrLoc].terrPerlinPoints = perlinPoints;
  }

  // Set the edge of the new chunk to the same values as the bordering chunks.
  // This is to create uniformity between chunks.
  void MatchEdges(float iWidth, float iHeight, int changeX, int changeZ,
                  ref float[, ] points) {
#if DEBUG_HEIGHTS || DEBUG_BORDERS_1 || DEBUG_BORDERS_2 || DEBUG_BORDERS_3 || DEBUG_BORDERS_4
    Debug.Log("MATCHING EDGES OF CHUNK (" + changeX + ", " + changeZ + ")");
// Debug.Log(
//     "(0,0) InterpolatedHeight = " +
//     (terrains[0].terrList.GetComponent<Terrain>().terrainData.GetInterpolatedHeight(
//          0, 0) /
//      terrains[0].terrList.GetComponent<Terrain>().terrainData.size.y));
#endif
    int b1 = GetTerrainWithCoord(changeX - 1, changeZ);  // Left
    int b2 = GetTerrainWithCoord(changeX, changeZ + 1);  // Top
    int b3 = GetTerrainWithCoord(changeX + 1, changeZ);  // Right
    int b4 = GetTerrainWithCoord(changeX, changeZ - 1);  // Bottom
    float[, ] newpoints = points;
    if (b1 >= 0 && terrains[b1].terrReady) {  // left
#if DEBUG_BORDERS_1
      Debug.Log("Border1(" + (changeX - 1) + "," + changeZ + "),(0,0): " +
                terrains[b1].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iHeight; i++) {
        newpoints[ i, 0 ] = terrains[b1].terrPoints[ i, (int)iWidth - 1 ];
      }
    }
    if (b2 >= 0 && terrains[b2].terrReady) {  // top
#if DEBUG_BORDERS_2
      Debug.Log("Border2(" + changeX + "," + (changeZ + 1) + "),(0,0): " +
                terrains[b2].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iWidth; i++) {
        newpoints[ (int)iHeight - 1, i ] = terrains[b2].terrPoints[ 0, i ];
      }
    }
    if (b3 >= 0 && terrains[b3].terrReady) {  // right
#if DEBUG_BORDERS_3
      Debug.Log("Border3(" + (changeX + 1) + "," + changeZ + "),(0,0): " +
                terrains[b3].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iHeight; i++) {
        newpoints[ i, (int)iWidth - 1 ] = terrains[b3].terrPoints[ i, 0 ];
      }
    }
    if (b4 >= 0 && terrains[b4].terrReady) {  // bottom
#if DEBUG_BORDERS_4
      Debug.Log("Border4(" + changeX + "," + (changeZ - 1) + "),(0,0): " +
                terrains[b4].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iWidth; i++) {
        newpoints[ 0, i ] = terrains[b4].terrPoints[ (int)iHeight - 1, i ];
      }
    }
    points = newpoints;
  }

  // This looks at how each chunk ends a tries to match them better so the
  // transition between chunks looks better and hides seams.
  void SmoothEdges(float iWidth, float iHeight, ref float[, ] points) {
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

  // The center of the chunk is displaced a random amount from the average of
  // the four corners. It is then split into four sections, each of which has
  // its center displaced from the average of the section's for corners. This
  // then splits each section into four more sections, and repeats until every
  // point of the heightmap is defined.
  void DivideNewGrid(ref float[, ] points, float dX, float dY, float dwidth,
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
    float Edge1, Edge2, Edge3, Edge4, Middle;
    float newWidth = (float)Math.Floor(dwidth / 2);
    float newHeight = (float)Math.Floor(dheight / 2);
    if (dwidth > 1 || dheight > 1) {
      // Reach and Cube are things I tried that had interesting outcomes that I
      // thought were cool and wanted to keep, but are not supported.
      if (GenMode.Reach) {
        // TODO: Remove try-catches
        try {
          c1 = points[ (int)dX - 1, (int)dY ];
        } catch (IndexOutOfRangeException e) {
          c1 = points[ (int)dX, (int)dY ];
        }
        try {
          c2 = points[ (int)dX, (int)dY + (int)dheight ];
        } catch (IndexOutOfRangeException e) {
          c2 = points[ (int)dX, (int)dY + (int)dheight - 1 ];
        }
        try {
          c3 = points[ (int)dX + (int)dwidth, (int)dY + (int)dheight ];
        } catch (IndexOutOfRangeException e) {
          c3 = points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        }
        try {
          c4 = points[ (int)dX + (int)dwidth - 1, (int)dY - 1 ];
        } catch (IndexOutOfRangeException e) {
          c4 = points[ (int)dX + (int)dwidth - 1, (int)dY ];
        }
      } else if (GenMode.Cube) {
        c1 = points[ (int)dX, (int)dY ];
        c2 = points[ (int)dX, (int)dY + (int)dheight - 1 ];
        c3 = points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        c4 = points[ (int)dX + (int)dwidth - 1, (int)dY ];
      }
      Middle = points[
        (int)Math.Floor((dX + dX + dwidth) / 2),
        (int)Math.Floor((dY + dY + dheight) / 2)
      ];
      // Find the center point height of each side of the section.
      Edge1 = points[ (int)dX, (int)Math.Floor((dY + dY + dheight) / 2) ];
      Edge2 = points
          [ (int)Math.Floor((dX + dX + dwidth) / 2), (int)(dY + dheight - 1) ];
      Edge3 = points
          [ (int)(dX + dwidth - 1), (int)Math.Floor((dY + dY + dheight) / 2) ];
      Edge4 = points[ (int)Math.Floor((dX + dX + dwidth) / 2), (int)dY ];

      if (c1 < 0) {
        c1 = points[ (int)dX, (int)dY ];
        // Only find a value for the point of it is not already defined.
        if (c1 <= EmptyPoint)
          c1 = AverageCorners(EmptyPoint, c2, c3, c4) +
               Displace(newWidth + newHeight);
        else
          Displace(0);
        // If it is somehow defined incorrectly, give it a random value because
        // something went wrong and this is the best fix.
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
      if (logCount > 0 && (Edge1 < 0 || Edge2 < 0 || Edge3 < 0 || Edge4 < 0 ||
                           c1 < 0 || c2 < 0 || c3 < 0 || c4 < 0)) {
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

      // Save points to array
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
        (int)(dX + dwidth - 1),
        (int)Math.Floor((dY + dY + dheight) / 2)
      ] = Edge3;
      points[ (int)Math.Floor((dX + dX + dwidth) / 2), (int)dY ] = Edge4;

      // Rectify corners then save them to array.
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
      DivideNewGrid(ref points, dX, dY, newWidth, newHeight, c1, Edge1, Middle,
                    Edge4);
      DivideNewGrid(ref points, dX + newWidth, dY, newWidth, newHeight, Edge4,
                    Middle, Edge3, c4);
      DivideNewGrid(ref points, dX + newWidth, dY + newHeight, newWidth,
                    newHeight, Middle, Edge2, c3, Edge3);
      DivideNewGrid(ref points, dX, dY + newHeight, newWidth, newHeight, Edge1,
                    c2, Edge2, Middle);

    } else {
      if (dheight < 1) dheight = 1;
      if (dwidth < 1) dwidth = 1;
      if (GenMode.Cube || GenMode.Reach) {
        c1 = points[ (int)dX, (int)dY ];
        c2 = points[ (int)dX, (int)dY + (int)dheight - 1 ];
        c3 = points[ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        c4 = points[ (int)dX + (int)dwidth - 1, (int)dY ];
      }
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
      // Does not repeat since the size of this section is 1x1. This means
      // it cannot be divided further and we are done.
    }
  }

  // Uses perlin noise to define a heightmap.
  void PerlinDivide(ref float[, ] points, float x, float y, float w, float h) {
    float xShifted = (x + (Seed * PerlinSeedModifier)) * (w - 1f);
    float yShifted = (y + (Seed * PerlinSeedModifier)) * (h - 1f);
    for (int r = 0; r < h; r++) {
      for (int c = 0; c < w; c++) {
        if (GenMode.Distort) {
          float noise =
              Mathf.PerlinNoise(PerlinRoughness * (xShifted + c) / (w - 1f),
                                PerlinRoughness * (yShifted + r) / (h - 1f));
          float f1 = Mathf.Log(1 - noise) * -PerlinRoughness * 0.3f;
          float f2 = -1 / (1 + Mathf.Pow(2.718f, 10 * (noise - 0.90f))) + 1;
          // e approx 2.718
          float blendStart = 0.9f;
          float blendEnd = 1.0f;
          // Distort the heightmap.
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
          float noise = Mathf.PerlinNoise(Mathf.Pow(PerlinRoughness, 1.2f) *
                                              (xShifted + c) / (w - 1.0f),
                                          Mathf.Pow(PerlinRoughness, 1.2f) *
                                              (yShifted + r) / (h - 1.0f)) +
                        yShift;

          points[ r, c ] = noise * PerlinHeight;
        }
        // Save highest and lowest values for debugging.
        if (points[ r, c ] < lowest) lowest = points[ r, c ];
        if (points[ r, c ] > highest) highest = points[ r, c ];
      }
    }
  }

  // Squash all values to a valid range.
  float Rectify(float iNum) {
    if (iNum < 0 + yShift) {
      iNum = 0 + yShift;
    } else if (iNum > 1.0 + yShift) {
      iNum = 1.0f + yShift;
    }
    return iNum;
  }

  // Randomly choose a value in a range that becomes smaller as the section
  // being defined becomes smaller.
  float Displace(float SmallSize) {
    float Max = SmallSize / gBigSize * gRoughness;
    return (float)(UnityEngine.Random.value - 0.5) * Max;
  }

  // Create and apply a texture to a chunk.
  void UpdateTexture(TerrainData terrainData, int num = 0) {
    float iTime = Time.realtimeSinceStartup;
    SplatPrototype[] tex = new SplatPrototype[TerrainTextures.Length];
    for (int i = 0; i < TerrainTextures.Length; i++) {
      tex[i] = new SplatPrototype();
      tex[i].texture = TerrainTextures[i];  // Sets the texture
      tex[i].tileSize = new Vector2(1, 1);  // Sets the size of the texture
    }
    terrainData.splatPrototypes = tex;
    times.DeltaTextureUpdate =
        (int)Math.Ceiling((Time.realtimeSinceStartup - iTime) * 1000);
  }

  // Give array index from coordinates.
  int GetTerrainWithCoord(int x, int z) {
    for (int i = 0; i < terrains.Count; i++) {
      if (terrains[i].terrList.name.Equals("Terrain(" + x + "," + z + ")")) {
#if DEBUG_ARRAY
        Debug.Log(
            terrains[i].terrList.name + "==Terrain(" + x + "," + z + ")" +
            " [" +
            terrains[i].terrList.name.Equals("Terrain(" + x + "," + z + ")") +
            "]{" + i + "}");
#endif
        return i;
      }
    }
    return -1;
  }

  // Give array index by comparing TerrainData.
  int GetTerrainWithData(TerrainData terr) {
    for (int i = 0; i < terrains.Count; i++) {
#if DEBUG_ARRAY
      Debug.Log(terrains[i].terrData + "==" + terr + " [" +
                (terrains[i].terrData == terr) + " (" + i + ")]");
#endif
      if (terrains[i].terrList && terrains[i].terrData == terr) {
        return i;
      }
    }
    return -1;
  }

  // Give array index by comparing Terrain GameObjects.
  int GetTerrainWithData(Terrain terr) {
    for (int i = 0; i < terrains.Count; i++) {
      // TODO: Remove try-catch.
      try {
#if DEBUG_ARRAY
        Debug.Log(terrains[i].terrList.name + "==" + terr.name + " [" +
                  (terrains[i].terrList.name == terr.name) + " (" + i + ")]");
#endif
        if (terrains[i].terrList && terrains[i].terrList.name == terr.name) {
          return i;
        }
      } catch (MissingReferenceException e) {
      } catch (NullReferenceException e) {
      }
    }
    return -1;
  }

  // Get the x coordinate from the array index.
  int GetXCoord(int index) {
    string name = terrains[index].terrList.name;
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
                     "\n" + name + "," + terrains[index].terrList.name);
      return -1;
    } else {
      return output;
    }
  }

  // Get the Z coordinate from the array index.
  int GetZCoord(int index) {
    string name = terrains[index].terrList.name;
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
                     "\n" + name + "," + terrains[index].terrList.name);
      return -1;
    } else {
      return output;
    }
  }

  // Find the average of up to 4 defined values. (EmptyPoint counts as
  // undefined)
  float AverageCorners(float c1, float c2, float c3, float c4) {
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

  // Find unique number for each given coordinate.
  int PerfectlyHashThem(short a, short b) {
    var A = (uint)(a >= 0 ? 2 * a : -2 * a - 1);
    var B = (uint)(b >= 0 ? 2 * b : -2 * b - 1);
    var C = (int)((A >= B ? A * A + A + B : A + B * B) / 2);
    return a < 0 && b < 0 || a >= 0 && b >= 0 ? C : -C - 1;
  }

  // Mix the two saved heightmaps (Perlin and Displace Divide) so that it may
  // be applied to the chunk.
  float[, ] MixHeights(int terrLoc) {
    if (GenMode.Perlin && !GenMode.DisplaceDivide) {
      return terrains[terrLoc].terrPerlinPoints;
    } else if (!GenMode.Perlin) {
      return terrains[terrLoc].terrPoints;
    } else {
      float[, ] output = new float[ heightmapWidth, heightmapHeight ];
      for (int i = 0; i < heightmapHeight * heightmapWidth; i++) {
        int z = i % heightmapHeight;
        int x = (int)Math.Floor((float)i / heightmapWidth);
        output[ x, z ] = Mathf.Lerp(terrains[terrLoc].terrPoints[ x, z ],
                                    terrains[terrLoc].terrPerlinPoints[ x, z ],
                                    GenMode.mixtureAmount);
      }
      return output;
    }
  }
}
