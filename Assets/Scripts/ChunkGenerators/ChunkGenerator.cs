using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType : byte { NONE, BOUNDARY, BOUNDARY2, GRASS, DIRT, SAND, ARID, MUD, SWAMPWATER }

public abstract class ChunkGenerator : MonoBehaviour
{
    protected static System.Random rand = new System.Random();

    public TileBase BoundaryTile;  //Used for seamless roads between chunks

    Queue<ChunkThreadInfo<ChunkControl>> ChunkThreadInfoQueue = new Queue<ChunkThreadInfo<ChunkControl>>();

    public void RequestChunkControl(Action<ChunkControl> callback, Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null)
    {
        ThreadStart threadStart = delegate
        {
            ChunkControlThread(callback, chunkCoord, ChunkSize, entrances);
        };
        new Thread(threadStart).Start();
    }

    void ChunkControlThread(Action<ChunkControl> callback, Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null)
    {
        ChunkControl chunkControl = GenerateChunk(chunkCoord, ChunkSize, entrances);
        lock (ChunkThreadInfoQueue)
        {
            ChunkThreadInfoQueue.Enqueue(new ChunkThreadInfo<ChunkControl>(callback, chunkControl));
        }
    }

    void Update()
    {
        if (ChunkThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < ChunkThreadInfoQueue.Count; i++)
            {
                ChunkThreadInfo<ChunkControl> threadInfo = ChunkThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }




    protected GameObjectInfo[] ExtractInfosFrom(GameObject[] collection)
    {
        GameObjectInfo[] goi = new GameObjectInfo[collection.Length];
        for (int i = 0; i < goi.Length; i++)
        {
            DoodadData dd = collection[i].GetComponent<DoodadData>();
            float distance = 0.01f;
            if (dd)
                distance = dd.MinGridRadiusDistanceFromOther;
            goi[i] = new GameObjectInfo(collection[i], distance);
        }
        return goi;
    }

    protected abstract ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null);

    protected void AddTilesToLoadQueue(ChunkControl cc, Dictionary<TileType, TileBase> TilesDict)
    {
        TilesDict.Add(TileType.BOUNDARY, BoundaryTile);
        for (int x = 0; x < cc.TilesInfos.GetLength(0); x++)
        {
            for (int y = 0; y < cc.TilesInfos.GetLength(1); y++)
            {
                if (cc.TilesInfos[x, y].type != TileType.NONE)
                    cc.AddTileToInstantiate(TilesDict[cc.TilesInfos[x, y].type], new Vector2Int(x, y));
            }
        }
    }

    protected void FillWith(ChunkControl cc, TileType tile)
    {
        for (int x = 1; x < cc.TilesInfos.GetLength(0) - 1; x++)
            for (int y = 1; y < cc.TilesInfos.GetLength(1) - 1; y++)
                cc.TilesInfos[x, y].type = tile;
    }

    protected void AddRoads(ChunkControl cc, TileType roadTile)
    {
        if (cc.Entrances == null) return;
        for (int i = 0; i < cc.Entrances.Length; i++)
        {
            if (cc.Entrances[i].x < 0)
                cc.Entrances[i].x = 0;
            else if (cc.Entrances[i].x > cc.GridSize)
                cc.Entrances[i].x = cc.GridSize;

            if (cc.Entrances[i].y < 0)
                cc.Entrances[i].y = 0;
            else if (cc.Entrances[i].y > cc.GridSize)
                cc.Entrances[i].y = cc.GridSize;
        }

        Vector2Int waypoint = new Vector2Int((int)((GaussianRandom.generateNormalRandom(0, 1) + 4) / 8 * cc.GridSize) + 1, (int)((GaussianRandom.generateNormalRandom(0, 1) + 4) / 8 * cc.GridSize) + 1);
        waypoint.Clamp(new Vector2Int(2, 2), new Vector2Int(cc.GridSize - 1, cc.GridSize - 1));
        foreach (Vector2Int entrance in cc.Entrances)
        {
            int x = entrance.x;
            int y = entrance.y;

            if (x < 2) x = 2;
            if (x > cc.GridSize - 1) x = cc.GridSize - 1;
            if (y < 2) y = 2;
            if (y > cc.GridSize - 1) y = cc.GridSize - 1;

            while (x != waypoint.x || y != waypoint.y)
            {
                SurroundTileWith(cc, x, y, roadTile, true);

                int weightX = Mathf.Abs(Mathf.Abs(x) - Mathf.Abs(waypoint.x));
                int weightY = Mathf.Abs(Mathf.Abs(y) - Mathf.Abs(waypoint.y));

                if (rand.Next(0, weightX + weightY) < weightX)
                {
                    if (x < waypoint.x)
                        x++;
                    else
                        x--;
                }
                else
                {
                    if (y < waypoint.y)
                        y++;
                    else
                        y--;
                }
            }
            SetBoundary(cc, entrance);
        }
    }

