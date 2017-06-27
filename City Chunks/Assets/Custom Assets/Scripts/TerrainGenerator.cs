// v0.0.5
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
// #define DEBUG_HEIGHTS
// #define DEBUG_MISC
// #define DEBUG_POSITION
// #define DEBUG_STEEPNESS
// #define DEBUG_UPDATES
// #define DEBUG_WATER
// #define DEBUG_HUD_POS
// #define DEBUG_HUD_TIMES
// #define DEBUG_HUD_LOADED
#pragma warning disable 0168
#pragma warning disable 0219

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable] public class GeneratorModes {
  [Header("Displace Divide")]
  [Tooltip("Enables generator mode that displaces points randomly within ranges that vary by the distance to the center of the chunk and averages surrounding points.")]
  public bool DisplaceDivide = false;
  [Tooltip("A modifier to Displace Divide that causes terrain to be smoother.")]
  public bool Reach = false;
  [Tooltip("A modifier to Displace Divide that causes the terrain to look cube-ish.")]
  public bool Cube = false;

  [Header("Perlin Noise")]
  [Tooltip("Uses Perlin noise to generate terrain.")]
  public bool Perlin = true;
  [Tooltip("A modifier to Perlin Noise that exaggerates heights increasingly the higher they get.")]
  public bool Distort = false;

  [Header("Settings")]
  [Tooltip("Used in a Lerp function between DisplaceDivide and Perlin.")]
  [Range(0.0f, 1.0f)]public float mixtureAmount = 0.5f;
  [Tooltip("Number of pixels to update per chunk per frame.")]
  [Range(0, 513*513)] public int HeightmapSpeed = 1000;
  [Tooltip("Splits calculating the heightmap among 4^slowAmount frames instead of doing it all in 1.")]
  public bool slowHeightmap = true;
  [Tooltip("Maximum depth the recursion should delay continuing.")]
  public int slowAmount = 1;
  [Tooltip("Number of seconds to wait between each delay.")]
  public float slowDelay = 0.1f;
  [Tooltip("Allows most of the hard work of generating chunks to happen in Start() rather than multiple frames. Only applies for spawn chunks.")]
  public bool PreLoadChunks = false;
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
  [Tooltip("Previous amount of time updating chunk splats took in milliseconds.")]
  public float DeltaSplatUpdate = 0;
  [Tooltip("Previous amount of time updating chunk splats, trees, and details took in milliseconds.")]
  public float DeltaDetailUpdate = 0;
  [Tooltip("Previous amount of time updating chunk textures took in milliseconds.")]
  public float DeltaTextureUpdate = 0;
  [Tooltip("Previous amount of time updating tree prototypes and instances took in milliseconds.")]
  public float DeltaTreeUpdate = 0;
  [Tooltip("Previous amount of time updating the delayed heightmap modifications took in milliseconds.")]
  public float DeltaLODUpdate = 0;
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
  [Tooltip("List of terrain data for setting heights. Equivalent to gameObject.GetComponent<Terrain>().terrainData.")]
  public TerrainData terrData;
  [Tooltip("Terrain GameObject.")]
  public GameObject gameObject;
  [Tooltip("List of terrain heightmap data points for setting heights over a period of time from the DeltaDivide Generator.")]
  public float[, ] terrPoints;
  [Tooltip("List of terrain heightmap data points for setting heights over a period of time from the Perlin generator.")]
  public float[, ] terrPerlinPoints;
  [Tooltip("List of trees currently on this chunk.")]
  public List<GameObject> TreeInstances = new List<GameObject>();
  [Tooltip("The water GameObject attatched to this chunk.")]
  public GameObject waterTile;
  [Tooltip("Whether this terrain chunk is ready for its splatmap to be updated.")]
  public bool splatQueue = false;
  [Tooltip("Whether this terrain chunk is ready for its textures to be updated.")]
  public bool texQueue = false;
  [Tooltip("Whether this terrain chunk is ready for its trees to be updated.")]
  public bool treeQueue = false;
  [Tooltip("Whether this terrain chunk is ready for its water tile to be updated.")]
  public bool waterQueue = false;
  [Tooltip("Whether this terrain chunk is ready for its grass and other details to be updated.")]
  public bool detailQueue = false;
  [Tooltip("Whether this terrain chunk is ready to be updated with points in terrPoints. True if points need to be flushed to terrainData.")]
  public bool terrQueue = false;
  [Tooltip("Whether this terrain chunk is ready for its delayed hightmap modifications to be updated.")]
  public bool LODQueue = false;
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
}
[Serializable] public class Textures {
  public int Length = 6;
  [Tooltip("Common/Backup Texture.")]
  public Texture2D Grass;
  [Tooltip("For beaches.")]
  public Texture2D Sand;
  [Tooltip("For steep slopes")]
  public Texture2D Rock;
  [Tooltip("For high altitudes")]
  public Texture2D Snow;
  [Tooltip("For Debugging")]
  public Texture2D White;
  [Tooltip("For Debugging")]
  public Texture2D Black;
}
[Serializable] public class TextureNormals {
  public int Length = 6;
  [Tooltip("Common/Backup Texture.")]
  public Texture2D Grass;
  [Tooltip("For beaches.")]
  public Texture2D Sand;
  [Tooltip("For steep slopes")]
  public Texture2D Rock;
  [Tooltip("For high altitudes")]
  public Texture2D Snow;
  [Tooltip("For Debugging")]
  public Texture2D White;
  [Tooltip("For Debugging")]
  public Texture2D Black;
}
public class TerrainGenerator : MonoBehaviour {
  public static float EmptyPoint = -100f;
  public static string Version = "v0.0.5";
  public static string worldID = "ERROR";

  [Header("Terrains (Auto-populated)")]
  [Tooltip("The list of all currently loaded chunks.")]
  [SerializeField] public List<Terrains> terrains = new List<Terrains>();

  [Header("Game Objects")]
  [Tooltip("Water Tile to instantiate with the terrain when generating a new chunk.")]
  [SerializeField] public GameObject waterTile;
  [Tooltip("Maximum number of trees per chunk to be generated.")]
  [SerializeField] public int maxNumTrees = 500;
  // Player for deciding when to load chunks based on position.
  GameObject player;
  private static List<InitPlayer> players = new List<InitPlayer>();
  int numIdentifiedPlayers = 0;
  [Tooltip("Whether or not to use the pre-determined seed or use Unity's random seed.")]
  [SerializeField] public bool useSeed = true;
  [Tooltip("The predetermined seed to use if Use Seed is false.")]
  [SerializeField] public int Seed = 4;
  [Tooltip("Modifier to shift the perlin noise map in order to reduce chance of finding the same patch of terrain again. The perlin noise map loops at every integer. This value is multiplied by the seed.")]
  [SerializeField] public float PerlinSeedModifier = 0.1213546f;
  [Header("HUD Text for Debug")]
  [Tooltip("The GUIText object that is used to display the position of the player.")]
  [SerializeField] public GUIText positionInfo;
  [Tooltip("The GUIText object that is used to display the list of loaded chunks.")]
  [SerializeField] public GUIText chunkListInfo;
  [Tooltip("Tracking of how long certain events take.")]
  [SerializeField] public Times times;
  [Tooltip("The previous values of times.")]
  [SerializeField] public Times previousTimes;
  [Header("Generator Settings")]
  [Tooltip("List of available terrain generators.")]
  [SerializeField] public GeneratorModes GenMode;
  [Tooltip("Distance the player must be from a chunk for it to be loaded.")]
  [SerializeField] public int loadDist = 50;
  [Tooltip("Roughness of terrain is modified by this value.")]
  [SerializeField] public float roughness = 1.0f;
  [Tooltip("Roughness of terrain is modified by this value.")]
  [SerializeField] public float PerlinRoughness = 0.2f;
  [Tooltip("Maximum height of Perlin Generator in percentage.")]
  [SerializeField] public float PerlinHeight = 0.8f;
  [Tooltip("How quickly biomes/terrain roughness changes.")]
  [SerializeField] public float BiomeRoughness = 0.1f;
  [Tooltip("Vertical shift of values pre-rectification.")]
  [SerializeField] public float yShift = 0.0f;
  [Header("Visuals")]
  [Tooltip("Array of textures to apply to the terrain.")]
  [SerializeField] public Textures TerrainTextures;
  [Tooltip("The normals that corespond to the textures.")]
  [SerializeField] public TextureNormals TerrainTextureNormals;
  [Tooltip("Tree prefabs to place on the terrain.")]
  [SerializeField] public GameObject[] TerrainTrees;
  [Tooltip("Detail Prototypes for grass.")]
  [SerializeField] public DetailPrototype[] TerrainGrasses;

