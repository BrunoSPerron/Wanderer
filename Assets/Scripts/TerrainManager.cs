using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Cardinal { E, NE, N, NW, W, SW, S, SE }
public class TerrainManager : MonoBehaviour
{
    public GameObject PlayerAvatar;
    [HideInInspector] public Vector2Int playerGridPosition;

    public int ChunkSize = 30;
    public int maxNbObjectLoadedEachFrame = 200;

    public BiomeGenerator BiomeGenerator;
    BiomeMapInfo BiomeMapInfo;

    public ChunkGenerator_Forest ForestChunkGenerator;
    public ChunkGenerator_Desert DesertChunkGenerator;

    private ChunkControl[,] Chunks;
    private List<Vector2Int> ChunksRequested;
    private List<ChunkControl> ChunksStillLoading;

    void Start()
    {
        Noise.Initiate();
        Chunks = new ChunkControl[200, 200];
        ChunksRequested = new List<Vector2Int>();
        ChunksStillLoading = new List<ChunkControl>();
        PlayerAvatar.transform.position = new Vector2(0, 800);

        Biome[] biomes = new Biome[2];
        biomes[0] = Biome.FOREST;
        biomes[1] = Biome.DESERT;
        BiomeMapInfo = BiomeGenerator.GenerateMap(200, 200, 50, biomes);
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 floatyPlayerGridPos = TerrainHelper.LocalToGrid(new Vector2(PlayerAvatar.transform.position.x, PlayerAvatar.transform.position.y)) / ChunkSize;
        playerGridPosition = new Vector2Int((int)floatyPlayerGridPos.x, (int)floatyPlayerGridPos.y);
        
        ActivateChunkAround(playerGridPosition);
       
        if (ChunksStillLoading.Count > 0)
        {
            if (ChunksStillLoading[0].KeepInstantiating(maxNbObjectLoadedEachFrame))
                ChunksStillLoading.RemoveAt(0);
        }
    }

    void OnChunkControlReceived(ChunkControl cc)
    {
        ChunksRequested.Remove(cc.ChunkCoord);
        Chunks[cc.ChunkCoord.x, cc.ChunkCoord.y] = cc;
        ChunksStillLoading.Add(cc);
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
        if (Chunks[coord.x, coord.y] != null)
        {
            Chunks[coord.x, coord.y].SetActive();
        }
        else if (!ChunksRequested.Contains(coord))
        {
            Vector2Int[] entrances = new Vector2Int[2];

            if (coord.y % 2 == 1)
                entrances = new Vector2Int[0];
            else
            {
                entrances = new Vector2Int[2];
                if (Chunks[coord.x - 1, coord.y] != null)
                {
                    foreach (Vector2Int entrance in Chunks[coord.x - 1, coord.y].Entrances)
                        if (entrance.x >= ChunkSize)
                            entrances[0] = new Vector2Int(0, entrance.y);
                    if (entrances[0] == new Vector2Int(0, 0))
                        entrances[0] = new Vector2Int(0, (int)((GaussianRandom.generateNormalRandom(0, 1) + 4) / 8 * ChunkSize));
                }
                else
                {
                    entrances[0] = new Vector2Int(0, (int)((GaussianRandom.generateNormalRandom(0, 1) + 4) / 8 * ChunkSize));
                }

                if (Chunks[coord.x + 1, coord.y] != null)
                {
                    foreach (Vector2Int entrance in Chunks[coord.x + 1, coord.y].Entrances)
                        if (entrance.x <= 2)
                            entrances[1] = new Vector2Int(ChunkSize, entrance.y);
                    if (entrances[1] == new Vector2Int(0, 0))
                        entrances[1] = new Vector2Int(ChunkSize, (int)((GaussianRandom.generateNormalRandom(0, 1) + 4) / 8 * ChunkSize));
                }
                else
                {
                    entrances[1] = new Vector2Int(ChunkSize, (int)((GaussianRandom.generateNormalRandom(0, 1) + 4) / 8 * ChunkSize));
                }
            }

            ChunksRequested.Add(coord);

            switch (BiomeMapInfo.BiomeMap[coord.x, coord.y])
            {
                case Biome.FOREST:
                    ForestChunkGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, entrances);
                    break;
                case Biome.DESERT:
                    DesertChunkGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, entrances);
                    break;
                default:
                    ForestChunkGenerator.RequestChunkControl(OnChunkControlReceived, coord, ChunkSize, entrances);
                    break;
            }

        }
    }
}


/*
    > 
   /   y
  /
 
  \
   \   x
    >
*/