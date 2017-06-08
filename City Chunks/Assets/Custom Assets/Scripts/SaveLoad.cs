using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A is Divide Points (float[,])
// B is Perlin Noise Points (float[,])
// C is Terrain Data
// C1 is Splatmaps (float[,,])
// C2 is Tree Instances
// C2I is Prototype Index (int[])
// C2P is Position (Vector3[])
// C3 is Detail layer (int[,,])
// D is Seed (int)

public
class SaveLoad : MonoBehaviour {
  // float[, ] testA = {{4f, 5f, 6f}, {6f, 5f, 4f}, {7f, 8f, 9f}};
  // float[, ] testB = {{4f, 5f, 6f}, {6f, 5f, 4f}, {7f, 8f, 9f}};
  // float[, ] testC = {{1f}, {2f}, {3f}};
  // float[, , ] testD = {{{1f}, {2f}}, {{3f}, {4f}}};
 private
  static string filenameChunk = "Temp";

  void Start() {
    filenameChunk = Application.persistentDataPath + "/Chunks/Chunk";
  }
  void Update() {
    // TerrainData terrData = new TerrainData();
    if (Input.GetKeyDown("t")) {
      float[, , ] map =
          GetComponent<Terrain>().terrainData.GetAlphamaps(0, 0, 16, 16);
      string output = "";
      for (int i = 0; i < map.GetLength(0); i++) {
        for (int j = 0; j < map.GetLength(1); j++) {
          output += "{";
          for (int k = 0; k < map.GetLength(2); k++) {
            output += map[ i, j, k ] + ", ";
          }
          output += "}, ";
        }
        output += "\n";
      }
      Debug.Log("Before:\n" + output);

      map = BytesToFloat3D(FloatToBytes(map));

      output = "";
      for (int i = 0; i < map.GetLength(0); i++) {
        for (int j = 0; j < map.GetLength(1); j++) {
          output += "{";
          for (int k = 0; k < map.GetLength(2); k++) {
            output += map[ i, j, k ] + ", ";
          }
          output += "}, ";
        }
        output += "\n";
      }
      Debug.Log("After:\n" + output);
    }
    // if (Input.GetKeyDown("w")) {
    //   Debug.Log(testA[ 0, 0 ] + ", " + testA[ 0, 1 ] + ", " + testA[ 0, 2 ] +
    //             "\n" + testA[ 1, 0 ] + ", " + testA[ 1, 1 ] + ", " +
    //             testA[ 1, 2 ] + "\n" + testA[ 2, 0 ] + ", " + testA[ 2, 1 ] +
    //             ", " + testA[ 2, 2 ]);
    //   WriteTerrain(0, 0, testA, testB, terrData);
    // }
    // if (Input.GetKeyDown("r")) {
    //   ReadTerrain(0, 0, ref testA, ref testB, ref terrData);
    //   Debug.Log(testA[ 0, 0 ] + ", " + testA[ 0, 1 ] + ", " + testA[ 0, 2 ] +
    //             "\n" + testA[ 1, 0 ] + ", " + testA[ 1, 1 ] + ", " +
    //             testA[ 1, 2 ] + "\n" + testA[ 2, 0 ] + ", " + testA[ 2, 1 ] +
    //             ", " + testA[ 2, 2 ]);
    // }
  }
 public
  static void WriteTerrain(int X, int Z, float[, ] DividePoints,
                           float[, ] PerlinPoints, TerrainData terrData,
                           string seed) {
    System.IO.Directory.CreateDirectory(Application.persistentDataPath +
                                        "/Chunks/");

    System.IO.File.Create(filenameChunk + seed + "A-" + X + "-" + Z + ".dat")
        .Close();
    System.IO.File.Create(filenameChunk + seed + "B-" + X + "-" + Z + ".dat")
        .Close();

    System.IO.File.WriteAllBytes(
        filenameChunk + seed + "A-" + X + "-" + Z + ".dat",
        FloatToBytes(DividePoints));
    System.IO.File.WriteAllBytes(
        filenameChunk + seed + "B-" + X + "-" + Z + ".dat",
        FloatToBytes(PerlinPoints));
    WriteTerrainData(X, Z, terrData, seed);
    Debug.Log("Done writing (" + X + ", " + Z + ")");
  }
 public
  static bool TerrainExists(int X, int Z, string seed) {
    if (!System.IO.File.Exists(filenameChunk + seed + "A-" + X + "-" + Z +
                               ".dat"))
      return false;
    if (!System.IO.File.Exists(filenameChunk + seed + "B-" + X + "-" + Z +
                               ".dat"))
      return false;
    if (!System.IO.File.Exists(filenameChunk + seed + "C1-" + X + "-" + Z +
                               ".dat"))
      return false;
    if (!System.IO.File.Exists(filenameChunk + seed + "C2I-" + X + "-" + Z +
                               ".dat"))
      return false;
    if (!System.IO.File.Exists(filenameChunk + seed + "C2P-" + X + "-" + Z +
                               ".dat"))
      return false;
    if (!System.IO.File.Exists(filenameChunk + seed + "C3-" + X + "-" + Z +
                               ".dat"))
      return false;
    return true;
  }
 public
  static void ReadTerrain(int X, int Z, ref float[, ] DividePoints,
                          ref float[, ] PerlinPoints, ref TerrainData terrData,
                          string seed) {
    DividePoints = BytesToFloat2D(System.IO.File.ReadAllBytes(
        filenameChunk + seed + "A-" + X + "-" + Z + ".dat"));
    PerlinPoints = BytesToFloat2D(System.IO.File.ReadAllBytes(
        filenameChunk + seed + "B-" + X + "-" + Z + ".dat"));
    ReadTerrainData(X, Z, ref terrData, seed);
    Debug.Log("Done reading (" + X + ", " + Z + ")");
  }
 private
  static void WriteTerrainData(int X, int Z, TerrainData terrData,
                               string seed) {
    System.IO.Directory.CreateDirectory(Application.persistentDataPath +
                                        "/Chunks/");
    System.IO.File.Create(filenameChunk + seed + "C1-" + X + "-" + Z + ".dat")
        .Close();
    System.IO.File.Create(filenameChunk + seed + "C2I-" + X + "-" + Z + ".dat")
        .Close();
    System.IO.File.Create(filenameChunk + seed + "C2P-" + X + "-" + Z + ".dat")
        .Close();
    System.IO.File.Create(filenameChunk + seed + "C3-" + X + "-" + Z + ".dat")
        .Close();

    System.IO.File.WriteAllBytes(
        filenameChunk + seed + "C1-" + X + "-" + Z + ".dat",
        FloatToBytes(terrData.GetAlphamaps(0, 0, terrData.alphamapWidth,
                                           terrData.alphamapHeight)));
    int[] indexes = new int[terrData.treeInstanceCount];
    Vector3[] positions = new Vector3[terrData.treeInstanceCount];

    for (int i = 0; i < terrData.treeInstanceCount; i++) {
      indexes[i] = terrData.treeInstances[i].prototypeIndex;
      positions[i] = terrData.treeInstances[i].position;
    }

    System.IO.File.WriteAllBytes(
        filenameChunk + seed + "C2I-" + X + "-" + Z + ".dat",
        IntToBytes(indexes));
    System.IO.File.WriteAllBytes(
        filenameChunk + seed + "C2P-" + X + "-" + Z + ".dat",
        Vector3ToBytes(positions));

    int[, , ] map = new int[
      terrData.detailPrototypes.Length,
      terrData.detailWidth,
      terrData.detailHeight
    ];
    for (int i = 0; i < terrData.detailPrototypes.Length; i++) {
      int[, ] layerMap = terrData.GetDetailLayer(0, 0, terrData.detailWidth,
                                                 terrData.detailHeight, i);
      for (int x = 0; x < terrData.detailWidth; x++) {
        for (int y = 0; y < terrData.detailHeight; y++) {
          map[ i, x, y ] = layerMap[ x, y ];
        }
      }
    }
    System.IO.File.WriteAllBytes(
        filenameChunk + seed + "C3-" + X + "-" + Z + ".dat", IntToBytes(map));
  }
 private
  static void ReadTerrainData(int X, int Z, ref TerrainData terrData,
                              string seed) {
    float[, , ] alphamaps = BytesToFloat3D(System.IO.File.ReadAllBytes(
        filenameChunk + seed + "C1-" + X + "-" + Z + ".dat"));
    int[] treeIndexes = BytesToInt(System.IO.File.ReadAllBytes(
        filenameChunk + seed + "C2I-" + X + "-" + Z + ".dat"));
    Vector3[] treePositions = BytesToVector3(System.IO.File.ReadAllBytes(
        filenameChunk + seed + "C2P-" + X + "-" + Z + ".dat"));
    int[, , ] detailLayers = BytesToInt3D(System.IO.File.ReadAllBytes(
        filenameChunk + seed + "C3-" + X + "-" + Z + ".dat"));

    TreeInstance[] treeInstances = new TreeInstance[treeIndexes.Length];
    for (int i = 0; i < treeInstances.Length; i++) {
      treeInstances[i] = new TreeInstance();
      treeInstances[i].prototypeIndex = treeIndexes[i];
      treeInstances[i].color = new Color(1, 1, 1);
      treeInstances[i].lightmapColor = new Color(1, 1, 1);
      treeInstances[i].heightScale = 1.0f;
      treeInstances[i].widthScale = 1.0f;
      treeInstances[i].position = treePositions[i];
    }

    terrData.SetAlphamaps(0, 0, alphamaps);
    terrData.treeInstances = treeInstances;
    int[, ] detailLayer =
        new int[ detailLayers.GetLength(1), detailLayers.GetLength(2) ];
    for (int i = 0; i < detailLayers.GetLength(0); i++) {
      for (int x = 0; x < detailLayers.GetLength(1); x++) {
        for (int y = 0; y < detailLayers.GetLength(2); y++) {
          detailLayer[ x, y ] = detailLayers[ i, x, y ];
        }
      }
      terrData.SetDetailLayer(0, 0, i, detailLayer);
    }
  }

