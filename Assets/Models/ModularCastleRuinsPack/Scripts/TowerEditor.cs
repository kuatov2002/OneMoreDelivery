using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TowerController))]
[CanEditMultipleObjects]
public class TowerEditor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI () {

        DrawDefaultInspector();
        TowerController tower = (TowerController) target;
        tower.updateFloors();
        if(GUILayout.Button("Generate")) {
            tower.Generate();
        }
        if(GUILayout.Button("Increase Height")) {
            tower.addFloor(1);
            tower.updateTopPosition();
        }
        if(GUILayout.Button("Decrease Height")) {
            tower.removeFloor(-1);
        tower.updateTopPosition();
        }
         
    }
}
