using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Cardinal : byte
{
    N = 0b0001,
    W = 0b0010,
    S = 0b0100,
    E = 0b1000
}

public class TerrainManager : MonoBehaviour
{
    public GameObject PlayerAvatar;
    [HideInInspector] public Vector2Int playerGridPosition;
    [HideInInspector] public Vector2Int lastplayerGridPosition;

    public int MapSize = 200;
    public int ChunkSize = 30;
    public int maxNbObjectLoadedEachFrame = 200;

    [HideInInspector] public BiomeMapInfo BiomeMapInfo;
    [HideInInspector] static public Dictionary<TileType, UnityEngine.Tilemaps.TileBase> TilesDictionary;
    public SerializableTilesCollection tilesCollection;


    public NoiseSettings BiomeMapPerlin;
    public int BiomeMapNoisePower;

    public ChunkGenerator_Separator SeparatorGenerator;
    public ChunkGenerator ForestChunkGenerator;
    public ChunkGenerator DesertChunkGenerator;
    public ChunkGenerator SwampChunkGenerator;

    //private Array2D<ChunkControl> Chunks;
    public int ChunkMaxDistance;
    private List<ChunkControl> LoadedChunks;
    private List<Vector2Int> ChunksRequested;
    private List<ChunkControl> ChunksStillLoading;

    private Array2D<Cardinal> Entrances;

    void Start()
    {
        TilesDictionary = new Dictionary<TileType, UnityEngine.Tilemaps.TileBase>();
        for (int i = 0; i < tilesCollection.Keys.Count; i++)
            TilesDictionary.Add(tilesCollection.Keys[i], tilesCollection.Values[i]);

        WorldData.Initiate();
        Debug.Log("Seed: " + WorldData.Seed);
        LoadedChunks = new List<ChunkControl>();
        Entrances = new Array2D<Cardinal>(MapSize, MapSize);
        ChunksRequested = new List<Vector2Int>();
        ChunksStillLoading = new List<ChunkControl>();
        PlayerAvatar.transform.position = new Vector2(0, MapSize * 4);

        Biome[] biomes = new Biome[3];
        biomes[0] = Biome.FOREST;
        biomes[1] = Biome.DESERT;
        biomes[2] = Biome.SWAMP;
        BiomeMapInfo = BiomeMapGenerator.GenerateMap(MapSize, MapSize, 50, biomes, BiomeMapPerlin, BiomeMapNoisePower);
        LinkBiomesWithRoads();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 floatyPlayerGridPos = IsoGridHelper.LocalToGrid(new Vector2(PlayerAvatar.transform.position.x, PlayerAvatar.transform.position.y)) / ChunkSize;
        lastplayerGridPosition = playerGridPosition;
        playerGridPosition = new Vector2Int((int)floatyPlayerGridPos.x, (int)floatyPlayerGridPos.y);
        if (lastplayerGridPosition != playerGridPosition)
        {
            DestroyChunksTooFar();
            ActivateChunkAround(playerGridPosition);
        }


        if (ChunksStillLoading.Count > 0)
        {
            if (ChunksStillLoading[0].KeepInstantiating(maxNbObjectLoadedEachFrame))
                ChunksStillLoading.RemoveAt(0);
        }
    }

    private void DestroyChunksTooFar()
    {
        Stack<int> indexesToRemove = new Stack<int>();
        for (int i = 0; i < LoadedChunks.Count; i++)
        {
            ChunkControl cc = LoadedChunks[i];
            if (Math.Abs(cc.ChunkCoord.x - playerGridPosition.x) > ChunkMaxDistance || Math.Abs(cc.ChunkCoord.y - playerGridPosition.y) > ChunkMaxDistance)
            {
                bool removedFromLoading = false;
                int j = 0;
                while (!removedFromLoading && j < ChunksStillLoading.Count)
                {
                    if (ChunksStillLoading[j].ChunkCoord == cc.ChunkCoord)
                    {
                        removedFromLoading = true;
                        ChunksStillLoading.RemoveAt(j);
                    }
                    else
                        j++;
                }

                indexesToRemove.Push(i);
            }
        }

        while (indexesToRemove.Count != 0)
        {
            Destroy(LoadedChunks[indexesToRemove.Peek()].Chunk);
            LoadedChunks.RemoveAt(indexesToRemove.Pop());
        }
    }

    void OnChunkControlReceived(ChunkControl cc)
    {
        ChunksRequested.Remove(cc.ChunkCoord);
        LoadedChunks.Add(cc);
        ChunksStillLoading.Add(cc);
        cc.SetActive();
    }

    void ActivateChunkAround(Vector2Int isoCoord)
    {
        ActivateChunk(isoCoord);
        ActivateChunk(isoCoord.x, isoCoord.y + 1);
        ActivateChunk(isoCoord.x, isoCoord.y - 1);
        ActivateChunk(isoCoord.x - 1, isoCoord.y + 1);
        ActivateChunk(isoCoord.x - 1, isoCoord.y);
        ActivateChunk(isoCoord.x - 1, isoCoord.y - 1);
        ActivateChunk(isoCoord.x + 1, isoCoord.y + 1);
        ActivateChunk(isoCoord.x + 1, isoCoord.y);
        ActivateChunk(isoCoord.x + 1, isoCoord.y - 1);
    }

