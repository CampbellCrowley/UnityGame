// Copyright (c) Campbell Crowley. All rights reserved.
// Author: Campbell Crowley (github@campbellcrowley.com)
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Building)), CanEditMultipleObjects]
public class BuildingEditor : Editor {
  public SerializedProperty floor_prop, ID_prop, doorPosition_prop,
                            dimensions_prop, center_prop;

  void OnEnable() {
    ID_prop = serializedObject.FindProperty("ID");
    floor_prop = serializedObject.FindProperty("floor");
    doorPosition_prop = serializedObject.FindProperty("doorPosition");
    dimensions_prop = serializedObject.FindProperty("dimensions");
    center_prop = serializedObject.FindProperty("centerOffset");
  }

  public override void OnInspectorGUI() {
    serializedObject.Update();

    EditorGUILayout.PropertyField(floor_prop);

    Building.Floor fl = (Building.Floor)floor_prop.enumValueIndex;

    EditorGUILayout.PropertyField(ID_prop, new GUIContent("ID"));
    EditorGUILayout.PropertyField(center_prop,
                                  new GUIContent("Center Offset"));
    EditorGUILayout.PropertyField(dimensions_prop,
                                  new GUIContent("Dimensions"));

    switch (fl) {
      case Building.Floor.GROUND:
        EditorGUILayout.PropertyField(doorPosition_prop,
                                      new GUIContent("Door Position"));
        break;
      case Building.Floor.MIDDLE:
      case Building.Floor.ROOF:
      case Building.Floor.COMPLETE:
      default:
        break;
    }
    serializedObject.ApplyModifiedProperties();
  }
}
