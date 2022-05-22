using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Space]
    [Header("Seed (0 => Random)")]
    public int seed;

    [Space]
    [Header("Basic")]
    public int mapWidth;
    public int mapHeight;

    [Space]
    [Header("Additional - Perlin")]
    public Vector2 offset;
    public float scale;
    public int octaves;
    public float persistance;
    public float lacunarity;

    [Space]
    [Header("Additional - voronoi")]
    public int blockSize;

    [Space]
    [Header("Miscellanous")]
    public bool autoUpdate;
    [Range(0f,1f)] public float noiseLimit; 

    public enum MAPS
    {
        NONE = 0,
        PERLIN = 1,
        ISLAND = 2,
        VORONOI = 3
    }

    public MAPS currentMap;
    
    float[,] map1D;
    Color[,] map3D;

    //DEFAULT VALUES
    //Offset 5.46, 54.89
    //Scale (width/100) * 36.8 
    //Octaves 4
    //Persistance -0.5
    //Lacunarity 2.42
    //Block size = 21-23
    //Noise limit = 0.061

    //blockSize relative to mapWidth
    //scale relative to mapWidth
    public Color[,] generateIslandMap()
    {
        currentMap = MAPS.ISLAND;
        map3D = IslandGenerator.generateIsland(mapWidth, mapHeight, seed, (int)(mapWidth / 100f * blockSize), offset, noiseLimit, mapWidth / 100f * scale, octaves, persistance, lacunarity);
        
        displayMap3D();
        return map3D;
    }

    public void generatePerlinNoiseMap()
    {
        currentMap = MAPS.PERLIN;
        map1D = Noise.generatePerlinNoise(mapWidth, mapHeight, seed, offset, scale, octaves, persistance, lacunarity);
        displayMap1D();
    }

    public void generateVoronoiNoiseMap()
    {
        currentMap = MAPS.VORONOI;
        map1D = Noise.generateVoronoiNoise(mapWidth, mapHeight, blockSize, seed);
        displayMap1D();
    }

    public void generateRadialMask() 
    {
        displayMap1D(Mask.radialMask(mapWidth, mapHeight));
    }

    public void applyCellularAutomata()
    {
        applyCellularAutomata(1);
    }

    public void applyCellularAutomata(int iterations)
    {
        if(map1D == null) throw new System.Exception("Map1D is null");
        if(currentMap == MAPS.VORONOI) throw new System.Exception("Cannot apply cell automata to voronoi noise map");

        Automata.applyCellularAutomata(map1D, iterations, noiseLimit);
        displayMap1D();
    }

    public void applyLloydRelaxation()
    {
        if(map1D == null) throw new System.Exception("Map1D is null");
        if(currentMap == MAPS.PERLIN) throw new System.Exception("Cannot apply Lloyd relaxation to perlin noise map");

        IslandGenerator.applyLloydRelaxation(map1D, blockSize);
        displayMap1D();
    }

    void displayMap1D()
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(map1D != null) display.drawMap1D(map1D);
        else throw new System.Exception("Map1D is null");
    }

    void displayMap1D(float[,] map)
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.drawMap1D(map);
    }

    void displayMap3D()
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(map3D != null) display.drawMap3D(map3D);
        else throw new System.Exception("Map3D is null");
    }

    void displayMap3D(Color[,] map)
    {
        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.drawMap3D(map);
    }
}
