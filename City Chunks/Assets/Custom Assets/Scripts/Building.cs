using UnityEngine;

public class Building : MonoBehaviour {
  public enum Floor { GROUND, MIDDLE, ROOF, COMPLETE };

  public int ID = 0;
  public Floor floor = Floor.GROUND;
  [Tooltip("Bottom center of floor relative to anchor.")]
  public Vector3 centerOffset = Vector3.zero;
  [Tooltip("Door position relative to the anchor point of the gameObject. Used for aligning with road.")]
  public Vector3 doorPosition = Vector3.zero;
  [Tooltip("Width, Height, Length in meters.")]
  public Vector3 dimensions = new Vector3(30f, 6f, 30f);
}
