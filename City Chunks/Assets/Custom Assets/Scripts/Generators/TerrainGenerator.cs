// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
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
// #define DEBUG_DIVIDE
// #define DEBUG_GRASS_NORMALS
// #define DEBUG_HEIGHTS
// #define DEBUG_MISC
// #define DEBUG_POSITION
// #define DEBUG_START
// #define DEBUG_STEEPNESS
// #define DEBUG_TEXTURES
// #define DEBUG_UPDATES
// #define DEBUG_WATER
// #define DEBUG_HUD_POS
// #define DEBUG_HUD_TIMES
// #define DEBUG_HUD_LOADED
// #define DEBUG_HUD_LOADING

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

[Serializable] public class GeneratorModes {
  [Header("Displace Divide")]
  [Tooltip("Enables generator mode that displaces points randomly within ranges that vary by the distance to the center of the chunk and averages surrounding points.")]
  public bool DisplaceDivide = true;
  [Tooltip("Offset middle of the sides of each chunk with perlin noise")]
  public bool midOffset = true;
  [Tooltip("Smooth the heightmap by averaging all surrounding points in the heightmap.")]
  public bool SmoothHeightmap = true;

  [Header("Perlin Noise")]
  [Tooltip("Uses Perlin noise to generate terrain.")]
  public bool Perlin = true;
  [Tooltip("A modifier to Perlin Noise that exaggerates heights increasingly the higher they get.")]
  public bool Distort = false;

  [Header("Settings")]
  [Tooltip("Used in a Lerp function between DisplaceDivide and Perlin. (0: Divide, 1: Perlin)")]
  [Range(0.0f, 1.0f)]public float mixtureAmount = 0.5f;
  [Tooltip("Number of pixels to update per chunk per frame. (0 is all at once)")]
  [Range(0, 257*257)] public int HeightmapSpeed = 0;
  [Tooltip("Allows the heightmap generation to take place in a seprate thread.")]
  public bool multiThreading = true;
  [Tooltip("Allows most of the hard work of generating chunks to happen in during a loading screen rather than while the player has spawned. Only applies for spawn chunks.")]
  public bool PreLoadChunks = true;
}
[Serializable] public class Times {
  [Tooltip("Shows a countdown until neighbors get updated again.")]
  public GUIText deltaNextUpdate;
  [Tooltip("Shows timing for all other measured events.")]
  public GUIText deltaTimes;
  [Tooltip("Pause the Unity Editor if a frame takes more time than any previous frame.")]
  public bool pauseIfNewMax = false;
  [Tooltip("Time between neighbor updates in seconds.")]
  [Range(0.1f, 500f)] public float UpdateSpeed = 5;
  [Tooltip("The last time neighbors were updated in seconds.")]
  public float lastUpdate = 0;
  [Tooltip("Previous amount of time calculating heightmap data took in milliseconds.")]
  public float DeltaDivide = 0;
  [Tooltip("Previous amount of time calculating heightmap data took in milliseconds divided by the number of divisions.")]
  public float DeltaDivideOnce = 0;
  [Tooltip("Previous amount of time calculating heightmap data and initializing arrays took in milliseconds. (Theoretically should be similar to DeltaDivide?)")]
  public float DeltaFractal = 0;
  [Tooltip("Previous amount of time instantiating a new chunk took in milliseconds.")]
  public float DeltaGenerate = 0;
  [Tooltip("Previous amount of time instantiating a new terrain component of a chunk took in milliseconds.")]
  public float DeltaGenerateTerrain = 0;
  [Tooltip("Previous amount of time instantiating a new water component of a chunk took in milliseconds.")]
  public float DeltaGenerateWater = 0;
  [Tooltip("Previous amount of time updating the water of a chunk took in milliseconds.")]
  public float DeltaWaterUpdate = 0;
  [Tooltip("Previous amount of time applying the heightmap to a chunk took in milliseconds.")]
  public float DeltaHeightmapApplied = 0;
  [Tooltip("Same as DeltaDivide but includes time it took to flip points and possible additional logging.")]
  public float DeltaGenerateHeightmap = 0;
  [Tooltip("Previous amount of time updating detail meshes took in milliseconds.")]
  public float DeltaGrassUpdate = 0;
  [Tooltip("Previous amount of time updating chunk splats, and details took in milliseconds.")]
  public float DeltaDetailUpdate = 0;
  [Tooltip("Previous amount of time updating rock instances took in milliseconds.")]
  public float DeltaRockUpdate = 0;
  [Tooltip("Previous amount of time updating cities took in milliseconds.")]
  public float DeltaCityUpdate = 0;
  [Tooltip("Previous amount of time updating neighbors took in milliseconds.")]
  public float DeltaUpdate = 0;
  [Tooltip("Previous amount of time unloading chunks took in milliseconds.")]
  public float DeltaUnload = 0;
  [Tooltip("Previous amount of time it took for all steps to occur during one frame if something significant happened, otherwise it does not change until a significant event occurs.")]
  public float DeltaTotal = 0;
  [Tooltip("Previous amount of time it took for all steps to occur during one frame.")]
  public float DeltaTotalReal = 0;
  public float DeltaTotalMax = 0;
  [Tooltip("Previous 1000 values of DeltaTotal for use in calculating the average total amount of time this script takes during a frame while doing everything.")]
  public float[] DeltaTotalAverageArray = new float[1000];
  [Tooltip("Average total amount of time this script takes in one frame while doing everything necessary.")]
  public float DeltaTotalAverage = 0;
  [Tooltip("Placeholder for next location to store DeltaTotal in DeltaTotalAverageArray which is especially important when the array is full and we loop back from the beginning and overwrite old data.")]
  public int avgEnd = -1;
}
[Serializable] public class Terrains {
  public int x = 0;
  public int z = 0;
  public float biome = 1.0f;
  [Tooltip("List of terrain data for setting heights. Equivalent to gameObject.GetComponent<Terrain>().terrainData.")]
  public TerrainData terrData;
  [Tooltip("Terrain GameObject.")]
  public GameObject gameObject;
  [Tooltip("List of terrain heightmap data points for setting heights over a period of time from the DeltaDivide Generator.")]
  public float[, ] terrPoints;
  [Tooltip("List of terrain heightmap data points for setting heights over a period of time from the Perlin generator.")]
  public float[, ] terrPerlinPoints;
  [Tooltip("Array of list of objects that sub-generators created.")]
  public List<GameObject>[] ObjectInstances;
  [Tooltip("The water GameObject attatched to this chunk.")]
  public GameObject waterTile;
  [Tooltip("Whether this terrain chunk is ready for its water tile to be updated.")]
  public bool waterQueue = false;
  [Tooltip("Whether this terrain chunk is ready to be updated with points in terrPoints. True if points need to be flushed to terrainData.")]
  public bool terrQueue = false;
  [Tooltip("Array of sub-generators that the chunk is ready to run.")]
  public bool[] subGeneratorQueue;
  [Tooltip("True if the heightmap is still being generated.")]
  public bool isDividing = false;
  [Tooltip("True if the heightmap has been generated.")]
  public bool hasDivided = false;
  [Tooltip("True if all points have been defined in terrPoints. Used for determining adjacent chunk heightmaps.")]
  public bool terrReady = false;
  [Tooltip("True if the chunk can be unloaded.")]
  public bool terrToUnload = false;
  [Tooltip("True if the chunk is being loaded from the disk and not generated.")]
  public bool loadingFromDisk = false;
  [Tooltip("True if the chunk was loaded from the disk and not generated.")]
  public bool loadedFromDisk = false;
  [Tooltip("True if the chunk was recently loaded from the disk and not generated and also not processed fully yet.")]
  public bool justLoadedFromDisk = false;
  [Tooltip("Number of frames the chunk should live after it has been flagged to be unloaded.")]
  public int startTTL = 1500;
  public int currentTTL = 1500;

  public override string ToString() {
    return string.Format("Terrain({0}, {1})", x, z);
  }

  public void InitializeSubGenerators(int num) {
    subGeneratorQueue = new bool[num];
    ObjectInstances = new List<GameObject>[num];
    for (int i = 0; i < ObjectInstances.Length; i++) {
      ObjectInstances[i] = new List<GameObject>();
    }
  }

  public void queueSubGenerators(bool state = true) {
    for (int i = 0; i < subGeneratorQueue.Length; i++) {
      subGeneratorQueue[i] = state;
    }
  }
  public int numSGQueued() {
    int num = 0;
    foreach(bool sg in subGeneratorQueue) if (sg) num++;
    return num;
  }
}
public class TerrainGenerator : MonoBehaviour {
  public const float EmptyPoint = -100f;
  public static string worldID = "ERROR";
  // To allow static functions easier access to instance variables.
  private static TerrainGenerator tg;

  [Header("Terrains (Auto-populated)")]
  [Tooltip("The list of all currently loaded chunks.")]
  // Should only be public for debugging. Private otherwise.
  public List<Terrains> terrains = new List<Terrains>();

  [Header("Game Objects")]
  [Tooltip("Water Tile to instantiate with the terrain when generating a new chunk.")]
  public GameObject waterTile;
  [Tooltip("Minimap icon to instantiate with the terrain when generating a new chunk.")]
  public GameObject miniMapIcon;
  [Tooltip("Whether or not to use the pre-determined seed (true) or use Unity's random seed (false).")]
  [Header("Seeds")]
  public bool useSeed = true;
  [Tooltip("The predetermined seed to use if Use Seed is false.")]
  public int Seed = 971;
  [Tooltip("Modifier to shift the perlin noise map in order to reduce chance of finding the same patch of terrain again. The perlin noise map loops at every integer. This value is multiplied by the seed.")]
  public float PerlinSeedModifier = 0.2541868f;
  [Header("HUD Text for Debug")]
  [Tooltip("The GUIText object that is used to display the position of the player.")]
  public GUIText positionInfo;
  [Tooltip("The GUIText object that is used to display the list of loaded chunks.")]
  public GUIText chunkListInfo;
  [Tooltip("Tracking of how long certain events take.")]
  [SerializeField] public Times times;
  [Tooltip("The previous values of times.")]
  [SerializeField] public Times previousTimes;
  [Header("Generator Settings")]
  [Tooltip("List of available terrain generators.")]
  [SerializeField] public GeneratorModes GenMode;
  // [Tooltip("Distance the player must be from a chunk for it to be loaded.")]
  // public int loadDist = 1500;
  [Tooltip("Roughness of terrain is modified by this value.")]
  public float roughness = 0.3f;
  [Tooltip("Multiplier to exaggerate the peaks.")]
  public float PeakMultiplier = 1.0f;
  [Tooltip("Roughness of terrain is modified by this value.")]
  public float PerlinRoughness = 0.1f;
  [Tooltip("Maximum height of Perlin Generator in percentage.")]
  [Range(0.0f, 1.0f)]
  public float PerlinHeight = 1.0f;
  [Tooltip("How quickly biomes/terrain roughness changes.")]
  public float BiomeRoughness = 0.05f;
  [Tooltip("Vertical shift of values pre-rectification.")]
  public float yShift = 0.0f;
  [Header("Visuals")]

  public const string version = "tg6";

  // True allows for things to continue even if this does not exists in the
  // scene. Gets set to false at the beginning of Start().
  public static bool loadingSpawn = false;
  public static bool doneLoadingSpawn = true;
  public static bool wasDoneLoadingSpawn = false;
  public static float waterHeight = 0f;
  public static float snowHeight = 960f;

  private static int terrWidth;  // Used to space the terrains when instantiating.
  private static int terrLength; // Size of the terrain chunk in normal units.
  private static int terrHeight; // Maximum height of the terrain chunk in normal units.
  int heightmapWidth;  // The size of an individual heightmap of each chunk.
  int heightmapHeight;

