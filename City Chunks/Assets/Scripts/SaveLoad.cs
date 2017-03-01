using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public
class SaveLoad : MonoBehaviour {
  float[, ] testA = {{4f, 5f, 6f}, {6f, 5f, 4f}, {7f, 8f, 9f}};
  float[, ] testB = {{4f, 5f, 6f}, {6f, 5f, 4f}, {7f, 8f, 9f}};
  void Start() {
    testA = BytesToFloat(FloatToBytes(testA));
    Debug.Log(testA[ 0, 0 ] + ", " + testA[ 0, 1 ] + ", " + testA[ 0, 2 ] +
              "\n" + testA[ 1, 0 ] + ", " + testA[ 1, 1 ] + ", " +
              testA[ 1, 2 ] + "\n" + testA[ 2, 0 ] + ", " + testA[ 2, 1 ] +
              ", " + testA[ 2, 2 ]);
    WriteTerrain(0, 0, ref testA, ref testB);
    ReadTerrain(0, 0, ref testA, ref testB);
    Debug.Log(testA[ 0, 0 ] + ", " + testA[ 0, 1 ] + ", " + testA[ 0, 2 ] +
              "\n" + testA[ 1, 0 ] + ", " + testA[ 1, 1 ] + ", " +
              testA[ 1, 2 ] + "\n" + testA[ 2, 0 ] + ", " + testA[ 2, 1 ] +
              ", " + testA[ 2, 2 ]);
  }
  static void WriteTerrain(int X, int Z, ref float[, ] DividePoints,
                           ref float[, ] PerlinPoints) {
    string filename = Application.persistentDataPath + "/Chunks/Chunk";

    System.IO.Directory.CreateDirectory(Application.persistentDataPath +
                                        "/Chunks/");

    System.IO.File.Create(filename + "A-" + X + "-" + Z + ".dat").Close();
    System.IO.File.Create(filename + "B-" + X + "-" + Z + ".dat").Close();

    System.IO.File.WriteAllBytes(filename + "A-" + X + "-" + Z + ".dat",
                                 FloatToBytes(DividePoints));
    System.IO.File.WriteAllBytes(filename + "B-" + X + "-" + Z + ".dat",
                                 FloatToBytes(PerlinPoints));
  }
  static void ReadTerrain(int X, int Z, ref float[, ] DividePoints,
                          ref float[, ] PerlinPoints) {
    DividePoints = BytesToFloat(
        System.IO.File.ReadAllBytes(Application.persistentDataPath +
                                    "/Chunks/ChunkA-" + X + "-" + Z + ".dat"));
    PerlinPoints = BytesToFloat(
        System.IO.File.ReadAllBytes(Application.persistentDataPath +
                                    "/Chunks/ChunkB-" + X + "-" + Z + ".dat"));
    Debug.Log("Done");
  }
 private
  static byte[] FloatToBytes(float[, ] input) {
    byte[] output = new byte[input.GetLength(0) * input.GetLength(1)];
    System.Buffer.BlockCopy(input, 0, output, 0,
                            input.GetLength(0) * input.GetLength(1));

    string debug = "";
    for (int i = 0; i < output.Length; i++) {
      debug += output[i] + " ";
    }
    debug += "\n";
    for (int i = 0; i < output.Length; i++) {
      debug += Mathf.Log(output[i], 2f) + " ";
    }
    Debug.Log(debug);

    return output;
  }
 private
  static float[, ] BytesToFloat(byte[] input) {
    int lengthSqrt = (int)Mathf.Sqrt(input.Length);
    float[, ] output = new float[ lengthSqrt, lengthSqrt ];
    System.Buffer.BlockCopy(input, 0, output, 0, input.Length);

    string debug = "";
    for (int i = 0; i < output.GetLength(0); i++) {
      for (int j = 0; j < output.GetLength(1); j++) {
        debug += output[ i, j ] + " ";
      }
    }
    debug += "\n";
    for (int i = 0; i < output.GetLength(0); i++) {
      for (int j = 0; j < output.GetLength(1); j++) {
        debug += Mathf.Log(output[ i, j ], 2f) + " ";
      }
    }
    Debug.Log(debug);

    return output;
  }
}
