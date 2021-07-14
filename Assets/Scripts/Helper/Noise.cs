using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static int[] Seeds;

    public static void Initiate()
    {
        Seeds = new int[4];
        for (int i = 0; i < Seeds.Length; i++)
            Seeds[i] = System.Guid.NewGuid().GetHashCode();
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random srand = new System.Random(Seeds[settings.seedIndex]);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency;

        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = srand.Next(-100000, 100000) + settings.offset.x + sampleCentre.x;
            float offsetY = srand.Next(-100000, 100000) + settings.offset.y + sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < settings.octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }


                noiseMap[x, y] = noiseHeight;

                float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight);
                noiseMap[x, y] = normalizedHeight;

            }
        }
        /*for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(0, maxPossibleHeight*500, noiseMap[x, y]);
            }
        }*/
        
        /*if (settings.normalizeMode == NormalizeMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }*/

        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings
{
    public float scale = 50;

    [Range(1, 15)]
    public int octaves = 6;
    [Range(0, 1)]
    public float persistance = .5f;
    [Range(1, 10)]
    public float lacunarity = 2.5f;

    [Range(0,4)]
    public int seedIndex;
    public Vector2 offset;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}