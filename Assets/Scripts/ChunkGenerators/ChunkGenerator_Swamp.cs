using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator_Swamp : ChunkGenerator
{
    public TileBase ExtraBoundary;
    public TileBase SwampGrassTile;
    public TileBase SwampDirtTile;
    public TileBase SwampWaterTile;

    public BiomeData_Swamp BiomeData;

    public GameObject[] Trees;
    public GameObject[] Bushes;
    public GameObject[] Cattails;
    public GameObject[] Waterlilies;

    private GameObjectInfo[] TreesWithInfos;
    private GameObjectInfo[] BushesWithInfos;
    private GameObjectInfo[] CattailsWithInfos;
    private GameObjectInfo[] WaterliliesWithInfos;

    void Start()
    {
        TreesWithInfos = ExtractInfosFrom(Trees);
        BushesWithInfos = ExtractInfosFrom(Bushes);
        CattailsWithInfos = ExtractInfosFrom(Cattails);
        WaterliliesWithInfos = ExtractInfosFrom(Waterlilies);
    }

    protected override ChunkControl GenerateChunk(Vector2Int chunkCoord, int ChunkSize, Vector2Int[] entrances = null)
    {
        ChunkControl cc = new ChunkControl(chunkCoord, ChunkSize, entrances);
        cc.individualRendererMode = true;

        FillWith(cc, TileType.GRASS);
        SetTilesFromPerlin(cc, BiomeData.WaterNoiseSettings, TileType.SWAMPWATER, BiomeData.WaterThreshold);
        RemoveSingletonTiles(cc, TileType.SWAMPWATER, TileType.GRASS);
        AddRoads(cc, TileType.MUD);
        ShatterGround(cc, TileType.GRASS, TileType.MUD, 100 - BiomeData.GroundCohesion, false);

        CustomPoissonDistribution(cc, TreesWithInfos, BiomeData.TreeSparcity);

        SuroundTileTypeWith(cc, TileType.SWAMPWATER, TileType.MUD, TileType.BOUNDARY2);

        PoissonDistributionWithPerlinNoiseOnTiletype(cc, BushesWithInfos, TileType.GRASS, BiomeData.ShrubSparcity, BiomeData.ShrubsNoiseSettings, BiomeData.BushesDistributionCurve);
        PoissonDistributionWithPerlinOnTileEdge(cc, CattailsWithInfos, TileType.SWAMPWATER, BiomeData.CattailSparcity,BiomeData.CattailNoiseSettings, BiomeData.CattailDistributionCurve);
        PoissonDistributionWithPerlinOnSurroundedTile(cc, WaterliliesWithInfos, TileType.SWAMPWATER, BiomeData.WaterlilySparcity, BiomeData.CattailNoiseSettings, BiomeData.WaterlilyDistributionCurve);

        Dictionary<TileType, TileBase> tileDict = new Dictionary<TileType, TileBase>();
        tileDict.Add(TileType.BOUNDARY2, ExtraBoundary);
        tileDict.Add(TileType.GRASS, SwampGrassTile);
        tileDict.Add(TileType.MUD, SwampDirtTile);
        tileDict.Add(TileType.SWAMPWATER, SwampWaterTile);
        AddTilesToLoadQueue(cc, tileDict);
        return cc;
    }

    /// <summary>
    /// Don't place on swampwater, replace all tiles it's on with grass and ensure water around is replaced with mud
    /// </summary>
    private void CustomPoissonDistribution(ChunkControl cc, GameObjectInfo[] objects, float averageDistance)
    {
        List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>> doodadToAdd = new List<Tuple<GameObjectInfo, Vector2, List<Vector2Int>>>();
        PoissonDiscSampler sampler = new PoissonDiscSampler(cc.GridSize - 3, cc.GridSize - 3, averageDistance);
        int i = 0;
        List<Vector2Int> tilesToReplaceIfWater = new List<Vector2Int>();
        foreach (Vector2 position in sampler.Samples())
        {
            Vector2 offsettedPos = position + new Vector2(1.5f, 1.5f);
            int currentGoiIndex = i % objects.Length;
            bool positionIsOK = cc.TilesInfos[(int)offsettedPos.x, (int)offsettedPos.y].type != TileType.SWAMPWATER;

            if (positionIsOK && !IsOverRoad(cc, offsettedPos, objects[currentGoiIndex].radius))
            {
                List<Vector2Int> tilesInRadius = GetTilesInRadius(cc, offsettedPos + Vector2.one, objects[currentGoiIndex].radius);
                foreach (Vector2Int v2i in tilesInRadius)
                {
                    foreach (Vector2Int positionNextTo in GetPositionsNextTo(cc, v2i.x, v2i.y))
                        if (!tilesToReplaceIfWater.Any(x => x.x == positionNextTo.x && x.y == positionNextTo.y))
                            tilesToReplaceIfWater.Add(positionNextTo);

                    cc.TilesInfos[v2i.x, v2i.y].type = TileType.GRASS;
                }
                doodadToAdd.Add(new Tuple<GameObjectInfo, Vector2, List<Vector2Int>>(objects[currentGoiIndex], offsettedPos, tilesInRadius));
                i++;
            }
        }

        foreach (Vector2Int v2i in tilesToReplaceIfWater)
        {
            if (cc.TilesInfos[v2i.x, v2i.y].type == TileType.SWAMPWATER)
                cc.TilesInfos[v2i.x, v2i.y].type = TileType.MUD;
        }

        foreach (Tuple<GameObjectInfo, Vector2, List<Vector2Int>> tuple in doodadToAdd)
            cc.AddDoodadAtPosition(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    
}