    void ActivateChunk(int x, int y) => ActivateChunk(new Vector2Int(x, y));
    void ActivateChunk(Vector2Int coord)
    {
        if (!ChunksRequested.Contains(coord))
        {
            bool alreadyInLoadedChunks = false;
            int i = 0;
            while (!alreadyInLoadedChunks && i < LoadedChunks.Count)
            {
                if (LoadedChunks[i].ChunkCoord == coord)
                    alreadyInLoadedChunks = true;
                else
                    i++;

            }

            if (!alreadyInLoadedChunks)
            {
                ChunksRequested.Add(coord);

                switch (BiomeMapInfo.BiomeMap[coord])
                {
                    case Biome.FOREST:
                        ForestChunkGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, Entrances[coord]);
                        break;
                    case Biome.DESERT:
                        DesertChunkGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, Entrances[coord]);
                        break;
                    case Biome.SWAMP:
                        SwampChunkGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, Entrances[coord]);
                        break;
                    case Biome.RIVER:
                        SeparatorGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, GetTopLeft(), GetTopRight(), GetBottomLeft(), GetBottomRight(), Entrances[coord]);
                        break;
                    default:
                        ForestChunkGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, Entrances[coord]);
                        break;
                }
            }
        }

        Biome GetTopLeft()
        {
            Biome biome = Biome.RIVER;
            if (BiomeMapInfo.BiomeMap[coord.x - 1, coord.y - 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x - 1, coord.y - 1];
            else if (BiomeMapInfo.BiomeMap[coord.x - 1, coord.y] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x - 1, coord.y];
            else if (BiomeMapInfo.BiomeMap[coord.x, coord.y - 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x, coord.y - 1];
            return biome;
        }
        Biome GetTopRight()
        {
            Biome biome = Biome.RIVER;
            if (BiomeMapInfo.BiomeMap[coord.x + 1, coord.y - 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x + 1, coord.y - 1];
            else if (BiomeMapInfo.BiomeMap[coord.x + 1, coord.y] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x + 1, coord.y];
            else if (BiomeMapInfo.BiomeMap[coord.x, coord.y - 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x, coord.y - 1];
            return biome;
        }
        Biome GetBottomLeft()
        {
            Biome biome = Biome.RIVER;
            if (BiomeMapInfo.BiomeMap[coord.x - 1, coord.y + 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x - 1, coord.y + 1];
            else if (BiomeMapInfo.BiomeMap[coord.x - 1, coord.y] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x - 1, coord.y];
            else if (BiomeMapInfo.BiomeMap[coord.x, coord.y + 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x, coord.y + 1];
            return biome;
        }
        Biome GetBottomRight()
        {
            Biome biome = Biome.RIVER;
            if (BiomeMapInfo.BiomeMap[coord.x + 1, coord.y + 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x + 1, coord.y + 1];
            else if (BiomeMapInfo.BiomeMap[coord.x + 1, coord.y] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x + 1, coord.y];
            else if (BiomeMapInfo.BiomeMap[coord.x, coord.y + 1] != Biome.RIVER)
                biome = BiomeMapInfo.BiomeMap[coord.x, coord.y + 1];
            return biome;
        }
    }


    void LinkBiomesWithRoads()
    {
        Dictionary<Vector2f, csDelaunay.Site> remainingCoords = new Dictionary<Vector2f, csDelaunay.Site>(BiomeMapInfo.Voronoi.SitesIndexedByLocation);
        foreach (KeyValuePair<Vector2f, csDelaunay.Site> kvp in BiomeMapInfo.Voronoi.SitesIndexedByLocation)
        {
            if (remainingCoords.ContainsKey(kvp.Key))
                remainingCoords.Remove(kvp.Key);

            List<Vector2f> neighbors = BiomeMapInfo.Voronoi.NeighborSitesForSite(kvp.Key);
            foreach (Vector2f vector2F in neighbors)
            {
                if (remainingCoords.ContainsKey(vector2F))
                {
                    Vector2Int start = new Vector2Int((int)kvp.Key.x, (int)kvp.Key.y);
                    Vector2Int end = new Vector2Int((int)vector2F.x, (int)vector2F.y);
                    AddRoad(start, end);
                }
            }
        }
    }

    private void AddRoad(Vector2Int start, Vector2Int end)
    {
        System.Random rand = new System.Random(WorldData.Seed);
        Vector2Int currentPos = start;
        while (currentPos != end)
        {
            Vector2Int nextPos = currentPos;
            int weightX = Math.Abs(end.x - currentPos.x);
            int weightY = Math.Abs(end.y - currentPos.y);
            if (rand.Next(0, weightX + weightY) < weightX)
            {
                if (nextPos.x > end.x)
                    nextPos.x--;
                else
                    nextPos.x++;
            }
            else
            {
                if (nextPos.y > end.y)
                    nextPos.y--;
                else
                    nextPos.y++;
            }
            AddSharedEntrance(currentPos, nextPos);
            currentPos = nextPos;
        }
    }

    private void AddSharedEntrance(Vector2Int position1, Vector2Int position2)
    {
        if (position1.x > position2.x)
        {
            Entrances[position1] |= Cardinal.W;
            Entrances[position2] |= Cardinal.E;
        }
        else if (position1.x < position2.x)
        {
            Entrances[position1] |= Cardinal.E;
            Entrances[position2] |= Cardinal.W;
        }
        else if (position1.y > position2.y)
        {
            Entrances[position1] |= Cardinal.S;
            Entrances[position2] |= Cardinal.N;
        }
        else
        {
            Entrances[position1] |= Cardinal.N;
            Entrances[position2] |= Cardinal.S;
        }
    }
}


/*
  Y <    > X 
     \  /
      \/
*/