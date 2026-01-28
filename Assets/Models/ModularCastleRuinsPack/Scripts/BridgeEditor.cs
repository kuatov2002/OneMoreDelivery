using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BridgeController))]
[CanEditMultipleObjects]
public class BridgeEditor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI () {
        DrawDefaultInspector();
        BridgeController bridge = (BridgeController) target;
        bridge.setSize();
        if(GUILayout.Button("Increase Size")) {
            bridge.increaseSize(true);
        }
        if(GUILayout.Button("Decrease Size")) {
            bridge.decreaseSize(true);
        }
        if(GUILayout.Button("Increase Height")) {
            bridge.updateHeight(1);
        }
        if(GUILayout.Button("Decrease Height")) {
            bridge.updateHeight(-1);
        }   
    }
}