 private
  static byte[] IntToBytes(int[] input) {
    byte[] output = new byte[input.Length];
    System.Buffer.BlockCopy(input, 0, output, 0, input.Length);
    return output;
  }
 private
  static byte[] IntToBytes(int[, ] input) {
    int length = input.GetLength(0) * input.GetLength(1) * sizeof(int);
    byte[] output = new byte[length];
    System.Buffer.BlockCopy(input, 0, output, 0, length);

    // Store the length of the first dimension in the last 2 bytes of the array.
    byte[] processed = new byte[output.Length + 2];
    output.CopyTo(processed, 0);
    processed[output.Length + 0] = (byte)input.GetLength(0);
    processed[output.Length + 1] = (byte)(input.GetLength(0) >> 8);
    return processed;
    // return output;
  }
 private
  static byte[] IntToBytes(int[, , ] input) {
    int length = input.GetLength(0) * input.GetLength(1) * input.GetLength(2) *
                 sizeof(int);
    byte[] output = new byte[length];
    System.Buffer.BlockCopy(input, 0, output, 0, length);

    // Store the length of the first and second dimensions in the last 4 bytes
    // of the array.
    byte[] processed = new byte[output.Length + 4];
    output.CopyTo(processed, 0);
    processed[output.Length + 0] = (byte)input.GetLength(0);
    processed[output.Length + 1] = (byte)(input.GetLength(0) >> 8);
    processed[output.Length + 2] = (byte)input.GetLength(1);
    processed[output.Length + 3] = (byte)(input.GetLength(1) >> 8);
    return processed;
    // return output;
  }
 private
  static byte[] Vector3ToBytes(Vector3[] input) {
    float[, ] output = new float[ input.Length, 3 ];
    for (int i = 0; i < input.Length; i++) {
      for (int j = 0; j < 3; j++) {
        output[ i, j ] = input[i][j];
      }
    }
    return FloatToBytes(output);
  }
 private
  static byte[] FloatToBytes(float[, ] input) {
    int length = input.GetLength(0) * input.GetLength(1) * sizeof(float);
    byte[] output = new byte[length];
    System.Buffer.BlockCopy(input, 0, output, 0, length);

    /*
bool error = false;
int length_ = length + 1;
do {
  error = false;
  length_--;
  try {
    System.Buffer.BlockCopy(input, 0, output, 0, length_);
  } catch (System.ArgumentException e) {
    error = true;
  }
} while (length_ > 0 && error);
Debug.Log("Length: " + length_);
*/

    // Store the length of the first dimension in the last 2 bytes of the array.
    byte[] processed = new byte[output.Length + 2];
    output.CopyTo(processed, 0);
    processed[output.Length + 0] = (byte)input.GetLength(0);
    processed[output.Length + 1] = (byte)(input.GetLength(0) >> 8);
    return processed;
    // return output;
  }
 private
  static byte[] FloatToBytes(float[, , ] input) {
    int length = input.GetLength(0) * input.GetLength(1) * input.GetLength(2) *
                 sizeof(float);
    byte[] output = new byte[length];
    System.Buffer.BlockCopy(input, 0, output, 0, length);

    // Store the length of the first and second dimensions in the last 4 bytes
    // of the array.
    byte[] processed = new byte[output.Length + 4];
    output.CopyTo(processed, 0);
    processed[output.Length + 0] = (byte)input.GetLength(0);
    processed[output.Length + 1] = (byte)(input.GetLength(0) >> 8);
    processed[output.Length + 2] = (byte)input.GetLength(1);
    processed[output.Length + 3] = (byte)(input.GetLength(1) >> 8);
    return processed;
    // return output;
  }

