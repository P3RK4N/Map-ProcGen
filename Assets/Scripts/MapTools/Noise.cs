using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{   
    //Perlin noise
    public static float[,] generatePerlinNoise(int width, int height, int seed, Vector2 offset, float scale, int octaves, float persistance, float lacunarity)
    {
        float[,] perlinNoise = new float[width,height];

        //Generate random seed for each octave
        System.Random rng = new System.Random(seed == 0 ? Random.Range(1, 2147483647) : seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rng.Next(-10000, 10000) + offset.x;
            float offsetY = rng.Next(-10000, 10000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                float halfWidth = width / 2f;
                float halfHeight = height / 2f;

                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                perlinNoise[x,y] = noiseHeight;
            }
        
        return perlinNoise;
    }
    
    //Choose point inside each block and assign it area closest to it
    public static float[,] generateVoronoiNoise(int width, int height, int blockSize, int seed)
    {
        blockSize = Mathf.Max(1, blockSize);

        int widthBlock = width/blockSize;
        int heightBlock = height/blockSize;

        seed = seed == 0 ? Random.Range(1, 2147483647) : seed;
        Random.InitState(seed);
        System.Random rng = new System.Random(seed);

        Vector2Int[,] points = new Vector2Int[widthBlock, heightBlock];
        float[,] pointVals = new float[widthBlock, heightBlock];

        HashSet<float> usedVals = new HashSet<float>();

        for(int x = 0; x < widthBlock; x++)
            for(int y = 0; y < heightBlock; y++)
            {
                points[x, y] = new Vector2Int(rng.Next(0, blockSize) + x * blockSize, rng.Next(0, blockSize) + y * blockSize);

                //Make sure all areas are of different value
                float newVal = Random.Range(0.1f, 1.0f);
                while(usedVals.Contains(newVal)) newVal = Random.Range(0.1f, 1.0f);
                usedVals.Add(newVal);

                pointVals[x, y] = newVal;
            }
        
        float[,] noiseMap = new float[width,height];
        
        //Find the closest cell point
        for (int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                Vector2Int currentPos = new Vector2Int(x,y);

                int blockX = x / blockSize;
                int blockY = y / blockSize;

                int left = Mathf.Max(0, blockX - 1);
                int right = Mathf.Min(widthBlock - 1, blockX + 1);

                int down = Mathf.Max(0, blockY - 1);
                int up = Mathf.Min(heightBlock - 1, blockY + 1);
                
                float minDist = float.MaxValue;
                Vector2Int closestPoint = new Vector2Int();

                for(int posX = left; posX <= right; posX++)
                    for(int posY = down; posY <= up; posY++)
                    {
                        Vector2Int point = points[posX, posY];
                        float newDistance = (point.x - currentPos.x) * (point.x - currentPos.x) + (point.y - currentPos.y) * (point.y - currentPos.y);
                        if(newDistance < minDist)
                        {
                            minDist = newDistance;
                            closestPoint = new Vector2Int(posX, posY);
                        }
                    }
                noiseMap[currentPos.x, currentPos.y] = pointVals[closestPoint.x, closestPoint.y];
            }
        
        return noiseMap;
    }

}