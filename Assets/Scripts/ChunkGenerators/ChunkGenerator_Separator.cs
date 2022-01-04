using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ChunkGenerator_Separator : ChunkGenerator
{
    public ChunkGenerator ForestChunkGenerator;
    public ChunkGenerator DesertChunkGenerator;
    public ChunkGenerator SwampChunkGenerator;

    public int RiverHalfWidth = 2;

    Dictionary<Biome, ChunkGenerator> generators;

    void Start()
    {
        generators = new Dictionary<Biome, ChunkGenerator>();
        generators.Add(Biome.FOREST, ForestChunkGenerator);
        generators.Add(Biome.DESERT, DesertChunkGenerator);
        generators.Add(Biome.SWAMP, SwampChunkGenerator);
    }

    public void RequestChunkControl(Action<ChunkControl> callback, Vector2Int chunkCoord, int ChunkSize, Biome topLeftCorner, Biome topRightCorner, Biome BottomLeftCorner, Biome BottomRightCorner, Cardinal entrances = 0)
    {
        ThreadStart threadStart = delegate
        {
            ChunkControlThread(callback, chunkCoord, ChunkSize, topLeftCorner, topRightCorner, BottomLeftCorner, BottomRightCorner, entrances);
        };
        new Thread(threadStart).Start();
    }

    protected void ChunkControlThread(Action<ChunkControl> callback, Vector2Int chunkCoord, int ChunkSize, Biome topLeftCorner, Biome topRightCorner, Biome BottomLeftCorner, Biome BottomRightCorner, Cardinal entrances = 0)
    {
        ChunkControl chunkControl = GenerateChunk(chunkCoord, ChunkSize, topLeftCorner, topRightCorner, BottomLeftCorner, BottomRightCorner, entrances);
        lock (ChunkThreadInfoQueue)
        {
            ChunkThreadInfoQueue.Enqueue(new ChunkThreadInfo<ChunkControl>(callback, chunkControl));
        }
    }


    protected ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Biome topLeftCorner, Biome topRightCorner, Biome BottomLeftCorner, Biome BottomRightCorner, Cardinal entrances = 0)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);
        cc.individualRendererMode = true;
        Vector2Int[] riverEntrances = GenerateRiverEntrances(chunkCoord, ChunkSize, topLeftCorner, topRightCorner, BottomLeftCorner, BottomRightCorner, out Vector2Int[] boundaryTilesPosition);
        Array2D<bool> riverMask = GenerateRiverMask(riverEntrances, ChunkSize, chunkCoord);
        Array2D<bool> extendedRiverMask = new Array2D<bool>(riverMask);
        Array2DHelper.Mask.Extend(ref extendedRiverMask);

        Dictionary<Biome, Array2D<bool>> biomeMasks = GenerateBiomeMasks(riverMask, topLeftCorner, topRightCorner, BottomLeftCorner, BottomRightCorner);
        Dictionary<Biome, Array2D<bool>> extendedBiomeMasks = new Dictionary<Biome, Array2D<bool>>();
        Dictionary<Biome, Array2D<bool>> shrunkBiomeMasks = new Dictionary<Biome, Array2D<bool>>();
        foreach (KeyValuePair<Biome, Array2D<bool>> kvp in biomeMasks)
        {
            Array2D<bool> extendedMask = new Array2D<bool>(kvp.Value);
            Array2D<bool> shrunkMask = new Array2D<bool>(kvp.Value);
            Array2DHelper.Mask.Extend(ref extendedMask, true);
            Array2DHelper.Mask.Substract(ref shrunkMask, extendedRiverMask);
            extendedBiomeMasks.Add(kvp.Key, extendedMask);
            shrunkBiomeMasks.Add(kvp.Key, shrunkMask);
        }

        Array2DHelper.Mask.RemoveBorder(ref extendedRiverMask);

        Dictionary<Biome, ChunkControl> generatedBiomes = GenerateNeededBiomes(chunkCoord, ChunkSize, topLeftCorner, topRightCorner, BottomLeftCorner, BottomRightCorner);
        

        for (int x = 1; x < riverMask.Width - 1; x++)
            for (int y = 1; y < riverMask.Height - 1; y++)
                if (riverMask[x, y])
                    cc.TilesInfos[x, y].type = TileType.RIVER;

        foreach (KeyValuePair<Biome, Array2D<bool>> kvp in biomeMasks)
        {
            ChunkControl biomeControl = generatedBiomes[kvp.Key];
            Array2D<bool> biomeMask = kvp.Value;
            for (int x = 0; x < biomeMask.Width; x++)
            {
                for (int y = 0; y < biomeMask.Height; y++)
                {
                    if (biomeMask[x, y])
                    {
                        if (extendedRiverMask[x, y])
                            cc.TilesInfos[x, y].type = generators[kvp.Key].RiverBank;
                        else
                            cc.TilesInfos[x, y] = biomeControl.TilesInfos[x, y];
                    }
                    else if (extendedBiomeMasks[kvp.Key][x, y])
                    {
                        cc.TilesInfos[x, y].type = generators[kvp.Key].BiomeRiver;
                    }
                }
            }
        }

        foreach (Vector2Int v2i in boundaryTilesPosition)
            cc.TilesInfos[v2i.x, v2i.y].type = TileType.RIVERBOUNDARY;

        AddTilesToLoadQueue(cc);

        foreach (KeyValuePair<Biome, ChunkControl> kvp in generatedBiomes)
        {
            Array2D<bool> biomeMask = shrunkBiomeMasks[kvp.Key];
            foreach (ObjectToInstantiate item in kvp.Value.ObjectsToInstantiate)
            {
                if (biomeMask[IsoGridHelper.LocalToGrid(item.localPos)])
                    cc.ObjectsToInstantiate.Push(item);
            }
        }

        return cc;
    }

    private Dictionary<Biome, Array2D<bool>> GenerateBiomeMasks(Array2D<bool> separatorMask, Biome topLeftCorner, Biome topRightCorner, Biome bottomLeftCorner, Biome bottomRightCorner)
    {
        Dictionary<Biome, Array2D<bool>> biomeMasks = new Dictionary<Biome, Array2D<bool>>();

        Tuple<Biome, Vector2Int>[] corners = new Tuple<Biome, Vector2Int>[4];
        corners[0] = new Tuple<Biome, Vector2Int>(topLeftCorner, new Vector2Int(0, 0));
        corners[1] = new Tuple<Biome, Vector2Int>(topRightCorner, new Vector2Int(separatorMask.Width - 1, 0));
        corners[2] = new Tuple<Biome, Vector2Int>(bottomLeftCorner, new Vector2Int(0, separatorMask.Height - 1));
        corners[3] = new Tuple<Biome, Vector2Int>(bottomRightCorner, new Vector2Int(separatorMask.Width - 1, separatorMask.Height - 1));


        foreach (Tuple<Biome, Vector2Int> tuple in corners)
        {
            Biome currentBiome = tuple.Item1;
            Array2D<bool> currentMask;

            if (biomeMasks.ContainsKey(currentBiome))
                currentMask = biomeMasks[currentBiome];
            else
                currentMask = new Array2D<bool>(separatorMask.Width, separatorMask.Height);

            Stack<Vector2Int> positionsToProcess = new Stack<Vector2Int>();
            positionsToProcess.Push(tuple.Item2);
            while (positionsToProcess.Count != 0)
            {
                Vector2Int currentPos = positionsToProcess.Pop();
                Vector2Int[] positionsAround = Array2DHelper.GetCardinalPositions(currentMask, currentPos);
                foreach (Vector2Int v2i in positionsAround)
                {
                    if (!currentMask[v2i] && !separatorMask[v2i])
                    {
                        currentMask[v2i] = true;
                        positionsToProcess.Push(v2i);
                    }
                }
            }
            biomeMasks[currentBiome] = currentMask;
        }

        return biomeMasks;
    }

    private Array2D<bool> GenerateRiverMask(Vector2Int[] riverEntrances, int chunkSize, Vector2Int chunkCoord)
    {
        Vector2 centerPoint = Vector2.zero;
        foreach (Vector2Int v2i in riverEntrances)
        {
            centerPoint.x += v2i.x;
            centerPoint.y += v2i.y;
        }
        centerPoint /= riverEntrances.Length;

        Array2D<bool> river = new Array2D<bool>(chunkSize + 2, chunkSize + 2);
        System.Random rand = new System.Random(WorldData.Seed + (chunkCoord.x << 16 + chunkCoord.y));

        Vector2 waypoint = new Vector2(((GaussianRandom.generateNormalRandom(rand, 0, 1) + 4) / 8 * chunkSize) + 1, ((GaussianRandom.generateNormalRandom(rand, 0, 1) + 4) / 8 * chunkSize) + 1);

        waypoint = Vector2.Lerp(centerPoint, waypoint, 0.5f);
        Vector2Int intWaypoint = new Vector2Int((int)waypoint.x, (int)waypoint.y);
        intWaypoint.Clamp(new Vector2Int(2, 2), new Vector2Int(chunkSize - 1, chunkSize - 1));
        foreach (Vector2Int entrance in riverEntrances)
        {
            river[entrance.x, entrance.y] = true;
            int x = entrance.x;
            int y = entrance.y;

            if (x < RiverHalfWidth + 2) x = RiverHalfWidth + 2;
            if (x > chunkSize - RiverHalfWidth) x = chunkSize - RiverHalfWidth;
            if (y < RiverHalfWidth + 2) y = RiverHalfWidth + 2;
            if (y > chunkSize - RiverHalfWidth) y = chunkSize - RiverHalfWidth;

            while (x != intWaypoint.x || y != intWaypoint.y)
            {
                river[x, y] = true;

                int weightX = Mathf.Abs(Mathf.Abs(x) - Mathf.Abs(intWaypoint.x));
                int weightY = Mathf.Abs(Mathf.Abs(y) - Mathf.Abs(intWaypoint.y));

                if (rand.Next(0, weightX + weightY) < weightX)
                {
                    if (x < intWaypoint.x)
                        x++;
                    else
                        x--;
                }
                else
                {
                    if (y < intWaypoint.y)
                        y++;
                    else
                        y--;
                }
            }
        }

        for (int i = 0; i < RiverHalfWidth; i++)
            Array2DHelper.Mask.Extend(ref river, true);

        return river;
    }

    private Dictionary<Biome, ChunkControl> GenerateNeededBiomes(Vector2Int chunkCoord, int ChunkSize, Biome topLeftCorner, Biome topRightCorner, Biome BottomLeftCorner, Biome BottomRightCorner)
    {
        Dictionary<Biome, ChunkControl> generatedBiomes = new Dictionary<Biome, ChunkControl>();

        if (!generatedBiomes.ContainsKey(topLeftCorner))
            generatedBiomes.Add(topLeftCorner, generators[topLeftCorner].GenerateChunk(chunkCoord, ChunkSize));
        if (!generatedBiomes.ContainsKey(topRightCorner))
            generatedBiomes.Add(topRightCorner, generators[topRightCorner].GenerateChunk(chunkCoord, ChunkSize));
        if (!generatedBiomes.ContainsKey(BottomLeftCorner))
            generatedBiomes.Add(BottomLeftCorner, generators[BottomLeftCorner].GenerateChunk(chunkCoord, ChunkSize));
        if (!generatedBiomes.ContainsKey(BottomRightCorner))
            generatedBiomes.Add(BottomRightCorner, generators[BottomRightCorner].GenerateChunk(chunkCoord, ChunkSize));
        return generatedBiomes;
    }

    private Vector2Int[] GenerateRiverEntrances(Vector2Int chunkCoord, int ChunkSize, Biome topLeftCorner, Biome topRightCorner, Biome BottomLeftCorner, Biome BottomRightCorner, out Vector2Int[] boundaryTilesPositions)
    {
        Stack<Vector2Int> boundaries = new Stack<Vector2Int>();
        List<Vector2Int> riverEntrances = new List<Vector2Int>();
        if (topLeftCorner != topRightCorner)
        {
            System.Random rand = new System.Random(WorldData.Seed + chunkCoord.x + (chunkCoord.y << 16));
            Vector2Int position = new Vector2Int((int)((GaussianRandom.generateNormalRandom(rand, 0, 1) + 4) / 8 * ChunkSize) + 1, 0);
            position.Clamp(new Vector2Int(RiverHalfWidth + 1, 0), new Vector2Int(ChunkSize - RiverHalfWidth + 1, 0));
            riverEntrances.Add(position);
            int center = position.x;
            boundaries.Push(new Vector2Int(center, position.y));
            for (int i = 1; i <= RiverHalfWidth; i++)
            {
                boundaries.Push(new Vector2Int(center - i, position.y));
                boundaries.Push(new Vector2Int(center + i, position.y));
            }
        }
        if (topRightCorner != BottomRightCorner)
        {
            System.Random rand = new System.Random(WorldData.Seed + (chunkCoord.x + 1 << 16 + chunkCoord.y));
            Vector2Int position = new Vector2Int(ChunkSize, (int)((GaussianRandom.generateNormalRandom(rand, 0, 1) + 4) / 8 * ChunkSize) + 1);
            position.Clamp(new Vector2Int(ChunkSize, RiverHalfWidth + 1), new Vector2Int(ChunkSize, ChunkSize - RiverHalfWidth + 1));
            riverEntrances.Add(position);
            int center = position.y;
            boundaries.Push(new Vector2Int(position.x + 1, center));
            for (int i = 1; i <= RiverHalfWidth; i++)
            {
                boundaries.Push(new Vector2Int(position.x + 1, center - i));
                boundaries.Push(new Vector2Int(position.x + 1, center + i));
            }
        }
        if (BottomRightCorner != BottomLeftCorner)
        {
            System.Random rand = new System.Random(WorldData.Seed + chunkCoord.x + ((chunkCoord.y + 1) << 16));
            Vector2Int position = new Vector2Int((int)((GaussianRandom.generateNormalRandom(rand, 0, 1) + 4) / 8 * ChunkSize) + 1, ChunkSize);
            position.Clamp(new Vector2Int(RiverHalfWidth + 1, ChunkSize), new Vector2Int(ChunkSize - RiverHalfWidth + 1, ChunkSize));
            riverEntrances.Add(position);
            int center = position.x;
            boundaries.Push(new Vector2Int(center, position.y + 1));
            for (int i = 1; i <= RiverHalfWidth; i++)
            {
                boundaries.Push(new Vector2Int(center - i, position.y + 1));
                boundaries.Push(new Vector2Int(center + i, position.y + 1));

            }
        }
        if (BottomLeftCorner != topLeftCorner)
        {
            System.Random rand = new System.Random(WorldData.Seed + ((chunkCoord.x) << 16 + chunkCoord.y));
            Vector2Int position = new Vector2Int(0, (int)((GaussianRandom.generateNormalRandom(rand, 0, 1) + 4) / 8 * ChunkSize) + 1);
            position.Clamp(new Vector2Int(0, RiverHalfWidth + 1), new Vector2Int(0, ChunkSize - RiverHalfWidth + 1));
            riverEntrances.Add(position);
            int center = position.y;
            boundaries.Push(new Vector2Int(position.x, center));
            for (int i = 1; i <= RiverHalfWidth; i++)
            {
                boundaries.Push(new Vector2Int(position.x, center - i));
                boundaries.Push(new Vector2Int(position.x, center + i));
            }
        }

        if (riverEntrances.Count == 0) throw new Exception("ERROR: Separator have nothing to split");

        boundaryTilesPositions = boundaries.ToArray();
        return riverEntrances.ToArray();
    }

    public override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Cardinal entrances = 0)
    => throw new System.Exception("ERROR: Separator biome shouldn't call this Method");
    public override void RequestChunkControl(Action<ChunkControl> callback, Vector2Int chunkCoord, int ChunkSize, Cardinal entrances = 0)
    => throw new System.Exception("ERROR: Separator biome shouldn't call this Method");
    protected override void ChunkControlThread(Action<ChunkControl> callback, Vector2Int chunkCoord, int ChunkSize, Cardinal entrances = 0)
    => throw new System.Exception("ERROR: Separator biome shouldn't call this Method");

}