  // Player for deciding when to load chunks based on position.
  private static InitPlayer player;

  // Used to identify the corners of the loaded terrain when not generating in a
  // radius from the player
  int width;  // Total size of heightmaps combined
  int height;
  int playerSpawnX = 0;
  int playerSpawnZ = 0;
  float radius = 0.0f;
  // Remaining number of messages to send to the console. Setting a limit
  // greatly improves performance since sending large amounts to the console in
  // a short amount of time is slow, and this limits the amount sent to the
  // console in a short amount of time. It also makes reading the output easier
  // since there are fewer lines to look at.
  int logCount = 1;
  float lastUpdate;
  // Previous terrain index whose heightmap was being applied.
  int lastTerrUpdateLoc = 0;
  // Chunk whose heightmap is being applied.
  Terrains lastTerrUpdated = new Terrains();
  // Array of points currently applied to the chunk.
  float[, ] TerrUpdatePoints;
  // Array of points to be applied to the chunk.
  float[, ] TerrTemplatePoints;
  // Lowest and highest points of loaded terrain.
  float lowest = 1.0f;
  float highest = 0.0f;
  // Number of times Displace Divide is called per chunk.
  int divideAmount = 0;
#if DEBUG_HUD_LOADING
  int divideAmount_ = 0;
#endif
  // Number of frames to wait before unloading chunks.
  int unloadWaitCount = 100;
  // Used for pausing the editor if a new maximum is set so the user can view
  // what the culprit was.
  float lastMaxUpdateTime = 0;
  bool preLoadingDone = false;
  bool preLoadingChunks = false;

  bool started = false;

  float threadTTL = 5f; // seconds
  float threadStart = -1f;
  Thread thread;
  IEnumerator divideEnumerator;
  System.Random rand;

  SubGenerator[] subGenerators;

#if DEBUG_HUD_LOADED || DEBUG_HUD_LOADING
  // List of chunks loaded as a list of coordinates.
  String LoadedChunkList = "";
#endif

  void Start() {
    started = true;
    Debug.Log("Terrain Generator Start!");
    GameData.AddLoadingScreen();
    tg = this;

    if (terrains.Count > 0) {
      Debug.LogError("Start was called but terrains already exist!");
      return;
    }

    if (GameData.Seed != 0) {
      Seed = GameData.Seed;
    } else {
      GameData.Seed = Seed;
    }
    if (GameData.PerlinSeedModifier != 0) {
      PerlinSeedModifier = GameData.PerlinSeedModifier;
    } else {
      GameData.PerlinSeedModifier = PerlinSeedModifier;
    }

    loadingSpawn = true;
    wasDoneLoadingSpawn = false;
    doneLoadingSpawn = false;
    preLoadingDone = false;
    preLoadingChunks = false;

    subGenerators = FindObjectsOfType<SubGenerator>();
    Debug.Log("Found " + subGenerators.Length + " sub-generators.");
    for (int i = 0; i < subGenerators.Length; i++) {
      subGenerators[i].Initialize(this, i);
    }

    player = null;

    terrains.Clear();
    if (waterTile != null)
      TerrainGenerator.waterHeight = waterTile.transform.position.y;

    // Fill array with -1 so we know there is no data yet.
    for (int i = 0; i < times.DeltaTotalAverageArray.Length; i++) {
      times.DeltaTotalAverageArray[i] = -1;
    }

    times.lastUpdate = Time.time;
    if (GenMode.Perlin && !useSeed) {
      for(int i=0; i<Time.realtimeSinceStartup * 100; i++) {
        Seed = (int)(500 * UnityEngine.Random.value);
      }
    }
    if (Seed == 0) Seed = 1;
    worldID =
        (Seed * PerlinSeedModifier).ToString() + GameData.longVersion + "-";
#if DEBUG_START
    if (GenMode.Perlin) {
      Debug.Log("Seed(" + Seed + ")*PerlinSeedModifier(" + PerlinSeedModifier +
                ")=" + Seed * PerlinSeedModifier);
    }
#endif
    if (GenMode.Perlin && GenMode.DisplaceDivide) {
#if DEBUG_START
      Debug.Log("Doubling Roughness because both engines enabled");
#endif
      roughness *= 2f;
    }


    // Generate height map. Disable slowing the generation because we want
    // everything do be done in this frame, but then return the feature to its
    // initial state later as the user may want it.
    bool multiThreading = GenMode.multiThreading;
    GenMode.multiThreading = false;

#if DEBUG_START
    Debug.Log("Generating spawn chunk");
#endif
    // Initialize variables based off of values defining the terrain and add
    // the spawn chunk to arrays for later reference.
    GenerateTerrainChunk(0, 0);

#if DEBUG_START
    Debug.Log("Creating spawn chunk fractal");
#endif
    FractalNewTerrains(0, 0);
#if DEBUG_START
    Debug.Log("Clearing trees");
#endif
    ClearTreeInstances(terrains[0]);
#if DEBUG_START
    Debug.Log("Applying spawn chunk height map");
#endif
    terrains[0].terrData.SetHeights(0, 0, MixHeights(0));
#if DEBUG_START
    Debug.Log("Running SubGenerators.");
#endif
    foreach (SubGenerator sg in subGenerators) { sg.Go(terrains[0]); }
#if DEBUG_START
    Debug.Log("Texturing spawn chunk");
#endif
    terrains[0].terrQueue = false;
    terrains[0].terrReady = true;
#if DEBUG_START
    Debug.Log("Attempting to save spawn chunk to disk");
#endif
    SaveChunk(0, 0);
    GenMode.multiThreading = multiThreading;
    radius = GameData.LoadDistance / ((terrWidth + terrLength) / 2.0f);

#if DEBUG_HUD_LOADED || DEBUG_HUD_LOADING
    LoadedChunkList = "";
#endif
#if NAVIGATION_MESH_BUILDER
    Debug.Log("Building Navigation Mesh Surface");
    NavMeshSurface nm = GetComponent<NavMeshSurface>();
    if (nm != null) nm.BuildNavMesh();
#endif

    // Choose player spawn location based off of the center of all pre-loaded
    // chunks.
    float playerX = terrWidth / 2f;
    float playerZ = terrWidth / 2f;
    float playerY = GetTerrainHeight(playerX, playerZ);
    // Starts off of edges  because there is a precision issue with generating
    // chunks that will cause chunks not to load if the player's position is
    // exactly (0,0).
    // TODO: Fix this in either Generator, or decide if this is a good enough
    // solution.
    //int tries = terrWidth;
    int tries = (int)(terrWidth * 1.5);
    while (tries < terrWidth * terrWidth &&
           playerY < TerrainGenerator.waterHeight) {
      playerX = tries % terrWidth;
      playerZ = (int)(tries / terrWidth);
      playerY = GetTerrainHeight(playerX, playerZ);
      tries++;
    }
    if (tries == terrWidth * terrWidth) {
      playerX = terrWidth / 2f;
      playerZ = terrWidth / 2f;
    }
    playerSpawnX = (int)playerX;
    playerSpawnZ = (int)playerZ;

    TerrUpdatePoints = new float[ heightmapWidth, heightmapHeight ];
    TerrTemplatePoints = new float[ heightmapWidth, heightmapHeight ];
    Debug.Log("Initialization done!");
  }

  void Update() {
    if (GameData.Seed != Seed ||
        GameData.PerlinSeedModifier != PerlinSeedModifier) {
      Debug.LogError("Detected Seed Change! This is not allowed! (" + started +
                     ")\nGD: " + GameData.Seed + ", " +
                     GameData.PerlinSeedModifier + ", TG: " + Seed + ", " +
                     PerlinSeedModifier);
      GameData.MainMenu();
      return;
    }
    checkDoneLoadingSpawn();

    if(GenMode.PreLoadChunks && !preLoadingDone && !preLoadingChunks) {
      preLoadingChunks = true;
      StartCoroutine(PreLoadChunks());
    }
    if (GameData.loading) CalculateLoadPercent();
    if (!preLoadingDone && GenMode.PreLoadChunks) return;
    if (GameData.loading && doneLoadingSpawn && !wasDoneLoadingSpawn)
      GameData.RemoveLoadingScreen();
    // Generates terrain based on player transform and generated terrain. Loads
    // chunks in a circle centered on the player and unloads all other chunks
    // that are not within this circle. The spawn chunk is exempt because it may
    // not be unloaded.
    float iRealTime = Time.realtimeSinceStartup;
    float iTime = -1;
    bool done = false;

#if DEBUG_HUD_TIMES
    if (Input.GetKeyDown("r")) {
      for (int i = 0; i < times.DeltaTotalAverageArray.Length; i++) {
        times.DeltaTotalAverageArray[i] = -1;
      }
      times.DeltaTotalAverage = 0f;
      times.DeltaTotalMax = 0f;
      times.avgEnd = -1;
      lastMaxUpdateTime = 0f;
    }
#endif

    Preparations();

    HandleMultiplayer();

    UpdateAllWater(ref done, ref iTime);

    ApplyHeightmap(ref done, ref iTime);

    UpdateAllSubGenerators(ref done, ref iTime);

    UpdateAllLoadedChunks(ref done, ref iTime);

    UpdateAllNeighbors();

    UnloadAllChunks(ref done, ref iTime);

    UpdateTimesAndHUD(iTime, iRealTime);
  }

  IEnumerator PreLoadChunks() {
    Debug.Log("Pre-Loading Chunks");
    GameData.loadingMessage = "Slicing and dicing...";
    float X_, Y_;
    int x, y;
    bool done = false;
    bool keepGoing;
    do {
      keepGoing = false;
      done = false;
      for (float X = 0; X < 2 * radius; X++) {
        for (float Y = 0; Y < 2 * radius; Y++) {
          X_ = X % 2 == 0 ? (X / -2) : ((X - 1) / 2);
          Y_ = Y % 2 == 0 ? (Y / -2) : ((Y - 1) / 2);
          x = Mathf.RoundToInt(X_);
          y = Mathf.RoundToInt(Y_);
          if (x * x + y * y <= radius * radius) {
            BeginChunkLoading(x, y, ref done);
            if (done) keepGoing = done;
          }
        }
      }
      yield return null;
    } while (keepGoing);
    Debug.Log("Done Pre-Loading Chunks (" + terrains.Count + ")");
    preLoadingDone = true;
    preLoadingChunks = false;
  }