    protected void SurroundTileWith(ChunkControl cc, int x, int y, TileType tile, bool addToRoad = false)
    {
        cc.TilesInfos[x + 1, y].type = tile;
        cc.TilesInfos[x - 1, y].type = tile;
        cc.TilesInfos[x, y + 1].type = tile;
        cc.TilesInfos[x, y - 1].type = tile;
        cc.TilesInfos[x + 1, y + 1].type = tile;
        cc.TilesInfos[x + 1, y - 1].type = tile;
        cc.TilesInfos[x - 1, y + 1].type = tile;
        cc.TilesInfos[x - 1, y - 1].type = tile;

        if (addToRoad)
        {
            cc.IsRoad[x, y] = true;
            cc.IsRoad[x, y - 1] = true;
            cc.IsRoad[x, y - 2] = true;
            cc.IsRoad[x - 1, y] = true;
            cc.IsRoad[x - 1, y - 2] = true;
            cc.IsRoad[x - 2, y] = true;
            cc.IsRoad[x - 2, y - 1] = true;
            cc.IsRoad[x - 2, y - 2] = true;
        }
    }

    protected void SuroundTileTypeWith(ChunkControl cc, TileType tileType, TileType surroundWith, TileType extraBoundaryType = TileType.BOUNDARY)
    {
        bool[,] toReplace = new bool[cc.GridSize + 2, cc.GridSize + 2];

        for (int x = 0; x < toReplace.GetLength(0); x++)
        {
            for (int y = 0; y < toReplace.GetLength(1); y++)
            {
                if (cc.TilesInfos[x, y].type == tileType || cc.TilesInfos[x, y].type == TileType.BOUNDARY)
                {
                    int minX = x - 1 < 0 ? 0 : x - 1;
                    int minY = y - 1 < 0 ? 0 : y - 1;
                    int maxX = x + 1 > toReplace.GetLength(0) - 1 ? toReplace.GetLength(0) - 1 : x + 1;
                    int maxY = y + 1 > toReplace.GetLength(1) - 1 ? toReplace.GetLength(1) - 1 : y + 1;

                    for (int x2 = minX; x2 <= maxX; x2++)
                    {
                        for (int y2 = minY; y2 <= maxY; y2++)
                        {
                            if (cc.TilesInfos[x2, y2].type != tileType)
                                toReplace[x2, y2] = true;
                        }
                    }
                }
            }
        }


        for (int x = 0; x < toReplace.GetLength(0); x++)
        {
            for (int y = 0; y < toReplace.GetLength(1); y++)
            {
                if (toReplace[x, y])
                {
                    if (x == 0 || y == 0 || x == toReplace.GetLength(0) - 1 || y == toReplace.GetLength(1) - 1)
                    {
                        if (cc.TilesInfos[x, y].type != TileType.BOUNDARY)
                            cc.TilesInfos[x, y].type = extraBoundaryType;
                    }
                    else
                    {
                        cc.TilesInfos[x, y].type = surroundWith;

                    }
                }
            }
        }
    }

    protected void SetTilesFromPerlin(ChunkControl cc, NoiseSettings noiseSettings, TileType tileType, float threshold, TileType boundaryType = TileType.BOUNDARY)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(cc.GridSize + 2, cc.GridSize + 2, noiseSettings, cc.ChunkCoord * cc.GridSize);