  public static bool doneLoadingSpawn = false;
  public static bool wasDoneLoadingSpawn = false;
  public static float waterHeight = 0f;
  public static float snowHeight = 960f;

  int terrWidth;  // Used to space the terrains when instantiating.
  int terrLength; // Size of the terrain chunk in normal units.
  int terrHeight; // Maximum height of the terrain chunk in normal units.
  int heightmapWidth;  // The size of an individual heightmap of each chunk.
  int heightmapHeight;
  int alphamapHeight;
  int alphamapWidth;
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
  float PeakModifier = 1;
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
  // Number of frames to wait before loading chunks.
  int loadWaitCount = 50;
  // Number of frames to wait before unloading chunks.
  int unloadWaitCount = 100;
  // Used for pausing the editor if a new maximum is set so the user can view what the culprut was.
  float lastMaxUpdateTime = 0;
  bool preLoadingDone = false;
  bool preLoadingChunks = false;
#if DEBUG_HUD_LOADED
  // List of chunks loaded as a list of coordinates.
  String LoadedChunkList = "";
#endif

  // void Awake() { Debug.Log("Terrain Generator Awake"); }

  void Start() {
    Debug.Log("Terrain Generator Start!");
    GameData.AddLoadingScreen();

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
        (Seed * PerlinSeedModifier).ToString() + TerrainGenerator.Version + "-";
    if (GenMode.Perlin) {
      Debug.Log("Seed(" + Seed + ")*PerlinSeedModifier(" + PerlinSeedModifier +
                ")=" + Seed * PerlinSeedModifier);
    }
    if (GenMode.Perlin && GenMode.DisplaceDivide) {
      Debug.Log("Doubling Roughness");
      roughness *= 2f;
    }


    // Generate height map. Disable slowing the generation because we want
    // everything do be done in this frame, but then return the feature to its
    // initial state later as the user may want it.
    bool slowHeightmap = GenMode.slowHeightmap;
    GenMode.slowHeightmap = false;
    int loadWaitCountTemp = loadWaitCount;
    loadWaitCount = 0;

    UpdateSplat(GetComponent<Terrain>().terrainData);

    Debug.Log("Generating spawn chunk");
    // Initialize variables based off of values defining the terrain and add
    // the spawn chunk to arrays for later reference.
    GenerateTerrainChunk(0, 0);

    Debug.Log("Creating spawn chunk fractal");
    FractalNewTerrains(0, 0);
    Debug.Log("Applying spawn chunk height map");
    terrains[0].terrData.SetHeights(0, 0, MixHeights(0));
    Debug.Log("Adding Trees to spawn chunk");
    UpdateTreePrototypes(terrains[0].terrData);
    UpdateTrees(terrains[0].terrData);
    Debug.Log("Adding Grass to spawn chunk");
    UpdateDetailPrototypes(terrains[0].terrData);
    UpdateGrass(terrains[0].terrData);
    Debug.Log("Texturing spawn chunk");
    UpdateTexture(terrains[0]);
    terrains[0].terrQueue = false;
    terrains[0].splatQueue = false;
    terrains[0].texQueue = false;
    terrains[0].treeQueue = false;
    terrains[0].detailQueue = false;
    terrains[0].terrReady = true;
    Debug.Log("Attempting to save spawn chunk to disk");
    // SaveChunk(0, 0);
    loadWaitCount = loadWaitCountTemp;
    GenMode.slowHeightmap = slowHeightmap;
    radius = loadDist / ((terrWidth + terrLength) / 2.0f);

#if DEBUG_HUD_LOADED
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
    // Get the player spawn height from the heightmap height at the coordinates
    // where the player will spawn.
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
    if (playerY < TerrainGenerator.waterHeight)
      playerY = TerrainGenerator.waterHeight;

    if (players.Count == 0) {
      if (GetComponent<UnityEngine.Networking.NetworkIdentity>() == null) {
        Debug.LogError(
            "Could not find player with InitPlayer script attatched to it. " +
            "Make sure there is exactly one GameObject with this script per " +
            "scene");
      } else {
        Debug.Log(
            "No player found, but multiplayer support detected. Possibly " +
            "spawning later");
      }
    } else if (players.Count > 1) {
      Debug.Log("Multiplayer detected!");
      numIdentifiedPlayers = players.Count;
      for(int i=0; i<players.Count; i++) {
        player = players[i].gameObject;
        Debug.Log("Valid player found: " + player.transform.name);
        // Tell the player where to spawn.
        (player.GetComponent<InitPlayer>()).go(playerX, playerY, playerZ);
      }
    } else {
      player = players[0].gameObject;
      Debug.Log("Valid player found: " + player.transform.name);
      // Tell the player where to spawn.
      (player.GetComponent<InitPlayer>()).go(playerX, playerY, playerZ);
    }
    TerrUpdatePoints = new float[ heightmapWidth, heightmapHeight ];
    TerrTemplatePoints = new float[ heightmapWidth, heightmapHeight ];
    Debug.Log("Initialization done!");
  }

  void Update() {
    if(GenMode.PreLoadChunks && !preLoadingDone && !preLoadingChunks) {
      preLoadingChunks = true;
      StartCoroutine(PreLoadChunks());
    }
    if (GameData.loading) CalculateLoadPercent();
    if (!preLoadingDone) return;
    if (GameData.loading && doneLoadingSpawn && !wasDoneLoadingSpawn)
      GameData.RemoveLoadingScreen();
    // Generates terrain based on player transform and generated terrain. Loads
    // chunks in a circle centered on the player and unloads all other chunks
    // that are not within this circle. The spawn chunk is exempt because it may
    // not be unloaded.
    float iRealTime = Time.realtimeSinceStartup;
    float iTime = -1;
    bool done = loadWaitCount > 0;

    if (Input.GetKeyDown("r")) {
      for (int i = 0; i < times.DeltaTotalAverageArray.Length; i++) {
        times.DeltaTotalAverageArray[i] = -1;
      }
      times.DeltaTotalAverage = 0f;
      times.DeltaTotalMax = 0f;
      times.avgEnd = -1;
      lastMaxUpdateTime = 0f;
    }

    Preparations();

    HandleMultiplayer();

    UpdateAllWater(ref done, ref iTime);

    ApplyHeightmap(ref done, ref iTime);

    UpdateAllLOD(ref done, ref iTime);

    UpdateAllTrees(ref done, ref iTime);

    UpdateAllSplats(ref done, ref iTime);

    UpdateAllTextures(ref done, ref iTime);

    UpdateAllDetails(ref done, ref iTime);

    UpdateAllLoadedChunks(ref done, ref iTime);

    UpdateAllNeighbors();

    checkDoneLoadingSpawn();

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
    float slowDelay = GenMode.slowDelay;
    GenMode.slowDelay = 0f;
    GenMode.slowAmount--;
    do {
      CustomDebug.Assert(false, "Start Frame");
      keepGoing = false;
      done = false;
      loadWaitCount = 0;
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
      CustomDebug.Assert(false, "End of frame");
      yield return null;
    } while (keepGoing);
    Debug.Log("Done Pre-Loading Chunks");
    GenMode.slowDelay = slowDelay;
    GenMode.slowAmount++;
    preLoadingDone = true;
    preLoadingChunks = false;
  }

