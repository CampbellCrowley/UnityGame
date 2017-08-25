using UnityEngine;

public class CityGenerator : MonoBehaviour {
  // TODO: Generate cities...
  // TODO: Make buildings modular vertically. Meaning they can stack on top of
  // each other.
  public GameObject[] buildingPrefabs;
  // Meters
  public float buildingWidth = 60f;
  public float roadWidth = 7.5f;

  private TerrainGenerator tg;

  public void Initialize(TerrainGenerator TG) {
    tg = TG;
    if (tg == null) Debug.LogWarning("Failed to find TG!");
    Debug.Log("City Generator Initialized");
  }

  public void Generate(Terrains terrain) {
    if (!terrain.cityQueue) return;
    if (tg == null) return;

    terrain.cityQueue = false;
  }
}
