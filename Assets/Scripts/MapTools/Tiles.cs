using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps;

public class Tiles : MonoBehaviour
{
    public TileBase tb1;
    public TileBase tb2;

    Tilemap map;
    MapGenerator mapGen;

    Color[,] island;

    public void putTiles()
    {
        map = GetComponent<Tilemap>();
        mapGen = FindObjectOfType<MapGenerator>();
        generateIsland();
        fillTilemap();
    }

    void generateIsland()
    {
        island = mapGen.generateIslandMap();
    }

    void fillTilemap()
    {
        int width = island.GetLength(0);
        int height = island.GetLength(1);

        Color tmp = new Color(0f, 0f, 0f, 0f);
        for(int i = 0; i < width; i++)
            for(int j = 0; j < height; j++)
            {
                map.SetTile(new Vector3Int(i, j, 0), island[i,j] == tmp ? tb2 : tb1);
            }
    }

    void printIsland(Color[,] island)
    {
        int width = island.GetLength(0);
        int height = island.GetLength(1);

        for(int i = 0; i < width; i++)
            for(int j = 0; j < height; j++)
            {
                print(island[i,j]);
            }
    }
}
