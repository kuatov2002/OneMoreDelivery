using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WallController))]
[CanEditMultipleObjects]
public class WallEditor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI () {
        DrawDefaultInspector();
        WallController wall = (WallController) target;
        wall.setSize();
        if(GUILayout.Button("Increase Size")) {
            wall.increaseSize(true);
        }
        if(GUILayout.Button("Decrease Size")) {
            wall.decreaseSize(true);
        }
    }
}
