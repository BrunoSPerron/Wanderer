using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using csDelaunay;
using System;

public enum Biome : byte { NULL, FOREST, DESERT, SWAMP, MUSHROOM, RIVER }

public class BiomeMapGenerator
{
    public static BiomeMapInfo GenerateMap(int width, int height, float distanceBetweenBiomes, Biome[] biomes, NoiseSettings fuzzPerlin, int noisePower)
    {
        Array2D<Biome> map = new Array2D<Biome>(width, height);

        PoissonDiscSampler psd = new PoissonDiscSampler(new System.Random(WorldData.Seed), width, height, distanceBetweenBiomes);

        List<Vector2f> points = new List<Vector2f>();
        foreach (Vector2 sample in psd.Samples())
            points.Add(new Vector2f(sample.x, sample.y));

        Rectf bounds = new Rectf(0, 0, width - 1, height - 1);

        Voronoi voronoi = new Voronoi(points, bounds, 2, new System.Random(WorldData.Seed));

        int currentBiome = 1;
        foreach (KeyValuePair<Vector2f, Site> site in voronoi.SitesIndexedByLocation)
        {
            FillPolygon(ref map, site.Value.Region(bounds), biomes[currentBiome % biomes.Length]);
            currentBiome++;
        }

        map = BlurWithPerlin(map, fuzzPerlin, noisePower);
        SeparateBiomes(ref map, in voronoi, Biome.RIVER);
        return new BiomeMapInfo(map, voronoi);
    }

    private static void SeparateBiomes(ref Array2D<Biome> map, in Voronoi voronoi, Biome separator)
    {
        List<Tuple<List<Vector2f>, Site>> BiomeGroups = new List<Tuple<List<Vector2f>, Site>>();
        foreach (KeyValuePair<Vector2f, Site> kvp in voronoi.SitesIndexedByLocation)
        {
            bool hasBeenPlaced = false;
            for (int i = 0; i < BiomeGroups.Count; i++)
            {
                if (!hasBeenPlaced && kvp.Value == BiomeGroups[i].Item2)
                {
                    BiomeGroups[i].Item1.Add(kvp.Key);
                    hasBeenPlaced = true;
                }
            }
            if (!hasBeenPlaced)
            {
                BiomeGroups.Add(new Tuple<List<Vector2f>, Site>(new List<Vector2f>() { kvp.Key }, kvp.Value));
            }
        }

        //Create Outline Around biomes
        foreach (Tuple<List<Vector2f>, Site> tuple in BiomeGroups)
        {
            Vector2Int biomeCenter = new Vector2Int((int)tuple.Item1[0].x, (int)tuple.Item1[0].y);
            Biome biome = map[biomeCenter];

            Array2D<bool> tilesAccounted = new Array2D<bool>(map.Width, map.Height);
            Stack<Vector2Int> tilesToCheck = new Stack<Vector2Int>();
            tilesToCheck.Push(biomeCenter);
            tilesAccounted[biomeCenter] = true;
            Stack<Vector2Int> separatorStack = new Stack<Vector2Int>();

            while (tilesToCheck.Count != 0)
            {
                Vector2Int[] positionsAround = Array2DHelper.GetPositionsAround(map, tilesToCheck.Pop());
                foreach (Vector2Int v2i in positionsAround)
                {
                    if (!tilesAccounted[v2i])
                    {
                        tilesAccounted[v2i] = true;
                        if (map[v2i] == biome)
                        {
                            tilesToCheck.Push(v2i);
                        }
                        else
                        {
                            map[v2i] = separator;
                            separatorStack.Push(v2i);
                        }
                    }
                }
            }

            while (separatorStack.Count != 0)
            {
                Vector2Int current = separatorStack.Pop();
                Vector2Int[] positionsAround = Array2DHelper.GetPositionsAround(map, current);
                Biome b = Biome.NULL;
                int i = 0;
                bool replaceTile = true;
                while (replaceTile && i < positionsAround.Length)
                {
                    if (map[positionsAround[i]] != separator)
                    {
                        if (b != Biome.NULL)
                        {
                            if (map[positionsAround[i]] != b)
                                replaceTile = false;
                        }
                        else
                        {
                            b = map[positionsAround[i]];
                        }
                    }
                    i++;
                }
                if (replaceTile && b != Biome.NULL)
                    map[current] = b;
            }
        }
    }

    public static Sprite GetSprite(Array2D<Biome> biomeMap)
    {
        Texture2D texture = new Texture2D(biomeMap.Width, biomeMap.Height);

        Color[] colorMap = new Color[biomeMap.Width * biomeMap.Height];
        for (int y = 0; y < biomeMap.Height; y++)
        {
            for (int x = 0; x < biomeMap.Width; x++)
            {
                colorMap[y * biomeMap.Width + x] = GetBiomeColor(biomeMap[x, y]);
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, biomeMap.Width, biomeMap.Height), new Vector2(0, 0));

        Color GetBiomeColor(Biome biome) => biome switch
        {
            Biome.NULL => Color.black,
            Biome.FOREST => new Color(0, 0.5f, 0),
            Biome.DESERT => new Color(0.885f, 0.8f, 0.45f),
            Biome.SWAMP => new Color(0.1f, 0.42f, 0.34f),
            Biome.RIVER => new Color(0.31f, 0.56f, 0.73f),
            _ => Color.black,
        };
    }

