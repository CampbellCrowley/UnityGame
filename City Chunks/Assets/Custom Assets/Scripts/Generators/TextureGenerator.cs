using UnityEngine;

class TextureGenerator : SubGenerator {
  [System.Serializable]
  public class Textures {
    public int Length = 4;
    [Tooltip("Common/Backup Texture.")]
    public Texture2D[] Grass;
    [Tooltip("For beaches.")]
    public Texture2D[] Sand;
    [Tooltip("For steep slopes")]
    public Texture2D[] Rock;
    [Tooltip("For high altitudes")]
    public Texture2D Snow;
  }
  [System.Serializable]
  public class TextureNormals {
    public int Length = 4;
    [Tooltip("Common/Backup Texture.")]
    public Texture2D[] Grass;
    [Tooltip("For beaches.")]
    public Texture2D[] Sand;
    [Tooltip("For steep slopes")]
    public Texture2D[] Rock;
    [Tooltip("For high altitudes")]
    public Texture2D Snow;
  }
  [Tooltip("Array of textures to apply to the terrain.")]
  [SerializeField] public Textures TerrainTextures;
  [Tooltip("The normals that correspond to the terrain textures.")]
  [SerializeField] public TextureNormals TerrainTextureNormals;
  SplatPrototype[] tex;

  protected override void Initialized() {
    tex = new SplatPrototype[TerrainTextures.Length];
    TerrainTextures.Length += TerrainTextures.Grass.Length +
                              TerrainTextures.Rock.Length +
                              TerrainTextures.Sand.Length - 3;
    if (TerrainTextures.Grass.Length != TerrainTextureNormals.Grass.Length) {
      Debug.LogWarning(
          "Terrain textures and terrain texture normals should have the same " +
          "number of items.");
    }
    for (int i = 0; i < tex.Length; i++) tex[i] = new SplatPrototype();

    if (TerrainTextures.Grass != null && TerrainTextures.Grass.Length > 0) {
      for (int i = 0; i < TerrainTextures.Grass.Length; i++) {
        tex[i].texture = TerrainTextures.Grass[i];
      }
      for (int i = 0; i < TerrainTextureNormals.Grass.Length; i++) {
        tex[i].normalMap = TerrainTextureNormals.Grass[i];
      }
    } else {
      Debug.LogError("Grass Texture must be defined!");
      return;
    }

    int startPoint = TerrainTextures.Grass.Length;

    // Fill all expected values to grass as a default.
    for (int i = startPoint; i < tex.Length; i++) {
      tex[i].texture = TerrainTextures.Grass[0];
    }

    if (TerrainTextures.Sand != null && TerrainTextures.Sand.Length > 0) {
      for (int i = 0; i < TerrainTextures.Sand.Length; i++) {
        tex[i + startPoint].texture = TerrainTextures.Sand[i];
      }
      for (int i = 0; i < TerrainTextureNormals.Sand.Length; i++) {
        tex[i + startPoint].normalMap = TerrainTextureNormals.Sand[i];
      }
    }
    startPoint += TerrainTextures.Sand.Length;
    if (TerrainTextures.Rock != null && TerrainTextures.Rock.Length > 0) {
      for (int i = 0; i < TerrainTextures.Rock.Length; i++) {
        tex[i + startPoint].texture = TerrainTextures.Rock[i];
      }
      for (int i = 0; i < TerrainTextureNormals.Rock.Length; i++) {
        tex[i + startPoint].normalMap = TerrainTextureNormals.Rock[i];
      }
    }
    startPoint += TerrainTextures.Rock.Length;
    if (TerrainTextures.Snow != null) {
      tex[startPoint].texture = TerrainTextures.Snow;
      tex[startPoint].normalMap = TerrainTextureNormals.Snow;
      tex[startPoint].metallic = 0.5f;
      tex[startPoint].smoothness = 0.3f;
    }
    startPoint++;

    for (int i = 0; i < tex.Length; i++) {
      tex[i].tileSize = new Vector2(8, 8);  // Sets the size of the texture
      tex[i].tileOffset = new Vector2(0, 0);  // Sets the offset of the texture
    }
  }
  protected override void Generate(Terrains terrain) {
    terrain.terrData.splatPrototypes = tex;

    if (terrain.terrData.splatPrototypes.Length != TerrainTextures.Length) return;

    float[,] heightmap = tg.MixHeights(terrain);

    int alphamapWidth = terrain.terrData.alphamapWidth;
    int alphamapHeight = terrain.terrData.alphamapHeight;
    float[, , ] map =
        new float[ alphamapWidth, alphamapHeight, TerrainTextures.Length ];

    for (int y = 0; y < alphamapWidth; y++) {
      for (int x = 0; x < alphamapHeight; x++) {
        int X = x * (terrain.terrData.heightmapWidth / alphamapWidth);
        int Y = y * (terrain.terrData.heightmapHeight / alphamapHeight);
        // Get terrain information at each point.
        // float angle = terrainData.GetSteepness(normX, normY);
        float angle = tg.GetSteepness(heightmap, X, Y);
        //float height =
        //    terrainData.GetHeight(Mathf.RoundToInt(normX * heightmapWidth),
        //                          Mathf.RoundToInt(normY * heightmapHeight));
        float height = heightmap[ X, Y ] * tg.GetTerrainMaxHeight();

        // if (x == alphamapHeight / 2 && x == y) Debug.Log(height + "@" + angle);

        // Steepness is given as an angle, 0..90 degrees. Divide
        // by 90 to get an alpha blending value in the range 0..1.
        float maxAngle = 30f;
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

        // Grass
        int startPoint = TerrainTextures.Grass.Length;
        values[UnityEngine.Random.Range(0, startPoint - 1)] =
            ((height <= TerrainGenerator.snowHeight) ? 1.0f - frac : 0f);

        // Sand
        values[UnityEngine.Random.Range(
            startPoint, startPoint + TerrainTextures.Sand.Length - 1)] =
            ((height <= TerrainGenerator.waterHeight + 2f)
                 ? ((height <= TerrainGenerator.waterHeight + 1f) ? 100f : 2f)
                 : 0f);

        // Rock
        startPoint += TerrainTextures.Sand.Length;
        values[UnityEngine.Random.Range(
            startPoint, startPoint + TerrainTextures.Rock.Length - 1)] =
            frac;

        // Snow
        startPoint += TerrainTextures.Rock.Length;
        values[startPoint] =
            ((height >= TerrainGenerator.snowHeight) ? 1f - frac : 0f);

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

        for (int i = 0; i < values.Length; i++) {
          map[ x, y, i ] = values[i];
        }
      }
    }
    terrain.terrData.SetAlphamaps(0, 0, map);
  }
}
