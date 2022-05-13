using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Mask
{
   //Center = 1 ; Border = 0 ; sqrt falloff
   public static float[,] radialMask(int width, int height)
   {
      float[,] mask = new float[width,height];

      Vector2 center = new Vector2(width/2f, height/2f);

      for(int x = 0; x < width; x++)
         for(int y = 0; y < height; y++)
         {
               float widthOffset = Mathf.Abs(center[0] - x) / (width / 2f);
               float heightOffset = Mathf.Abs(center[1] - y) / (height / 2f);
               float distanceFromCenter = Mathf.Sqrt(widthOffset * widthOffset + heightOffset * heightOffset);
               float noiseValue = distanceFromCenter;
               mask[x,y] = 1 - noiseValue;
         }

      return mask;
   }
}