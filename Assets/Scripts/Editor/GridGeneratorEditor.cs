using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (GridGenerator))]
public class GridGeneratorEditor : Editor
{
    public override void OnInspectorGUI() 
    {
        GridGenerator grid = (GridGenerator)target;

        DrawDefaultInspector();

        if(GUILayout.Button("Put tiles")) grid.putTiles();

        if(GUILayout.Button("Clear tiles")) grid.clearPrevIsland();
    }
}
