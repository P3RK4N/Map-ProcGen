using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI() 
    {
        MapGenerator mapGen = (MapGenerator)target;

        if(DrawDefaultInspector())
        {
            if(mapGen.autoUpdate)
            {
                mapGen.generatePerlinNoiseMap();
            }
        }

        if(GUILayout.Button("Generate Map"))
        {
            mapGen.generateMap();
        }

        if(GUILayout.Button("Generate perlin noise"))
        {
            mapGen.generatePerlinNoiseMap();
        }

        if(GUILayout.Button("Apply cellular automata"))
        {
            mapGen.generateCellularAutomataMap();
        }
    }
}
