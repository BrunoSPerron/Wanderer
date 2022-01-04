using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_Forest : ChunkGenerator
{
    public BiomeData_Forest BiomeData;

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
        TreesWithInfos = ExtractInfosFrom(Trees);
        BushesWithInfos = ExtractInfosFrom(Bushes);
        SmallBushesWithInfos = ExtractInfosFrom(SmallBushes);
        SmallPlantsWithInfos = ExtractInfosFrom(SmallPlants);
        RocksWithInfos = ExtractInfosFrom(Rocks);
    }

    public override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Cardinal entrances = 0)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);
        System.Random rand = new System.Random(WorldData.Seed + (cc.ChunkCoord.x << 16 + cc.ChunkCoord.y));

        FillWith(cc, TileType.FORESTGRASS);
        AddRoads(cc, TileType.FORESTDIRT, 0.3f);
        AddSome(cc, RocksWithInfos, rand.Next(BiomeData.MinAmountOfRock, BiomeData.MaxAmountOfRock));
        PoissonDistribution(cc, TreesWithInfos, BiomeData.TreesSparcity);
        PoissonDistributionWithPerlinNoise(cc, BushesWithInfos, BiomeData.BushesSparcity, BiomeData.BushesNoiseSettings, BiomeData.BushesDistributionCurve);
        PoissonDistributionWithPerlinNoise(cc, SmallBushesWithInfos, BiomeData.SmallBushesSparcity, BiomeData.BushesNoiseSettings, BiomeData.SmallBushesDistributionCurve);
        PoissonDistributionWithPerlinNoise(cc, SmallPlantsWithInfos, BiomeData.FlowerSparcity, BiomeData.BushesNoiseSettings, BiomeData.FlowerChance, BiomeData.SmallBushesDistributionCurve, true, true);
        
        ShatterGround(cc, TileType.FORESTGRASS, TileType.FORESTDIRT, 100 - BiomeData.GroundCohesion, true);

        AddTilesToLoadQueue(cc);
        return cc;
    }
}
