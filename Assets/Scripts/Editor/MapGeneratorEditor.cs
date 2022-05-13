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

        if(DrawDefaultInspector()) if(mapGen.autoUpdate) refreshMap();

        if(GUILayout.Button("Refresh")) refreshMap();

        EditorGUILayout.Space(10);
        GUILayout.Label("Island");

        if(GUILayout.Button("Generate island")) mapGen.generateIslandMap();

        EditorGUILayout.Space(10);
        GUILayout.Label("Noises");

        if(GUILayout.Button("Generate perlin noise")) mapGen.generatePerlinNoiseMap();

        if(GUILayout.Button("Generate voronoi noise")) mapGen.generateVoronoiNoiseMap();

        EditorGUILayout.Space(10);
        GUILayout.Label("Masks");

        if(GUILayout.Button("Radial mask")) mapGen.generateRadialMask();

        EditorGUILayout.Space(10);
        GUILayout.Label("Map algorithms");

        if(GUILayout.Button("Apply cellular automata")) mapGen.applyCellularAutomata();

        if(GUILayout.Button("Apply Lloyd relaxation")) mapGen.applyLloydRelaxation();

        void refreshMap()
        {
            switch(mapGen.currentMap)
                {
                    case MapGenerator.MAPS.PERLIN:
                        mapGen.generatePerlinNoiseMap();
                        break;
                    case MapGenerator.MAPS.ISLAND:
                        mapGen.generateIslandMap();
                        break;
                    case MapGenerator.MAPS.VORONOI:
                        mapGen.generateVoronoiNoiseMap();
                        break;
                }
        }
    }
}