  void CalculateLoadPercent() {
      float total = 0f;
      int num = 137;
      int count = 8;
      for(int i=0; i<terrains.Count; i++) {
        if (terrains[i].isDividing) total += 0.5f / count;
        else if (terrains[i].waterQueue) total += 1.5f / count;
        else if (terrains[i].terrQueue) total += 2.5f / count;
        else if (terrains[i].LODQueue) total += 3.5f / count;
        else if (terrains[i].treeQueue) total += 4.5f / count;
        else if (terrains[i].splatQueue) total += 5.5f / count;
        else if (terrains[i].texQueue) total += 6.5f / count;
        else if (terrains[i].detailQueue) total += 7.5f / count;
        else if (terrains[i].terrReady) total += 1.0f;
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
#if DEBUG_HUD_LOADED
    LoadedChunkList = "";
    if (TerrainGenerator.doneLoadingSpawn) {
      LoadedChunkList += "Spawn is done\n";
    } else {
      LoadedChunkList += "\n";
    }
#endif
    GameData.previousLoadingMessage = GameData.loadingMessage;
    GameData.loadingMessage =
        "Waiting for someone special to see what I've made for them.";

    // Remove all undefined chunks from the array because they have been
    // unloaded.
    for (int i = 0; i < terrains.Count; i++) {
      if (!terrains[i].gameObject) {
        terrains.RemoveAt(i);
        i--;
      }
      if (!terrains[i].loadingFromDisk && terrains[i].justLoadedFromDisk &&
          terrains[i].loadedFromDisk) {
        terrains[i].texQueue = false;
        terrains[i].detailQueue = false;
        terrains[i].treeQueue = false;
        terrains[i].isDividing = false;
        terrains[i].hasDivided = true;
        terrains[i].terrReady = true;
        terrains[i].terrQueue = true;
        terrains[i].waterQueue = true;
        terrains[i].justLoadedFromDisk = false;
      }
    }

    if (loadWaitCount > 0) loadWaitCount--;

    // If any chunks are dividing, don't do anything that would take time.
    if (anyChunksDividing()) {
      if (loadWaitCount <= 0) loadWaitCount = 5;
    }

    // Flag all chunks to be unloaded. If they should not be unloaded, they will
    // be unflagged and stay loaded before any chunks are actually unloaded.
    if (unloadWaitCount <= 0) {
      for (int i = 0; i < terrains.Count; i++) {
        terrains[i].terrToUnload = true;
      }
    } else {
      if (!anyChunksDividing()) unloadWaitCount--;
    }
  }

  void HandleMultiplayer() {
    float playerX = playerSpawnX;
    float playerZ = playerSpawnZ;
    // Get the player spawn height from the heightmap height at the
    // coordinates where the player will spawn.
    //players = GameObject.FindObjectsOfType<InitPlayer>();
    if (player == null) {
      if (players.Count == 1) {
        player = players[0].gameObject;
        Debug.Log("Valid player found: " + player.transform.name);
        // Tell the player where to spawn.
        float playerY = GetTerrainHeight(playerX, playerZ);
        if (playerY < TerrainGenerator.waterHeight) {
          playerY = TerrainGenerator.waterHeight;
        }
        (player.GetComponent<InitPlayer>()).go(playerX, playerY, playerZ);
      } else if (players.Count == 0 && TerrainGenerator.doneLoadingSpawn) {
        Debug.Log("Resetting TerrainGenerator");
        TerrainGenerator.doneLoadingSpawn = false;
        loadWaitCount = 50;
        unloadWaitCount = 100;
        Start();
        return;
      } else {
        return;
      }
    }
    if (players.Count > 1 && numIdentifiedPlayers < players.Count) {
      Debug.Log("New player connected!");
      numIdentifiedPlayers = players.Count;
      for (int i = 0; i < players.Count; i++) {
        player = players[i].gameObject;
        // Tell the player where to spawn.
        float playerY = GetTerrainHeight(playerX, playerZ);
        if (playerY < TerrainGenerator.waterHeight) {
          playerY = TerrainGenerator.waterHeight;
        }
        (player.GetComponent<InitPlayer>()).go(playerX, playerY, playerZ);
      }
    } else if (numIdentifiedPlayers > players.Count) {
      Debug.Log("Player disconnected.");
      numIdentifiedPlayers = players.Count;
    }
  }

  void UpdateAllLoadedChunks(ref bool done, ref float iTime) {
    for (int num = 0; num < players.Count; num++) {
      player = players[num].gameObject;
      // Make sure the player stays above the terrain
      float xCenter = (player.transform.position.x - terrWidth / 2) / terrWidth;
      float yCenter =
          (player.transform.position.z - terrLength / 2) / terrLength;
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
            Input.GetAxis("Mouse Y") + ")(" + Input.GetAxis("Horizontal") +
            ", " + Input.GetAxis("Vertical") + ")(" + Input.GetAxis("Sprint") +
            ")\n" + "Player" + player.transform.position + "\n" + "Coord(" +
            xCenter + ", " + yCenter + ")(" + terrLoc + ")\n" +
            "TerrainHeight: " + TerrainHeight + "\nHighest Point: " + highest +
            "\nLowest Point: " + lowest;
#else
        if (positionInfo != null) positionInfo.text = "";
#endif
        if (player.transform.position.y < TerrainHeight - 10.0f) {
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
  }

  void UpdateLoadedChunks(float xCenter, float yCenter, ref bool done,
                          ref float iTime) {
    bool chunkUpdated = false;
    if (!TerrainGenerator.doneLoadingSpawn) loadWaitCount = 0;
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
    if (chunkUpdated) GameData.loadingMessage = "Slicing and dicing...";
#if DEBUG_HUD_LOADED
    if (chunkUpdated) LoadedChunkList += "Generating and Fractaling\n";
    else LoadedChunkList += "\n";
#endif
  }

  void ApplyHeightmap(ref bool done, ref float iTime) {
    // Delay applying a heightmap if a chunk was loaded or textures were updated
    // on a chunk to help with performance.
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
            // flag
            // it for texturing.
            if (x >= heightmapWidth) {
              terrains[tileCnt].terrQueue = false;
              terrains[tileCnt].splatQueue = true;
              terrains[tileCnt].texQueue = true;
              terrains[tileCnt].treeQueue = true;
              terrains[tileCnt].detailQueue = true;
              terrains[tileCnt].LODQueue = true;
              break;
            }

            TerrUpdatePoints[ z, x ] = TerrTemplatePoints[ z, x ];

            lastTerrUpdateLoc++;
          }
        } else {
          terrains[tileCnt].terrQueue = false;
          terrains[tileCnt].splatQueue = true;
          terrains[tileCnt].texQueue = true;
          terrains[tileCnt].treeQueue = true;
          terrains[tileCnt].detailQueue = true;
          terrains[tileCnt].LODQueue = true;
          TerrUpdatePoints = TerrTemplatePoints;
        }

        // TODO: Remove try-catch.
        try {
          // Set the terrain heightmap to the defined points.
          terrains[tileCnt].terrData.SetHeightsDelayLOD(0, 0, TerrUpdatePoints);
        } catch (ArgumentException e) {
          Debug.LogWarning(
              "TerrUpdatePoints is incorrect size " + heightmapHeight + "x" +
              heightmapWidth + " instead of " +
              terrains[tileCnt].terrData.heightmapWidth + "x" +
              terrains[tileCnt].terrData.heightmapHeight + "\n" + e);
        }

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
    }
#if DEBUG_HUD_LOADED
    if (heightmapApplied) LoadedChunkList += "Applying Heightmap\n";
    else LoadedChunkList += "\n";
#endif
    if (heightmapApplied) {
      GameData.loadingMessage = "Creating something from nothing...";
      done = heightmapApplied;
      times.DeltaHeightmapApplied = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateAllLOD(ref bool done, ref float iTime) {
    bool LODUpdated = false;
    float iTime2 = -1;
    if (!done) {
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].LODQueue && !terrains[i].loadingFromDisk) {
          if (iTime == -1) iTime = Time.realtimeSinceStartup;
          if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
          terrains[i]
              .gameObject.GetComponent<Terrain>()
              .ApplyDelayedHeightmapModification();
          terrains[i].LODQueue = false;
          LODUpdated = true;
          break;
        }
      }
    }
#if DEBUG_HUD_LOADED
    if (LODUpdated) LoadedChunkList += "Updating LOD\n";
    else LoadedChunkList += "\n";
#endif
    if (LODUpdated) {
      GameData.loadingMessage = "Turning that FPS back up...";
      // GameData.loadingMessage = GameData.previousLoadingMessage;
      done = LODUpdated;
      times.DeltaLODUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
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
          GenerateWaterChunk(GetXCoord(i), GetZCoord(i));
          terrains[i].waterQueue = false;
          waterUpdated = true;
          break;
        }
      }
    }
