// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEngine;

public class Weapon : MonoBehaviour {
  public enum WeaponType { RELAX, UNARMED, GUN, MELEE };
  public enum GunType { UNARMED, PISTOL, RIFLE, LAUNCHER };

  public int weaponID = 0;
  public WeaponType weaponType = WeaponType.RELAX;
  public GunType gunType = GunType.UNARMED;
  public Vector3 relativeMuzzleTip = Vector3.zero;
  public Vector3 relativeMuzzleDirection = Vector3.forward;

  public void Trigger() {
    // Begin attack (Shoot/punch/etc).
    // Animate
  }

  public void Reload() {
    // If gun, show reload animation.
    // Other weapon: Wipe off/clean?
  }

  public Weapon() {}
  public Weapon(int id, WeaponType weapon) {
    weaponID = id;
    weaponType = weapon;
  }
  public Weapon(int id, WeaponType weapon, GunType gun, Vector3 tip, Vector3 dir) {
    weaponID = id;
    weaponType = weapon;
    gunType = gun;
    relativeMuzzleTip = tip;
    relativeMuzzleDirection = dir;
  }

  new public string ToString() {
    return weaponType.ToString() + "-" + gunType.ToString() + "-" + weaponID +
           "-" + transform.name;
  }

#if UNITY_EDITOR
  void OnDrawGizmosSelected() {
    if (weaponType == WeaponType.GUN && gunType == GunType.RIFLE ||
        gunType == GunType.PISTOL) {
      Vector3 tip = relativeMuzzleTip;
      Vector3 anchor = transform.position;
      Vector3 direction = relativeMuzzleDirection;
      Gizmos.color = new Color(1, 0, 0, 1f);
      Gizmos.DrawRay(transform.rotation * tip + anchor,
                     transform.rotation * direction * 0.1f);
      Gizmos.DrawSphere(transform.rotation * tip + anchor, 0.01f);
    }
  }
#endif
}
