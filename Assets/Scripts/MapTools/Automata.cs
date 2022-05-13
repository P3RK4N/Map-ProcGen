using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Automata
{
    //Smoothens edges and makes it look more like a cave edge
    //Works with floats with noiseLimit between 0 and 1
    public static void applyCellularAutomata(float[,] noiseMap, int iterations, float noiseLimit, int neighbourLimit = 4)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        for(int iter = 0; iter < iterations; iter++)
        {
            float[,] tmpGrid = (float[,]) noiseMap.Clone();

            for(int x = 0; x < width; x++)
                for(int y = 0; y < height; y++)
                {
                    int walls = 0;

                    for(int i = x-1; i < x+2; i++)
                        for(int j = y-1; j < y+2; j++)
                        {
                            if(i >= 0 && i < width && j >= 0 && j < height)
                            {
                                if(tmpGrid[i,j] < noiseLimit)
                                {
                                    walls += 1;
                                }
                            }
                            else
                            {
                                walls += 1;
                            }
                        }
                    
                    if(walls > neighbourLimit)
                    {
                        noiseMap[x,y] = 0f;
                    }
                    else
                    {
                        noiseMap[x,y] = 1f;
                    }
                }
        }
    }

    //Smoothens edges and makes it look more like a cave edge
    //Works with integers with 0 and 1 only
    public static void applyCellularAutomata(int[,] noiseMap, int iterations, int neighbourLimit = 4)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        for(int iter = 0; iter < iterations; iter++)
        {
            float[,] tmpGrid = (float[,]) noiseMap.Clone();

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
                    
                    if(walls > neighbourLimit)
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
