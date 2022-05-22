using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridGenerator : MonoBehaviour
{
    public float tileSize = 0.16f;
    public GameObject[] tiles;

    MapGenerator mapGen;

    Color[,] island;
    int width;
    int height;

    //TYPES OF TILES
    static List<bool[,]> compositions = new List<bool[,]>
    {
        //All sides filled
        new bool[,]
        {
            {true, true, true},
            {true, true, true},
            {true, true, true}
        },

        //One corner missing
        new bool[,]
        {
            {true, true, true},
            {true, true, true},
            {true, true, false}
        },

        //Two close corners missing
        new bool[,]
        {
            {true, true, true},
            {true, true, true},
            {false, true, false}
        },

        //Two far corners missing
        new bool[,]
        {
            {false, true, true},
            {true, true, true},
            {true, true, false}
        },

        //Three corners missing
        new bool[,]
        {
            {false, true, true},
            {true, true, true},
            {false, true, false}
        },

        //One edge missing
        new bool[,]
        {
            {false, true, true},
            {false, true, true},
            {false, true, true}
        },

        //Two close edges missing
        new bool[,]
        {
            {true, true, false},
            {true, true, false},
            {false, false, false}
        },

        //Two far edges missing
        new bool[,]
        {
            {false, true, false},
            {false, true, false},
            {false, true, false}
        },

        //Three edges missing
        new bool[,]
        {
            {false, false, false},
            {true, true, false},
            {false, false, false}
        },

        //One corner, one edge missing clockwise
        new bool[,]
        {
            {false, true, true},
            {false, true, true},
            {false, true, false}
        },

        //One corner, one edge missing counter-clockwise
        new bool[,]
        {
            {false, true, false},
            {false, true, true},
            {false, true, true}
        },

        //Two corners, opposite edge missing
        new bool[,]
        {
            {false, true, false},
            {true, true, false},
            {false, true, false}
        },

        //Two edges, opposite corner missing
        new bool[,]
        {
            {false, false, false},
            {false, true, true},
            {false, true, false}
        }
    };

    public void putTiles()
    {
        mapGen = FindObjectOfType<MapGenerator>();
        clearPrevIsland();
        generateIsland();
        fillTilemap();
    }

    void generateIsland()
    {
        island = mapGen.generateIslandMap();
    }

    void fillTilemap()
    {
        width = island.GetLength(0);
        height = island.GetLength(1);

        for(int i = 0; i < width; i++)
            for(int j = 0; j < height; j++)
                tileAutomata(new Vector2Int(i,j));
    }

    void tileAutomata(Vector2Int pos)
    {
        List<GameObject> tilesAt = new List<GameObject>();
        if(isSea(pos)) 
        {
            GameObject tile = Instantiate(tiles[0], transform);
            tile.GetComponent<SpriteRenderer>().sortingOrder = -10;
            tilesAt.Add(tile);
        }
        else
        {
            bool[,] around = new bool[3,3];
            
            for(int i = pos.x - 1; i <= pos.x + 1; i++)
                for(int j = pos.y - 1; j <= pos.y + 1; j++)
                {
                    if(i < 0 || j < 0 || i >= width || j >= height || isSea(new Vector2Int(i,j))) around[pos.x+1-i, pos.y+1-j] = false;
                    else around[pos.x+1-i, pos.y+1-j] = true;
                }

            cleanAround(around);
            tilesAt.AddRange(getTileType(around));
        }

        Vector3 absolutePos = new Vector3(pos.x * tileSize, pos.y * tileSize, 0);

        foreach (GameObject tile in tilesAt)
        {
            tile.transform.localPosition = absolutePos;
        }
    }

    List<GameObject> getTileType(bool[,] around)
    {
        if(isEqual(around, compositions[0])) 
        {
            GameObject tile = Instantiate(tiles[1], transform);
            return new List<GameObject>{tile};
        }
        else
        {
            for(int i = 1; i < compositions.Count; i++)
            {
                for(int rotation = 0; rotation < 4; rotation++)
                {
                    if(isEqual(around, compositions[i]))
                    {
                        GameObject groundTile = Instantiate(tiles[i+1], transform);
                        groundTile.transform.localEulerAngles = new Vector3(0, 0, rotation * 90f);

                        GameObject seaTile = Instantiate(tiles[0], transform);
                        seaTile.GetComponent<SpriteRenderer>().sortingOrder = -10;
                        return new List<GameObject>{groundTile, seaTile};
                    }
                    else
                    {
                        rotate90Clockwise(around, 3);
                    }
                }
            }
        }
        GameObject errorTile = Instantiate(tiles[1], transform);
        return new List<GameObject>{errorTile};
    }

    public void clearPrevIsland()
    {
        int count = transform.childCount;
        for(int i = 0; i < count; i++)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    bool isEqual(bool[,] first, bool[,] second)
    {
        for(int i = 0; i < 3; i++)
            for(int j = 0; j < 3; j++)
            {
                if(first[i,j] != second[i,j]) return false;
            }
        return true;
    }

    bool isSea(Vector2Int pos)
    {
        return !(island[pos.x, pos.y].r != 0f || island[pos.x, pos.y].g != 0f || island[pos.x, pos.y].b != 0f);
    }

    public void rotate90Clockwise(bool[,] matrix, int n)
    {
        for (int i = 0; i < n/2; i++) 
            for (int j = 0; j < n/2 + n%2; j++) 
            {
                bool tmp = matrix[i,j];
                matrix[i,j] = matrix[n-1-j, i];
                matrix[n-1-j, i] = matrix[n-1-i, n-1-j];
                matrix[n-1-i, n-1-j] = matrix[j, n-1-i];
                matrix[j, n-1-i] = tmp;
            }
    }

    void printMatrix(bool[,] matrix)
    {
        string mat = "";
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                mat += string.Format("{0} ", matrix[i,j]);
            }
            mat += "\n";
        }
        print(mat);
    }

    void cleanAround(bool[,] around)
    {
        if(around[0,1] == false)
        {
            around[0,0] = false;
            around[0,2] = false;
        }
        if(around[1,0] == false)
        {
            around[0,0] = false;
            around[2,0] = false;
        }
        if(around[2,1] == false)
        {
            around[2,0] = false;
            around[2,2] = false;
        }
        if(around[1,2] == false)
        {
            around[0,2] = false;
            around[2,2] = false;
        }
    }

    void printIsland()
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
