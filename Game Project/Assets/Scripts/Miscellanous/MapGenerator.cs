using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Space]
    [Header("Osnovno")]
    public int mapWidth;
    public int mapHeight;
    [Space]
    [Header("Dodatno")]
    public float scale;
    public int octaves;
    public float persistance;
    public float lacunarity;
    [Space]
    [Header("Miscellanous")]
    public bool autoUpdate;



    bool currentInt = false;

    int[,] noiseMap;

    public void generateMap() 
    {
        noiseMap = Noise.generateNoiseMap(mapWidth, mapHeight);
        currentInt = true;
        displayMap();
    }

    public void generatePerlinNoiseMap()
    {
        float[,] perlinNoiseMap = Noise.generatePerlinNoise(mapWidth, mapHeight, scale, octaves, persistance, lacunarity);
        currentInt = false;

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.drawNoiseMap(ref perlinNoiseMap);
    }

    public void generateCellularAutomataMap()
    {
        generateCellularAutomataMap(1);
    }

    public void generateCellularAutomataMap(int iterations)
    {
        if(noiseMap == null) return;
        if(!currentInt)
        {
            Debug.Log("You need integer map to apply cellular automata!");
            return;
        }
        Noise.applyCellularAutomata(ref noiseMap, 1);

        displayMap();
    }

    void displayMap()
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.drawNoiseMap(ref noiseMap);
    }
}
