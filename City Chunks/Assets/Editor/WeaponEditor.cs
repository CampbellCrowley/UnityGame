// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Weapon)), CanEditMultipleObjects]
public class WeaponEditor : Editor {
  public SerializedProperty weaponID_prop, weaponType_prop, gunType_prop,
        relativeMuzzleTip_prop, relativeMuzzleDirection_prop;

  void OnEnable() {
      weaponID_prop = serializedObject.FindProperty("weaponID");
      weaponType_prop = serializedObject.FindProperty("weaponType");
      gunType_prop = serializedObject.FindProperty("gunType");
      relativeMuzzleTip_prop =
          serializedObject.FindProperty("relativeMuzzleTip");
      relativeMuzzleDirection_prop =
          serializedObject.FindProperty("relativeMuzzleDirection");
  }

  public override void OnInspectorGUI() {
    serializedObject.Update();

    EditorGUILayout.PropertyField(weaponID_prop);
    if (weaponID_prop.intValue != 0) {
      EditorGUILayout.PropertyField(weaponType_prop,
                                    new GUIContent("Weapon Type"));
    }

    Weapon.WeaponType weaponType =
        (Weapon.WeaponType)weaponType_prop.enumValueIndex;
    Weapon.GunType gunType = (Weapon.GunType)gunType_prop.enumValueIndex;

    relativeMuzzleDirection_prop.vector3Value =
        Vector3.Normalize(relativeMuzzleDirection_prop.vector3Value);

    switch(weaponType) {
      case Weapon.WeaponType.UNARMED:
        break;
      case Weapon.WeaponType.GUN:
        EditorGUILayout.PropertyField(gunType_prop, new GUIContent("Gun Type"));
        switch(gunType) {
          case Weapon.GunType.RIFLE:
          case Weapon.GunType.PISTOL:
            EditorGUILayout.PropertyField(
                relativeMuzzleTip_prop,
                new GUIContent("Relative Muzzle Tip Position"));
            EditorGUILayout.PropertyField(
                relativeMuzzleDirection_prop,
                new GUIContent("Relative Muzzle Direction"));
            EditorUtility.SetDirty(target);
            break;
          default:
            break;
        }
        break;
      case Weapon.WeaponType.MELEE:
        break;
      default:
        break;
    }
    serializedObject.ApplyModifiedProperties();
    HandleUtility.Repaint();
  }
}
