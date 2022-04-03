using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static int[,] generateNoiseMap(int width, int height)
    {
        int[,] noiseMap = new int[width,height];

        Vector2 center = new Vector2(width/2f, height/2f);

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float widthOffset = Mathf.Abs(center[0] - x) / (width / 2f);
                float heightOffset = Mathf.Abs(center[1] - y) / (height / 2f);
                float distanceFromCenter = Mathf.Sqrt(widthOffset * widthOffset + heightOffset * heightOffset);
                noiseMap[x,y] = Mathf.RoundToInt(1 - Random.Range(0.0f,1.0f) * distanceFromCenter);
            }
        return noiseMap;
    }

    public static float[,] generatePerlinNoise(int width, int height, float scale, int octaves, float persistance, float lacunarity)
    {
        float[,] perlinNoise = new float[width,height];

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = x / scale * frequency;
                    float sampleY = y / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                perlinNoise[x,y] = noiseHeight;
            }
        
        return perlinNoise;
    }

    public static void applyCellularAutomata(ref int[,] noiseMap, int iterations)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        for(int iter = 0; iter < iterations; iter++)
        {
            int[,] tmpGrid = (int[,]) noiseMap.Clone();

            for(int x = 0; x < width; x++)
                for(int y = 0; y < height; y++)
                {
                    int walls = 0;

                    for(int i = x-1; i < x+2; i++)
                        for(int j = y-1; j < y+2; j++)
                        {
                            if(i >= 0 && i < width && j >= 0 && j < height)
                            {
                                if(tmpGrid[i,j] == 0)
                                {
                                    walls += 1;
                                }
                            }
                            else
                            {
                                walls += 1;
                            }
                        }
                    
                    if(walls > 4)
                    {
                        noiseMap[x,y] = 0;
                    }
                    else
                    {
                        noiseMap[x,y] = 1;
                    }
                }
        }
    }
}