  void CalculateLoadPercent() {
      float total = 0f;
      int num = terrains.Count;
      if (!preLoadingDone) {
        num = Mathf.RoundToInt(Mathf.Pow(GameData.LoadDistance / terrWidth, 2) *
                               Mathf.PI);
      }
      int sg = subGenerators.Length;
      int count = 4 + sg;
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].isDividing) total += 1.0f / count;
        else if (terrains[i].waterQueue) total += 2.0f / count;
        else if (terrains[i].terrQueue) total += 2.5f / count;
        else if (terrains[i].numSGQueued() > 0)
          total += (2.5f + sg - terrains[i].numSGQueued()) / count;
        else if (terrains[i].terrReady) total += 1.0f;
        else num--;
      }
      GameData.loadingPercent = total / num;
  }

  void Preparations() {
#if DEBUG_POSITION
    Debug.Log ("P: ("
            + player.transform.position.x
            + ", " + player.transform.position.y
            + ", " + player.transform.position.z
            + ")");
#endif
    if (thread != null) {
      if (thread.ThreadState == ThreadState.Stopped && anyChunksDividing()) {
        Debug.LogWarning("Thread exited unexpectedly. Attempting recovery.");
        for (int i = 0; i < terrains.Count; i++) {
          if (terrains[i].isDividing) terrains[i].isDividing = false;
        }
      }
      if (Time.realtimeSinceStartup - threadStart >= threadTTL &&
          thread.ThreadState != ThreadState.Stopped) {
        thread.Abort();
        Debug.LogError("DIVIDE THREAD EXCEEDED TIME TO LIVE! ABORTING! (" +
                       (Time.realtimeSinceStartup - threadStart) + "/" +
                       threadTTL + ")");
        if (thread.ThreadState != ThreadState.AbortRequested) {
          Debug.LogError("THREAD ABORT FAILED!");
        }
      }
      if (thread.ThreadState == ThreadState.AbortRequested) {
        Debug.LogWarning("Thread Abort Requested.");
      }
      if (thread.ThreadState == ThreadState.Aborted) {
        Debug.LogWarning("Thread Aborted.");
      }
    }
#if DEBUG_HUD_LOADED || DEBUG_HUD_LOADING
    LoadedChunkList = "";
#endif
#if DEBUG_HUD_LOADING
    if (TerrainGenerator.doneLoadingSpawn) {
      LoadedChunkList += "Spawn is done\n";
    } else {
      LoadedChunkList += "\n";
    }
    if (thread != null) {
      LoadedChunkList +=
          "(TTL: " + (threadTTL - (Time.realtimeSinceStartup - threadStart)) +
          ", %" + ((float)divideAmount / (float)divideAmount_) * 100f + ", " +
          thread.ThreadState + ", AnyDiv: " + anyChunksDividing() + ")\n";
    }
    if (anyChunksDividing()) {
      LoadedChunkList += "Dividing\n";
    } else {
      LoadedChunkList += "\n";
    }
#endif
    if (GameData.loading) {
      GameData.previousLoadingMessage = GameData.loadingMessage;
      GameData.loadingMessage =
          "Waiting for someone special to see what I've made for them.";
    }

    // Remove all undefined chunks from the array because they have been
    // unloaded.
    for (int i = 0; i < terrains.Count; i++) {
      if (!terrains[i].gameObject) {
        terrains.RemoveAt(i);
        i--;
      } else if (!terrains[i].loadingFromDisk &&
                 terrains[i].justLoadedFromDisk && terrains[i].loadedFromDisk) {
        terrains[i].isDividing = false;
        terrains[i].hasDivided = true;
        terrains[i].terrReady = true;
        terrains[i].terrQueue = true;
        terrains[i].waterQueue = true;
        terrains[i].justLoadedFromDisk = false;
        terrains[i].queueSubGenerators(false);
      }
    }

    // Flag all chunks to be unloaded. If they should not be unloaded, they will
    // be unflagged and stay loaded before any chunks are actually unloaded.
    if (unloadWaitCount <= 0) {
      for (int i = 0; i < terrains.Count; i++) {
        terrains[i].terrToUnload = true;
      }
    } else {
      unloadWaitCount--;
    }

    // In case LoadDistance changes
    radius = GameData.LoadDistance / ((terrWidth + terrLength) / 2.0f);
  }

  void HandleMultiplayer() {
    if (player == null) {
      float playerX = playerSpawnX;
      float playerZ = playerSpawnZ;
      // Get the player spawn height from the heightmap height at the
      // coordinates where the player will spawn.
      InitPlayer[] players = GameObject.FindObjectsOfType<InitPlayer>();
      for (int i = 0; i < players.Length; i++) {
        if(players[i].controller.isLocalPlayer) {
          player = players[i];
          break;
        }
      }
      Debug.Log("Valid player found: " + player.transform.name);
      // Tell the player where to spawn.
      float playerY = GetTerrainHeight(playerX, playerZ);
      player.go(playerX, playerY, playerZ);
    }
  }

  void UpdateAllLoadedChunks(ref bool done, ref float iTime) {
    // Make sure the player stays above the terrain
    float xCenter = (player.transform.position.x - terrWidth / 2) / terrWidth;
    float yCenter = (player.transform.position.z - terrLength / 2) / terrLength;
    int terrLoc = GetTerrainWithCoord(Mathf.RoundToInt(xCenter),
                                      Mathf.RoundToInt(yCenter));
    if (terrLoc != -1) {
      float TerrainHeight =
          terrains[terrLoc].gameObject.GetComponent<Terrain>().SampleHeight(
              player.transform.position);
#if DEBUG_HUD_LOADED
      LoadedChunkList +=
          "x: " + xCenter + ", y: " + yCenter + ", r: " + radius + "\n";
#endif

#if DEBUG_HUD_POS
      positionInfo.text =
          "Joystick(" + Input.GetAxis("Mouse X") + ", " +
          Input.GetAxis("Mouse Y") + ")(" + Input.GetAxis("Horizontal") + ", " +
          Input.GetAxis("Vertical") + ")(" + Input.GetAxis("Sprint") + ")\n" +
          "Player" + player.transform.position + "\n" + "Coord(" + xCenter +
          ", " + yCenter + ")(" + terrLoc + ")\n" + "Coord(" +
          Mathf.RoundToInt(xCenter) + ", " + Mathf.RoundToInt(yCenter) +
          ")\nTerrainHeight: " + TerrainHeight + "\nHighest Point: " +
          (highest * terrHeight) + "\nLowest Point: " + (lowest * terrHeight) +
          "\nPeakMultiplier: " + terrains[terrLoc].biome;
#else
      if (positionInfo != null) positionInfo.text = "";
#endif
      if (player.transform.position.y < TerrainHeight - 1.0f) {
#if DEBUG_POSITION
        Debug.Log("Player at " + player.transform.position + "\nCoord: (" +
                  xCenter + ", " + yCenter + ")" + "\nPlayer(" +
                  player.transform.position + ")" + "\nTerrain Height: " +
                  TerrainHeight + "\n\n");
#endif
        movePlayerToTop(player.GetComponent<InitPlayer>());
      }
    }

    UpdateLoadedChunks(xCenter, yCenter, ref done, ref iTime);
  }

  void UpdateLoadedChunks(float xCenter, float yCenter, ref bool done,
                          ref float iTime) {
    bool chunkUpdated = false;
    for (float X = 0; X < 2 * radius; X++) {
      for (float Y = 0; Y < 2 * radius; Y++) {
        float X_ = xCenter + (X % 2 == 0 ? (X / -2) : ((X - 1) / 2));
        float Y_ = yCenter + (Y % 2 == 0 ? (Y / -2) : ((Y - 1) / 2));
        int x = Mathf.RoundToInt(X_);
        int y = Mathf.RoundToInt(Y_);
        // don't have to take the square root, it's slow
        if ((x - xCenter) * (x - xCenter) + (y - yCenter) * (y - yCenter) <=
            radius * radius) {
          // If the chunk has not been loaded yet, create it. Otherwise, make
          // sure the chunk doesn't get unloaded.
          bool wasdone = done;
          if (iTime == -1 && !done) iTime = Time.realtimeSinceStartup;
          BeginChunkLoading(x, y, ref done);
          if (!wasdone && !done) iTime = -1;
          else if(!wasdone && done) chunkUpdated = true;
        }
      }
#if DEBUG_HUD_LOADED
      LoadedChunkList += "\n";
#endif
    }
    if (chunkUpdated) GameData.loadingMessage = "Thinking really hard...";
#if DEBUG_HUD_LOADING
    if (chunkUpdated) LoadedChunkList += "Generating and Fractaling\n";
    else LoadedChunkList += "\n";
#endif
  }

  void ApplyHeightmap(ref bool done, ref float iTime) {
    bool heightmapApplied = false;
    float iTime2 = -1;
    if (!done) {
      // Find next chunk that needs heightmap to be applied or continue the last
      // chunk if it was not finished.
      int tileCnt = GetTerrainWithCoord(lastTerrUpdated.x, lastTerrUpdated.z);
      if (tileCnt < 0 || !terrains[tileCnt].terrQueue ||
          terrains[tileCnt].loadingFromDisk) {
        for (int i = 0; i < terrains.Count; i++) {
          if (terrains[i].terrQueue && !terrains[i].loadingFromDisk) {
            tileCnt = i;
            lastTerrUpdateLoc = 0;
            TerrTemplatePoints = MixHeights(tileCnt);
            break;
          }
        }
      }
      // Apply heightmap to chunk GenMode.HeightmapSpeed points at a time.
      if (tileCnt >= 0 && terrains[tileCnt].terrQueue &&
          !terrains[tileCnt].loadingFromDisk) {
        heightmapApplied = true;
        if (iTime == -1) iTime = Time.realtimeSinceStartup;
        if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
        int lastTerrUpdateLoc_ = lastTerrUpdateLoc;
        int heightmapSpeed = GenMode.HeightmapSpeed;

        if (GenMode.HeightmapSpeed != 0) {
          for (int i = lastTerrUpdateLoc_;
               i < lastTerrUpdateLoc_ + heightmapSpeed; i++) {
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
            // Heightmap is done being applied, remove it from the queue and
            // flag it for subgenerator queue.
            if (x >= heightmapWidth) {
              terrains[tileCnt].terrQueue = false;
              terrains[tileCnt].queueSubGenerators();
              break;
            }

            TerrUpdatePoints[ z, x ] = TerrTemplatePoints[ z, x ];

            lastTerrUpdateLoc++;
          }
        } else {
          terrains[tileCnt].terrQueue = false;
          terrains[tileCnt].queueSubGenerators();
          TerrUpdatePoints = TerrTemplatePoints;
        }

        // Set the terrain heightmap to the defined points.
        terrains[tileCnt].terrData.SetHeights(0, 0, TerrUpdatePoints);

        // Push all changes to the terrain.
        terrains[tileCnt].gameObject.GetComponent<Terrain>().Flush();
        // Make sure this chunk will continue being updated next frame.
        lastTerrUpdated = terrains[tileCnt];
        // The terrain has been removed from the queue so we should allow for
        // the another chunk to be loaded.
        if (!terrains[tileCnt].terrQueue) {
          TerrUpdatePoints = new float[ heightmapHeight, heightmapWidth ];
          lastTerrUpdateLoc = -1;
          lastTerrUpdated = new Terrains();
        }
      }
      if (GameData.loading)
        GameData.loadingMessage = "Creating something from nothing...";
    }
#if DEBUG_HUD_LOADING
    if (heightmapApplied) LoadedChunkList += "Applying Heightmap\n";
    else LoadedChunkList += "\n";
#endif
    if (heightmapApplied) {
      done = heightmapApplied;
      times.DeltaHeightmapApplied = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateAllWater(ref bool done, ref float iTime) {
    bool waterUpdated = false;
    float iTime2 = -1;
    if (!done) {
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].waterQueue && !terrains[i].loadingFromDisk) {
          if (iTime == -1) iTime = Time.realtimeSinceStartup;
          if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
          GenerateWaterChunk(terrains[i].x, terrains[i].z);
          terrains[i].waterQueue = false;
          waterUpdated = true;
          break;
        }
      }
      if (GameData.loading)
        GameData.loadingMessage = "Slowly drowning in a tea pee (tipi?)...";
    }
#if DEBUG_HUD_LOADING
    if (waterUpdated) LoadedChunkList += "Updating Water\n";
    else LoadedChunkList += "\n";
