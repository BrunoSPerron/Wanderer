using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_Forest : ChunkGenerator
{
    public TileBase ForestGrassTile;
    public TileBase ForestDirtTile;

    public BiomeData_Forest ForestData;

    public GameObject[] Trees;
    public GameObject[] Bushes;
    public GameObject[] SmallBushes;
    public GameObject[] SmallPlants;
    public GameObject[] Rocks;

    private GameObjectInfo[] TreesWithInfos;
    private GameObjectInfo[] BushesWithInfos;
    private GameObjectInfo[] SmallBushesWithInfos;
    private GameObjectInfo[] SmallPlantsWithInfos;
    private GameObjectInfo[] RocksWithInfos;

    void Start()
    {
        TreesWithInfos = ExtractInfoFrom(Trees);
        BushesWithInfos = ExtractInfoFrom(Bushes);
        SmallBushesWithInfos = ExtractInfoFrom(SmallBushes);
        SmallPlantsWithInfos = ExtractInfoFrom(SmallPlants);
        RocksWithInfos = ExtractInfoFrom(Rocks);
    }

    protected override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);

        FillWith(cc, TileType.GRASS);
        AddRoads(cc, TileType.DIRT);

        AddSome(cc, RocksWithInfos, rand.Next(0, 5));
        PoissonDistribution(cc, TreesWithInfos, ForestData.TreesSparcity);
        PoissonDistributionWithPerlinNoise(cc, BushesWithInfos, ForestData.BushesSparcity, ForestData.BushesNoiseSettings, ForestData.BushesDistributionCurve);
        PoissonDistributionWithPerlinNoise(cc, SmallBushesWithInfos, ForestData.SmallBushesSparcity, ForestData.BushesNoiseSettings, ForestData.BushesDistributionCurve);
        PoissonDistributionWithPerlinNoise(cc, SmallPlantsWithInfos, 1.2f, ForestData.BushesNoiseSettings, ForestData.FlowerChance, ForestData.BushesDistributionCurve, true, true);

        Dictionary<TileType, TileBase> tileDict = new Dictionary<TileType, TileBase>();
        tileDict.Add(TileType.GRASS, ForestGrassTile);
        tileDict.Add(TileType.DIRT, ForestDirtTile);
        AddTilesToLoadQueue(cc, tileDict);
        return cc;
    }
}
