using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkControl
{
    public GameObject Chunk;

    public int GridSize;
    public Vector2Int ChunkCoord;
    public Vector2Int[] Entrances;

    public tileInfo[,] TilesInfos;
    public bool[,] IsRoad;

    public Tilemap TileMap;

    private Stack<TileToInstantiate> TilesToInstantiate;

    public ChunkControl(Vector2Int coord, int gridSize, Vector2Int[] entrances)
    {
        TilesToInstantiate = new Stack<TileToInstantiate>();

        ChunkCoord = coord;
        Entrances = entrances;
        GridSize = gridSize;
        IsRoad = new bool[gridSize, gridSize];
        TilesInfos = new tileInfo[GridSize + 2, GridSize + 2];

        for (int x = 0; x < TilesInfos.GetLength(0); x++)
        {
            for (int y = 0; y < TilesInfos.GetLength(1); y++)
            {
                TilesInfos[x, y] = new tileInfo(TileType.NONE);
            }
        }
    }

    public ObjectToInstantiate AddDoodadAtPosition(GameObjectInfo go,  Vector2 gridPos)
    {
        Vector2 localPos = TerrainHelper.GridToLocal(gridPos);
        int x = Mathf.RoundToInt(gridPos.x);
        int y = Mathf.RoundToInt(gridPos.y);
        try
        {
            TilesInfos[x, y].objectsOnTileInfos.Add(go);
            TilesInfos[x, y].objectsGridPositions.Add(gridPos);
            TilesInfos[x, y].objectToInstantiates.Push(new ObjectToInstantiate(go.gameObject, localPos));
        }
        catch
        {
            throw new System.Exception("error while preparing chunk. Failed to add doodad at grid pos: " + gridPos + " - local pos: " + localPos);
        }
        return TilesInfos[x, y].objectToInstantiates.Peek();
    }

    public TileToInstantiate AddTileToInstantiate(TileBase tile, Vector2Int gridPos)
    {
        TilesToInstantiate.Push(new TileToInstantiate(tile, gridPos));
        return TilesToInstantiate.Peek();
    }

    public bool KeepInstantiating(int amount)
    {
        if (Chunk == null)
        {
            Chunk = new GameObject("Chunk " + ChunkCoord);

            Grid grid = Chunk.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(.5f, .25f, .5f);

            GameObject mapContainer = new GameObject("Tilemap");
            mapContainer.transform.parent = Chunk.transform;
            mapContainer.transform.position = new Vector3(0, -0.25f, 0);
            TileMap = mapContainer.AddComponent<Tilemap>();

            TilemapRenderer tileMapRenderer = mapContainer.AddComponent<TilemapRenderer>();
            tileMapRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            tileMapRenderer.sortingLayerName = "Floor";
            Chunk.transform.position = TerrainHelper.GridToLocal(ChunkCoord) * GridSize;
        }

        if (TilesToInstantiate.Count > 0)
        {
            if (amount > TilesToInstantiate.Count)
                amount = TilesToInstantiate.Count;
            for (int i = 0; i < amount; i++)
            {
                TileToInstantiate current = TilesToInstantiate.Pop();
                TileMap.SetTile(new Vector3Int(current.gridPos.x, current.gridPos.y, 0), current.tile);
                while (TilesInfos[current.gridPos.x, current.gridPos.y].objectToInstantiates.Count != 0)
                {
                    ObjectToInstantiate goi = TilesInfos[current.gridPos.x, current.gridPos.y].objectToInstantiates.Pop();
                    GameObject goInstance = Object.Instantiate(goi.gameObject, goi.localPos, Quaternion.identity, Chunk.transform);
                    goInstance.transform.position += Chunk.transform.position;
                    goInstance.GetComponent<DoodadData>().tileAttachedTo = current.gridPos;
                    i++;
                }
            }
        }
        
        else return true;

        return false;
    }

    public int GetEntrance(Cardinal direction)
    {
        switch (direction)
        {
            case Cardinal.NE:
                foreach (Vector2Int entrance in Entrances)
                {
                    if (entrance.x == GridSize)
                        return entrance.y;
                }
                break;
            case Cardinal.NW:
                foreach (Vector2Int entrance in Entrances)
                {
                    if (entrance.y == GridSize)
                        return entrance.x;
                }
                break;
            case Cardinal.SW:
                foreach (Vector2Int entrance in Entrances)
                {
                    if (entrance.x == 0)
                        return entrance.y;
                }
                break;
            case Cardinal.SE:
                foreach (Vector2Int entrance in Entrances)
                {
                    if (entrance.y == 0)
                        return entrance.x;
                }
                break;
        }
        return -1;
    }

    public void SetActive(bool b = true)
    {
        if (Chunk != null)
            Chunk.SetActive(b);
    }

}

public struct tileInfo
{
    public TileType type;
    public List<GameObjectInfo> objectsOnTileInfos;
    public List<Vector2> objectsGridPositions;
    public Stack<ObjectToInstantiate> objectToInstantiates;
    public tileInfo(TileType type)
    {
        this.type = type;
        objectsOnTileInfos = new List<GameObjectInfo>();
        objectsGridPositions = new List<Vector2>();
        objectToInstantiates = new Stack<ObjectToInstantiate>();
    }
}

public struct ObjectToInstantiate
{
    public GameObject gameObject;
    public Vector2 localPos;

    public ObjectToInstantiate(GameObject gameObject, Vector2 localPos)
    {
        this.gameObject = gameObject;
        this.localPos = localPos;
    }
}

public struct TileToInstantiate
{
    public TileBase tile;
    public Vector2Int gridPos;

    public TileToInstantiate(TileBase tile, Vector2Int gridPos)
    {
        this.tile = tile;
        this.gridPos = gridPos;
    }
}