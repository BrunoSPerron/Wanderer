using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_TEST : ChunkGenerator
{
    public TileBase ForestGrassTile;
    public TileBase ForestDirtTile;
    public TileBase DesertSandTile;

    public GameObject[] Rocks;
    private GameObjectInfo[] RocksWithInfos;

    // Start is called before the first frame update
    void Start()
    {
        RocksWithInfos = ExtractInfoFrom(Rocks);
    }

    protected override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);

        FillWith(cc, TileType.GRASS);
        AddRoads(cc, TileType.DIRT);

        //AddSome(cc, RocksWithInfos, 200);
        //PoissonDistribution(cc, RocksWithInfos, 0.5f);
        ShatterGround(cc, TileType.GRASS, TileType.SAND, 100, true);

        Dictionary<TileType, TileBase> tileDict = new Dictionary<TileType, TileBase>();
        tileDict.Add(TileType.GRASS, ForestGrassTile);
        tileDict.Add(TileType.DIRT, ForestDirtTile);
        tileDict.Add(TileType.SAND, DesertSandTile);
        AddTilesToLoadQueue(cc, tileDict);

        return cc;
    }
}
