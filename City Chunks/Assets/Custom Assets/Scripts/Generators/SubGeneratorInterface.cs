using UnityEngine;

[RequireComponent(typeof(TerrainGenerator))]
public abstract class SubGenerator : MonoBehaviour {
  public bool enabled = true;
  private bool firstError = false;
  protected TerrainGenerator tg;
  public void Initialize(TerrainGenerator TG) {
    tg = TG;
    Initialized();
  }
  protected abstract void Initialized();
  public void Go(Terrains terrain) {
    if (tg == null && firstError && enabled) {
      Debug.LogError("Go was called before Initialize! This is not allowed!");
      firstError = false;
    } else if (tg != null && enabled) {
      Generate(terrain);
    }
  }
  protected abstract void Generate(Terrains terrain);
}