#if DEBUG_HUD_LOADED
    if (waterUpdated) LoadedChunkList += "Updating Water\n";
    else LoadedChunkList += "\n";
#endif
    if (waterUpdated) {
      GameData.loadingMessage = "Slowly drowning...";
      done = waterUpdated;
      times.DeltaWaterUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateAllSplats(ref bool done, ref float iTime) {
    bool splatsUpdated = false;
    float iTime2 = -1;
    if (!done) {
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].splatQueue && !terrains[i].loadingFromDisk) {
          if (iTime == -1) iTime = Time.realtimeSinceStartup;
          if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
          UpdateSplat(terrains[i].terrData);
          terrains[i].splatQueue = false;
          splatsUpdated = true;
          break;
        }
      }
    }
#if DEBUG_HUD_LOADED
    if (splatsUpdated) LoadedChunkList += "Updating Splats\n";
    else LoadedChunkList += "\n";
#endif
    if (splatsUpdated) {
      GameData.loadingMessage = "Discovering color...";
      done = splatsUpdated;
      times.DeltaSplatUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateAllTrees(ref bool done, ref float iTime) {
    bool treesUpdated = false;
    float iTime2 = -1;
    if(!done) {
      for(int i=0; i< terrains.Count; i++) {
        if(terrains[i].treeQueue && !terrains[i].loadingFromDisk) {
          if(iTime==-1) iTime = Time.realtimeSinceStartup;
          if(iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
          UpdateTreePrototypes(terrains[i].terrData);
          UpdateTrees(terrains[i].terrData);
          terrains[i].treeQueue = false;
          treesUpdated = true;
          break;
        }
      }
    }
#if DEBUG_HUD_LOADED
    if (treesUpdated) LoadedChunkList += "Updating Trees\n";
    else LoadedChunkList += "\n";
#endif
    if (treesUpdated) {
      done = treesUpdated;
      GameData.loadingMessage = "Let there be life!";
      times.DeltaTreeUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateAllDetails(ref bool done, ref float iTime) {
    bool detailsUpdated = false;
    float iTime2 = -1;
    if(!done) {
      for(int i=0; i< terrains.Count; i++) {
        if(terrains[i].detailQueue && !terrains[i].loadingFromDisk) {
          UpdateDetailPrototypes(terrains[i].terrData);
          UpdateGrass(terrains[i].terrData);
          terrains[i].detailQueue = false;
          detailsUpdated = true;
          break;
        }
      }
    }
#if DEBUG_HUD_LOADED
    if (detailsUpdated) LoadedChunkList += "Updating Details\n";
    else LoadedChunkList += "\n";
#endif
    if (detailsUpdated) {
      done = detailsUpdated;
      times.DeltaDetailUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
      GameData.loadingMessage = "Watching grass grow...";
    }
  }

  void UpdateAllTextures(ref bool done, ref float iTime) {
    // Delay applying textures until later if a chunk was loaded this frame to
    // help with performance.
    bool textureUpdated = false;
    float iTime2 = -1;
    if (!done) {
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].texQueue && !terrains[i].loadingFromDisk) {
          if (iTime == -1) iTime = Time.realtimeSinceStartup;
          if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
          UpdateTexture(terrains[i]);
          textureUpdated = true;
          break;
        }
      }
    }
#if DEBUG_HUD_LOADED
    if (textureUpdated) LoadedChunkList += "Texturing\n";
    else LoadedChunkList += "\n";
#endif
    if (textureUpdated) {
      done = textureUpdated;
      times.DeltaTextureUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
      GameData.loadingMessage = "Painting a masterpiece...";
    }
  }

  void UpdateAllNeighbors() {
    // Update terrain neighbors every times.UpdateSpeed seconds.
    // TODO: Is this even necessary? I don't really know what updating neighbors
    // does.
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
      times.DeltaUpdate = (Time.realtimeSinceStartup - iTime2) * 1000;
    }
  }

  void UpdateTerrainNeighbors(int X, int Z, int count = 2) {
    if (count > 0) {
      Terrain LeftTerr = null, TopTerr = null, RightTerr = null,
              BottomTerr = null;
      try {
        LeftTerr = terrains[GetTerrainWithCoord(X - 1, Z)]
                       .gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X - 1, Z, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        TopTerr = terrains[GetTerrainWithCoord(X, Z + 1)]
                      .gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z + 1, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        RightTerr = terrains[GetTerrainWithCoord(X + 1, Z)]
                        .gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X + 1, Z, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        BottomTerr = terrains[GetTerrainWithCoord(X, Z - 1)]
                         .gameObject.GetComponent<Terrain>();
        UpdateTerrainNeighbors(X, Z - 1, count - 1);
      } catch (ArgumentOutOfRangeException e) {
      }
      try {
        Terrain MidTerr = terrains[GetTerrainWithCoord(X, Z)]
                              .gameObject.GetComponent<Terrain>();
        MidTerr.SetNeighbors(LeftTerr, TopTerr, RightTerr, BottomTerr);
      } catch (ArgumentOutOfRangeException e) {
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
    if (!anyChunksDividing() && !done) {
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].terrToUnload && !terrains[i].loadingFromDisk) {
#if DEBUG_HUD_LOADED
          LoadedChunkList += "(" + GetXCoord(i) + ", " + GetZCoord(i) + ", " +
                             Mathf.RoundToInt(terrains[i].currentTTL) + "), ";
          if (i % 7 == 0) LoadedChunkList += "\n";
#endif
          if (terrains[i].currentTTL <= 0) {
            SaveChunk(GetXCoord(i), GetZCoord(i));
            if (i != 0) {
              if (iTime == -1) iTime = Time.realtimeSinceStartup;
              if (iTime2 == -1) iTime2 = Time.realtimeSinceStartup;
              UnloadTerrainChunk(i);
              chunksUnloaded = true;
            } else {
              terrains[i].currentTTL = 60000;
            }
          } else {
            terrains[i].currentTTL--;
          }
        }
      }
    } else {
      for (int i = 0; i < terrains.Count; i++) {
        if (terrains[i].terrToUnload && !terrains[i].loadingFromDisk) {
#if DEBUG_HUD_LOADED
          LoadedChunkList += "(" + GetXCoord(i) + ", " + GetZCoord(i) + ", " +
                             Mathf.RoundToInt(terrains[i].currentTTL) + "), ";
          if (i % 7 == 0) LoadedChunkList += "\n";
#endif
          terrains[i].currentTTL--;
        }
      }
    }