 private
  static int[] BytesToInt(byte[] input) {
    int[] output = new int[input.Length];
    System.Buffer.BlockCopy(input, 0, output, 0, input.Length);
    return output;
  }
 private
  static int[, , ] BytesToInt3D(byte[] input) {
    int length1 =
        (int)input[input.Length - 4] + (int)(input[input.Length - 3] << 8);
    int length2 =
        (int)input[input.Length - 2] + (int)(input[input.Length - 1] << 8);
    int length3 = (input.Length - 4) / length1 / length2 / sizeof(int);
    int[, , ] output = new int[ length1, length2, length3 ];
    System.Buffer.BlockCopy(input, 0, output, 0, input.Length - 4);
    return output;
  }
 private
  static Vector3[] BytesToVector3(byte[] input) {
    float[, ] intermediate = BytesToFloat2D(input);
    Vector3[] output = new Vector3[intermediate.GetLength(0)];
    for (int i = 0; i < output.Length; i++) {
      for (int j = 0; j < 3; j++) {
        output[i][j] = intermediate[ i, j ];
      }
    }
    return output;
  }
 private
  static float[, ] BytesToFloat2D(byte[] input) {
    int length1 =
        (int)input[input.Length - 2] + (int)(input[input.Length - 1] << 8);
    int length2 = (input.Length - 2) / length1 / sizeof(float);
    float[, ] output = new float[ length1, length2 ];
    System.Buffer.BlockCopy(input, 0, output, 0, input.Length - 2);
    return output;
  }
 private
  static float[, , ] BytesToFloat3D(byte[] input) {
    int length1 =
        (int)input[input.Length - 4] + (int)(input[input.Length - 3] << 8);
    int length2 =
        (int)input[input.Length - 2] + (int)(input[input.Length - 1] << 8);
    int length3 = (input.Length - 4) / length1 / length2 / sizeof(float);
    float[, , ] output = new float[ length1, length2, length3 ];
    System.Buffer.BlockCopy(input, 0, output, 0, input.Length - 4);
    return output;
  }
}
