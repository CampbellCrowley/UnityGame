// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
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
 public
  class TerrainBuffer {
   public
    string RW = "UNKNOWN";
   public
    int X;
   public
    int Z;
   public
    string seed;
   public
    float[, ] DividePoints;
   public
    float[, ] PerlinPoints;
   public
    float[, , ] alphamaps;
   public
    int[, , ] detailmaps;
   public
    int[] indexes;
   public
    float[, ] positions;
   public
    Terrains terrain;
   public
    TreeInstance[] treeInstances;
   public
    bool readyToSerialize = false;
   public
    virtual void Serialize() {}
  }

  public class TerrainWriteBuffer : TerrainBuffer {
   public
    TerrainWriteBuffer(int X, int Z, float[, ] DividePoints,
                       float[, ] PerlinPoints, TerrainData terrData,
                       string seed) {
      this.RW = "WRITE";
      this.X = X;
      this.Z = Z;
      this.DividePoints = DividePoints;
      this.PerlinPoints = PerlinPoints;
      this.seed = seed;

      // Need to deserialize terrData since it can only be accessed from the
      // main thread.
      this.alphamaps = terrData.GetAlphamaps(0, 0, terrData.alphamapWidth,
                                             terrData.alphamapHeight);
      this.detailmaps = new int[
        terrData.detailPrototypes.Length,
        terrData.detailWidth,
        terrData.detailHeight
      ];
      for (int i = 0; i < terrData.detailPrototypes.Length; i++) {
        int[, ] layerMap = terrData.GetDetailLayer(0, 0, terrData.detailWidth,
                                                   terrData.detailHeight, i);
        for (int x = 0; x < terrData.detailWidth; x++) {
          for (int y = 0; y < terrData.detailHeight; y++) {
            detailmaps[ i, x, y ] = layerMap[ x, y ];
          }
        }
      }
      this.indexes = new int[terrData.treeInstanceCount];
      Vector3[] positions = new Vector3[terrData.treeInstanceCount];

      for (int i = 0; i < terrData.treeInstanceCount; i++) {
        this.indexes[i] = terrData.treeInstances[i].prototypeIndex;
        positions[i] = terrData.treeInstances[i].position;
      }
      this.positions = Vector3ToFloat(positions);
    }
  }

  public class TerrainReadBuffer : TerrainBuffer {
   public
    TerrainReadBuffer(Terrains terrain, string seed) {
      this.RW = "READ";
      this.terrain = terrain;
      this.seed = seed;
      this.X = terrain.x;
      this.Z = terrain.z;
    }
   public
    override void Serialize() {
      terrain.terrPoints = this.DividePoints;
      terrain.terrPerlinPoints = this.PerlinPoints;
      int[, ] detailLayer =
          new int[ detailmaps.GetLength(1), detailmaps.GetLength(2) ];
      for (int i = 0; i < detailmaps.GetLength(0); i++) {
        for (int x = 0; x < detailmaps.GetLength(1); x++) {
          for (int y = 0; y < detailmaps.GetLength(2); y++) {
            detailLayer[ x, y ] = detailmaps[ i, x, y ];
          }
        }
        terrain.terrData.SetDetailLayer(0, 0, i, detailLayer);
      }
      terrain.terrData.treeInstances = treeInstances;
      terrain.terrData.SetAlphamaps(0, 0, alphamaps);
      terrain.loadedFromDisk = true;
      terrain.justLoadedFromDisk = true;
      terrain.loadingFromDisk = false;
    }
  }

  private static bool threadIsRunning = false;
 private
  static int currentIndex = 0;
 public
  static bool loadAsync = true;
 public
  static List<TerrainBuffer> Buffer = new List<TerrainBuffer>();
 private
  static string filenameChunk = "IfYouSeeThisSomethingBrokeMoo";
 private
  static string directoryChunk = "IfYouSeeThisSomethingBrokeOink";

  void Start() {
    directoryChunk = Application.persistentDataPath + "/Chunks/";
    filenameChunk = directoryChunk + "Chunk";
  }

  void Update() {
    foreach (TerrainBuffer buf in Buffer) {
      if (buf.readyToSerialize) {
        buf.Serialize();
      }
    }
    // After serializing buffer.
    if (!threadIsRunning && Buffer.Count > 0 && currentIndex == 0) {
      Buffer.Clear();
    }
  }

 private
  static void BeginProcessing() {
    threadIsRunning = true;
    while (threadIsRunning) {
      switch (Buffer[currentIndex].RW) {
        case "READ":
          ReadTerrain_(currentIndex);
          break;
        case "WRITE":
          WriteTerrain_(currentIndex);
          break;
        default:
          break;
      }
      currentIndex++;
      if (currentIndex >= Buffer.Count) {
        currentIndex = 0;
        threadIsRunning = false;
      }
    }
  }

 public
  static void WriteTerrain(int X, int Z, float[, ] DividePoints,
                           float[, ] PerlinPoints, TerrainData terrData,
                           string seed) {
    Buffer.Add(new TerrainWriteBuffer(X, Z, DividePoints, PerlinPoints,
                                      terrData, seed));
    Debug.Log("Writing Terrain Chunk(" + X + ", " + Z + ") (" + seed + ")");
    if (!threadIsRunning) {
      Thread thread = new Thread(BeginProcessing);
      thread.Start();
      while (!loadAsync && thread.IsAlive) {
        Thread.Sleep(10);
      }
      if (!loadAsync) Buffer.RemoveAt(Buffer.Count - 1);
    }
  }

 private
  static void WriteTerrain_(int index) {
    int X = Buffer[index].X;
    int Z = Buffer[index].Z;
    string seed = Buffer[index].seed;

    Directory.CreateDirectory(directoryChunk);

    File.Create(filenameChunk + seed + "A-" + X + "-" + Z + ".dat").Close();
    File.Create(filenameChunk + seed + "B-" + X + "-" + Z + ".dat").Close();

    File.WriteAllBytes(filenameChunk + seed + "A-" + X + "-" + Z + ".dat",
                       FloatToBytes(Buffer[index].DividePoints));
    File.WriteAllBytes(filenameChunk + seed + "B-" + X + "-" + Z + ".dat",
                       FloatToBytes(Buffer[index].PerlinPoints));
    WriteTerrainData(index);
  }

 public
  static bool TerrainExists(int X, int Z, string seed) {
    if (!File.Exists(filenameChunk + seed + "A-" + X + "-" + Z + ".dat"))
      return false;
    if (!File.Exists(filenameChunk + seed + "B-" + X + "-" + Z + ".dat"))
      return false;
    if (!File.Exists(filenameChunk + seed + "C1-" + X + "-" + Z + ".dat"))
      return false;
    if (!File.Exists(filenameChunk + seed + "C2I-" + X + "-" + Z + ".dat"))
      return false;
    if (!File.Exists(filenameChunk + seed + "C2P-" + X + "-" + Z + ".dat"))
      return false;
    if (!File.Exists(filenameChunk + seed + "C3-" + X + "-" + Z + ".dat"))
      return false;
    return true;
  }

 public
  static void ReadTerrain(Terrains terrain, string seed) {
    terrain.loadingFromDisk = true;
    Buffer.Add(new TerrainReadBuffer(terrain, seed));
    Debug.Log("Reading Terrain Chunk(" + terrain.x + ", " + terrain.z + ") (" +
              seed + ")");
    if (!threadIsRunning) {
      Thread thread = new Thread(BeginProcessing);
      thread.Start();
      while (!loadAsync && thread.IsAlive) {
        Thread.Sleep(10);
      }
      if (!loadAsync) {
        Buffer[Buffer.Count - 1].Serialize();
        Buffer.RemoveAt(Buffer.Count - 1);
      }
    }
  }

 private
  static void ReadTerrain_(int index) {
    Buffer[index].DividePoints = BytesToFloat2D(
        File.ReadAllBytes(filenameChunk + Buffer[index].seed + "A-" +
                          Buffer[index].X + "-" + Buffer[index].Z + ".dat"));

    Buffer[index].PerlinPoints = BytesToFloat2D(
        File.ReadAllBytes(filenameChunk + Buffer[index].seed + "B-" +
                          Buffer[index].X + "-" + Buffer[index].Z + ".dat"));

    ReadTerrainData(index);
    Buffer[index].readyToSerialize = true;
  }

 private
  static void WriteTerrainData(int index) {
    int X = Buffer[index].X;
    int Z = Buffer[index].Z;
    string seed = Buffer[index].seed;

    Directory.CreateDirectory(directoryChunk);
    File.Create(filenameChunk + seed + "C1-" + X + "-" + Z + ".dat").Close();
    File.Create(filenameChunk + seed + "C2I-" + X + "-" + Z + ".dat").Close();
    File.Create(filenameChunk + seed + "C2P-" + X + "-" + Z + ".dat").Close();
    File.Create(filenameChunk + seed + "C3-" + X + "-" + Z + ".dat").Close();

    File.WriteAllBytes(filenameChunk + seed + "C1-" + X + "-" + Z + ".dat",
                       FloatToBytes(Buffer[index].alphamaps));
    File.WriteAllBytes(filenameChunk + seed + "C2I-" + X + "-" + Z + ".dat",
                       IntToBytes(Buffer[index].indexes));
    File.WriteAllBytes(filenameChunk + seed + "C2P-" + X + "-" + Z + ".dat",
                       FloatToBytes(Buffer[index].positions));
    File.WriteAllBytes(filenameChunk + seed + "C3-" + X + "-" + Z + ".dat",
                       IntToBytes(Buffer[index].detailmaps));
  }

 private
  static void ReadTerrainData(int index) {
    float[, , ] alphamaps = BytesToFloat3D(
        File.ReadAllBytes(filenameChunk + Buffer[index].seed + "C1-" +
                          Buffer[index].X + "-" + Buffer[index].Z + ".dat"));
    int[] treeIndexes = BytesToInt(
        File.ReadAllBytes(filenameChunk + Buffer[index].seed + "C2I-" +
                          Buffer[index].X + "-" + Buffer[index].Z + ".dat"));
    Vector3[] treePositions = BytesToVector3(
        File.ReadAllBytes(filenameChunk + Buffer[index].seed + "C2P-" +
                          Buffer[index].X + "-" + Buffer[index].Z + ".dat"));
    int[, , ] detailmaps = BytesToInt3D(
        File.ReadAllBytes(filenameChunk + Buffer[index].seed + "C3-" +
                          Buffer[index].X + "-" + Buffer[index].Z + ".dat"));

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

    Buffer[index].alphamaps = alphamaps;
    Buffer[index].treeInstances = treeInstances;
    Buffer[index].detailmaps = detailmaps;
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
  static float[, ] Vector3ToFloat(Vector3[] input) {
    float[, ] output = new float[ input.Length, 3 ];
    for (int i = 0; i < input.Length; i++) {
      for (int j = 0; j < 3; j++) {
        output[ i, j ] = input[i][j];
      }
    }
    return output;
  }

 private
  static byte[] Vector3ToBytes(Vector3[] input) {
    return FloatToBytes(Vector3ToFloat(input));
  }

 private
  static byte[] FloatToBytes(float[, ] input) {
    int length = input.GetLength(0) * input.GetLength(1) * sizeof(float);
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