#if DEBUG_HUD_LOADED
    chunkListInfo.text = LoadedChunkList;
#else
    if (chunkListInfo != null) chunkListInfo.text = "";
#endif
    if(chunksUnloaded) {
      done = chunksUnloaded;
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
          "Trees(" + times.DeltaTreeUpdate + "ms) -- " +
          "Tex("+ times.DeltaTextureUpdate + "ms) -- " +
          "Grass(" + times.DeltaGrassUpdate + "ms),\n" +
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
          "Trees(" + previousTimes.DeltaTreeUpdate + "ms) -- " +
          "Grass(" + previousTimes.DeltaGrassUpdate + "ms),\n" +
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
    previousTimes.DeltaTreeUpdate        = times.DeltaTreeUpdate;
    previousTimes.DeltaGrassUpdate       = times.DeltaGrassUpdate;
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
    if (!done) pointIndex = GetTerrainWithCoord(x, z);
    if (pointIndex == -1 && !done) {
      GenerateTerrainChunk(x, z);
      FractalNewTerrains(x, z);
      done = true;
      loadWaitCount = 10;
    } else if (pointIndex >= 0 && pointIndex < terrains.Count &&
               (terrains[pointIndex].isDividing ||
                (terrains[pointIndex].hasDivided &&
                 !terrains[pointIndex].terrReady) ||
                (!terrains[pointIndex].isDividing &&
                 !terrains[pointIndex].hasDivided &&
                 !terrains[pointIndex].loadingFromDisk)) &&
               !done && !terrains[pointIndex].loadingFromDisk) {
      FractalNewTerrains(x, z);
      loadWaitCount = 10;
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
      alphamapHeight = this.GetComponent<Terrain>().terrainData.alphamapHeight;
      alphamapWidth = this.GetComponent<Terrain>().terrainData.alphamapWidth;

      Debug.Log("terrWidth: " + terrWidth + ", terrLength: " + terrLength +
                ", terrHeight: " + terrHeight + ", waterHeight: " +
                TerrainGenerator.waterHeight + "\nheightmapWidth: " +
                heightmapWidth + ", heightmapHeight: " + heightmapHeight);

      // Adjust heightmap by it's resolution so it appears the same no matter
      // how high resolution it is.
      roughness *= 65f / heightmapWidth;

      terrains.Add(new Terrains());
      terrains[0].x = 0;
      terrains[0].z = 0;
      terrains[0].terrData = GetComponent<Terrain>().terrainData;
      terrains[0].gameObject = this.gameObject;
      terrains[0].terrPoints = new float[ terrWidth, terrLength ];
      terrains[0].terrPerlinPoints = new float[ terrWidth, terrLength ];
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
          Instantiate(terrains[0].gameObject);
      terrains[terrains.Count - 1]
          .gameObject.GetComponent<Terrain>()
          .terrainData = terrains[terrains.Count - 1].terrData;

      terrains[terrains.Count - 1]
          .gameObject.GetComponent<TerrainCollider>()
          .terrainData = terrains[terrains.Count - 1].terrData;

      // Game Object and Components
      foreach (
          Transform child in terrains[terrains.Count - 1].gameObject.transform) {
        Destroy(child.gameObject);
      }
      foreach (Component comp in terrains[terrains.Count - 1]
                   .gameObject.GetComponents<Component>()) {
        if (!(comp is Transform) && !(comp is Terrain) &&
            !(comp is TerrainCollider) &&
            !(comp is UnityEngine.Networking.NetworkIdentity)) {
          Destroy(comp);
        }
      }
      foreach (Component comp in terrains[terrains.Count - 1]
                   .gameObject.GetComponents<Component>()) {
        if (!(comp is Transform) && !(comp is Terrain) &&
            !(comp is TerrainCollider)) {
          Destroy(comp);
        }
      }

      terrains[terrains.Count - 1].gameObject.name =
          "Terrain(" + cntX + "," + cntZ + ")";
      terrains[terrains.Count - 1].gameObject.transform.Translate(
          cntX * terrWidth, 0f, cntZ * terrLength);
      terrains[terrains.Count - 1].gameObject.layer =
          terrains[0].gameObject.layer;

      times.DeltaGenerateTerrain = (Time.realtimeSinceStartup - iTime2) * 1000;

      terrains[terrains.Count - 1].terrPoints =
          new float[ terrWidth, terrLength ];
      terrains[terrains.Count - 1].terrPerlinPoints =
          new float[ terrWidth, terrLength ];
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
    Destroy(terrains[loc].waterTile);
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
    if (GenMode.DisplaceDivide || GenMode.Reach || GenMode.Cube ||
        GenMode.Distort) {
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
      if (changeX == 0 && changeZ == 0 && !terrains[0].loadedFromDisk) {
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
        if (!MatchEdges(iWidth, iHeight, changeX, changeZ, ref points))
          return;
      }
    }

    float iTime2 = Time.realtimeSinceStartup;

    // If the Displace Divide generator is being used, create a heightmap with
    // this generator.
    if (GenMode.DisplaceDivide) {
      // Divide chunk into 4 sections and displace the center thus creating 4
      // more sections per section until every pixel is defined.
      if (useSeed) {
        UnityEngine.Random.InitState(
            (int)(Seed + PerfectlyHashThem((short)(changeX * 3 - 3),
                                           (short)(changeZ * 3 - 3))));
      }
      float[, ] modifier = new float[ 2, 2 ];
      PerlinDivide(ref modifier, changeX, changeZ, 2, 2,
                   PerlinSeedModifier * 2f, BiomeRoughness);
      PeakModifier = modifier[ 0, 0 ];
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
        divideAmount = 0;
        // If we are using GenMode.slowHeightmap, then the terrain has not
        // been finished being divided. Otherwise, the whole process could
        // happen before the function returns.
        CustomDebug.Assert(false, "Pre-Divide");
        StartCoroutine(
            DivideNewGrid(changeX, changeZ, 0, 0, iWidth, iHeight,
                          points[ 0, 0 ], points[ 0, (int)iHeight - 1 ],
                          points[ (int)iWidth - 1, (int)iHeight - 1 ],
                          points[ (int)iWidth - 1, 0 ]));
        if (terrains[terrIndex].isDividing || !terrains[terrIndex].hasDivided) {
          return;
        }
        CustomDebug.Assert(false, "Post-Divide");
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
      // Smooth the edge of chunks where they meet in order to hide seams
      // better.
      // SmoothEdges(iWidth, iHeight, ref flippedPoints);

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
  /*void DivideNewGrid(ref float[, ] points, float dX, float dY, float dwidth,
                     float dheight, float c1, float c2, float c3, float c4) {
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
                 if (GenMode.slowHeightmap && recursiveDepth <= 1)
                               yield return new WaitForSeconds(0.25f);
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
      Rectify(ref Middle);
      Rectify(ref Edge1);
      Rectify(ref Edge2);
      Rectify(ref Edge3);
      Rectify(ref Edge4);

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
      Rectify(ref c1);
      Rectify(ref c2);
      Rectify(ref c3);
      Rectify(ref c4);
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
      float c = AverageCorners(c1, c2, c3, c4);
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
  }*/
  // Same as above, but splits up work between frames.
  IEnumerator DivideNewGrid(int chunkX, int chunkY, float dX, float dY,
                            float dwidth, float dheight, float c1, float c2,
                            float c3, float c4, int index = -1,
                            int recursiveDepth = 0) {
    if (recursiveDepth == 0) CustomDebug.Assert(false, "Begin Divide");
    if (useSeed && recursiveDepth == 0) {
      UnityEngine.Random.InitState(
          (int)(Seed +
                PerfectlyHashThem((short)(chunkX * 3), (short)(chunkY * 3))));
    }
    if (index == -1) index = GetTerrainWithCoord(chunkX, chunkY);
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
      // Reach and Cube are things I tried that had interesting outcomes that I
      // thought were cool and wanted to keep, but are not supported.
      if (GenMode.Reach) {
        // TODO: Remove try-catches
        try {
          c1 = terrains[index].terrPoints[ (int)dX - 1, (int)dY ];
        } catch (IndexOutOfRangeException e) {
          c1 = terrains[index].terrPoints[ (int)dX, (int)dY ];
        }
        try {
          c2 = terrains[index].terrPoints[ (int)dX, (int)dY + (int)dheight ];
        } catch (IndexOutOfRangeException e) {
          c2 = terrains[index]
                   .terrPoints[ (int)dX, (int)dY + (int)dheight - 1 ];
        }
        try {
          c3 =
              terrains[index]
                  .terrPoints[ (int)dX + (int)dwidth, (int)dY + (int)dheight ];
        } catch (IndexOutOfRangeException e) {
          c3 = terrains[index].terrPoints
               [ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        }
        try {
          c4 = terrains[index]
                   .terrPoints[ (int)dX + (int)dwidth - 1, (int)dY - 1 ];
        } catch (IndexOutOfRangeException e) {
          c4 = terrains[index].terrPoints[ (int)dX + (int)dwidth - 1, (int)dY ];
        }
      } else if (GenMode.Cube) {
        c1 = terrains[index].terrPoints[ (int)dX, (int)dY ];
        c2 = terrains[index].terrPoints[ (int)dX, (int)dY + (int)dheight - 1 ];
        c3 = terrains[index].terrPoints
             [ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        c4 = terrains[index].terrPoints[ (int)dX + (int)dwidth - 1, (int)dY ];
      }
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
        string list = "";
        if(chunkListInfo != null) {
          list = "Loaded Chunk List: " + chunkListInfo.text;
        }
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
            chunkX + ", " + chunkY + ")" + "\n" + list);
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
      if (recursiveDepth == 0) CustomDebug.Assert(false, "Begin 1/4");
      if (useSeed && recursiveDepth == 0) {
        UnityEngine.Random.InitState(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 2),
                                           (short)(chunkY * 3 - 2))));
      }
      if (GenMode.slowHeightmap && recursiveDepth < GenMode.slowAmount) {
        yield return StartCoroutine(
            DivideNewGrid(chunkX, chunkY, dX, dY, newWidth, newHeight, c1,
                          Edge1, Middle, Edge4, index, recursiveDepth + 1));
      } else {
        StartCoroutine(DivideNewGrid(chunkX, chunkY, dX, dY, newWidth,
                                     newHeight, c1, Edge1, Middle, Edge4, index,
                                     recursiveDepth + 1));
      }
      if (recursiveDepth == 0) CustomDebug.Assert(false, "End 1/4");
      if (GenMode.slowHeightmap && recursiveDepth <= GenMode.slowAmount) {
        UnityEngine.Random.State seedState = UnityEngine.Random.state;
        yield return new WaitForSeconds(GenMode.slowDelay);
        // yield return null;
        UnityEngine.Random.state = seedState;
        index = GetTerrainWithCoord(chunkX, chunkY);
      }

      if (recursiveDepth == 0) CustomDebug.Assert(false, "Begin 2/4");
      // 2/4
      if (useSeed && recursiveDepth == 0) {
        UnityEngine.Random.InitState(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 1),
                                           (short)(chunkY * 3 - 2))));
      }
      if (GenMode.slowHeightmap && recursiveDepth < GenMode.slowAmount) {
        yield return StartCoroutine(DivideNewGrid(
            chunkX, chunkY, dX + newWidth, dY, newWidth, newHeight, Edge4,
            Middle, Edge3, c4, index, recursiveDepth + 1));
      } else {
        StartCoroutine(DivideNewGrid(chunkX, chunkY, dX + newWidth, dY,
                                     newWidth, newHeight, Edge4, Middle, Edge3,
                                     c4, index, recursiveDepth + 1));
      }
      if (recursiveDepth == 0) CustomDebug.Assert(false, "End 2/4");
      if (GenMode.slowHeightmap && recursiveDepth <= GenMode.slowAmount) {
        UnityEngine.Random.State seedState = UnityEngine.Random.state;
        yield return new WaitForSeconds(GenMode.slowDelay);
        // yield return null;
        UnityEngine.Random.state = seedState;
        index = GetTerrainWithCoord(chunkX, chunkY);
      }

      if (recursiveDepth == 0) CustomDebug.Assert(false, "Begin 3/4");
      // 3/4
      if (useSeed && recursiveDepth == 0) {
        UnityEngine.Random.InitState(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 1),
                                           (short)(chunkY * 3 - 1))));
      }
      if (GenMode.slowHeightmap && recursiveDepth < GenMode.slowAmount) {
        yield return StartCoroutine(DivideNewGrid(
            chunkX, chunkY, dX + newWidth, dY + newHeight, newWidth, newHeight,
            Middle, Edge2, c3, Edge3, index, recursiveDepth + 1));
      } else {
        StartCoroutine(DivideNewGrid(
            chunkX, chunkY, dX + newWidth, dY + newHeight, newWidth, newHeight,
            Middle, Edge2, c3, Edge3, index, recursiveDepth + 1));
      }
      if (recursiveDepth == 0) CustomDebug.Assert(false, "End 3/4");
      if (GenMode.slowHeightmap && recursiveDepth <= GenMode.slowAmount) {
        UnityEngine.Random.State seedState = UnityEngine.Random.state;
        yield return new WaitForSeconds(GenMode.slowDelay);
        // yield return null;
        UnityEngine.Random.state = seedState;
        index = GetTerrainWithCoord(chunkX, chunkY);
      }

      if (recursiveDepth == 0) CustomDebug.Assert(false, "Begin 4/4");
      // 4/4
      if (useSeed && recursiveDepth == 0) {
        UnityEngine.Random.InitState(
            (int)(Seed + PerfectlyHashThem((short)(chunkX * 3 - 2),
                                           (short)(chunkY * 3 - 1))));
      }
      if (GenMode.slowHeightmap && recursiveDepth < GenMode.slowAmount) {
        yield return StartCoroutine(DivideNewGrid(
            chunkX, chunkY, dX, dY + newHeight, newWidth, newHeight, Edge1, c2,
            Edge2, Middle, index, recursiveDepth + 1));
      } else {
        StartCoroutine(DivideNewGrid(chunkX, chunkY, dX, dY + newHeight,
                                     newWidth, newHeight, Edge1, c2, Edge2,
                                     Middle, index, recursiveDepth + 1));
      }
      if (recursiveDepth == 0) CustomDebug.Assert(false, "End 4/4");
      if (GenMode.slowHeightmap && recursiveDepth <= GenMode.slowAmount) {
        UnityEngine.Random.State seedState = UnityEngine.Random.state;
        yield return new WaitForSeconds(GenMode.slowDelay);
        // yield return null;
        UnityEngine.Random.state = seedState;
        index = GetTerrainWithCoord(chunkX, chunkY);
      }

      if (recursiveDepth == 0) CustomDebug.Assert(false, "Done");
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
      if (GenMode.Cube || GenMode.Reach) {
        c1 = terrains[index].terrPoints[ (int)dX, (int)dY ];
        c2 = terrains[index].terrPoints[ (int)dX, (int)dY + (int)dheight - 1 ];
        c3 = terrains[index].terrPoints
             [ (int)dX + (int)dwidth - 1, (int)dY + (int)dheight - 1 ];
        c4 = terrains[index].terrPoints[ (int)dX + (int)dwidth - 1, (int)dY ];
      }
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
  }
  // Uses perlin noise to define a heightmap.
  void PerlinDivide(ref float[, ] points, float x, float y, float w, float h,
                    float PerlinSeedModifier_ = -1,
                    float PerlinRoughness_ = -1) {
    if (PerlinSeedModifier_ == -1) PerlinSeedModifier_ = PerlinSeedModifier;
    if (PerlinRoughness_ == -1) PerlinRoughness_ = PerlinRoughness;
    float xShifted = (x + (Seed * PerlinSeedModifier_)) * (w - 1f);
    float yShifted = (y + (Seed * PerlinSeedModifier_)) * (h - 1f);
    for (int r = 0; r < h; r++) {
      for (int c = 0; c < w; c++) {
        if (GenMode.Distort) {
          float noise =
              Mathf.PerlinNoise(PerlinRoughness_ * (xShifted + c) / (w - 1f),
                                PerlinRoughness_ * (yShifted + r) / (h - 1f));
          float f1 = Mathf.Log(1 - noise) * -PerlinRoughness_ * 0.3f;
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
          float noise = Mathf.PerlinNoise(Mathf.Pow(PerlinRoughness_, 1.2f) *
                                              (xShifted + c) / (w - 1.0f),
                                          Mathf.Pow(PerlinRoughness_, 1.2f) *
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
    float Max = SmallSize / gBigSize * gRoughness;
    return (float)(UnityEngine.Random.value - 0.5) * Max;
  }

  void UpdateSplat(TerrainData terrainData) {
    SplatPrototype[] tex = new SplatPrototype[TerrainTextures.Length];

    terrainData.RefreshPrototypes();

    for (int i = 0; i < tex.Length; i++) {
      tex[i] = new SplatPrototype();
    }

    if (TerrainTextures.Grass != null) {
      tex[0].texture = TerrainTextures.Grass;
      tex[0].normalMap = TerrainTextureNormals.Grass;
    } else {
      Debug.LogError("Grass Texture must be defined!");
      return;
    }

    for (int i = 1; i < tex.Length; i++) {
      tex[i].texture = TerrainTextures.Grass;
      //tex[i].normalMap = TerrainTextureNormals.Grass;
    }

    if (TerrainTextures.Sand != null) {
      tex[1].texture = TerrainTextures.Sand;
      tex[1].normalMap = TerrainTextureNormals.Sand;
    }
    if (TerrainTextures.Rock != null) {
      tex[2].texture = TerrainTextures.Rock;
      tex[2].normalMap = TerrainTextureNormals.Rock;
    }
    if (TerrainTextures.Snow != null) {
      tex[3].texture = TerrainTextures.Snow;
      tex[3].normalMap = TerrainTextureNormals.Snow;
      tex[3].metallic = 0.5f;
      tex[3].smoothness = 0.3f;
    }
    if (TerrainTextures.White != null) {
      tex[4].texture = TerrainTextures.White;
      tex[4].normalMap = TerrainTextureNormals.White;
    }
    if (TerrainTextures.Black != null) {
      tex[5].texture = TerrainTextures.Black;
      tex[5].normalMap = TerrainTextureNormals.Black;
    }

    for (int i = 0; i < tex.Length; i++) {
      tex[i].tileSize = new Vector2(16, 16);  // Sets the size of the texture
      tex[i].tileOffset = new Vector2(0, 0);  // Sets the offset of the texture
      //tex[i].texture.Apply(true);
    }

    terrainData.splatPrototypes = tex;
  }

  // For use before SetHeights is called.
  float GetSteepness(float[, ] Heightmap, int x, int y) {
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

    slopeX *= terrHeight;
    slopeZ *= terrHeight;

    Vector3 normal = new Vector3(-slopeX, 2, slopeZ);
    normal.Normalize();
    return Mathf.Acos(Vector3.Dot(normal, Vector3.up)) * 180f / Mathf.PI;
  }

  // Create and apply a texture to a chunk.
  void UpdateTexture(Terrains terrain) {
    float iTime = Time.realtimeSinceStartup;

    if (terrain.terrData.splatPrototypes.Length != TerrainTextures.Length) return;

    int index = GetTerrainWithData(terrain.terrData);

    float[,] heightmap = MixHeights(index);

    float[, , ] map =
        new float[ alphamapWidth, alphamapHeight, TerrainTextures.Length ];

    for (int y = 0; y < alphamapWidth; y++) {
      for (int x = 0; x < alphamapHeight; x++) {
        int X = x * (heightmapWidth / alphamapWidth);
        int Y = y * (heightmapHeight / alphamapHeight);
        // Get terrain information at each point.
        // float angle = terrainData.GetSteepness(normX, normY);
        float angle = GetSteepness(heightmap, X, Y);
        //float height =
        //    terrainData.GetHeight(Mathf.RoundToInt(normX * heightmapWidth),
        //                          Mathf.RoundToInt(normY * heightmapHeight));
        float height = heightmap[ X, Y ] * terrHeight;

        // if (x == alphamapHeight / 2 && x == y) Debug.Log(height + "@" + angle);

        // Steepness is given as an angle, 0..90 degrees. Divide
        // by 90 to get an alpha blending value in the range 0..1.
        float maxAngle = 75f;
        float unmodifiedFrac = angle / maxAngle;
        float frac = 1f;
        if (angle < maxAngle) {
          // Curve values to blend nicer.
          float A = 12f;
          frac = -(A / 6) * Mathf.Pow(unmodifiedFrac, 3) +
                 (A / 4) * Mathf.Pow(unmodifiedFrac, 2);
        }
        // Calculate relative amounts of each texture based off the terrain
        // height and slope.
        float[] values = new float[TerrainTextures.Length];
        values[0] = ((height <= TerrainGenerator.snowHeight) ? 1.0f - frac : 0f);
        values[1] =
            ((height <= TerrainGenerator.waterHeight + 2f)
                 ? ((height <= TerrainGenerator.waterHeight + 1f) ? 100f : 2f)
                 : 0f);
        values[2] = frac;
        values[3] = ((height >= TerrainGenerator.snowHeight) ? 1f - frac : 0f);
        values[4] = 0f;
        values[5] = 0f;

        // Normalize values
        float total = 0f;
        for (int i = 0; i < values.Length; i++) {
          total += values[i];
        }
        if (total != 0) {
          for (int i = 0; i < values.Length; i++) {
            values[i] /= total;
          }
        }

        map[ x, y, 0 ] = values[0];
        map[ x, y, 1 ] = values[1];
        map[ x, y, 2 ] = values[2];
        map[ x, y, 3 ] = values[3];
        map[ x, y, 4 ] = values[4];
        map[ x, y, 5 ] = values[5];
      }
    }

    terrain.terrData.SetAlphamaps(0, 0, map);
    terrain.texQueue = false;
  }

  void UpdateTreePrototypes(TerrainData terrainData) {
    int terrID = GetTerrainWithData(terrainData);
    if (terrID < 0) return;

    List<TreePrototype> list = new List<TreePrototype>();
    for (int i = 0; i < TerrainTrees.Length; i++) {
      if (TerrainTrees[i].GetComponent<LODGroup>() == null) {
        TreePrototype newTreePrototype = new TreePrototype();
        newTreePrototype.prefab = TerrainTrees[i];
        list.Add(newTreePrototype);
      }
    }

    terrainData.treePrototypes = list.ToArray();
    terrainData.RefreshPrototypes();
  }
  void UpdateTrees(TerrainData terrainData) {
    if (TerrainTrees.Length <= 0) return;
    int terrID = GetTerrainWithData(terrainData);
    if (terrID < 0) return;

    float[, ] numberModifier = new float[ 2, 2 ];
    PerlinDivide(ref numberModifier, GetXCoord(terrID), GetZCoord(terrID), 2, 2,
                 PerlinSeedModifier * 2f, 0.1f);
    int numberOfTrees = (int)(numberModifier[ 0, 0 ] * maxNumTrees);
    terrains[terrID].TreeInstances.Clear();
    List<TreeInstance> newTrees = new List<TreeInstance>();
    for (int i = 0; i < numberOfTrees; i++) {
      float X = UnityEngine.Random.value;
      float Z = UnityEngine.Random.value;
      float Y;

      // If trees have a LOD group, there is no reason to add it to the terrain,
      // so we just instantiate it as a game object.
      int TreeID = UnityEngine.Random.Range(0, TerrainTrees.Length);
      if (TerrainTrees[TreeID].GetComponent<LODGroup>() == null) {
        Y = terrainData.GetInterpolatedHeight(X, Z);
        if (Y <= TerrainGenerator.waterHeight) continue;
        if (terrainData.GetSteepness(X, Z) > 30f) continue;
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
        newTreeInstance.position = new Vector3(X, (Y - 2f) / terrHeight, Z);
        newTrees.Add(newTreeInstance);
      } else {
        Y = terrainData.GetHeight((int)(X * heightmapHeight),
                                  (int)(Z * heightmapWidth));
        if (Y <= TerrainGenerator.waterHeight) continue;
        if (terrainData.GetSteepness(X, Z) > 30f) continue;
        terrains[terrID].TreeInstances.Add(Instantiate(
            TerrainTrees[TreeID],
            new Vector3(X * terrWidth + terrWidth * terrains[terrID].x, Y,
                        Z * terrLength + terrLength * terrains[terrID].z),
            Quaternion.identity, terrains[terrID].gameObject.transform));
      }
      terrainData.treeInstances = newTrees.ToArray();
      Terrain terrain = terrains[terrID].gameObject.GetComponent<Terrain>();
      terrain.Flush();
      // Refresh the collider since unity doesn't update this unless
      // SetHeights() is called or toggling the collider off and on.
      terrain.GetComponent<Collider>().enabled = false;
      terrain.GetComponent<Collider>().enabled = true;
    }
  }

  void UpdateDetailPrototypes(TerrainData terrainData) {
    int terrID = GetTerrainWithData(terrainData);
    if (terrID < 0) return;

    if(terrID != 0) {
      terrainData.detailPrototypes = TerrainGrasses;
      terrainData.RefreshPrototypes();
    } else {
      TerrainGrasses = terrainData.detailPrototypes;
    }
  }
  void UpdateGrass(TerrainData terrainData) {
    float iTime = Time.realtimeSinceStartup;
    int[,] map = new int[terrainData.detailWidth, terrainData.detailHeight];
    // Grass is horribly un-optimized in unity so...
    // TODO: Find a better solution to grass.
    float max = 16f;

    for (int x = 0; x < terrainData.detailWidth; x++) {
      for (int z = 0; z < terrainData.detailHeight; z++) {
        // map[ x, z ] = (int)((float)x / (float)terrainData.detailWidth * max);
        float height = terrainData.GetInterpolatedHeight(
            (float)x / (float)(terrainData.detailWidth - 1),
            (float)z / (float)(terrainData.detailHeight - 1));
        if (height <= TerrainGenerator.waterHeight ||
            height >= TerrainGenerator.snowHeight) {
          map[ z, x ] = 0;
        } else {
          map[ z, x ] =
              (int)((1 - (terrainData.GetSteepness(
                              (float)x / (float)terrainData.detailWidth,
                              (float)z / (float)terrainData.detailHeight) /
                          30f)) *
                    max);
        }
      }
    }

    Terrain t = terrains[GetTerrainWithData(terrainData)]
                    .gameObject.GetComponent<Terrain>();

    for (int i = 0; i < terrainData.detailPrototypes.Length; i++) {
      terrainData.SetDetailLayer(0, 0, i, map);
    }
    t.Flush();

    times.DeltaGrassUpdate = (Time.realtimeSinceStartup - iTime) * 1000;
  }

 public
  void SaveAllChunks() {
    for (int i = 0; i < terrains.Count; i++) {
      SaveChunk(GetXCoord(i), GetZCoord(i));
    }
  }
  void SaveChunk(int X, int Z) {
    if (GetComponent<SaveLoad>() == null || !GetComponent<SaveLoad>().enabled)
      return;
    // Debug.Log("Saving Chunk (" + X + "," + Z + "," + worldID + ") to disk.");
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
    // Debug.Log("Loading Chunk (" + X + "," + Z + "," + worldID + ") from disk.");
    UpdateSplat(terrains[terrID].terrData);
    UpdateDetailPrototypes(terrains[terrID].terrData);
    UpdateTreePrototypes(terrains[terrID].terrData);
    SaveLoad.ReadTerrain(terrains[terrID], worldID);
  }

  // Give array index from coordinates.
  int GetTerrainWithCoord(int x, int z) {
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

  // Get the X coordinate from the array index.
  int GetXCoord(int index) {
    string name = terrains[index].gameObject.name;
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
                     "\n" + name + "," + terrains[index].gameObject.name);
      return -1;
    } else {
      return output;
    }
  }

  // Get the Z coordinate from the array index.
  int GetZCoord(int index) {
    string name = terrains[index].gameObject.name;
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
                     "\n" + name + "," + terrains[index].gameObject.name);
      return -1;
    } else {
      return output;
    }
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
  // Returns the height of the terrain at the player's current location in
  // global units.
 public
  float GetTerrainHeight() { return GetTerrainHeight(player); }
 public
  float GetTerrainHeight(GameObject player) {
    return GetTerrainHeight(player.transform.position);
  }
 public float GetTerrainHeight(float x, float z) {
    return GetTerrainHeight(new Vector3(x, 0, z));
 }
 public
  float GetTerrainHeight(Vector3 position) {
    int xCenter = Mathf.RoundToInt((position.x - terrWidth / 2) / terrWidth);
    int yCenter = Mathf.RoundToInt((position.z - terrLength / 2) / terrLength);
    int terrLoc = GetTerrainWithCoord(xCenter, yCenter);
    if (terrLoc != -1) {
      float TerrainHeight =
          terrains[terrLoc].gameObject.GetComponent<Terrain>().SampleHeight(
              position);
      return TerrainHeight;
    }
    return 0;
  }
 public
  void movePlayerToTop() { movePlayerToTop(player.GetComponent<InitPlayer>()); }
 public
  void movePlayerToTop(GameObject player) {
    movePlayerToTop(player.GetComponent<InitPlayer>());
  }
 public
  void movePlayerToTop(InitPlayer player) {
    // Make sure the player stays above the terrain
    if (player != null) {
      player.updatePosition(player.transform.position.x, GetTerrainHeight(),
                            player.transform.position.z);
    }
  }

 public
  bool anyChunksDividing() {
    for (int i = 0; i < terrains.Count; i++) {
      if (terrains[i].isDividing) return true;
    }
    return false;
  }
 public
  void checkDoneLoadingSpawn() {
    TerrainGenerator.wasDoneLoadingSpawn = TerrainGenerator.doneLoadingSpawn;
    if (TerrainGenerator.doneLoadingSpawn) return;
    if (loadWaitCount > 0 || terrains.Count <= 1 || anyChunksDividing()) {
      TerrainGenerator.doneLoadingSpawn = false;
      return;
    }
    for (int i = 1; i < terrains.Count; i++) {
      if (!terrains[i].terrReady || !terrains[i].hasDivided ||
          terrains[i].terrQueue || terrains[i].texQueue ||
          terrains[i].treeQueue || terrains[i].detailQueue ||
          terrains[i].LODQueue || terrains[i].waterQueue) {
        TerrainGenerator.doneLoadingSpawn = false;
        return;
      }
    }
    TerrainGenerator.doneLoadingSpawn = true;
  }

 public
  static void RemovePlayer(InitPlayer player) {
    for (int i = 0; i < players.Count; i++) {
      if (players[i] == player) {
        players.RemoveAt(i);
        return;
      }
    }
  }

 public
  static void AddPlayer(InitPlayer newPlayer) {
    players.Add(newPlayer);
  }
}
