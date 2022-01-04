using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_TEST : ChunkGenerator
{
    public GameObject[] Rocks;
    private GameObjectInfo[] RocksWithInfos;

    // Start is called before the first frame update
    void Start()
    {
        RocksWithInfos = ExtractInfosFrom(Rocks);
    }

    public override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Cardinal entrances = 0)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);

        FillWith(cc, TileType.FORESTGRASS);
        AddRoads(cc, TileType.FORESTDIRT);

        //AddSome(cc, RocksWithInfos, 200);
        //PoissonDistribution(cc, RocksWithInfos, 0.5f);
        ShatterGround(cc, TileType.FORESTGRASS, TileType.DESERTSAND, 100, true);

        AddTilesToLoadQueue(cc);

        return cc;
    }
}
