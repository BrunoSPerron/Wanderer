using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType { NONE, BOUNDARY, GRASS, DIRT, SAND, ARID }

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

    protected GameObjectInfo[] ExtractInfoFrom(GameObject[] collection)
    {
        GameObjectInfo[] goi = new GameObjectInfo[collection.Length];
        for (int i = 0; i < goi.Length; i++)
        {
            DoodadData dd = collection[i].GetComponent<DoodadData>();
            float distance = 0.01f;
            if (dd)
                distance = dd.minDistanceFromOther;
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

    protected void AddSome(ChunkControl cc, GameObjectInfo[] objects, int amount, bool avoidRoad = true, uint maxIterations = 100)
    {
        int objectIndex = rand.Next(0, objects.Length);
        for (int i = 0; i < amount; i++)
        {
            int it = 0;
            bool hasBeenPlaced = false;
            while (!hasBeenPlaced)
            {
                int x = rand.Next(1, cc.GridSize - 1);
                int y = rand.Next(1, cc.GridSize - 1);
                if (avoidRoad && !CheckForRoadAround(cc, new Vector2Int(x, y)))
                {
                    if (cc.TilesInfos[x, y].objectsOnTileInfos.Count == 0)
                    {
                        cc.AddDoodadAtPosition(objects[objectIndex], new Vector2(x, y));
                        objectIndex++;
                        objectIndex %= objects.Length;
                        hasBeenPlaced = true;
                    }
                }
                if (it > maxIterations)
                    hasBeenPlaced = true;
                it++;
            }
        }
    }

    protected void PoissonDistribution(ChunkControl cc, GameObjectInfo[] objects, float averageDistance, ObjectToInstantiate[] placeAwayFrom = null, bool avoidRoad = true)
    {
        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize -1, cc.GridSize - 1, averageDistance);
        int i = 0;
        foreach (Vector2 position in sampler.Samples())
        {
            Vector2 offsettedPos = position + Vector2.one;
            int currentGoiIndex = i % objects.Length;
            bool positionIsOK = true;
            if (avoidRoad)
                positionIsOK = !CheckForRoadAround(cc, new Vector2Int((int)position.x, (int)position.y));

            if (positionIsOK && CheckPlacementValidity(cc, objects[currentGoiIndex], offsettedPos))
            {
                Vector2 localPos = TerrainHelper.GridToLocal(new Vector2(offsettedPos.x, offsettedPos.y));
                cc.AddDoodadAtPosition(objects[currentGoiIndex], offsettedPos);
                i++;
            }
        }
    }

    protected void PoissonDistributionWithPerlinNoise(ChunkControl cc, GameObjectInfo[] objects, float averageDistance, NoiseSettings noiseSettings, int arbitraryChance = 100, AnimationCurve distributionCurve = null, bool avoidRoad = true, bool reverseNoise = false)
    {
        AnimationCurve curveInstance;
        if (distributionCurve == null)
            curveInstance = AnimationCurve.Linear(0, 0, 1, 1);
        else
            curveInstance = new AnimationCurve(distributionCurve.keys);

        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize - 1, cc.GridSize - 1, averageDistance);
        float[,] noiseMap = Noise.GenerateNoiseMap(cc.GridSize, cc.GridSize, noiseSettings, cc.ChunkCoord * cc.GridSize);

        int amountPlaced = 0;
        foreach (Vector2 position in sampler.Samples())
        {
            if (arbitraryChance > rand.Next(0, 100))
            {
                Vector2 offsettedPos = position + Vector2.one;
                int x = (int)offsettedPos.x;
                int y = (int)offsettedPos.y;
                bool placeThisOne = true;
                int currentGoiIndex = amountPlaced % objects.Length;
                if (avoidRoad)
                {
                    if (objects[currentGoiIndex].minDistanceFromOther < 0.4)
                        placeThisOne = !cc.IsRoad[x, y];
                    else
                        placeThisOne = !CheckForRoadAround(cc, new Vector2Int(x, y));
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

                if (placeThisOne && CheckPlacementValidity(cc, objects[currentGoiIndex], offsettedPos))
                {
                    cc.AddDoodadAtPosition(objects[currentGoiIndex], offsettedPos);
                    amountPlaced++;
                }
            }
        }
    }
    protected void PoissonDistributionWithPerlinNoise(ChunkControl cc, GameObjectInfo[] objects, float averageDistance, NoiseSettings noise, AnimationCurve distributionCurve)
    {
        PoissonDistributionWithPerlinNoise(cc, objects, averageDistance, noise, 100, distributionCurve);
    }
    private bool CheckPlacementValidity(ChunkControl cc, GameObjectInfo goi, Vector2 gridPosition, int extendedRadiusCheck = 1)
    {
        Stack<Tuple<Vector2, float>> checkAgainst = new Stack<Tuple<Vector2, float>>();

        int radiusCheck = (int)(goi.minDistanceFromOther * 2 + extendedRadiusCheck + 1);
        Vector2Int topLeft = new Vector2Int((int)gridPosition.x, (int)gridPosition.y);
        for (int x = -radiusCheck; x <= radiusCheck; x++)
        {
            for (int y = -radiusCheck; y <= radiusCheck; y++)
            {
                int indexX = (int)gridPosition.x + x;
                int indexY = (int)gridPosition.y + y;

                if (indexX >= 0 && indexX < cc.TilesInfos.GetLength(0) && indexY >= 0 && indexY < cc.TilesInfos.GetLength(1))
                    for (int i = 0; i < cc.TilesInfos[indexX, indexY].objectsGridPositions.Count; i++)
                    {
                        Vector2 position = cc.TilesInfos[indexX, indexY].objectsGridPositions[i];
                        float minDistance = cc.TilesInfos[indexX, indexY].objectsOnTileInfos[i].minDistanceFromOther;
                        checkAgainst.Push(new Tuple<Vector2, float>(position, minDistance));
                    }
            }
        }

        while (checkAgainst.Count != 0)
        {
            Tuple<Vector2, float> currentCheck = checkAgainst.Pop();

            if (TerrainHelper.IsWithinDistance(gridPosition, currentCheck.Item1, goi.minDistanceFromOther + currentCheck.Item2))
                return false;
        }

        return true;
    }

    private bool CheckForRoadAround(ChunkControl cc, Vector2Int gridPosition)
    {
        if (gridPosition.x < cc.GridSize - 1)
        {
            if (cc.IsRoad[gridPosition.x + 1, gridPosition.y] == true) return true;
            if (gridPosition.y < cc.GridSize - 1)
                if (cc.IsRoad[gridPosition.x + 1, gridPosition.y + 1] == true) return true;
            if (gridPosition.y > 0)
                if (cc.IsRoad[gridPosition.x + 1, gridPosition.y - 1] == true) return true;
        }
        if (gridPosition.x > 0)
        {
            if (cc.IsRoad[gridPosition.x - 1, gridPosition.y] == true) return true;
            if (gridPosition.y < cc.GridSize - 1)
                if (cc.IsRoad[gridPosition.x - 1, gridPosition.y + 1] == true) return true;
            if (gridPosition.y > 0)
                if (cc.IsRoad[gridPosition.x - 1, gridPosition.y - 1] == true) return true;
        }
        if (gridPosition.y < cc.GridSize - 1)
            if (cc.IsRoad[gridPosition.x, gridPosition.y + 1] == true) return true;
        if (gridPosition.y > 0)
            if (cc.IsRoad[gridPosition.x, gridPosition.y - 1] == true) return true;
        return false;
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
    public readonly float minDistanceFromOther;

    public GameObjectInfo(GameObject gameObject, float minDistanceFromOther)
    {
        this.gameObject = gameObject;
        this.minDistanceFromOther = minDistanceFromOther;
    }
}
