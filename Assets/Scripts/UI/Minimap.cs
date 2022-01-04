using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public GameObject terrainManagerObject;
    TerrainManager terrainManagerScript;
    Color colorUnderPlayerPos;
    
    Texture2D image;
    // Start is called before the first frame update
    void Start()
    {
        terrainManagerScript = terrainManagerObject.GetComponent<TerrainManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (image is null)
        {
            GetComponent<Image>().sprite = BiomeMapGenerator.GetSprite(terrainManagerScript.BiomeMapInfo.BiomeMap);
            image = GetComponent<Image>().sprite.texture;
            colorUnderPlayerPos = image.GetPixel(terrainManagerScript.playerGridPosition.x, terrainManagerScript.playerGridPosition.y);
            image.filterMode = FilterMode.Point;
        }

        if (terrainManagerScript.lastplayerGridPosition != terrainManagerScript.playerGridPosition)
        {
            int width = image.width;
            Color[] colorMap = image.GetPixels();
            colorMap[terrainManagerScript.lastplayerGridPosition.y * width + terrainManagerScript.lastplayerGridPosition.x] = colorUnderPlayerPos;

            colorUnderPlayerPos = image.GetPixel(terrainManagerScript.playerGridPosition.x, terrainManagerScript.playerGridPosition.y);
            colorMap[terrainManagerScript.playerGridPosition.y * width + terrainManagerScript.playerGridPosition.x] = Color.red;
            image.SetPixels(colorMap);
            image.Apply();
        }
    }
}
