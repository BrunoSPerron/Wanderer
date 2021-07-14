using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using csDelaunay;
using System;

public enum Biome : byte { NULL, FOREST, DESERT, SWAMP, MUSHROOM }

public class BiomeGenerator : MonoBehaviour
{
    public NoiseSettings fuzzPerlin;
    public int NoisePower;
    [HideInInspector]public BiomeMapInfo currentMap;

    public BiomeMapInfo GenerateMap(int width, int height, float distanceBetweenBiomes, Biome[] biomes)
    {
        Biome[,] map = new Biome[width, height];

        PoissonDiscSampler psd = new PoissonDiscSampler(width, height, distanceBetweenBiomes);

        List<Vector2f> points = new List<Vector2f>();
        foreach (Vector2 sample in psd.Samples())
            points.Add(new Vector2f(sample.x, sample.y));

        Rectf bounds = new Rectf(0, 0, width - 1, height - 1);

        Voronoi voronoi = new Voronoi(points, bounds, 2);

        int currentBiome = 1;
        foreach (KeyValuePair<Vector2f, Site> site in voronoi.SitesIndexedByLocation)
        {
            FillPolygon(ref map, site.Value.Region(bounds), biomes[currentBiome % biomes.Length]);
            currentBiome++;
        }

        currentMap = new BiomeMapInfo(BlurWithPerlin(map), voronoi);
        return currentMap;
    }

    public Sprite GetSprite(Biome[,] biomeMap)
    {
        int width = biomeMap.GetLength(0);
        int height = biomeMap.GetLength(0);

        Texture2D texture = new Texture2D(width, height);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = GetBiomeColor(biomeMap[x, y]);
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0, 0));

        Color GetBiomeColor(Biome biome) => biome switch
            {
                Biome.NULL => Color.black,
                Biome.FOREST => new Color(0, 0.5f, 0),
                Biome.DESERT => new Color(0.885f, 0.8f, 0.45f),
                Biome.SWAMP => new Color(0.1f, 0.42f, 0.34f),
                _ => Color.black,
            };
        }

    private Biome[,] BlurWithPerlin(Biome[,] oldMap)
    {
        int size = oldMap.GetLength(0);
        Biome[,] fuzzyMap = new Biome[size, size];

        float[,] verticalNoise = Noise.GenerateNoiseMap(size, size, fuzzPerlin, Vector2.zero);
        float[,] horizontalNoise = Noise.GenerateNoiseMap(size, size, fuzzPerlin, new Vector2(2 * size, 2 * size));

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                int targetX = x + (int)((horizontalNoise[x, y] - .5f) * NoisePower);
                int targetY = y + (int)((verticalNoise[x, y] - .5f) * NoisePower);
                if (targetX < 0)
                    targetX = 0;
                else if (targetX >= size)
                    targetX = size - 1;
                if (targetY < 0)
                    targetY = 0;
                else if (targetY >= size)
                    targetY = size - 1;

                fuzzyMap[x, y] = oldMap[targetX, targetY];
                if (fuzzyMap[x, y] == Biome.NULL)
                    fuzzyMap[x, y] = GetANeighborBiome(oldMap, targetX, targetY);
            }
        }

        return fuzzyMap;

    }

    private Biome GetANeighborBiome(Biome[,] map, int x, int y)
    {
        if (x > 0 && map[x - 1, y] != Biome.NULL) 
            return map[x - 1, y];
        if (y > 0 && map[x, y - 1] != Biome.NULL) 
            return map[x, y - 1];
        if (x > 0 && y > 0 && map[x - 1, y - 1] != Biome.NULL) 
            return map[x - 1, y - 1];
        if (x > 0 && y < map.GetLength(1) - 1 && map[x - 1, y + 1] != Biome.NULL) 
            return map[x - 1, y + 1];
        if (x < map.GetLength(0) - 1 && y > 0 && map[x + 1, y - 1] != Biome.NULL) 
            return map[x + 1, y - 1];
        if (x < map.GetLength(0) - 1 && map[x + 1, y] != Biome.NULL) 
            return map[x + 1, y];
        if (x > 0 && y < map.GetLength(1) - 1 && map[x, y + 1] != Biome.NULL) 
            return map[x, y + 1];
        if (x < map.GetLength(0) - 1 && y < map.GetLength(1) - 1 && map[x + 1, y + 1] != Biome.NULL) 
            return map[x + 1, y + 1];

        return Biome.FOREST;
    }

    private void FillPolygon(ref Biome[,] biomeMap, List<Vector2f> vertices, Biome biome)
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
    public readonly Biome[,] BiomeMap;
    public readonly Voronoi Voronoi;

    public BiomeMapInfo(Biome[,] biomeMap, Voronoi voronoi)
    {
        BiomeMap = biomeMap;
        Voronoi = voronoi;
    }
}