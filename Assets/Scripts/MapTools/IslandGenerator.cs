using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IslandGenerator
{
    //2D -> up, right, down, left
    private static Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0,1),
            new Vector2Int(1,0),
            new Vector2Int(0,-1),
            new Vector2Int(-1,0)
        };

    //Generate island with radial mask and perlin noise
    public static Color[,] generateIsland(int width, int height, int seed, int blockSize, Vector2 offset, float noiseLimit, float scale, int octaves, float persistance, float lacunarity)
    {
        bool valid = false;
        bool random = seed == 0;
        float[,] radialMask = Mask.radialMask(width, height);
        float[,] perlinNoiseMap;
        float[,] islandMap = new float[width,height];

        int passes = 0;
        //While smaller than 30% of map create next one
        while(!valid)
        {
            passes++;
            seed = random ? Random.Range(1, 2147483647) : seed;
            perlinNoiseMap = Noise.generatePerlinNoise(width, height, seed, offset, scale, octaves, persistance, lacunarity);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    float val = radialMask[x, y] * perlinNoiseMap[x, y];
                    val = val < noiseLimit ? 0f : 1f;
                    islandMap[x, y] = val;
                }
            Automata.applyCellularAutomata(islandMap, 1, noiseLimit);
            islandMap = clearIslands(islandMap, ref valid);

            if(!random && !valid)
                seed = (int)(((long)seed+1)%2147483647);
        }

        Debug.Log(string.Format("Seed: {0} - Passes: {1}",seed,passes));

        //Get dictionary cellCenter -> cells
        float[,] voronoiNoise = Noise.generateVoronoiNoise(width, height, blockSize, seed);

        // Apply regions to island
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                if(islandMap[x, y] > 0f)
                    islandMap[x, y] *= voronoiNoise[x, y];
        
        applyLloydRelaxation(islandMap, blockSize);
        Dictionary<Vector2Int, List<Vector2Int>> regions = applyLloydRelaxation(islandMap, blockSize);

        regions = splitRegions(islandMap, regions);
        mergeRegions(islandMap, regions);

        regions = applySmallLloydRelaxation(islandMap, regions);

        regions = splitRegions(islandMap, regions);
        mergeRegions(islandMap, regions);

        regions = applySmallLloydRelaxation(islandMap, regions);

        int step = width / 15;
        while(step >= 4)
        {
            regions = applyVoronoiBorder(islandMap, regions, seed, step);
            step /= 15;
        }
        
        regions = splitRegions(islandMap, regions);
        mergeRegions(islandMap, regions);

        Color[,] biomeMap = assignBiomes(islandMap, regions);

        return biomeMap;
    }

    //Assign biomes (randomly)
    public static Color[,] assignBiomes(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
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

    //Merges to (default = 6) regions
    public static void mergeRegions(float[,] islandMap, Dictionary<Vector2Int,List<Vector2Int>> regions, int amount = 6)
    {
        if(regions.Keys.Count == amount) return;

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
            usedPoints.Add(regions[smallestMiddlePoint][0]);

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

            //Merge smallest with to smallest neighbour
            foreach (Vector2Int point in regions[smallestMiddlePoint])
            {
                //Adds point to new region
                regions[smallestNeighbourMiddlePoint].Add(point);
                //Maps point to new middlePoint
                pointMiddlePoint[point] = smallestNeighbourMiddlePoint;
                //Changes value to new region
                islandMap[point.x, point.y] = islandMap[regions[smallestNeighbourMiddlePoint][0].x,regions[smallestNeighbourMiddlePoint][0].y];
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
    public static Dictionary<Vector2Int, List<Vector2Int>> applySmallLloydRelaxation(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
    {
        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        //New middlePoint mapped to its color
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
                if(islandMap[x, y] == 0.0f) continue;

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

        return newRegions;
    }

    //Makes borders more random with smaller voronoi cells
    public static Dictionary<Vector2Int, List<Vector2Int>> applyVoronoiBorder(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions, int seed, int blockSize = 5)
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
                int beginX = blockX * blockSize;
                int beginY = blockY * blockSize;

                //Choosing point
                Vector2Int smallMiddlePoint = new Vector2Int(0,0);
                //Making sure i find non zero value if it exists(if i dont succeed in 3 attempts)
                for(int i = 0; i < 3; i++)
                {
                    smallMiddlePoint = new Vector2Int(rng.Next(0, blockSize) + beginX, rng.Next(0, blockSize) + beginY);
                    if(islandMap[smallMiddlePoint.x, smallMiddlePoint.y] > 0.0f) break;
                } 
                if(islandMap[smallMiddlePoint.x, smallMiddlePoint.y] == 0.0f)
                {
                    for(int x = beginX; x < beginX + blockSize; x++)
                    {
                        for(int y = beginY; y < beginY + blockSize; y++)
                        {
                            if(islandMap[x,y] > 0.0f)
                            {
                                smallMiddlePoint = new Vector2Int(x,y);
                                break;
                            }
                        }
                        if(islandMap[smallMiddlePoint.x, smallMiddlePoint.y] > 0.0f) break;
                    }
                }
                //--------------------------------

                //Giving it a value of that position on map
                smallMiddlePoints[smallMiddlePoint] = islandMap[smallMiddlePoint.x, smallMiddlePoint.y];

                //Putting it in its block
                blocks[blockX, blockY] = smallMiddlePoint;
            }

        //There is a chance that all 9 blocks will have sea, that means we need to BFS later to fix it
        List<Vector2Int> unused = new List<Vector2Int>();
        //So we know when to stop, when we get to closest used
        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

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
                
                if(minDist == float.MaxValue) 
                {
                    //Sranje
                    unused.Add(currentPos);
                    continue;
                }
                
                //Marking it newly coloured
                used.Add(currentPos);

                //Choosing new color for each point
                islandMap[x, y] = smallMiddlePoints[closestPoint];
                
                //Adding that point to list of an old parent
                newRegions[colorToPoint[islandMap[x, y]]].Add(new Vector2Int(x, y));
            }
        
        //Adding edge cases to closest region
        foreach (Vector2Int pos in unused)
        {
            Queue<Vector2Int> BFS = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            BFS.Enqueue(pos);
            visited.Add(pos);

            while(!used.Contains(pos))
            {
                Vector2Int current = BFS.Dequeue();
                foreach (Vector2Int dir in directions)
                {   
                    Vector2Int nextPos = current + dir;
                    if(used.Contains(nextPos))
                    {
                        islandMap[pos.x, pos.y] = islandMap[nextPos.x, nextPos.y];
                        newRegions[colorToPoint[islandMap[pos.x, pos.y]]].Add(pos);
                        used.Add(pos);
                        break;
                    }
                    else if(visited.Contains(nextPos) || nextPos.x < 0 || nextPos.y < 0 || nextPos.x >= width || nextPos.y >= height || islandMap[nextPos.x, nextPos.y] == 0.0f)
                        continue;

                    visited.Add(nextPos);
                    BFS.Enqueue(nextPos);
                }
            }
        }

        return newRegions;
    }

    //Split splitted areas
    public static Dictionary<Vector2Int, List<Vector2Int>> splitRegions(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
    {
        int width = islandMap.GetLength(0);
        int height = islandMap.GetLength(1);

        //Set for all values; When we split and assign new color, we must choose different than each in this set
        HashSet<float> colorValues = new HashSet<float>();

        //New regions map
        Dictionary<Vector2Int, List<Vector2Int>> newRegions = new Dictionary<Vector2Int, List<Vector2Int>>();

        //Fill colorValues
        foreach(List<Vector2Int> area in regions.Values) colorValues.Add(islandMap[area[0].x, area[0].y]);

        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        // Splitting areas
        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> region in regions)
        {
            int part = 0;

            // Passes trough every pos and DFS-es it, leaving only unconnected parts, which later form another region
            foreach (Vector2Int currentPos in region.Value)
            {
                if(used.Contains(currentPos)) continue;

                //Use same color on first part, otherwise generate new
                part++;
                float colorValue = islandMap[currentPos.x, currentPos.y];
                if(part > 1)
                {
                    while(colorValues.Contains(colorValue)) colorValue = Random.Range(0.1f, 1.0f);
                    colorValues.Add(colorValue);
                }

                List<Vector2Int> newArea = new List<Vector2Int>();
                Vector2Int newCentre = new Vector2Int(0,0);

                Stack<Vector2Int> DFS = new Stack<Vector2Int>();

                used.Add(currentPos);
                DFS.Push(currentPos);
                newArea.Add(currentPos);
                newCentre += currentPos;

                while(DFS.Count > 0)
                {
                    Vector2Int tmp = DFS.Pop();
                    float prevCol = islandMap[tmp.x, tmp.y];
                    islandMap[tmp.x, tmp.y] = colorValue;
                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int nextPos = dir + tmp;
                        if(used.Contains(nextPos) || nextPos.x < 0 || nextPos.y < 0 || nextPos.x >= width || nextPos.y >= height || islandMap[nextPos.x, nextPos.y] == 0.0f || islandMap[nextPos.x, nextPos.y] != prevCol) continue;

                        used.Add(nextPos);
                        DFS.Push(nextPos);
                        newArea.Add(nextPos);
                        newCentre += nextPos;
                    }
                }

                newCentre /= newArea.Count;
                newRegions[newCentre] = newArea;
            }
        }
        return newRegions;
    }

    //HELPER FUNCTIONS
    //HELPER FUNCTIONS
    //HELPER FUNCTIONS

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

    // Checks if region is pointing at correct tiles
    public static void checkDict(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
    {
        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> region in regions)
        {
            float val = islandMap[region.Value[0].x, region.Value[0].y];
            foreach (Vector2Int point in region.Value)
            {
                float nextVal = islandMap[point.x, point.y];
                if(val != nextVal) 
                {
                    Debug.Log(string.Format("{0} is {1}, {2} is {3}",region.Value[0],val,point,nextVal));
                    Debug.Log("False");
                    return;
                }
                used.Add(point);
            }
        }

        for(int i = 0; i < islandMap.GetLength(0); i++)
            for(int j = 0; j < islandMap.GetLength(1); j++)
            {
                Vector2Int pos = new Vector2Int(i,j);
                if(used.Contains(pos)) continue;
                else
                {
                    if(islandMap[i,j] != 0.0f) 
                    {
                        Debug.Log("More je krivo");
                        Debug.Log("False");
                        return;
                    }
                }
            }

        Debug.Log("True");
    }

    public static void printRegions(float[,] islandMap, Dictionary<Vector2Int, List<Vector2Int>> regions)
    {
        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> region in regions)
        {
            Debug.Log(string.Format("Region {0}\n",region.Key));
            foreach (Vector2Int pos in region.Value)
            {
                Debug.Log(string.Format("{0}, val: {1}", pos, islandMap[pos.x,pos.y]));
            }
        }
    }
}
