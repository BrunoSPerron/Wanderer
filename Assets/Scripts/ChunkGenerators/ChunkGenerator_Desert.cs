using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_Desert : ChunkGenerator
{
    public BiomeData_Desert BiomeData;

    public GameObject[] Cacti;
    public GameObject[] Rocks;
    public GameObject[] Bones;
    public GameObject[] Trees;
    public GameObject[] Shrubs;
    private GameObjectInfo[] CactiInfos;
    private GameObjectInfo[] RocksInfos;
    private GameObjectInfo[] BonesInfos;
    private GameObjectInfo[] TreesInfos;
    private GameObjectInfo[] ShrubsInfos;

    void Start()
    {
        CactiInfos = ExtractInfosFrom(Cacti);
        RocksInfos = ExtractInfosFrom(Rocks);
        BonesInfos = ExtractInfosFrom(Bones);
        TreesInfos = ExtractInfosFrom(Trees);
        ShrubsInfos = ExtractInfosFrom(Shrubs);
    }

    public override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Cardinal entrances = 0)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);
        System.Random rand = new System.Random(WorldData.Seed + (cc.ChunkCoord.x << 16 + cc.ChunkCoord.y));

        FillWith(cc, TileType.DESERTSAND);
        AddRoads(cc, TileType.DESERTARID, 0.8f);

        int nbOfBonesToAdd = 0;
        for (int i = 0; i < BiomeData.maxBones; i++)
            if (rand.Next(0, 100) < BiomeData.boneChance)
                nbOfBonesToAdd++;

        AddSome(cc, RocksInfos, rand.Next(BiomeData.MinAmountOfRock, BiomeData.MaxAmountOfRock));
        AddSome(cc, BonesInfos, nbOfBonesToAdd);
        PoissonDistributionWithPerlinNoise(cc, TreesInfos, BiomeData.TreeSparcity, BiomeData.NoiseSettings, BiomeData.TreeChance, BiomeData.TreesDistributionCurve);
        PoissonDistributionWithPerlinNoise(cc, ShrubsInfos, BiomeData.ShrubSparcity, BiomeData.NoiseSettings, BiomeData.ShrubChance, BiomeData.ShrubsDistributionCurve);
        PoissonDistribution(cc, CactiInfos, BiomeData.CactiSparcity);

        AddTilesToLoadQueue(cc);

        return cc;
    }
}
