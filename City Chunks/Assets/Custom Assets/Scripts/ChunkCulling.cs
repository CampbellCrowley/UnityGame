using UnityEngine;

public class ChunkCulling : MonoBehaviour {

  public bool DebugAngles = false;
  Terrain terrain;
  TerrainGenerator tg;

  void Start() {
    terrain = GetComponent<Terrain>();
    tg = FindObjectOfType<TerrainGenerator>();
  }
  void LateUpdate() {
    if (Camera.main == null) return;
    if (terrain == null) GetComponent<Terrain>();
    if (terrain == null) this.enabled = false;

    Vector3 CPos = Camera.main.transform.position;
    Vector3 TPos =
        transform.position + tg.GetTerrainWidth() / 2f * Vector3.right +
        tg.GetTerrainLength() / 2f * Vector3.forward + CPos.y * Vector3.up;

    if (DebugAngles)
      Debug.DrawLine(CPos, CPos + Camera.main.transform.forward * 10f,
                     Color.red);

    if ((TPos - CPos).sqrMagnitude > 300 * 300 &&
        Vector3.Angle(Camera.main.transform.forward, TPos - CPos) > 95f) {
      if (DebugAngles) Debug.DrawLine(CPos, TPos, Color.blue);
      terrain.drawHeightmap = false;
      terrain.drawTreesAndFoliage = false;
    } else {
      if (DebugAngles) Debug.DrawLine(CPos, TPos, Color.green);
      terrain.drawHeightmap = true;
      terrain.drawTreesAndFoliage = true;
    }
  }
}
