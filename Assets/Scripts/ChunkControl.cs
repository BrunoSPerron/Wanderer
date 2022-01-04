using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkControl
{
    public GameObject Chunk;

    public int GridSize;
    public Vector2Int ChunkCoord;
    public Cardinal Entrances;

    public tileInfo[,] TilesInfos;
    public bool[,] IsRoad;

    public Tilemap TileMap;

    private Stack<TileToInstantiate> TilesToInstantiate;

    public Stack<ObjectToInstantiate> ObjectsToInstantiate;

    internal bool individualRendererMode = false;

    public ChunkControl(Vector2Int coord, int gridSize, Cardinal entrances = 0)
    {
        TilesToInstantiate = new Stack<TileToInstantiate>();
        ObjectsToInstantiate = new Stack<ObjectToInstantiate>();

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

    public void InitiateChunk()
    {
        Chunk = new GameObject("Chunk " + ChunkCoord);

        Grid grid = Chunk.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(.5f, .25f, .5f);

        GameObject mapContainer = new GameObject("Tilemap");
        mapContainer.transform.parent = Chunk.transform;
        mapContainer.transform.position = new Vector3(0, -0.1f, 0);
        TileMap = mapContainer.AddComponent<Tilemap>();

        TilemapRenderer tileMapRenderer = mapContainer.AddComponent<TilemapRenderer>();
        tileMapRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
        tileMapRenderer.sortingLayerName = "Floor";
        
        if (individualRendererMode)
            tileMapRenderer.mode = TilemapRenderer.Mode.Individual;
        Chunk.transform.position = IsoGridHelper.GridToLocal(ChunkCoord) * GridSize;
    }

    public void AddDoodadAtPosition(GameObjectInfo go, Vector2 gridPos, List<Vector2Int> tilesInRadius)
    {
        Vector2 localPos = IsoGridHelper.GridToLocal(gridPos + Vector2.one);
        //int x = (int)gridPos.x + 1;
        //int y = (int)gridPos.y + 1;
        try
        {
            foreach (Vector2Int v2i in tilesInRadius)
            {
                TilesInfos[v2i.x, v2i.y].objectsRadius.Add(go.radius);
                TilesInfos[v2i.x, v2i.y].objectsGridPositions.Add(gridPos);
            }
        }
        catch
        {
            throw new System.Exception("ChunkError_DoodadPlacement: Invalid tile position in Doodad radius");
        }

        ObjectsToInstantiate.Push(new ObjectToInstantiate(go.gameObject, localPos));
    }

    public TileToInstantiate AddTileToInstantiate(TileBase tile, Vector2Int gridPos)
    {
        TilesToInstantiate.Push(new TileToInstantiate(tile, gridPos));
        return TilesToInstantiate.Peek();
    }

    public bool KeepInstantiating(int amount)
    {
        if (Chunk == null)
            InitiateChunk();

        if (TilesToInstantiate.Count > 0)
        {
            if (amount > TilesToInstantiate.Count)
                amount = TilesToInstantiate.Count;
            for (int i = 0; i < amount; i++)
            {
                TileToInstantiate current = TilesToInstantiate.Pop();
                TileMap.SetTile(new Vector3Int(current.gridPos.x, current.gridPos.y, 0), current.tile);
            }
        }
        else if (ObjectsToInstantiate.Count > 0)
        {
            if (amount > ObjectsToInstantiate.Count)
                amount = ObjectsToInstantiate.Count;

            for (int i = 0; i < amount; i++)
            {
                ObjectToInstantiate current = ObjectsToInstantiate.Pop();
                GameObject goInstance = Object.Instantiate(current.gameObject, current.localPos, Quaternion.identity, Chunk.transform);
                goInstance.transform.position += Chunk.transform.position;
            }
        }

        else return true;

        return false;
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
    public List<float> objectsRadius;
    public List<Vector2> objectsGridPositions;
    public tileInfo(TileType type)
    {
        this.type = type;
        objectsRadius = new List<float>();
        objectsGridPositions = new List<Vector2>();
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