#endif
    if (waterUpdated) {
      done = waterUpdated;
      times.DeltaWaterUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateAllSubGenerators(ref bool done, ref float iTime) {
    bool subGeneratorsUpdated = false;
    float iTime2 = -1;
    if (!done && subGenerators.Length > 0) {
      int SGIndex = -1;
      int TIndex = -1;
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].loadingFromDisk) continue;
        for (int j = 0; j < terrains[i].subGeneratorQueue.Length; j++) {
          if (terrains[i].subGeneratorQueue[j] &&
              (SGIndex == -1 ||
               subGenerators[j].priority > subGenerators[SGIndex].priority) &&
              subGenerators[j].enabled) {
            TIndex = i;
            SGIndex = j;
          }
        }
      }
      if (GameData.loading)
        GameData.loadingMessage = "Probably doing thing #" + (SGIndex + 1) +
                                  " of " + subGenerators.Length + " things...";
      if (SGIndex != -1 && TIndex != -1) {
        if (iTime == -1) iTime = Time.realtimeSinceStartup;
        if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
        if (subGenerators[SGIndex].Go(terrains[TIndex])) {
          subGeneratorsUpdated = true;
        }
        terrains[TIndex].subGeneratorQueue[SGIndex] = false;
      }
    }
#if DEBUG_HUD_LOADING
    if (subGeneratorsUpdated) LoadedChunkList += "Running SubGenerators\n";
    else LoadedChunkList += "\n";
#endif
    if (subGeneratorsUpdated) {
      done = subGeneratorsUpdated;
      times.DeltaRockUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateAllNeighbors() {
    // Update terrain neighbors every times.UpdateSpeed seconds.
    // TODO: Is this even necessary? I don't really know what updating neighbors
    // does.
    if (Time.time > times.lastUpdate + times.UpdateSpeed && player != null) {
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
      times.DeltaUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateTerrainNeighbors(int X, int Z, int count = 3) {
    if (count > 0) {
      Terrain LeftTerr = null, TopTerr = null, RightTerr = null,
              BottomTerr = null;
      int temp = GetTerrainWithCoord(X - 1, Z);
      if (temp != -1) {
        LeftTerr = terrains[temp].gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X - 1, Z, count - 1);
      }
      temp = GetTerrainWithCoord(X, Z + 1);
      if (temp != -1) {
        TopTerr = terrains[temp].gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z + 1, count - 1);
      }
      temp = GetTerrainWithCoord(X + 1, Z);
      if (temp != -1) {
        RightTerr = terrains[temp].gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X + 1, Z, count - 1);
      }
      temp = GetTerrainWithCoord(X, Z - 1);
      if (temp != -1) {
        BottomTerr = terrains[temp].gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z - 1, count - 1);
      }
      temp = GetTerrainWithCoord(X, Z);
      if (temp != -1) {
        Terrain MidTerr = terrains[temp].gameObject.GetComponent<Terrain>();
        MidTerr.SetNeighbors(LeftTerr, TopTerr, RightTerr, BottomTerr);
      }
    }
  }

  void UnloadAllChunks(ref bool done, ref float iTime) {
#if DEBUG_HUD_LOADED
    LoadedChunkList += "Unloading: \n";
#endif
    bool chunksUnloaded = false;
    float iTime2 = -1;
    // Unload all chunks flagged for unloading.
    for (int i = 0; i < terrains.Count; i++) {
      if (terrains[i].terrToUnload && !terrains[i].loadingFromDisk) {
#if DEBUG_HUD_LOADED
        LoadedChunkList += "(" + terrains[i].x + ", " + terrains[i].z + ", " +
                           Mathf.RoundToInt(terrains[i].currentTTL) + "), ";
        if (i % 7 == 0) LoadedChunkList += "\n";
#endif
        if (terrains[i].currentTTL <= 0 && !chunksUnloaded) {
          if (i != 0) {
            if (iTime == -1) iTime = Time.realtimeSinceStartup;
            if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
            SaveChunk(terrains[i].x, terrains[i].z);
            UnloadTerrainChunk(i);
            chunksUnloaded = true;
          } else {
            terrains[i].currentTTL = 60000;
            SaveChunk(terrains[i].x, terrains[i].z);
          }
        } else {
          terrains[i].currentTTL--;
        }
      }
    }

#if DEBUG_HUD_LOADED || DEBUG_HUD_LOADING
    chunkListInfo.text = LoadedChunkList;
#else
    if (chunkListInfo != null) chunkListInfo.text = "";
#endif
    if(chunksUnloaded) {
      times.DeltaUnload = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateTimesAndHUD(float iTime, float iRealTime = -1) {
    // Figure out timings and averages.
    if (iTime > -1) {
      times.DeltaTotal = (Time.realtimeSinceStartup - iTime) * 1000;
      times.DeltaTotalReal = (Time.realtimeSinceStartup - iRealTime) * 1000;
      times.avgEnd++;
      if (times.avgEnd >= times.DeltaTotalAverageArray.Length) times.avgEnd = 0;
      times.DeltaTotalAverageArray[times.avgEnd] = times.DeltaTotal;
      times.DeltaTotalAverage = 0;
      int DeltaNum = 0;
      for (int i = 0; i < times.DeltaTotalAverageArray.Length; i++) {
        if (times.DeltaTotalAverageArray[i] != -1) {
          times.DeltaTotalAverage += times.DeltaTotalAverageArray[i];
          if (times.DeltaTotalMax < times.DeltaTotalAverageArray[i]) {
            times.DeltaTotalMax = times.DeltaTotalAverageArray[i];
          }
          DeltaNum++;
        }
      }
      if (times.DeltaTotalMax > lastMaxUpdateTime && times.pauseIfNewMax) {
        lastMaxUpdateTime = times.DeltaTotalMax;
        Debug.Break();
      }
      times.DeltaTotalAverage /= (float)DeltaNum;
    }
#if DEBUG_HUD_TIMES
    times.deltaNextUpdate.text =
        (Time.time - times.lastUpdate - times.UpdateSpeed).ToString() + "s";
    times.deltaTimes.text = "Current Frame:\n" +
        "Generate(" + times.DeltaGenerate + "ms)<--" +
          "T(" + times.DeltaGenerateTerrain + "ms)<--" +
          "W(" + times.DeltaGenerateWater + "ms) -- " +
          "WU(" + times.DeltaWaterUpdate + "ms),\n" +
        "Splats(" + times.DeltaSplatUpdate + "ms) -- " +
          "Tex("+ times.DeltaTextureUpdate + "ms) -- " +
        "Heightmap Application(" + times.DeltaHeightmapApplied + "ms)<--" +
          "Generation(" + times.DeltaGenerateHeightmap + "ms),\n" +
        "Fractal(" + times.DeltaFractal + "ms)<--" +
          "Divide(" + times.DeltaDivide + "ms)<--" +
          "Once(" + times.DeltaDivide / divideAmount + "ms)*" +
          "(" + divideAmount + " points),\n" +
        "Last Total(" + times.DeltaTotal + "ms) -- " +
          "Real: " + times.DeltaTotalReal + "ms) -- " +
          "Avg: " + times.DeltaTotalAverage + "ms -- " +
          "Max: " + times.DeltaTotalMax + "ms,\n" +
        "Update Neighbors(" + times.DeltaUpdate + "ms)\n" +
        "Unload(" + times.DeltaUnload + "ms),\n" +
        "Frame Count(" + Time.frameCount + ") -- " +
          "Time Slot(" + times.avgEnd + ")";

    times.deltaTimes.text += "\n\nPrevious Frame:\n" +
        "Generate(" + previousTimes.DeltaGenerate + "ms)<--" +
          "T(" + previousTimes.DeltaGenerateTerrain + "ms)<--" +
          "W(" + previousTimes.DeltaGenerateWater + "ms) -- " +
          "WU(" + previousTimes.DeltaWaterUpdate + "ms),\n" +
        "Tex("+ previousTimes.DeltaTextureUpdate + "ms),\n" +
        "Stuff(" + previousTimes.DeltaSplatUpdate + "ms)<--" +
          "Splat(" + previousTimes.DeltaSplatUpdate + "ms) -- " +
        "Heightmap Application(" + previousTimes.DeltaHeightmapApplied + "ms)<--" +
          "Generation(" + previousTimes.DeltaGenerateHeightmap + "ms),\n" +
        "Fractal(" + previousTimes.DeltaFractal + "ms)<--" +
          "Divide(" + previousTimes.DeltaDivide + "ms)<--" +
          "Once(" + previousTimes.DeltaDivide / divideAmount + "ms)*" +
          "(" + divideAmount + " points),\n" +
        "Last Total(" + previousTimes.DeltaTotal + "ms) -- " +
          "Real: " + previousTimes.DeltaTotalReal + "ms) -- " +
          "Avg: " + previousTimes.DeltaTotalAverage + "ms -- " +
          "Max: " + previousTimes.DeltaTotalMax + "ms -- " +
          "Delta: " + (Time.deltaTime*1000f) + "ms,\n" +
        "Update Neighbors(" + previousTimes.DeltaUpdate + "ms),\n" +
        "Unload(" + previousTimes.DeltaUnload + "ms),\n" +
        "Frame Count(" + (Time.frameCount-1) + ") -- " +
          "Time Slot(" + previousTimes.avgEnd + ")";

    previousTimes.DeltaGenerate          = times.DeltaGenerate;
    previousTimes.DeltaGenerateTerrain   = times.DeltaGenerateTerrain;
    previousTimes.DeltaGenerateWater     = times.DeltaGenerateWater;
    previousTimes.DeltaWaterUpdate       = times.DeltaWaterUpdate;
    previousTimes.DeltaTextureUpdate     = times.DeltaTextureUpdate;
    previousTimes.DeltaSplatUpdate       = times.DeltaSplatUpdate;
    previousTimes.DeltaSplatUpdate       = times.DeltaSplatUpdate;
    previousTimes.DeltaHeightmapApplied  = times.DeltaHeightmapApplied;
    previousTimes.DeltaGenerateHeightmap = times.DeltaGenerateHeightmap;
    previousTimes.DeltaFractal           = times.DeltaFractal;
    previousTimes.DeltaDivide            = times.DeltaDivide;
    previousTimes.DeltaTotal             = times.DeltaTotal;
    previousTimes.DeltaTotalReal         = times.DeltaTotalReal;
    previousTimes.DeltaTotalAverage      = times.DeltaTotalAverage;
    previousTimes.DeltaTotalMax          = times.DeltaTotalMax;
    previousTimes.DeltaUpdate            = times.DeltaUpdate;
    previousTimes.DeltaUnload            = times.DeltaUnload;
    previousTimes.avgEnd                 = times.avgEnd;

#else
    if (times.deltaTimes != null) times.deltaTimes.text = "";
    if (times.deltaNextUpdate != null) times.deltaNextUpdate.text = "";
#endif
  }

  void BeginChunkLoading(int x, int z, ref bool done) {
#if DEBUG_HUD_LOADED
    LoadedChunkList += "+(" + x + ", " + z + ") ";
#endif
    int pointIndex = -1;
    pointIndex = GetTerrainWithCoord(x, z);
    if (pointIndex == -1 && !done && !anyChunksDividing()) {
      GenerateTerrainChunk(x, z);
      FractalNewTerrains(x, z);
      done = true;
    } else if (pointIndex >= 0 && pointIndex < terrains.Count &&
               (terrains[pointIndex].isDividing ||
                (terrains[pointIndex].hasDivided &&
                 !terrains[pointIndex].terrReady) ||
                (!terrains[pointIndex].isDividing &&
                 !terrains[pointIndex].hasDivided &&
                 !terrains[pointIndex].loadingFromDisk)) &&
               !done && !terrains[pointIndex].loadingFromDisk) {
      FractalNewTerrains(x, z);
      done = true;
    } else {
      if (pointIndex >= 0) {
        terrains[pointIndex].terrToUnload = false;
        terrains[pointIndex].currentTTL = terrains[pointIndex].startTTL;
      }
    }
  }

  // Instantiate a new chunk and define its properties based off of the spawn
  // chunk.
  void GenerateTerrainChunk(int cntX, int cntZ) {
    if (GetTerrainWithCoord(cntX, cntZ) != -1) return;
    float iTime = Time.realtimeSinceStartup;
    if (cntZ == 0 && cntX == 0) {
      terrWidth = (int)this.GetComponent<Terrain>().terrainData.size.x;
      terrLength = (int)this.GetComponent<Terrain>().terrainData.size.z;
      terrHeight = (int)this.GetComponent<Terrain>().terrainData.size.y;
      heightmapWidth = this.GetComponent<Terrain>().terrainData.heightmapWidth;
      heightmapHeight =
          this.GetComponent<Terrain>().terrainData.heightmapHeight;
      ChangeGrassDensity(GameData.GrassDensity);

      Debug.Log("terrWidth: " + terrWidth + ", terrLength: " + terrLength +
                ", terrHeight: " + terrHeight + ", waterHeight: " +
                TerrainGenerator.waterHeight + "\nheightmapWidth: " +
                heightmapWidth + ", heightmapHeight: " + heightmapHeight);

      // Adjust heightmap by it's resolution so it appears the same no matter
      // how high resolution it is. Doesn't work but whatever, I'm leaving it
      // in.
      roughness *= 65f / heightmapWidth;

      if (terrains.Count > 0) {
        terrains[0] = new Terrains();
        Debug.LogError(
            "Terrains Exist but requested new spawn chunk! This is not " +
            "supported!");
      } else {
        terrains.Add(new Terrains());
      }
      terrains[0].x = 0;
      terrains[0].z = 0;
      terrains[0].terrData = GetComponent<Terrain>().terrainData;
      terrains[0].gameObject = this.gameObject;
      terrains[0].terrPoints = new float[ terrWidth, terrLength ];
      terrains[0].terrPerlinPoints = new float[ terrWidth, terrLength ];
      terrains[0].InitializeSubGenerators(subGenerators.Length);
      terrains[0].gameObject.name = "Terrain(" + cntX + "," + cntZ + ")";
#if DEBUG_MISC
      Debug.Log("Added Terrain (0,0){" + terrains.Count - 1 + "}");
#endif
      gBigSize = terrWidth + terrLength;
    } else {
      float iTime2 = Time.realtimeSinceStartup;

      // Add Terrain
      terrains.Add(new Terrains());
      terrains[terrains.Count - 1].x = cntX;
      terrains[terrains.Count - 1].z = cntZ;
      // Terrain Data
      terrains[terrains.Count - 1].terrData = new TerrainData();
      terrains[terrains.Count - 1].terrData.name =
          "TerrainData(" + cntX + "," + cntZ + ")";

      terrains[terrains.Count - 1].terrData.heightmapResolution =
          terrains[0].terrData.heightmapResolution;
      terrains[terrains.Count - 1].terrData.size = terrains[0].terrData.size;

      terrains[terrains.Count - 1].terrData.SetDetailResolution(
          terrains[0].terrData.detailResolution,
          /*terrains[0].terrData.detailWidth*/8);
      terrains[terrains.Count - 1].terrData.wavingGrassAmount =
          terrains[0].terrData.wavingGrassAmount;
      terrains[terrains.Count - 1].terrData.wavingGrassSpeed =
          terrains[0].terrData.wavingGrassSpeed;
      terrains[terrains.Count - 1].terrData.wavingGrassStrength =
          terrains[0].terrData.wavingGrassStrength;

      terrains[terrains.Count - 1].terrData.alphamapResolution =
          terrains[0].terrData.alphamapResolution;
      terrains[terrains.Count - 1].terrData.baseMapResolution =
          terrains[0].terrData.baseMapResolution;

      // Terrain
      terrains[terrains.Count - 1].gameObject =
          Instantiate(terrains[0].gameObject,
                      new Vector3(cntX * terrWidth, 0f, cntZ * terrLength),
                      Quaternion.identity);
      terrains[terrains.Count - 1]
          .gameObject.GetComponent<Terrain>()
          .terrainData = terrains[terrains.Count - 1].terrData;
      terrains[terrains.Count - 1]
          .gameObject.GetComponent<Terrain>()
          .detailObjectDensity = GameData.GrassDensity;

      terrains[terrains.Count - 1]
          .gameObject.GetComponent<TerrainCollider>()
          .terrainData = terrains[terrains.Count - 1].terrData;

      // Game Object and Components
      foreach (Transform child in terrains[terrains.Count - 1]
                   .gameObject.transform) {
        Destroy(child.gameObject);
      }
      foreach (Component comp in terrains[terrains.Count - 1]
                   .gameObject.GetComponents<Component>()) {
        if (!(comp is TerrainGenerator) && !(comp is Transform) &&
            !(comp is Terrain) && !(comp is TerrainCollider) &&
            !(comp is NavMeshSourceTag) && !(comp is ChunkCulling)) {
          Destroy(comp);
        }
      }
      foreach (Component comp in terrains[terrains.Count - 1]
                   .gameObject.GetComponents<Component>()) {
        if (!(comp is Transform) && !(comp is Terrain) &&
            !(comp is TerrainCollider) && !(comp is NavMeshSourceTag) &&
            !(comp is ChunkCulling)) {
          Destroy(comp);
        }
      }

      if (miniMapIcon != null)
        Instantiate(miniMapIcon,
                    terrains[terrains.Count - 1].gameObject.transform.position,
                    Quaternion.identity,
                    terrains[terrains.Count - 1].gameObject.transform);

      terrains[terrains.Count - 1].gameObject.name =
          "Terrain(" + cntX + "," + cntZ + ")";
      terrains[terrains.Count - 1].gameObject.layer =
          terrains[0].gameObject.layer;

      times.DeltaGenerateTerrain = (Time.realtimeSinceStartup - iTime2) * 1000;

      terrains[terrains.Count - 1].terrPoints =
          new float[ terrWidth, terrLength ];
      terrains[terrains.Count - 1].terrPerlinPoints =
          new float[ terrWidth, terrLength ];
      terrains[terrains.Count - 1].InitializeSubGenerators(
          subGenerators.Length);
    }
    LoadChunk(cntX, cntZ);
    times.DeltaGenerate = (Time.realtimeSinceStartup - iTime) * 1000;
  }

  void GenerateWaterChunk(int cntX, int cntZ) {
    // Add Water
    float iTime = Time.realtimeSinceStartup;
    float waterHeightRectified = TerrainGenerator.waterHeight / terrHeight;
    if(cntX == 0 && cntZ == 0) return;
    int terrID = GetTerrainWithCoord(cntX, cntZ);
    if (terrID < 0) return;
    Terrains terrain = terrains[terrID];
    if (waterTile != null && terrain != null && terrain.terrReady) {
      bool generateWater = false;
      float[, ] mixedHeights = MixHeights(terrID);
      int i=0, j=0;
      for (i = 0; i < heightmapWidth; i += 1) {
        for (j = 0; j < heightmapWidth; j += 1) {
          if (mixedHeights[ i, j ] < waterHeightRectified) {
            generateWater = true;
            break;
          }
        }
        if (generateWater) break;
      }
      if (!generateWater) {
        if (terrain.waterTile != null) Destroy(terrain.waterTile);
        return;
      } else if (generateWater && terrain.waterTile != null) {
        return;
      }
#if DEBUG_WATER
      Debug.Log("Generating water tile at (" + cntX + ", " + cntZ + ") at (" +
                i + ", " + j + ") (terrHeight: " +
                mixedHeights[ i, j ] * terrHeight + ", waterTile: " +
                waterTile.transform.position.y + ")");
#endif
      Vector3 terrVector3 =
          terrain.gameObject.GetComponent<Terrain>().transform.position;
      Vector3 waterVector3 = terrVector3;
      waterVector3.y += waterTile.transform.position.y;
      //waterVector3.x += terrWidth / 2;
      //waterVector3.z += terrLength / 2;
      terrains[terrID].waterTile =
          Instantiate(waterTile, waterVector3, Quaternion.identity,
                      terrain.gameObject.transform);
      times.DeltaGenerateWater = (Time.realtimeSinceStartup - iTime) * 1000;
    }
  }

  // Generate a heightmap for a chunk.
  void FractalNewTerrains(int changeX, int changeZ) {
    float iTime = Time.realtimeSinceStartup;

    int tileCnt = GetTerrainWithCoord(changeX, changeZ);
#if DEBUG_ARRAY
    Debug.Log("TileCount: " + tileCnt + ", X: " + changeX + " Z: " + changeZ +
              "\nterrains.Count: " + terrains.Count + ", terrains[tileCnt]: " +
              terrains[tileCnt].terrData + "\nTerrain Name: " +
              terrains[tileCnt].gameObject.name);
#endif
    if (tileCnt >= 0 && tileCnt < terrains.Count) {
      GenerateNew(changeX, changeZ, roughness);
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
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              0, 0) +
          "\nTerrain Steepness(1,0): " +
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              1, 0) +
          "\nTerrain Steepness(0,1): " +
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              0, 1) +
          "\nTerrain Steepness(1,1): " +
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetSteepness(
              1, 1) +
          "\nTerrain Height(0,0): " +
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(0,
                                                                          0) +
          "\nTerrain Height(1,0): " +
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(1,
                                                                          0) +
          "\nTerrain Height(0,1): " +
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(0,
                                                                          1) +
          "\nTerrain Height(1,1): " +
          gameObject[tileCnt].GetComponent<Terrain>().terrainData.GetHeight(1,
                                                                          1));
