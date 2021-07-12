using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_Desert : ChunkGenerator
{
    public TileBase SandTile;
    public TileBase AridTile;

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
        CactiInfos = ExtractInfoFrom(Cacti);
        RocksInfos = ExtractInfoFrom(Rocks);
        BonesInfos = ExtractInfoFrom(Bones);
        TreesInfos = ExtractInfoFrom(Trees);
        ShrubsInfos = ExtractInfoFrom(Shrubs);
    }

    protected override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);

        FillWith(cc, TileType.SAND);
        AddRoads(cc, TileType.ARID);

        int nbOfBonesToAdd = 0;
        for (int i = 0; i < BiomeData.maxBones; i++)
            if (rand.Next(0, 100) < BiomeData.boneChance)
                nbOfBonesToAdd++;

        AddSome(cc, RocksInfos, rand.Next(BiomeData.MinAmountOfRock, BiomeData.MaxAmountOfRock));
        AddSome(cc, BonesInfos, nbOfBonesToAdd);
        PoissonDistributionWithPerlinNoise(cc, TreesInfos, BiomeData.TreeSparcity, BiomeData.NoiseSettings, BiomeData.TreeChance, BiomeData.TreesDistributionCurve);
        PoissonDistributionWithPerlinNoise(cc, ShrubsInfos, BiomeData.ShrubSparcity, BiomeData.NoiseSettings, BiomeData.ShrubChance, BiomeData.ShrubsDistributionCurve);
        PoissonDistribution(cc, CactiInfos, BiomeData.CactiSparcity);

        Dictionary<TileType, TileBase> tileDict = new Dictionary<TileType, TileBase>();
        tileDict.Add(TileType.SAND, SandTile);
        tileDict.Add(TileType.ARID, AridTile);
        AddTilesToLoadQueue(cc, tileDict);

        return cc;
    }
}