        for (int x = 0; x < noiseMap.GetLength(0); x++)
        {
            for (int y = 0; y < noiseMap.GetLength(1); y++)
            {
                if (noiseMap[x, y] < threshold)
                {
                    if (x == 0 || y == 0 || x == noiseMap.GetLength(0) - 1 || y == noiseMap.GetLength(1) - 1)
                        cc.TilesInfos[x, y].type = boundaryType;
                    else
                        cc.TilesInfos[x, y].type = tileType;
                }
            }
        }
    }

    protected void RemoveSingletonTiles(ChunkControl cc, TileType tileType, TileType replaceBy)
    {
        for (int x = 1; x < cc.TilesInfos.GetLength(0) - 1; x++)
        {
            for (int y = 1; y < cc.TilesInfos.GetLength(1) - 1; y++)
            {
                if (cc.TilesInfos[x, y].type == tileType)
                {
                    if (cc.TilesInfos[x - 1, y].type != tileType && cc.TilesInfos[x + 1, y].type != tileType && cc.TilesInfos[x, y - 1].type != tileType && cc.TilesInfos[x, y + 1].type != tileType)
                        cc.TilesInfos[x, y].type = replaceBy;
                }
            }
        }
    }

    private void SetBoundary(ChunkControl cc, Vector2Int entrance)
    {
        if (entrance.x == 0)
        {
            cc.TilesInfos[0, entrance.y - 1].type = TileType.BOUNDARY;
            cc.TilesInfos[0, entrance.y].type = TileType.BOUNDARY;
            cc.TilesInfos[0, entrance.y + 1].type = TileType.BOUNDARY;
        }
        else if (entrance.x == cc.GridSize)
        {
            cc.TilesInfos[cc.GridSize + 1, entrance.y - 1].type = TileType.BOUNDARY;
            cc.TilesInfos[cc.GridSize + 1, entrance.y].type = TileType.BOUNDARY;
            cc.TilesInfos[cc.GridSize + 1, entrance.y + 1].type = TileType.BOUNDARY;
        }
        if (entrance.y == 0)
        {
            cc.TilesInfos[entrance.x - 1, 0].type = TileType.BOUNDARY;
            cc.TilesInfos[entrance.x, 0].type = TileType.BOUNDARY;
            cc.TilesInfos[entrance.x + 1, 0].type = TileType.BOUNDARY;
        }
        else if (entrance.y == cc.GridSize)
        {
            cc.TilesInfos[entrance.x - 1, cc.GridSize + 1].type = TileType.BOUNDARY;
            cc.TilesInfos[entrance.x, cc.GridSize + 1].type = TileType.BOUNDARY;
            cc.TilesInfos[entrance.x + 1, cc.GridSize + 1].type = TileType.BOUNDARY;
        }
    }




    protected void AddSome(ChunkControl cc, GameObjectInfo[] objects, int amount, bool avoidRoad = true, uint maxIterations = 30)
    {
        int objectIndex = rand.Next(0, objects.Length);
        for (int i = 0; i < amount; i++)
        {
            int it = 0;
            bool hasBeenPlaced = false;
            while (!hasBeenPlaced)
            {
                int x = rand.Next(0, cc.GridSize);
                int y = rand.Next(0, cc.GridSize);
                if (avoidRoad && !IsOverRoad(cc, new Vector2Int(x, y), objects[objectIndex].radius))
                {
                    if (cc.TilesInfos[x, y].objectsGridPositions.Count == 0)
                    {
                        if (CheckPlacementValidity(cc, objects[objectIndex], new Vector2(x + 0.5f, y + 0.5f), out List<Vector2Int> tilesInRadius))
                        {
                            cc.AddDoodadAtPosition(objects[objectIndex], new Vector2(x + 0.5f, y + 0.5f), tilesInRadius);
                            objectIndex++;
                            objectIndex %= objects.Length;
                            hasBeenPlaced = true;
                        }
                    }
                }
                if (it > maxIterations)
                    hasBeenPlaced = true;
                it++;
            }
        }
    }

    protected void PoissonDistribution(ChunkControl cc, GameObjectInfo[] objects, float averageDistance, bool avoidRoad = true)
    {
        List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>> doodadToAdd = new List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>>();
        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize, cc.GridSize, averageDistance);
        int i = 0;
        foreach (Vector2 position in sampler.Samples())
        {
            int currentGoiIndex = i % objects.Length;
            bool positionIsOK = true;
            if (avoidRoad)
                positionIsOK = !IsOverRoad(cc, position, objects[currentGoiIndex].radius);

            if (positionIsOK && CheckPlacementValidity(cc, objects[currentGoiIndex], position, out List<Vector2Int> tilesOverlapping))
            {
                doodadToAdd.Add(new Tuple<GameObjectInfo, Vector2, List<Vector2Int>>(objects[currentGoiIndex], position, tilesOverlapping));
                i++;
            }
        }

        foreach (Tuple<GameObjectInfo, Vector2, List<Vector2Int>> tuple in doodadToAdd)
            cc.AddDoodadAtPosition(tuple.Item1, tuple.Item2, tuple.Item3);
    }



    protected void ShatterGround(ChunkControl cc, TileType original, TileType replaceBy, int percentChance = 20, bool awayFromRoad = false)
    {
        for (int x = 1; x < cc.TilesInfos.GetLength(0); x++)
            for (int y = 1; y < cc.TilesInfos.GetLength(1); y++)
                if (!awayFromRoad || !TileIsNextToRoad(cc, new Vector2Int(x - 1, y - 1)))
                    if (cc.TilesInfos[x, y].type == original && rand.Next(0, 100) < percentChance)
                        cc.TilesInfos[x, y].type = replaceBy;
    }


    protected void PoissonDistributionWithPerlinNoise(ChunkControl cc, GameObjectInfo[] objects, float averageDistance, NoiseSettings noiseSettings, int arbitraryChance = 100, AnimationCurve distributionCurve = null, bool avoidRoad = true, bool reverseNoise = false)
    {
        AnimationCurve curveInstance;
        if (distributionCurve == null)
            curveInstance = AnimationCurve.Linear(0, 0, 1, 1);
        else
            curveInstance = new AnimationCurve(distributionCurve.keys);

        List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>> doodadToAdd = new List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>>();

        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize, cc.GridSize, averageDistance);
        float[,] noiseMap = Noise.GenerateNoiseMap(cc.GridSize, cc.GridSize, noiseSettings, cc.ChunkCoord * cc.GridSize);

        int amountPlaced = 0;
        foreach (Vector2 position in sampler.Samples())
        {
            if (arbitraryChance > rand.Next(0, 100))
            {
                int x = (int)position.x;
                int y = (int)position.y;
                bool placeThisOne = true;
                int currentGoiIndex = amountPlaced % objects.Length;
                if (avoidRoad)
                {
                    placeThisOne = !IsOverRoad(cc, new Vector2Int(x, y), objects[currentGoiIndex].radius);
                }

                if (reverseNoise)
                {
                    if (noiseMap[x, y] > curveInstance.Evaluate((float)rand.NextDouble()))
                        placeThisOne = false;
                }
                else
                {
                    if (noiseMap[x, y] < curveInstance.Evaluate((float)rand.NextDouble()))
                        placeThisOne = false;
                }

                if (placeThisOne && CheckPlacementValidity(cc, objects[currentGoiIndex], position, out List<Vector2Int> tilesOverlapping))
                {
                    doodadToAdd.Add(new Tuple<GameObjectInfo, Vector2, List<Vector2Int>>(objects[currentGoiIndex], position, tilesOverlapping));
                    amountPlaced++;
                }
            }
        }
        foreach (Tuple<GameObjectInfo, Vector2, List<Vector2Int>> tuple in doodadToAdd)
        {
            cc.AddDoodadAtPosition(tuple.Item1, tuple.Item2, tuple.Item3);
        }
    }
    protected void PoissonDistributionWithPerlinNoise(ChunkControl cc, GameObjectInfo[] objects, float averageDistance, NoiseSettings noise, AnimationCurve distributionCurve)
    {
        PoissonDistributionWithPerlinNoise(cc, objects, averageDistance, noise, 100, distributionCurve);
    }

    protected void PoissonDistributionWithPerlinOnTileEdge(ChunkControl cc, GameObjectInfo[] objects, TileType onTile, float averageDistance, NoiseSettings noiseSettings, AnimationCurve distributionCurve = null, bool avoidRoad = true, int arbitraryChance = 100, bool reverseNoise = false)
    {
        AnimationCurve curveInstance;
        if (distributionCurve == null)
            curveInstance = AnimationCurve.Linear(0, 0, 1, 1);
        else
            curveInstance = new AnimationCurve(distributionCurve.keys);

        List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>> doodadToAdd = new List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>>();

        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize, cc.GridSize, averageDistance);
        float[,] noiseMap = Noise.GenerateNoiseMap(cc.GridSize, cc.GridSize, noiseSettings, cc.ChunkCoord * cc.GridSize);

        int amountPlaced = 0;
        foreach (Vector2 position in sampler.Samples())
        {
            bool placeThisOne = false;
            int x = (int)position.x;
            int y = (int)position.y;

            if (cc.TilesInfos[x + 1, y + 1].type == onTile)
                foreach (Vector2Int v2i in GetPositionsNextTo(cc, x, y))
                    if (cc.TilesInfos[v2i.x, v2i.y].type != onTile && cc.TilesInfos[v2i.x, v2i.y].type != TileType.BOUNDARY)
                        placeThisOne = true;

            if (placeThisOne)
            {
                if (arbitraryChance > rand.Next(0, 100))
                {
                    int currentGoiIndex = amountPlaced % objects.Length;
                    if (avoidRoad)
                    {
                        placeThisOne = !IsOverRoad(cc, new Vector2Int(x, y), objects[currentGoiIndex].radius);
                    }

                    if (reverseNoise)
                    {
                        if (noiseMap[x, y] > curveInstance.Evaluate((float)rand.NextDouble()))
                            placeThisOne = false;
                    }
                    else
                    {
                        if (noiseMap[x, y] < curveInstance.Evaluate((float)rand.NextDouble()))
                            placeThisOne = false;
                    }

                    if (placeThisOne && CheckPlacementValidity(cc, objects[currentGoiIndex], position, out List<Vector2Int> tilesOverlapping))
                    {
                        doodadToAdd.Add(new Tuple<GameObjectInfo, Vector2, List<Vector2Int>>(objects[currentGoiIndex], position, tilesOverlapping));
                        amountPlaced++;
                    }
                }
            }
        }
        foreach (Tuple<GameObjectInfo, Vector2, List<Vector2Int>> tuple in doodadToAdd)
        {
            cc.AddDoodadAtPosition(tuple.Item1, tuple.Item2, tuple.Item3);
        }
    }

    protected void PoissonDistributionWithPerlinOnSurroundedTile(ChunkControl cc, GameObjectInfo[] objects, TileType onTile, float averageDistance, NoiseSettings noiseSettings, AnimationCurve distributionCurve = null, bool avoidRoad = true, bool reverseNoise = false)
    {
        AnimationCurve curveInstance;
        if (distributionCurve == null)
            curveInstance = AnimationCurve.Linear(0, 0, 1, 1);
        else
            curveInstance = new AnimationCurve(distributionCurve.keys);

        List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>> doodadToAdd = new List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>>();

        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize, cc.GridSize, averageDistance);
        float[,] noiseMap = Noise.GenerateNoiseMap(cc.GridSize, cc.GridSize, noiseSettings, cc.ChunkCoord * cc.GridSize);

        int amountPlaced = 0;
        foreach (Vector2 position in sampler.Samples())
        {
            bool placeThisOne = false;
            int x = (int)position.x;
            int y = (int)position.y;

            if (cc.TilesInfos[x + 1, y + 1].type == onTile)
            {
                placeThisOne = true;
                foreach (Vector2Int v2i in GetPositionsNextTo(cc, x, y))
                    if (cc.TilesInfos[v2i.x + 1, v2i.y + 1].type != onTile)
                        placeThisOne = false;
            }

            if (placeThisOne)
            {
                int currentGoiIndex = amountPlaced % objects.Length;
                if (avoidRoad)
                {
                    placeThisOne = !IsOverRoad(cc, new Vector2Int(x, y), objects[currentGoiIndex].radius);
                }

                if (reverseNoise)
                {
                    if (noiseMap[x, y] > curveInstance.Evaluate((float)rand.NextDouble()))
                        placeThisOne = false;
                }
                else
                {
                    if (noiseMap[x, y] < curveInstance.Evaluate((float)rand.NextDouble()))
                        placeThisOne = false;
                }

                if (placeThisOne && CheckPlacementValidity(cc, objects[currentGoiIndex], position, out List<Vector2Int> tilesOverlapping))
                {
                    doodadToAdd.Add(new Tuple<GameObjectInfo, Vector2, List<Vector2Int>>(objects[currentGoiIndex], position, tilesOverlapping));
                    amountPlaced++;
                }

            }
        }
        foreach (Tuple<GameObjectInfo, Vector2, List<Vector2Int>> tuple in doodadToAdd)
        {
            cc.AddDoodadAtPosition(tuple.Item1, tuple.Item2, tuple.Item3);
        }
    }
    protected void PoissonDistributionWithPerlinNoiseOnTiletype(ChunkControl cc, GameObjectInfo[] objects, TileType onTile, float averageDistance, NoiseSettings noiseSettings, AnimationCurve distributionCurve = null, bool avoidRoad = true, int arbitraryChance = 100, bool reverseNoise = false)
    {
        AnimationCurve curveInstance;
        if (distributionCurve == null)
            curveInstance = AnimationCurve.Linear(0, 0, 1, 1);
        else
            curveInstance = new AnimationCurve(distributionCurve.keys);

        List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>> doodadToAdd = new List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>>();

        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize, cc.GridSize, averageDistance);
        float[,] noiseMap = Noise.GenerateNoiseMap(cc.GridSize, cc.GridSize, noiseSettings, cc.ChunkCoord * cc.GridSize);

        int amountPlaced = 0;
        foreach (Vector2 position in sampler.Samples())
        {
            if (cc.TilesInfos[(int)position.x + 1, (int)position.y + 1].type == onTile)
            {
                if (arbitraryChance > rand.Next(0, 100))
                {
                    int x = (int)position.x;
                    int y = (int)position.y;
                    bool placeThisOne = true;
                    int currentGoiIndex = amountPlaced % objects.Length;
                    if (avoidRoad)
                    {
                        placeThisOne = !IsOverRoad(cc, new Vector2Int(x, y), objects[currentGoiIndex].radius);
                    }

                    if (reverseNoise)
                    {
                        if (noiseMap[x, y] > curveInstance.Evaluate((float)rand.NextDouble()))
                            placeThisOne = false;
                    }
                    else
                    {
                        if (noiseMap[x, y] < curveInstance.Evaluate((float)rand.NextDouble()))
                            placeThisOne = false;
                    }

                    if (placeThisOne && CheckPlacementValidity(cc, objects[currentGoiIndex], position, out List<Vector2Int> tilesOverlapping))
                    {
                        doodadToAdd.Add(new Tuple<GameObjectInfo, Vector2, List<Vector2Int>>(objects[currentGoiIndex], position, tilesOverlapping));
                        amountPlaced++;
                    }
                }
            }
        }
        foreach (Tuple<GameObjectInfo, Vector2, List<Vector2Int>> tuple in doodadToAdd)
        {
            cc.AddDoodadAtPosition(tuple.Item1, tuple.Item2, tuple.Item3);
        }
    }

    protected bool CheckPlacementValidity(ChunkControl cc, GameObjectInfo goi, Vector2 gridPosition, out List<Vector2Int> tilesInRadius)
    {
        Stack<Tuple<Vector2, float>> checkAgainst = new Stack<Tuple<Vector2, float>>();
        tilesInRadius = GetTilesInRadius(cc, gridPosition, goi.radius);

        foreach (Vector2Int v2i in tilesInRadius)
        {
            for (int i = 0; i < cc.TilesInfos[v2i.x, v2i.y].objectsGridPositions.Count; i++)
            {
                Vector2 position = cc.TilesInfos[v2i.x, v2i.y].objectsGridPositions[i];
                float minDistance = cc.TilesInfos[v2i.x, v2i.y].objectsRadius[i];
                checkAgainst.Push(new Tuple<Vector2, float>(position, minDistance));
            }
        }

        while (checkAgainst.Count != 0)
        {
            Tuple<Vector2, float> currentCheck = checkAgainst.Pop();

            if (TerrainHelper.IsWithinDistance(gridPosition, currentCheck.Item1, goi.radius + currentCheck.Item2))
                return false;
        }

        return true;
    }

    protected bool IsOverRoad(ChunkControl cc, Vector2 gridPosition, float objectRadius)
    {
        float radiusSquared = objectRadius * objectRadius;

        int minX = Mathf.FloorToInt(gridPosition.x - objectRadius);
        int maxX = Mathf.CeilToInt(gridPosition.x + objectRadius);
        int minY = Mathf.FloorToInt(gridPosition.y - objectRadius);
        int maxY = Mathf.CeilToInt(gridPosition.y + objectRadius);

        if (minX < 0)
            minX = 0;
        if (maxX > cc.IsRoad.GetLength(0) - 1)
            maxX = cc.IsRoad.GetLength(0) - 1;
        if (minY < 0)
            minY = 0;
        if (maxY > cc.IsRoad.GetLength(1) - 1)
            maxY = cc.IsRoad.GetLength(1) - 1;

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y < maxY; y++)
                if (cc.IsRoad[x, y] && CircleOverlap(new Vector2Int(x, y), gridPosition, radiusSquared))
                    return true;

        return false;
    }
    private bool TileIsNextToRoad(ChunkControl cc, Vector2Int gridPosition, int checkDistance = 1)
    {
        for (int x = gridPosition.x - checkDistance; x <= gridPosition.x + checkDistance; x++)
            for (int y = gridPosition.y - checkDistance; y <= gridPosition.y + checkDistance; y++)
                if (InBounds(cc.IsRoad, x, y) && cc.IsRoad[x, y])
                    return true;

        return false;
    }

    protected List<Vector2Int> GetPositionsNextTo(ChunkControl cc, int x, int y)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        if (x > 0)
            positions.Add(new Vector2Int(x - 1, y));
        if (y > 0)
            positions.Add(new Vector2Int(x, y - 1));
        if (x < cc.TilesInfos.GetLength(0))
            positions.Add(new Vector2Int(x + 1, y));
        if (y < cc.TilesInfos.GetLength(1))
            positions.Add(new Vector2Int(x, y + 1));
        return positions;
    }
    protected List<Vector2Int> GetTilesInRadius(ChunkControl cc, Vector2 circleCenter, float radius)
    {
        List<Vector2Int> tilesInRadius = new List<Vector2Int>();
        float radiusSquared = radius * radius;

        int minX = Mathf.FloorToInt(circleCenter.x - radius);
        int maxX = Mathf.CeilToInt(circleCenter.x + radius);
        int minY = Mathf.FloorToInt(circleCenter.y - radius);
        int maxY = Mathf.CeilToInt(circleCenter.y + radius);

        if (minX < 0)
            minX = 0;
        if (maxX > cc.TilesInfos.GetLength(0) - 1)
            maxX = cc.TilesInfos.GetLength(0) - 1;
        if (minY < 0)
            minY = 0;
        if (maxY > cc.TilesInfos.GetLength(1) - 1)
            maxY = cc.TilesInfos.GetLength(1) - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                Vector2Int current = new Vector2Int(x, y);
                if (CircleOverlap(current, circleCenter, radiusSquared))
                    tilesInRadius.Add(current);
            }
        }
        return tilesInRadius;
    }

    private bool CircleOverlap(Vector2Int tileCoord, Vector2 circleCenter, float radiusSquared)
    {
        Vector2 tileHalfSize = new Vector2(0.5f, 0.5f);
        Vector2 tileCenter = tileCoord + tileHalfSize;
        Vector2 diff = tileCenter - circleCenter;
        Vector2 diffPositive = new Vector2(Math.Abs(diff.x), Math.Abs(diff.y));
        Vector2 closest = diffPositive - tileHalfSize;
        Vector2 closestPositive = new Vector2(Math.Max(closest.x, 0), Math.Max(closest.y, 0));
        return closestPositive.sqrMagnitude <= radiusSquared;
    }

    protected bool InBounds<T>(in T[,] array, int x, int y)
    {
        return x >= 0 && x < array.GetLength(0) && y >= 0 && y < array.GetLength(1);
    }


    struct ChunkThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public ChunkThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct GameObjectInfo
{
    public readonly GameObject gameObject;
    public readonly float radius;

    public GameObjectInfo(GameObject gameObject, float radius)
    {
        this.gameObject = gameObject;
        this.radius = radius;
    }
}
