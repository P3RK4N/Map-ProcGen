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
            if(mapGen.autoUpdate) refreshMap();
        }

        if(GUILayout.Button("Refresh"))
        {
            refreshMap();
        }

        if(GUILayout.Button("Generate radial filter"))
        {
            mapGen.generateRadialMask();
        }

        if(GUILayout.Button("Generate temperature filter"))
        {
            mapGen.generateTemperatureMask();
        }

        if(GUILayout.Button("Generate humidity filter"))
        {
            mapGen.generateHumidityMask();
        }

        if(GUILayout.Button("Generate perlin noise"))
        {
            mapGen.generatePerlinNoiseMap();
        }

        if(GUILayout.Button("Generate voronoi noise"))
        {
            mapGen.generateVoronoiMap();
        }

        if(GUILayout.Button("Generate island"))
        {
            mapGen.generateIslandMap();
        }

        if(GUILayout.Button("Apply cellular automata"))
        {
            mapGen.applyCellularAutomata();
        }

        if(GUILayout.Button("Apply Lloyd relaxation"))
        {
            mapGen.applyLloydRelaxation();
        }

        void refreshMap()
        {
            switch(mapGen.currentMap)
                {
                    case MapGenerator.MAPS.NOISE:
                        mapGen.generateRadialMask();
                        break;
                    case MapGenerator.MAPS.PERLIN:
                        mapGen.generatePerlinNoiseMap();
                        break;
                    case MapGenerator.MAPS.ISLAND:
                        mapGen.generateIslandMap();
                        break;
                    case MapGenerator.MAPS.VORONOI:
                        mapGen.generateVoronoiMap();
                        break;
                    case MapGenerator.MAPS.HUMIDITY:
                        mapGen.generateHumidityMask();
                        break;
                }
        }
    }
}
