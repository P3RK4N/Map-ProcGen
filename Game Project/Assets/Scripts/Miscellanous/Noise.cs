using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    //2D -> up, right, down, left
    private static Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0,1),
            new Vector2Int(1,0),
            new Vector2Int(0,-1),
            new Vector2Int(-1,0)
        };
    
    //Ensures that big island spawns in the center
    public static float[,] generateRadialMask(int width, int height)
    {
        float[,] noiseMap = new float[width,height];

        Vector2 center = new Vector2(width/2f, height/2f);

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float widthOffset = Mathf.Abs(center[0] - x) / (width / 2f);
                float heightOffset = Mathf.Abs(center[1] - y) / (height / 2f);
                float distanceFromCenter = Mathf.Sqrt(widthOffset * widthOffset + heightOffset * heightOffset);
                // float noiseValue = Random.Range(0.0f,1.0f) * distanceFromCenter;
                float noiseValue = distanceFromCenter;
                noiseMap[x,y] = 1 - noiseValue;
            }

        return noiseMap;
    }

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

    //Generate island with radial mask and perlin noise
    public static Color[,] generateIsland(int width, int height, int seed, int blockSize, Vector2 offset, float noiseLimit, float scale, int octaves, float persistance, float lacunarity)
    {
        bool valid = false;
        bool random = seed == 0;
        float[,] noiseMap = generateRadialMask(width, height);
        float[,] perlinNoiseMap;
        float[,] islandMap = new float[width,height];

        //While smaller than 30% of map create next one
        while(!valid)
        {
            seed = random ? Random.Range(1, 2147483647) : seed;
            perlinNoiseMap = generatePerlinNoise(width, height, seed, offset, scale, octaves, persistance, lacunarity);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    float val = noiseMap[x, y] * perlinNoiseMap[x, y];
                    val = val < noiseLimit ? 0f : 1f;
                    islandMap[x, y] = val;
                }
            applyCellularAutomata(islandMap, 1, noiseLimit);
            islandMap = clearIslands(islandMap, ref valid);

            if(!random && !valid)
                seed++;
        }

        Debug.Log(seed);

        // //Get dictionary cellCenter -> cells
        float[,] voronoiNoise = generateVoronoiNoise(width, height, blockSize, seed);

        // //Apply regions to island
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                if(islandMap[x, y] > 0f)
                    islandMap[x, y] *= voronoiNoise[x, y];

        applyLloydRelaxation(islandMap, blockSize);
        Dictionary<Vector2Int, List<Vector2Int>> regions = applyLloydRelaxation(islandMap, blockSize);


    	//INSERT FIX FOR SPLIT AREA FUNCTION

        // mergeRegions(islandMap, regions);

        applySmallLloydRelaxation(islandMap, regions);

        //POTENTIALLY INSERT FIX FOR SPLIT HERE TOO

        Debug.Log(checkDict(islandMap, regions));

        // applyVoronoiBorder(islandMap, regions, seed);

        //POTENTIONALLY INSERT FIX FOR SPLIT HERE TOO

        Color[,] biomeMap = assignBiomes2(islandMap, regions);

        return biomeMap;
    }


    //NOTE: 

    //1) HOW TO FIX SPLITTED AREA ---> BEFORE MERGING MAKE FUNCTION THAT CHECKS ALL REGIONS AND IF THEY ARE SEPARATED, SEPARATE THEM IN 2 NEW REGIONS, THAT WAY THEY WILL BE MERGED WITH CLOSER ONES

    //2) MAKE SURE CHECKDICT RETURNS TRUE BETWEEN AAAAALLLLLL FUNCTIONS after second lloyd relaxation

    //3) FIX VORONOI BORDER (IF IT NEEDS TO BE FIXED AFTER FIRST 2 STEPS)


    //Generate temperature map
    public static float[,] generateTemperatureMask(int width, int height)
    {
        float[,] tempMap = new float[width, height];

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                tempMap[x, y] = Mathf.Clamp(0.8f - Mathf.SmoothStep(0f, 1f, 1f * x / width), 0f, 1f);
        
        return tempMap;
    }

    //Generate humidity map
    public static float[,] generateHumidityMask(int width, int height, int seed, float scale = 15.55f, int octaves = 4, float persistance = 0.06f, float lacunarity = 1.24f)
    {
        scale = 1f * width / 100 * scale;
        return generatePerlinNoise(width, height, seed, new Vector2(0f, 0f), scale, octaves, persistance, lacunarity);
    }

    //Assign biomes : using temperature map and humidity map
    public static Vector3[,] assignBiomes(Dictionary<Vector2Int,List<Vector2Int>> regionsMap, int seed, int width, int height)
    {
        Vector3[,] biomeMap = new Vector3[width, height];
        
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                biomeMap[x, y] = new Vector3(0f, 0f, 0f);

        float[,] tempMask = generateTemperatureMask(width, height);
        float[,] humidityMask = generateHumidityMask(width, height, seed);

        foreach (Vector2Int middlePoint in regionsMap.Keys)
        {
            //Desert
            if(tempMask[middlePoint.x, middlePoint.y] >= 0.67f && humidityMask[middlePoint.x, middlePoint.y] < 0.4f)
                biomeMap[middlePoint.x, middlePoint.y] = new Vector3(1, 0.92f, 0.016f);
            //Dark forest
            else if(tempMask[middlePoint.x, middlePoint.y] >= 0.67f && humidityMask[middlePoint.x, middlePoint.y] >= 0.4f)
                biomeMap[middlePoint.x, middlePoint.y] = new Vector3(0, 0.33f, 0);
            //Light forest
            else if(tempMask[middlePoint.x, middlePoint.y] > 0.33f && tempMask[middlePoint.x, middlePoint.y] <= 0.67f && humidityMask[middlePoint.x, middlePoint.y] > 0.0f && humidityMask[middlePoint.x, middlePoint.y] <= 1f)
                biomeMap[middlePoint.x, middlePoint.y] = new Vector3(0.56f, 0.92f, 0.55f);
            //Taiga
            else if(tempMask[middlePoint.x, middlePoint.y] <= 0.33f && tempMask[middlePoint.x, middlePoint.y] > 0.15f && humidityMask[middlePoint.x, middlePoint.y] <= 0.60f)
                biomeMap[middlePoint.x, middlePoint.y] = new Vector3(0.39f, 0.26f, 0.13f);
            //Snow
            else//(tempMask[middlePoint.x, middlePoint.y] <= 0.33f && humidityMask[middlePoint.x, middlePoint.y] >= 0.6f)
                biomeMap[middlePoint.x, middlePoint.y] = new Vector3(0.9f, 0.9f, 1f);
        }

        foreach (KeyValuePair<Vector2Int,List<Vector2Int>> pair in regionsMap)
            foreach (Vector2Int point in pair.Value)
                biomeMap[point.x, point.y] = biomeMap[pair.Key.x, pair.Key.y];

        return biomeMap;
    }

    //Assign biomes 2 : random
    public static Color[,] assignBiomes2(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
    {
        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        Color[,] biomeMap = new Color[width, height];

        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> region in regions)
        {
            Color col = Random.ColorHSV();
            while(col.r < 0.1f && col.g < 0.1f && col.b < 0.1f) col = Random.ColorHSV();
            col.a = 1f;
            foreach(Vector2Int point in region.Value)
                biomeMap[point.x, point.y] = col;
        }

        return biomeMap;
    }

    //Removes smaller islands, keeps largest
    public static float[,] clearIslands(float[,] islandMap, ref bool valid)
    {
        //Check if tile is used in O(1) time
        HashSet<Vector2Int> usedTiles = new HashSet<Vector2Int>();
        //Array of islands(arrays of land)
        List<List<Vector2Int>> islands = new List<List<Vector2Int>>();
        
        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        //Finding all islands, skipping sea and used, DFS on unused land
        for(int x = 0; x < width; x++)
            for(int y = 0; y < width; y++)
            {
                Vector2Int current = new Vector2Int(x,y);
                if(usedTiles.Contains(current) || islandMap[current.x,current.y] == 0f) continue;
                else
                {
                    List<Vector2Int> DFS = new List<Vector2Int>();
                    List<Vector2Int> newIsland = new List<Vector2Int>();

                    usedTiles.Add(current);
                    DFS.Add(current);

                    while (DFS.Count > 0)
                    {
                        Vector2Int tmp = DFS[DFS.Count - 1];
                        DFS.RemoveAt(DFS.Count - 1);
                        newIsland.Add(tmp);

                        foreach (Vector2Int dir in directions)
                        {
                            Vector2Int newPos = tmp + dir;
                            //skip if used, invalid or sea
                            if(usedTiles.Contains(newPos) || newPos.x < 0 || newPos.x >= width || newPos.y < 0 || newPos.y >= height || islandMap[newPos.x,newPos.y] == 0f) continue;

                            usedTiles.Add(newPos);
                            DFS.Add(newPos);
                        }
                    }

                    islands.Add(newIsland);
                }
            }

        islands.Sort((island1, island2) => -island1.Count.CompareTo(island2.Count));
        
        //Delete smaller islands
        for(int i = 1; i < islands.Count; i++)
        {
            foreach (Vector2Int pos in islands[i])
            {
                islandMap[pos.x,pos.y] = 0f;   
            }
        }

        float area = (1f * islands[0].Count / width / height) * 100;

        // UnityEngine.Debug.Log(area);
        if(area < 30f) valid = false;
        else 
        {
            valid = true;
        }

        return islandMap;
    }
    
    //Merges to (amount = 6) regions
    public static void mergeRegions(float[,] islandMap, Dictionary<Vector2Int,List<Vector2Int>> regions, int amount = 6)
    {
        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        //Dictionary point -> middlePoint of region
        Dictionary<Vector2Int, Vector2Int> pointMiddlePoint = new Dictionary<Vector2Int, Vector2Int>();

        //Putting all points and mapping them to middlePoint
        foreach (KeyValuePair<Vector2Int,List<Vector2Int>> region in regions)
            foreach (Vector2Int point in region.Value)
                pointMiddlePoint[point] = region.Key;

        //Reducing number of regions until there are 6 only
        while(regions.Keys.Count > 6)
        {
            //Finding smallest region's middlePoint
            Vector2Int smallestMiddlePoint = new Vector2Int();
            int count = int.MaxValue;

            foreach (KeyValuePair<Vector2Int,List<Vector2Int>> region in regions)
                if(region.Value.Count < count)
                {
                    count = region.Value.Count;
                    smallestMiddlePoint = region.Key;
                }

            //Finding neighbours
            HashSet<Vector2Int> neighbourMiddlePoints = new HashSet<Vector2Int>();
            HashSet<Vector2Int> usedPoints = new HashSet<Vector2Int>();

            //DFS on smallest region
            List<Vector2Int> DFS = new List<Vector2Int>();
            DFS.Add(regions[smallestMiddlePoint][0]);
            usedPoints.Add(smallestMiddlePoint);

            while(DFS.Count > 0)
            {
                Vector2Int currentPoint = DFS[DFS.Count - 1];
                DFS.RemoveAt(DFS.Count - 1);

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int nextPoint = currentPoint + dir;
                    if(usedPoints.Contains(nextPoint) || nextPoint.x < 0 || nextPoint.y < 0 || nextPoint.x >= width || nextPoint.y >= height || islandMap[nextPoint.x, nextPoint.y] == 0f) 
                        continue;
                    else if(pointMiddlePoint[currentPoint] != pointMiddlePoint[nextPoint])
                    {
                        // Debug.Log(string.Format("Next point is: {0}", nextPoint));
                        neighbourMiddlePoints.Add(pointMiddlePoint[nextPoint]);
                    }
                    else
                        DFS.Add(nextPoint);
                    usedPoints.Add(nextPoint);
                }
            }

            //Finding smallest neighbourMiddlePoint
            Vector2Int smallestNeighbourMiddlePoint = new Vector2Int();
            count = int.MaxValue;

            foreach (Vector2Int middlePoint in neighbourMiddlePoints)
                if(regions[middlePoint].Count < count)
                {
                    count = regions[middlePoint].Count;
                    smallestNeighbourMiddlePoint = middlePoint;
                }

            // Debug.Log(string.Format("Smallest middle point is: {0}", smallestMiddlePoint));
            //Merge smallest with to smallest neighbour
            foreach (Vector2Int point in regions[smallestMiddlePoint])
            {
                //Adds point to new region
                regions[smallestNeighbourMiddlePoint].Add(point);
                //Maps point to new middlePoint
                pointMiddlePoint[point] = smallestNeighbourMiddlePoint;
                //Changes value to new region
                islandMap[point.x, point.y] = islandMap[smallestNeighbourMiddlePoint.x, smallestNeighbourMiddlePoint.y];
            }
            //removes old smallest region
            regions.Remove(smallestMiddlePoint);
        }
    }

    //Balances out sizes of areas by taking centre of mass and giving closest area to them
    //Returns dictionary MIDDLEPOINT -> list(REGION_POINTS)
    public static Dictionary<Vector2Int, List<Vector2Int>> applyLloydRelaxation(float[,] islandMap, int blockSize)
    {
        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        int widthBlocks = width / blockSize;
        int heightBlocks = height / blockSize;

        //middlePoint -> points in region (to return)
        Dictionary<Vector2Int, List<Vector2Int>> regions = new Dictionary<Vector2Int, List<Vector2Int>>();

        //color -> list of points in area (used to find middlePoints)
        Dictionary<float, List<Vector2Int>> areas = new Dictionary<float, List<Vector2Int>>();

        //newMiddlePoint -> color (to decide color of region)
        Dictionary<Vector2Int, float> middlePoints = new Dictionary<Vector2Int, float>();

        //Blocks -> newMiddlePoints in them
        Dictionary<Vector2Int, List<Vector2Int>> blocks = new Dictionary<Vector2Int, List<Vector2Int>>();
        
        //Puts all regions into dictionary with color as key
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                if(islandMap[x, y] == 0f) continue;
                else if(areas.ContainsKey(islandMap[x, y])) areas[islandMap[x, y]].Add(new Vector2Int(x, y));
                else
                {
                    areas[islandMap[x, y]] = new List<Vector2Int>();
                    areas[islandMap[x, y]].Add(new Vector2Int(x, y));
                }
            }
        
        //Finds new middlePoints and maps them to color and puts in block
        foreach (KeyValuePair<float, List<Vector2Int>> area in areas)
        {
            Vector2Int sum = new Vector2Int(0, 0);
            foreach (Vector2Int point in area.Value)
                sum += point;
            
            sum /= area.Value.Count;

            middlePoints[sum] = area.Key;

            //Putting middlePoint in corresponding block
            int blockX = sum.x / blockSize;
            int blockY = sum.y / blockSize;

            Vector2Int block = new Vector2Int(blockX, blockY);

            if(!blocks.ContainsKey(block)) blocks[block] = new List<Vector2Int>();

            blocks[block].Add(sum);
        }

        //Find closest middlePoint for each dot that is not sea
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                if(islandMap[x, y] == 0f) continue;

                Vector2Int currentPos = new Vector2Int(x,y);

                int blockX = x / blockSize;
                int blockY = y / blockSize;

                int left = Mathf.Max(0, blockX - 1);
                int right = Mathf.Min(widthBlocks - 1, blockX + 1);

                int down = Mathf.Max(0, blockY - 1);
                int up = Mathf.Min(heightBlocks - 1, blockY + 1);
                
                float minDist = float.MaxValue;
                Vector2Int closestMiddlePoint = new Vector2Int();

                //For all middlePoints in 9 surrounding blocks, find closest one
                for(int posX = left; posX <= right; posX++)
                    for(int posY = down; posY <= up; posY++)
                    {
                        Vector2Int block = new Vector2Int(posX, posY);
                        if(blocks.ContainsKey(block))
                            foreach (Vector2Int middlePoint in blocks[block])
                            {
                                float newDistance = (middlePoint.x - currentPos.x) * (middlePoint.x - currentPos.x) + (middlePoint.y - currentPos.y) * (middlePoint.y - currentPos.y);
                                    
                                if(newDistance < minDist)
                                {
                                    minDist = newDistance;
                                    closestMiddlePoint = middlePoint;
                                }
                            }
                    }
                //Give currentpoint color of closest middlePoint
                islandMap[currentPos.x, currentPos.y] = middlePoints[closestMiddlePoint];

                //Add point to its region List
                if(!regions.ContainsKey(closestMiddlePoint)) regions[closestMiddlePoint] = new List<Vector2Int>();
                regions[closestMiddlePoint].Add(currentPos);
            }
    
        return regions;
    }

    //Lloyd relaxation optimized (or better say, unoptimized, but works better for smaller cases) for small amount of regions (<10)
    //Modifies arguments
    public static void applySmallLloydRelaxation(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
    {
        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        //Old middlePoint mapped to its color
        Dictionary<Vector2Int, float> middlePoints = new Dictionary<Vector2Int, float>();

        //Will be swapped with old dictionary (regions)
        Dictionary<Vector2Int, List<Vector2Int>> newRegions = new Dictionary<Vector2Int, List<Vector2Int>>();

        //Fils newRegions with centres of mass as new middlePoints and maps them to list of closest points (region)
        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> region in regions)
        {
            Vector2Int avg = new Vector2Int();
            foreach (Vector2Int point in region.Value)
                avg += point;
            avg /= region.Value.Count;

            //Adds empty new region
            newRegions[avg] = new List<Vector2Int>();

            //Maps newMiddlePoint to the color of its predecessor
            middlePoints[avg] = islandMap[region.Value[0].x, region.Value[0].y];
        }

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                if(islandMap[x, y] == 0f) continue;

                float minDist = float.MaxValue;
                Vector2Int closestMiddlePoint = new Vector2Int();

                foreach (Vector2Int middlePoint in newRegions.Keys)
                {
                    float dist = (x - middlePoint.x) * (x - middlePoint.x) + (y - middlePoint.y) * (y - middlePoint.y);

                    if(dist < minDist)
                    {
                        minDist = dist;
                        closestMiddlePoint = middlePoint;
                    }
                }

                newRegions[closestMiddlePoint].Add(new Vector2Int(x, y));
                islandMap[x, y] = middlePoints[closestMiddlePoint];
            }
        
        regions = newRegions;
    }

    //Smoothens edges and makes it look more like a cave edge
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

    //Makes borders more random with smaller voronoi cells
    public static void applyVoronoiBorder(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions, int seed, int blockSize = 5)
    {
        long tmpSeed = ((long)seed + 337) * 312432;
        tmpSeed %= 2147483647;
        seed = (int)tmpSeed;

        System.Random rng = new System.Random(seed);

        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        int widthBlocks = width / blockSize;
        int heightBlocks = height / blockSize;
        
        Dictionary<Vector2Int, List<Vector2Int>> newRegions = new Dictionary<Vector2Int, List<Vector2Int>>();

        //Small points for randomizing borders mapped to their color
        Dictionary<Vector2Int, float> smallMiddlePoints = new Dictionary<Vector2Int, float>();

        //Region color to middlePoint
        Dictionary<float, Vector2Int> colorToPoint = new Dictionary<float, Vector2Int>();

        //Filling colorToPoint and initializing newRegions with old middlePoints
        foreach (Vector2Int middlePoint in regions.Keys)
        {
            //First point of area (to get color from it since middlePoint doesnt need to be that color)
            Vector2Int pos = regions[middlePoint][0];
            colorToPoint[islandMap[pos.x, pos.y]] = middlePoint;

            newRegions[middlePoint] = new List<Vector2Int>();
        }

        //Block -> smallMiddlePoint
        Vector2Int[,] blocks = new Vector2Int[width, height];

        //Filling smallMiddlePoints
        for(int blockX = 0; blockX < widthBlocks; blockX++)
            for(int blockY = 0; blockY < heightBlocks; blockY++)
            {
                //Choosing point
                Vector2Int smallMiddlePoint = new Vector2Int(rng.Next(0, blockSize) + blockX * blockSize, rng.Next(0, blockSize) + blockY * blockSize);

                //Giving it a value of that position on map
                smallMiddlePoints[smallMiddlePoint] = islandMap[smallMiddlePoint.x, smallMiddlePoint.y];

                //Putting it in its block
                blocks[blockX, blockY] = smallMiddlePoint;
            }

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                if(islandMap[x, y] == 0f) continue;

                Vector2Int currentPos = new Vector2Int(x, y);

                int blockX = x / blockSize;
                int blockY = y / blockSize;

                int left = Mathf.Max(0, blockX - 1);
                int right = Mathf.Min(widthBlocks - 1, blockX + 1);

                int down = Mathf.Max(0, blockY - 1);
                int up = Mathf.Min(heightBlocks - 1, blockY + 1);
                
                float minDist = float.MaxValue;
                Vector2Int closestPoint = new Vector2Int();
                
                for(int posX = left; posX <= right; posX++)
                    for(int posY = down; posY <= up; posY++)
                    {
                        Vector2Int smallMiddlePoint = blocks[posX, posY];
                        if(smallMiddlePoints[smallMiddlePoint] == 0f) continue;

                        float newDistance = (smallMiddlePoint.x - currentPos.x) * (smallMiddlePoint.x - currentPos.x) + (smallMiddlePoint.y - currentPos.y) * (smallMiddlePoint.y - currentPos.y);
                        if(newDistance < minDist)
                        {
                            minDist = newDistance;
                            closestPoint = smallMiddlePoint;
                        }
                    }
                
                //Choosing new color for each point
                islandMap[x, y] = smallMiddlePoints[closestPoint];
                
                //Adding that point to list of an old parent
                newRegions[colorToPoint[islandMap[x, y]]].Add(new Vector2Int(x, y));
            }

        regions = newRegions;
    }

    //Returns cell center pointing to its area
    public static Dictionary<Vector2Int, List<Vector2Int>> voronoiDict(int width, int height, int blockSize, int seed)
    {
        blockSize = Mathf.Max(1, blockSize);

        int widthBlock = width/blockSize;
        int heightBlock = height/blockSize;

        seed = seed == 0 ? Random.Range(1, 2147483647) : seed;
        Random.InitState(seed);
        System.Random rng = new System.Random(seed);

        Vector2Int[,] points = new Vector2Int[widthBlock, heightBlock];
        Dictionary<Vector2Int, List<Vector2Int>> cells = new Dictionary<Vector2Int, List<Vector2Int>>();

        for(int x = 0; x < widthBlock; x++)
            for(int y = 0; y < heightBlock; y++)
                points[x, y] = new Vector2Int(rng.Next(0, blockSize) + x * blockSize, rng.Next(0, blockSize) + y * blockSize);
        
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
                            closestPoint = point;
                        }
                    }

                if(!cells.ContainsKey(closestPoint))
                    cells[closestPoint] = new List<Vector2Int>();

                cells[closestPoint].Add(currentPos);
            }
        
        return cells;
    }

    public static bool checkDict(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
    {
        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> region in regions)
        {
            float val = islandMap[region.Value[0].x, region.Value[0].y];
            foreach (Vector2Int point in region.Value)
            {
                float nextVal = islandMap[point.x, point.y];
                if(val != nextVal) return false;
            }
        }
        return true;
    }
}
