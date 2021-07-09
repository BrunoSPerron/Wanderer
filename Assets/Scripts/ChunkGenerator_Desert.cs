using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_Desert : ChunkGenerator
{
    public TileBase SandTile;
    public TileBase AridTile;

    //public BiomeData_Forest ForestData;

    public GameObject[] Cacti;

    private GameObjectInfo[] CactiInfos;

    void Start()
    {
        CactiInfos = ExtractInfoFrom(Cacti);
    }

    protected override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);

        FillWith(cc, TileType.SAND);
        AddRoads(cc, TileType.ARID);

        PoissonDistribution(cc, CactiInfos, 12);

        Dictionary<TileType, TileBase> tileDict = new Dictionary<TileType, TileBase>();
        tileDict.Add(TileType.SAND, SandTile);
        tileDict.Add(TileType.ARID, AridTile);
        AddTilesToLoadQueue(cc, tileDict);

        return cc;
    }
}