#endif
    } else {
      Debug.LogError("Invalid tileCnt: " + tileCnt +
                     "\nTried to find Terrain(" + changeX + "," + changeZ +
                     ")");
    }
    times.DeltaFractal = (Time.realtimeSinceStartup - iTime) * 1000;
  }

  // Unload a chunk by destroying its GameObject then removing it from the array
  // of all chunks. Also make sure that if it was previously applying its
  // heightmap, that it does not continue to try and apply this heightmap now
  // that the chunk is gone.
  void UnloadTerrainChunk(int X, int Z) {
    UnloadTerrainChunk(GetTerrainWithCoord(X, Z));
  }
  void UnloadTerrainChunk(int loc) {
    if (loc == 0) return; // Spawn chunk may not be unloaded.
    Destroy(terrains[loc].gameObject);
    // All objects should be parented to this gameObject, and should be
    // destroyed with it.
    terrains.RemoveAt(loc);
    if (GetTerrainWithCoord(lastTerrUpdated.x, lastTerrUpdated.z) == loc) {
      lastTerrUpdateLoc = -1;
      lastTerrUpdated = new Terrains();
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
    int terrIndex = GetTerrainWithCoord(changeX, changeZ);

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
    if (GenMode.DisplaceDivide && !anyChunksDividing()) {
      if (useSeed) {
        UnityEngine.Random.InitState(
            (int)(Seed + PerfectlyHashThem((short)(changeX * 3 - 3),
                                           (short)(changeZ * 3 - 3))));
      }
      float[, ] modifier = new float[ 2, 2 ];
      PerlinDivide(ref modifier, changeX, changeZ, 1, 1,
                   PerlinSeedModifier * 2f, BiomeRoughness);
      terrains[terrIndex].biome = modifier[ 0, 0 ] * PeakMultiplier;
#if DEBUG_HEIGHTS || DEBUG_ARRAY
      Debug.Log(terrains[terrIndex].gameObject.GetComponent<Terrain>().name +
                ",(0,0): " + points[ 0, 0 ]);
      Debug.Log(terrains[terrIndex].gameObject.GetComponent<Terrain>().name +
                ",(" + iWidth + "," + iHeight + "): " +
                points[ (int)iWidth - 1, (int)iHeight - 1 ]);
#endif

      gRoughness = iRoughness;

      logCount = 11;
      // Set all borders to the middle of valid points to cause the spawn chunk
      // to be around the middle of the possible heights and has a lower chance
      // of clipping extreme heights.
      if (!terrains[0].loadedFromDisk) {
        points[ 0, 0 ] = 0.5f;
        points[ (int)iWidth - 1, 0 ] = 0.5f;
        points[ 0, (int)iHeight - 1 ] = 0.5f;
        points[ (int)iWidth - 1, (int)iHeight - 1 ] = 0.5f;
        if (GenMode.midOffset) {
          int halfX = Mathf.RoundToInt(iWidth / 2);
          int halfZ = Mathf.RoundToInt(iHeight / 2);
          float[, ] offset = new float[ 2, 2 ];
          float[, ] offsets = new float[ 2, 2 ];

          PerlinDivide(ref offset, changeX, changeZ, 2, 2, PerlinSeedModifier,
                       10f);
          offsets[ 0, 0 ] = offset[ 0, 0 ];
          offsets[ 0, 1 ] = offset[ 0, 1 ];

          // halfX
          PerlinDivide(ref offset, changeX + 1, changeZ, 2, 2,
                       PerlinSeedModifier, 10f);
          offsets[ 1, 0 ] = offset[ 0, 0 ];

          // halfZ
          PerlinDivide(ref offset, changeX, changeZ + 1, 2, 2,
                       PerlinSeedModifier, 10f);
          offsets[ 1, 1 ] = offset[ 0, 1 ];

          // Limit range.
          for (int i = 0; i < offsets.GetLength(0); i++) {
            for (int j = 0; j < offsets.GetLength(1); j++) {
              offsets[i, j] = ((offsets[i, j] - 0.5f) *
                               (terrains[terrIndex].biome * 0.5f)) +
                              0.5f;
            }
          }

          points[ halfX, 0 ] = offsets[ 0, 0 ];
          points[ halfX, (int)iHeight - 1 ] = offsets[ 1, 0 ];

          points[ 0, halfZ ] = offsets[ 0, 1 ];
          points[ (int)iWidth - 1, halfZ ] = offsets[ 1, 1 ];
        }
      }
    }

    float iTime2 = Time.realtimeSinceStartup;

    // If the Displace Divide generator is being used, create a heightmap with
    // this generator.
    if (GenMode.DisplaceDivide) {
      // Divide chunk into 4 sections and displace the center thus creating 4
      // more sections per section until every pixel is defined.
      if (terrIndex == -1) {
        Debug.LogError("Chunk was not generated before fractaling!\nIndex: " +
                       terrIndex + ", Coord(" + changeX + ", " + changeZ + ")");
        return;
      } else if (terrains[terrIndex].isDividing) {
        terrains[terrIndex].terrToUnload = false;
        terrains[terrIndex].currentTTL = terrains[terrIndex].startTTL;
        return;
      } else if (terrains[terrIndex].hasDivided) {
        terrains[terrIndex].terrToUnload = false;
        terrains[terrIndex].currentTTL = terrains[terrIndex].startTTL;
        points = terrains[terrIndex].terrPoints;
        terrains[terrIndex].terrReady = true;
      } else {
#if DEBUG_DIVIDE
        Debug.Log("Dividing new grid.\nIndex: " + terrIndex + ", Coord(" +
                  changeX + ", " + changeZ + ")");
#endif
        terrains[terrIndex].terrPoints = points;
        terrains[terrIndex].terrToUnload = false;
        terrains[terrIndex].isDividing = true;
        terrains[terrIndex].currentTTL = terrains[terrIndex].startTTL;
#if DEBUG_HUD_LOADING
        divideAmount_ = divideAmount;
#endif
        divideAmount = 0;
        // If we are using GenMode.multiThreading, then the terrain has not
        // been finished being divided. Otherwise, the whole process could
        // happen before the function returns.
        IEnumerator enumerator =
            DivideNewGrid(changeX, changeZ, 0, 0, iWidth, iHeight,
                          points[ 0, 0 ], points[ 0, (int)iHeight - 1 ],
                          points[ (int)iWidth - 1, (int)iHeight - 1 ],
                          points[ (int)iWidth - 1, 0 ]);
        if(GenMode.multiThreading) {
          threadStart = Time.realtimeSinceStartup;
          divideEnumerator = enumerator;
          thread = new Thread(DivideHelper);
          thread.Start();
          return;
        } else {
          while (enumerator.MoveNext()) {
          }
        }
      }
    }

    // If the Perlin Noise generator is being used, create a heightmap with it.
    if (GenMode.Perlin) {
      PerlinDivide(ref perlinPoints, changeX, changeZ, iWidth, iHeight);
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

          // points[ (int)r, (int)c ] =
          //     Mathf.Sqrt(changeX * changeX + changeZ * changeZ) / 10f + 0.00001f;

          points[(int)r, (int)c] = Mathf.Sin((changeX*iWidth + r)/50f);
        }
      }
    }

    times.DeltaDivide = (Time.realtimeSinceStartup - iTime2) * 1000;

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
    // heightmaps given as (z,x). This doesn't flip points anymore since this
    // was moved to where the heightmap is being applied since this is easier
    // for matching edges. This does, however, still check every point and find
    // and fix any undefined points.
    float[, ] flippedPoints = points;
    logCount = 10;
    if (GenMode.DisplaceDivide) {
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
              if (logCount >= 0) {
                Debug.LogError("Flipping points found undefined area! (" + c +
                               ", " + r + ")");
              }
              logCount--;
            } else {
              Displace(0);
            }
            flippedPoints[ c, r ] = p;
          } else {
            Displace(0);
          }
        }
        if (logCount < 0) {
          Debug.LogWarning(-logCount + " additional suppressed warnings.");
        }
      }

      if (GenMode.SmoothHeightmap) SmoothHeightmap(ref flippedPoints);

      // Double check that all the edges match up.
      if (!MatchEdges(iWidth, iHeight, changeX, changeZ, ref flippedPoints)) {
        Debug.LogError("This shouldn't happen... (You broke something)");
      }
    }

    times.DeltaGenerateHeightmap = (Time.realtimeSinceStartup - iTime) * 1000;

    // Save each heightmap to the array to be applied later.
    terrains[terrIndex].terrPoints = flippedPoints;
    if (!terrains[terrIndex].loadedFromDisk)
      terrains[terrIndex].terrPerlinPoints = perlinPoints;
    terrains[terrIndex].terrQueue = true;
    terrains[terrIndex].waterQueue = true;
  }

  void SmoothHeightmap(ref float[, ] points) {
    float[, ] smoothedPoints = points;
    for (int i = 0; i < smoothedPoints.GetLength(0); i++) {
      for (int j = 0; j < smoothedPoints.GetLength(1); j++) {
        smoothedPoints[i, j] = AverageSurroundingPoints(ref points, i, j);
      }
    }
    points = smoothedPoints;
  }

  // Set the edge of the new chunk to the same values as the bordering chunks.
  // This is to create uniformity between chunks.
  bool MatchEdges(float iWidth, float iHeight, int changeX, int changeZ,
                  ref float[, ] points) {
#if DEBUG_HEIGHTS || DEBUG_BORDERS_1 || DEBUG_BORDERS_2 || DEBUG_BORDERS_3 || DEBUG_BORDERS_4
    Debug.Log("MATCHING EDGES OF CHUNK (" + changeX + ", " + changeZ + ")");
// Debug.Log(
//     "(0,0) InterpolatedHeight = " +
//     (terrains[0].gameObject.GetComponent<Terrain>().terrainData.GetInterpolatedHeight(
//          0, 0) /
//      terrains[0].gameObject.GetComponent<Terrain>().terrainData.size.y));
#endif
    int b1 = GetTerrainWithCoord(changeX - 1, changeZ);  // Left
    int b2 = GetTerrainWithCoord(changeX, changeZ + 1);  // Top
    int b3 = GetTerrainWithCoord(changeX + 1, changeZ);  // Right
    int b4 = GetTerrainWithCoord(changeX, changeZ - 1);  // Bottom
    float[, ] newpoints = points;
    if (b1 >= 0 && terrains[b1].hasDivided) {  // left
#if DEBUG_BORDERS_1
      Debug.Log("Border1(" + (changeX - 1) + "," + changeZ + "),(0,0): " +
                terrains[b1].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iHeight; i++) {
        newpoints[ i, 0 ] = terrains[b1].terrPoints[ i, (int)iWidth - 1 ];
      }
    } else if(b1 >= 0) return false;
    if (b2 >= 0 && terrains[b2].hasDivided) {  // top
#if DEBUG_BORDERS_2
      Debug.Log("Border2(" + changeX + "," + (changeZ + 1) + "),(0,0): " +
                terrains[b2].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iWidth; i++) {
        newpoints[ (int)iHeight - 1, i ] = terrains[b2].terrPoints[ 0, i ];
      }
    } else if(b2 >= 0) return false;
    if (b3 >= 0 && terrains[b3].hasDivided) {  // right
#if DEBUG_BORDERS_3
      Debug.Log("Border3(" + (changeX + 1) + "," + changeZ + "),(0,0): " +
                terrains[b3].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iHeight; i++) {
        newpoints[ i, (int)iWidth - 1 ] = terrains[b3].terrPoints[ i, 0 ];
      }
    } else if(b3 >= 0) return false;
    if (b4 >= 0 && terrains[b4].hasDivided) {  // bottom
#if DEBUG_BORDERS_4
      Debug.Log("Border4(" + changeX + "," + (changeZ - 1) + "),(0,0): " +
                terrains[b4].terrPoints[ 0, 0 ]);
#endif
      for (int i = 0; i < iWidth; i++) {
        newpoints[ 0, i ] = terrains[b4].terrPoints[ (int)iHeight - 1, i ];
      }
    } else if(b4 >= 0) return false;
    points = newpoints;
    return true;
  }

  // Intended to be run as a separate thread.
  void DivideHelper() {
    while (divideEnumerator.MoveNext()) {
    }
  }

  // The center of the chunk is displaced a random amount from the average of
  // the four corners. It is then split into four sections, each of which has
  // its center displaced from the average of the section's four corners. This
  // then splits each section into four more sections, and repeats until every
  // point of the heightmap is defined.
  //
  // Each recursive step can be split among a configurable number of frames,
  // time delay, or the whole thing can be given its own thread.
  IEnumerator DivideNewGrid(int chunkX, int chunkY, float dX, float dY,
                            float dwidth, float dheight, float c1, float c2,
                            float c3, float c4, int index = -1,
                            int recursiveDepth = 0) {

    if (index == -1) index = GetTerrainWithCoord(chunkX, chunkY);
    if (useSeed && recursiveDepth == 0) {
      rand = new System.Random(
          (int)(Seed +
                PerfectlyHashThem((short)(chunkX * 3), (short)(chunkY * 3))));
    }
    divideAmount++;
    if (logCount > -1 && dwidth != dheight) {
      Debug.LogWarning("Width-Height Mismatch: Expected square grid.\nDX: " +
                       dX + ", DY: " + dY + ", dwidth: " + dwidth +
                       ", dheight: " + dheight);
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
      Middle = terrains[index].terrPoints[
        (int)Math.Floor((dX + dX + dwidth) / 2),
        (int)Math.Floor((dY + dY + dheight) / 2)
      ];
      // Find the center point height of each side of the section.
      Edge1 =
          terrains[index]
              .terrPoints[ (int)dX, (int)Math.Floor((dY + dY + dheight) / 2) ];
      Edge2 =
          terrains[index].terrPoints
          [ (int)Math.Floor((dX + dX + dwidth) / 2), (int)(dY + dheight - 1) ];
      Edge3 =
          terrains[index].terrPoints
          [ (int)(dX + dwidth - 1), (int)Math.Floor((dY + dY + dheight) / 2) ];
      Edge4 =
          terrains[index]
              .terrPoints[ (int)Math.Floor((dX + dX + dwidth) / 2), (int)dY ];

      if (c1 < 0) {
        c1 = terrains[index].terrPoints[ (int)dX, (int)dY ];
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
        c2 = terrains[index].terrPoints[ (int)dX, (int)dY + (int)dheight - 1 ];
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
        c3 = terrains[index].terrPoints
             [ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
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
        c4 = terrains[index].terrPoints[ (int)dX + (int)dwidth - 1, (int)dY ];
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
        Middle =
            ((c1 + c2 + c3 + c4) / 4) +
            Displace((dwidth + dheight) *
                     terrains[index].biome);  // Randomly displace the midpoint!
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
        Debug.LogWarning(
            "Divide(Pre-Rectify):\n" + "C1: (0, 0):      " +
            terrains[index].terrPoints[ 0, 0 ] + "/" + c1 + "\n" + "C2: (0, " +
            (int)(dheight - 1) + "):      " +
            terrains[index].terrPoints[ 0, (int)dheight - 1 ] + "/" + c2 +
            "\n" + "C3: (" + (int)(dwidth - 1) + ", " + (int)(dheight - 1) +
            "):      " +
            terrains[index].terrPoints[ (int)dwidth - 1, (int)dheight - 1 ] +
            "/" + c3 + "\n" + "C4: (" + (int)(dwidth - 1) + ", 0):      " +
            terrains[index].terrPoints[ (int)dwidth - 1, 0 ] + "/" + c4 + "\n"

            + "Edge1: (0, " + (int)Math.Floor((dheight) / 2) + "):      " +
            terrains[index].terrPoints[ 0, (int)Math.Floor((dheight) / 2) ] +
            "/" + Edge1 + "\n" + "Edge2: (" + (int)Math.Floor((dwidth) / 2) +
            ", " + (int)(dheight - 1) + "):      " +
            terrains[index].terrPoints
            [ (int)Math.Floor((dwidth) / 2), (int)dheight - 1 ] +
            "/" + Edge2 + "\n" + "Edge3: (" + (int)(dwidth - 1) + ", " +
            (int)Math.Floor((dheight - 1) / 2) + "):      " +
            terrains[index].terrPoints
            [ (int)dwidth - 1, (int)Math.Floor((dheight) / 2) ] +
            "/" + Edge3 + "\n" + "Edge4: (" + (int)Math.Floor((dwidth) / 2) +
            ", 0):      " +
            terrains[index].terrPoints[ (int)Math.Floor((dwidth) / 2), 0 ] +
            "/" + Edge4 + "\n"

            + "Middle: (" + (int)Math.Floor((dwidth - 1) / 2) + ", " +
            (int)Math.Floor((dheight - 1) / 2) + "):      " +
            terrains[index].terrPoints[
              (int)Math.Floor((dwidth - 1) / 2),
              (int)Math.Floor((dheight - 1) / 2)
            ] +
            "/" + Middle + "\n" + "\n" + "dX: " + dX + ", dY: " + dY +
            ", dwidth: " + dwidth + ", dheight: " + dheight + ", Coord(" +
            chunkX + ", " + chunkY + ")");
        logCount--;
      }

      // Make sure that the midpoint doesn't accidentally randomly displace past
      // the boundaries.
      Rectify(ref Middle);
      Rectify(ref Edge1);
      Rectify(ref Edge2);
      Rectify(ref Edge3);
      Rectify(ref Edge4);

      // Save points to array
      terrains[index].terrPoints[
        (int)Math.Floor((dX + dX + dwidth) / 2),
        (int)Math.Floor((dY + dY + dheight) / 2)
      ] = Middle;

      terrains[index]
          .terrPoints[ (int)dX, (int)Math.Floor((dY + dY + dheight) / 2) ] =
          Edge1;
      terrains[index].terrPoints[
        (int)Math.Floor((dX + dX + dwidth) / 2),
        (int)(dY + dheight - 1)
      ] = Edge2;
      terrains[index].terrPoints[
        (int)(dX + dwidth - 1),
        (int)Math.Floor((dY + dY + dheight) / 2)
      ] = Edge3;
      terrains[index]
          .terrPoints[ (int)Math.Floor((dX + dX + dwidth) / 2), (int)dY ] =
          Edge4;

      // Rectify corners then save them to array.
      Rectify(ref c1);
      Rectify(ref c2);
      Rectify(ref c3);
      Rectify(ref c4);
      terrains[index].terrPoints[ (int)dX, (int)dY ] = c1;
      terrains[index].terrPoints[ (int)dX, (int)dY + (int)dheight - 1 ] = c2;
      terrains[index].terrPoints
          [ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ] = c3;
      terrains[index].terrPoints[ (int)dX + (int)dwidth - 1, (int)dY ] = c4;

      if (ShowWarning) {
        Debug.LogWarning(
            "Divide(Post-Rectify):\n" + "C1: (0, 0):      " +
            terrains[index].terrPoints[ 0, 0 ] + "/" + c1 + "\n" + "C2: (0, " +
            (int)(dheight - 1) + "):      " +
            terrains[index].terrPoints[ 0, (int)dheight - 1 ] + "/" + c2 +
            "\n" + "C3: (" + (int)(dwidth - 1) + ", " + (int)(dheight - 1) +
            "):      " +
            terrains[index].terrPoints[ (int)dwidth - 1, (int)dheight - 1 ] +
            "/" + c3 + "\n" + "C4: (" + (int)(dwidth - 1) + ", 0):      " +
            terrains[index].terrPoints[ (int)dwidth - 1, 0 ] + "/" + c4 + "\n"

            + "Edge1: (0, " + (int)Math.Floor((dheight) / 2) + "):      " +
            terrains[index].terrPoints[ 0, (int)Math.Floor((dheight) / 2) ] +
            "/" + Edge1 + "\n" + "Edge2: (" + (int)Math.Floor((dwidth) / 2) +
            ", " + (int)(dheight - 1) + "):      " +
            terrains[index].terrPoints
            [ (int)Math.Floor((dwidth) / 2), (int)dheight - 1 ] +
            "/" + Edge2 + "\n" + "Edge3: (" + (int)(dwidth - 1) + ", " +
            (int)Math.Floor((dheight - 1) / 2) + "):      " +
            terrains[index].terrPoints
            [ (int)dwidth - 1, (int)Math.Floor((dheight) / 2) ] +
            "/" + Edge3 + "\n" + "Edge4: (" + (int)Math.Floor((dwidth) / 2) +
            ", 0):      " +
            terrains[index].terrPoints[ (int)Math.Floor((dwidth) / 2), 0 ] +
            "/" + Edge4 + "\n"

            + "Middle: (" + (int)Math.Floor((dwidth - 1) / 2) + ", " +
            (int)Math.Floor((dheight - 1) / 2) + "):      " +
            terrains[index].terrPoints[
              (int)Math.Floor((dwidth - 1) / 2),
              (int)Math.Floor((dheight - 1) / 2)
            ] +
            "/" + Middle + "\n" + "\n" + "dX: " + dX + ", dY: " + dY +
            ", dwidth: " + dwidth + ", dheight: " + dheight);
      }

      // Do the operation over again for each of the four new
      // grids.

      // 1/4
      if (useSeed && recursiveDepth == 0) {
        rand = new System.Random(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 2),
                                           (short)(chunkY * 3 - 2))));
      }
      IEnumerator enumerator =
          DivideNewGrid(chunkX, chunkY, dX, dY, newWidth, newHeight, c1, Edge1,
                        Middle, Edge4, index, recursiveDepth + 1);
      while (enumerator.MoveNext()) {
      }

      // 2/4
      if (useSeed && recursiveDepth == 0) {
        rand = new System.Random(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 1),
                                           (short)(chunkY * 3 - 2))));
      }
      enumerator =
          DivideNewGrid(chunkX, chunkY, dX + newWidth, dY, newWidth, newHeight,
                        Edge4, Middle, Edge3, c4, index, recursiveDepth + 1);
      while (enumerator.MoveNext()) {
      }

      // 3/4
      if (useSeed && recursiveDepth == 0) {
        rand = new System.Random(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 1),
                                           (short)(chunkY * 3 - 1))));
      }
      enumerator = DivideNewGrid(chunkX, chunkY, dX + newWidth, dY + newHeight,
                                 newWidth, newHeight, Middle, Edge2, c3, Edge3,
                                 index, recursiveDepth + 1);
      while (enumerator.MoveNext()) {
      }

      // 4/4
      if (useSeed && recursiveDepth == 0) {
        rand = new System.Random(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 2),
                                           (short)(chunkY * 3 - 1))));
      }
      enumerator =
          DivideNewGrid(chunkX, chunkY, dX, dY + newHeight, newWidth, newHeight,
                        Edge1, c2, Edge2, Middle, index, recursiveDepth + 1);
      while (enumerator.MoveNext()) {
      }

      if (recursiveDepth == 0) {
#if DEBUG_DIVIDE
        Debug.Log("Terrain divide has finished chunk " + index + " (" + chunkX +
                  ", " + chunkY + ")");
#endif
        terrains[index].isDividing = false;
        terrains[index].hasDivided = true;
      }

    } else {
      if (dheight < 1) dheight = 1;
      if (dwidth < 1) dwidth = 1;
      // The four corners of the grid piece will be averaged and drawn as a
      // single pixel.
      float c = AverageCorners(c1, c2, c3, c4);
      terrains[index].terrPoints[ (int)(dX), (int)(dY) ] = c;
      if (dwidth == 2) {
        terrains[index].terrPoints[ (int)(dX + 1), (int)(dY) ] = c;
      }
      if (dheight == 2) {
        terrains[index].terrPoints[ (int)(dX), (int)(dY + 1) ] = c;
      }
      if ((dwidth == 2) && (dheight == 2)) {
        terrains[index].terrPoints[ (int)(dX + 1), (int)(dY + 1) ] = c;
      }
      // Does not repeat since the size of this section is 1x1. This means
      // it cannot be divided further and we are done.
    }
    yield break;
  }
  // Uses perlin noise to define a heightmap.
  public void PerlinDivide(ref float[, ] points, float x, float y, float w, float h,
                    float PerlinSeedModifier_ = -1,
                    float PerlinRoughness_ = -1) {
    if (PerlinSeedModifier_ == -1) PerlinSeedModifier_ = PerlinSeedModifier;
    if (PerlinRoughness_ == -1) PerlinRoughness_ = PerlinRoughness;
    if (w > 1) w--;
    if (h > 1) h--;
    float xShifted = (x + (Seed * PerlinSeedModifier_)) * w;
    float yShifted = (y + (Seed * PerlinSeedModifier_)) * h;
    for (int r = 0; r <= h; r++) {
      for (int c = 0; c <= w; c++) {
        if (GenMode.Distort) {
          float noise =
              Mathf.PerlinNoise(PerlinRoughness_ * (xShifted + c) / w,
                                PerlinRoughness_ * (yShifted + r) / h);
          float f1 = Mathf.Log(1 - noise) * -PerlinRoughness_ * 0.3f;
          float f2 = -1 / (1 + Mathf.Pow(2.718f, 10 * (noise - 0.90f))) + 1;
          // e approx 2.718
          float blendStart = 0.9f;
          float blendEnd = 1.0f;
          // Distort the heightmap.
          if (noise > 0 && noise <= blendStart) {
            points[r, c] = f1 + yShift;
          } else if (noise < blendEnd && noise > blendStart) {
            points[r, c] =
                ((f1 * ((blendEnd - blendStart) - (noise - blendStart))) +
                 (f2 * (noise - blendStart))) /
                    (blendEnd - blendStart) +
                yShift;
          } else {
            points[r, c] = f2 + yShift;
          }
        } else {
          float noise =
              Mathf.PerlinNoise(
                  Mathf.Pow(PerlinRoughness_, 1.2f) * (xShifted + c) / w,
                  Mathf.Pow(PerlinRoughness_, 1.2f) * (yShifted + r) / h) +
              yShift;

          points[ r, c ] = noise * PerlinHeight;
        }
      }
    }
  }

  // Squash all values to a valid range.
  void Rectify(ref float iNum) {
    iNum = iNum < 0 + yShift ? yShift
                             : (iNum > 1 + yShift ? iNum = 1 + yShift : iNum);
    // if (iNum < 0 + yShift) {
    //   iNum = 0 + yShift;
    // } else if (iNum > 1.0 + yShift) {
    //   iNum = 1.0f + yShift;
    // }
  }

  // Randomly choose a value in a range that becomes smaller as the section
  // being defined becomes smaller.
  float Displace(float SmallSize) {
    if (rand.NextDouble() > 0.98) SmallSize *= 2f;
    float Max = SmallSize / gBigSize * gRoughness;
    return (float)(rand.NextDouble() - 0.5) * Max;
  }


  // For use before SetHeights is called.
  public float GetSteepness(float[, ] Heightmap, int x, int y) {
    return GetSteepness(GetNormal(Heightmap, x, y));
  }
  public float GetSteepness(Vector3 normal) {
    return Mathf.Acos(Vector3.Dot(normal, Vector3.up)) * 180f / Mathf.PI;
  }
  public Vector3 GetNormal(float[, ] Heightmap, int x, int y) {
    if (x >= Heightmap.GetLength(0)) x = Heightmap.GetLength(0) - 1;
    if (y >= Heightmap.GetLength(1)) y = Heightmap.GetLength(1) - 1;
    if (x < 0) x = 0;
    if (y < 0) y = 0;
    float slopeX1 = Heightmap[ x, y ] -
                    Heightmap[ x < Heightmap.GetLength(0) - 1 ? x + 1 : x, y ];
    float slopeZ1 = Heightmap[ x, y ] -
                    Heightmap[ x, y < Heightmap.GetLength(1) - 1 ? y + 1 : y ];
    float slopeX2 = Heightmap[ x > 0 ? x - 1 : x, y ] - Heightmap[ x, y ];
    float slopeZ2 = Heightmap[ x, y > 0 ? y - 1 : y ] - Heightmap[ x, y ];

    float slopeX = (slopeX1 + slopeX2) / 2f;
    float slopeZ = (slopeZ1 + slopeZ2) / 2f;

    if (x == 0 || x == Heightmap.GetLength(0) - 1) slopeX *= 2;
    if (y == 0 || y == Heightmap.GetLength(1) - 1) slopeZ *= 2;

    slopeX *= terrHeight / 2f;
    slopeZ *= terrHeight / 2f;

    Vector3 normal = new Vector3(slopeX, 2, slopeZ);
    normal.Normalize();
    return normal;
  }

  void ClearTreeInstances(Terrains terrain) {
    TreeInstance[] emptyTrees = new TreeInstance[0];
    terrain.terrData.treeInstances = emptyTrees;
    terrain.gameObject.GetComponent<Terrain>().Flush();
  }

  public void SaveAllChunks() {
    if (GetComponent<SaveLoad>() == null || !GetComponent<SaveLoad>().enabled)
      return;
    for (int i = 0; i < terrains.Count; i++) {
      SaveChunk(terrains[i].x, terrains[i].z);
    }
  }
  void SaveChunk(int X, int Z) {
    if (GetComponent<SaveLoad>() == null || !GetComponent<SaveLoad>().enabled)
      return;
    int terrID = GetTerrainWithCoord(X, Z);
    SaveLoad.WriteTerrain(X, Z, terrains[terrID].terrPoints,
                          terrains[terrID].terrPerlinPoints,
                          terrains[terrID].terrData, worldID);
  }
  void LoadChunk(int X, int Z) {
    if (GetComponent<SaveLoad>() == null || !GetComponent<SaveLoad>().enabled)
      return;
    int terrID = GetTerrainWithCoord(X, Z);
    if (terrains[terrID].loadedFromDisk ||
        !SaveLoad.TerrainExists(X, Z, worldID))
      return;
    SaveLoad.ReadTerrain(terrains[terrID], worldID);
  }

  // Give array index from coordinates.
  private int GetTerrainWithCoord(int x, int z) {
    for (int i = 0; i < terrains.Count; i++) {
      if (terrains[i].x == x && terrains[i].z == z) {
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
      if (terrains[i].gameObject && terrains[i].terrData == terr) {
        return i;
      }
    }
    return -1;
  }

  // Give array index by comparing Terrain GameObjects.
  int GetTerrainWithData(Terrain terr) {
    return GetTerrainWithData(terr.terrainData);
  }

  // Find the average of up to 4 defined values. EmptyPoint counts as undefined
  // and is ignored.
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

  float AverageSurroundingPoints(ref float[,] points, int x, int z) {
    int numAvg = 0;
    float Avg = 0f;
    if (points[x, z] != EmptyPoint) {
      Avg = points[x,z];
      numAvg = 1;
    }
    if (x - 1 >= 0 && points[x - 1, z] != EmptyPoint) {
      Avg += points[x - 1, z];
      numAvg++;
    }
    if (x + 1 < points.GetLength(0) && points[x + 1, z] != EmptyPoint) {
      Avg += points[x + 1, z];
      numAvg++;
    }
    if (z - 1 >= 0 && points[x, z - 1] != EmptyPoint) {
      Avg += points[x, z - 1];
      numAvg++;
    }
    if (z + 1 < points.GetLength(1) && points[x, z + 1] != EmptyPoint) {
      Avg += points[x, z + 1];
      numAvg++;
    }
    if (numAvg == 0) return EmptyPoint;
    return Avg / numAvg;
  }

  // Find unique number for each given coordinate.
  public int PerfectlyHashThem(short a, short b) {
    var A = (uint)(a >= 0 ? 2 * a : -2 * a - 1);
    var B = (uint)(b >= 0 ? 2 * b : -2 * b - 1);
    var C = (int)((A >= B ? A * A + A + B : A + B * B) / 2);
    return a < 0 && b < 0 || a >= 0 && b >= 0 ? C : -C - 1;
  }

  // Mix the two saved heightmaps (Perlin and Displace Divide) so that it may
  // be applied to the chunk.
  public float[, ] MixHeights(int terrLoc) { return MixHeights(terrains[terrLoc]); }
  public float[, ] MixHeights(Terrains terrain) {
    if (GenMode.Perlin && !GenMode.DisplaceDivide) {
      return terrain.terrPerlinPoints;
    } else if (!GenMode.Perlin) {
      return terrain.terrPoints;
    } else {
      float[, ] output = new float[ heightmapWidth, heightmapHeight ];
      for (int i = 0; i < heightmapHeight * heightmapWidth; i++) {
        int z = i % heightmapHeight;
        int x = (int)Math.Floor((float)i / heightmapWidth);
        output[x, z] =
            Mathf.Lerp(terrain.terrPoints[x, z], terrain.terrPerlinPoints[x, z],
                       GenMode.mixtureAmount);
        // Save highest and lowest values for debugging.
        if (output[x, z] < lowest && output[x, z] >= 0) lowest = output[x, z];
        if (output[ x, z ] > highest) highest = output[ x, z ];
      }
      return output;
    }
  }
  // Returns the height of the terrain at the player's current location in
  // global units.
  public static float GetTerrainHeight() { return GetTerrainHeight(player); }
  public static float GetTerrainHeight(InitPlayer player) {
    return GetTerrainHeight(player.gameObject.transform.position);
  }
  public static float GetTerrainHeight(GameObject player) {
    return GetTerrainHeight(player.transform.position);
  }
  public static float GetTerrainHeight(float x, float z) {
    return GetTerrainHeight(new Vector3(x, 0, z));
  }
  public static float GetTerrainHeight(Vector3 position) {
    int xCenter = Mathf.RoundToInt((position.x - terrWidth / 2) / terrWidth);
    int yCenter = Mathf.RoundToInt((position.z - terrLength / 2) / terrLength);
    int terrLoc = tg.GetTerrainWithCoord(xCenter, yCenter);
    if (terrLoc != -1) {
      float TerrainHeight =
          tg.terrains[terrLoc].gameObject.GetComponent<Terrain>().SampleHeight(
              position);
      return TerrainHeight;
    }
    return 0;
  }
  public float GetTerrainWidth() { return terrWidth; }
  public float GetTerrainLength() { return terrLength; }
  public float GetTerrainMaxHeight() { return terrHeight; }
  public static Vector3 GetPointOnTerrain(Vector3 position) {
    return new Vector3(position.x, GetTerrainHeight(position), position.z);
  }
  public void movePlayerToTop() { movePlayerToTop(player); }
  public void movePlayerToTop(GameObject player) {
    movePlayerToTop(player.GetComponent<InitPlayer>());
  }
  public void movePlayerToTop(InitPlayer player) {
    // Make sure the player stays above the terrain
    if (player != null) {
      player.updatePosition(player.transform.position.x,
                            GetTerrainHeight(player),
                            player.transform.position.z);
    }
  }

  public void ForceUnloadAll() {
    foreach (Terrains t in terrains) { t.currentTTL = 1; }
  }

  public bool anyChunksDividing() {
    for (int i = 0; i < terrains.Count; i++) {
      if (terrains[i].isDividing) return true;
    }
    return false;
  }
  public void ChangeGrassDensity(float density) {
   foreach (Terrains t in terrains) {
     if (t.gameObject)
       t.gameObject.GetComponent<Terrain>().detailObjectDensity = density;
   }
  }
  public void checkDoneLoadingSpawn() {
    TerrainGenerator.wasDoneLoadingSpawn = TerrainGenerator.doneLoadingSpawn;
    if (TerrainGenerator.doneLoadingSpawn) return;
    if ((terrains.Count <= 1 && GenMode.PreLoadChunks) || anyChunksDividing()) {
      TerrainGenerator.doneLoadingSpawn = false;
      return;
    }
    for (int i = 1; i < terrains.Count; i++) {
      if (!terrains[i].terrReady || !terrains[i].hasDivided ||
          terrains[i].terrQueue || terrains[i].waterQueue ||
          terrains[i].numSGQueued() > 0) {
        TerrainGenerator.doneLoadingSpawn = false;
        return;
      }
    }
    Debug.Log("Done loading spawn!");
    TerrainGenerator.doneLoadingSpawn = true;
    TerrainGenerator.loadingSpawn = false;
  }

  public int getSGID(string name) {
    for (int i = 0; i < subGenerators.Length; i++) {
      if (subGenerators[i].Name == name) return i;
    }
    return -1;
  }

  public void DestroyEverything() {
    for(int i=0; i<terrains.Count; i++) {
      UnloadTerrainChunk(i);
    }
    lastTerrUpdated = new Terrains();
    lastTerrUpdateLoc = -1;
    terrains.Clear();
    Destroy(gameObject);
  }
}