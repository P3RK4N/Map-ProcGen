using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;

    public void drawMap1D(float[,] noiseMap) 
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        Color[] colorMap = new Color[height*width];

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                colorMap[width * x + y] = Color.Lerp(Color.black, Color.white, noiseMap[x,y]);
            }

        texture.SetPixels(colorMap);
        texture.Apply();

        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(100, 1, 100);
    }

    public void drawMap3D(Color[,] map) 
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        Color[] colorMap = new Color[height*width];

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                Color col = map[x, y];
                colorMap[width * x + y] = col;
            }

        texture.SetPixels(colorMap);
        texture.Apply();

        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(100, 1, 100);
    }
}
