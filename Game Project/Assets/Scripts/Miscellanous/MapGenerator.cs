using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Space]
    [Header("Seed (0 => Random)")]
    public int seed;
    [Space]
    [Header("Osnovno")]
    public int mapWidth;
    public int mapHeight;
    [Space]
    [Header("Dodatno - Perlin")]
    public Vector2 offset;
    public float scale;
    public int octaves;
    public float persistance;
    public float lacunarity;
    [Space]
    [Header("Humidity")]
    public Vector2 humidityOffset;
    public float humidityScale;
    public int humidityOctaves;
    public float humidityPersistance;
    public float humidityLacunarity;
    [Space]
    [Header("Dodatno - voronoi")]
    public int blockSize;
    [Space]
    [Header("Miscellanous")]
    public bool autoUpdate;
    [Range(0f,1f)] public float noiseLimit; 

    public enum MAPS
    {
        NOISE = 0,
        PERLIN = 1,
        ISLAND = 2,
        VORONOI = 3,
        HUMIDITY = 4
    }

    public MAPS currentMap;
    
    float[,] noiseMap;

    public void generateRadialMask() 
    {
        currentMap = MAPS.NOISE;
        noiseMap = Noise.generateRadialMask(mapWidth, mapHeight);
        displayMap1D();
    }

    public void generateTemperatureMask()
    {
        noiseMap = Noise.generateTemperatureMask(mapWidth, mapHeight);
        displayMap1D();
    }

    public void generateHumidityMask()
    {
        currentMap = MAPS.HUMIDITY;
        noiseMap = Noise.generateHumidityMask(mapWidth, mapHeight, seed, humidityScale, humidityOctaves, humidityPersistance, humidityLacunarity);
        displayMap1D();
    }

    public void generatePerlinNoiseMap()
    {
        currentMap = MAPS.PERLIN;
        noiseMap = Noise.generatePerlinNoise(mapWidth, mapHeight, seed, offset, scale, octaves, persistance, lacunarity);
        displayMap1D();
    }

    public void generateVoronoiMap()
    {
        currentMap = MAPS.VORONOI;
        noiseMap = Noise.generateVoronoiNoise(mapWidth, mapHeight, blockSize, seed);
        displayMap1D();
    }

    //DEFAULT VALUES
    //Offset 5.46, 54.89
    //Scale (width/100) * 36.8 
    //Octaves 4
    //Persistance -0.5
    //Lacunarity 2.42
    //Block size = 21
    //Noise limit = 0.061

    public void generateIslandMap()
    {
        currentMap = MAPS.ISLAND;
        var colorMap = Noise.generateIsland(mapWidth, mapHeight, seed, (int)(mapWidth / 100f * blockSize), offset, noiseLimit, mapWidth / 100f * scale, octaves, persistance, lacunarity);
        
        displayMap3D(colorMap);
    }

    public void applyCellularAutomata()
    {
        applyCellularAutomata(1);
    }

    public void applyCellularAutomata(int iterations)
    {
        if(noiseMap == null) return;

        Noise.applyCellularAutomata(noiseMap, 1, noiseLimit);

        displayMap1D();
    }

    public void applyLloydRelaxation()
    {
        if(noiseMap == null) return;

        Noise.applyLloydRelaxation(noiseMap, blockSize);

        displayMap1D();
    }

    void displayMap1D()
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.drawMap1D(noiseMap);
    }

    void displayMap3D(Color[,] map)
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.drawMap3D(map);
    }
}