    private static Array2D<Biome> BlurWithPerlin(Array2D<Biome> oldMap, NoiseSettings fuzzPerlin, int NoisePower)
    {
        Array2D<Biome> fuzzyMap = new Array2D<Biome>(oldMap.Width, oldMap.Height);

        float[,] verticalNoise = Noise.GenerateNoiseMap(oldMap.Width, oldMap.Height, fuzzPerlin, Vector2.zero);
        float[,] horizontalNoise = Noise.GenerateNoiseMap(oldMap.Width, oldMap.Height, fuzzPerlin, new Vector2(2 * oldMap.Width, 2 * oldMap.Height));

        for (int x = 0; x < oldMap.Width; x++)
        {
            for (int y = 0; y < oldMap.Height; y++)
            {
                int targetX = x + (int)((horizontalNoise[x, y] - .5f) * NoisePower);
                int targetY = y + (int)((verticalNoise[x, y] - .5f) * NoisePower);
                if (targetX < 0)
                    targetX = 0;
                else if (targetX >= oldMap.Width)
                    targetX = oldMap.Width - 1;
                if (targetY < 0)
                    targetY = 0;
                else if (targetY >= oldMap.Height)
                    targetY = oldMap.Height - 1;

                fuzzyMap[x, y] = oldMap[targetX, targetY];
                if (fuzzyMap[x, y] == Biome.NULL)
                    fuzzyMap[x, y] = GetANeighborBiome(oldMap, targetX, targetY);
            }
        }

        return fuzzyMap;

    }

    private static Biome GetANeighborBiome(Array2D<Biome> map, int x, int y)
    {
        if (x > 0 && map[x - 1, y] != Biome.NULL)
            return map[x - 1, y];
        if (y > 0 && map[x, y - 1] != Biome.NULL)
            return map[x, y - 1];
        if (x > 0 && y > 0 && map[x - 1, y - 1] != Biome.NULL)
            return map[x - 1, y - 1];
        if (x > 0 && y < map.Height - 1 && map[x - 1, y + 1] != Biome.NULL)
            return map[x - 1, y + 1];
        if (x < map.Width - 1 && y > 0 && map[x + 1, y - 1] != Biome.NULL)
            return map[x + 1, y - 1];
        if (x < map.Width - 1 && map[x + 1, y] != Biome.NULL)
            return map[x + 1, y];
        if (x > 0 && y < map.Height - 1 && map[x, y + 1] != Biome.NULL)
            return map[x, y + 1];
        if (x < map.Width - 1 && y < map.Height - 1 && map[x + 1, y + 1] != Biome.NULL)
            return map[x + 1, y + 1];

        return Biome.FOREST;
    }

    private static void FillPolygon(ref Array2D<Biome> biomeMap, List<Vector2f> vertices, Biome biome)
    {
        // Set polygon bounding box.
        int IMAGE_TOP = (int)vertices.Max(v => v.y);
        int IMAGE_BOTTOM = (int)vertices.Min(v => v.y);
        int IMAGE_RIGHT = (int)vertices.Max(v => v.x);
        int IMAGE_LEFT = (int)vertices.Min(v => v.x);

        int POLY_CORNERS = vertices.Count;

        // Decompose vertex components into parallel lists for looping.
        List<float> polyX = vertices.Select(v => v.x).ToList();
        List<float> polyY = vertices.Select(v => v.y).ToList();

        int[] nodeX = new int[POLY_CORNERS];
        int nodes, pixelX, pixelY, i, j, swap;

        // Scan through each row of the polygon.
        for (pixelY = IMAGE_BOTTOM; pixelY < IMAGE_TOP; pixelY++)
        {
            nodes = 0; j = POLY_CORNERS - 1;

            // Build list of nodes.
            for (i = 0; i < POLY_CORNERS; i++)
            {
                if (polyY[i] < (float)pixelY && polyY[j] >= (float)pixelY
                    || polyY[j] < (float)pixelY && polyY[i] >= (float)pixelY)
                {
                    nodeX[nodes] = (int)(polyX[i] + (pixelY - polyY[i]) /
                    (polyY[j] - polyY[i]) * (polyX[j] - polyX[i]));
                    nodes++;
                }
                j = i;
            }

            // Sort the nodes.
            i = 0;
            while (i < nodes - 1)
            {
                if (nodeX[i] > nodeX[i + 1])
                {
                    swap = nodeX[i];
                    nodeX[i] = nodeX[i + 1];
                    nodeX[i + 1] = swap;

                    if (i > 0)
                    {
                        i--;
                    }
                }
                else
                {
                    i++;
                }
            }

            // Fill the pixels between node pairs.
            for (i = 0; i < nodes; i += 2)
            {
                if (nodeX[i] >= IMAGE_RIGHT)
                {
                    break;
                }

                if (nodeX[i + 1] > IMAGE_LEFT)
                {
                    // Bind nodes past the left border.
                    if (nodeX[i] < IMAGE_LEFT)
                    {
                        nodeX[i] = IMAGE_LEFT;
                    }
                    // Bind nodes past the right border.
                    if (nodeX[i + 1] > IMAGE_RIGHT)
                    {
                        nodeX[i + 1] = IMAGE_RIGHT;
                    }
                    // Fill pixels between current node pair.
                    for (pixelX = nodeX[i]; pixelX < nodeX[i + 1]; pixelX++)
                    {
                        biomeMap[pixelX, pixelY] = biome;
                    }
                }
            }
        }
    }
}

public struct BiomeMapInfo
{
    public readonly Array2D<Biome> BiomeMap;
    public readonly Voronoi Voronoi;

    public BiomeMapInfo(Array2D<Biome> biomeMap, Voronoi voronoi)
    {
        BiomeMap = biomeMap;
        Voronoi = voronoi;
    }
}