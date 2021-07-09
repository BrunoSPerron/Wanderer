using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public GameObject terrainManagerObject;
    TerrainManager terrainManagerScript;
    Color colorUnderPlayerPos;
    Vector2Int lastPosition;
    
    BiomeGenerator biomeGenerator;
    Texture2D image;
    // Start is called before the first frame update
    void Start()
    {
        terrainManagerScript = terrainManagerObject.GetComponent<TerrainManager>();
        biomeGenerator = terrainManagerObject.GetComponent<BiomeGenerator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (image is null)
        {
            GetComponent<Image>().sprite = biomeGenerator.GetSprite(biomeGenerator.currentMap.BiomeMap);
            image = GetComponent<Image>().sprite.texture;
            colorUnderPlayerPos = image.GetPixel(terrainManagerScript.playerGridPosition.x, terrainManagerScript.playerGridPosition.y);
            image.filterMode = FilterMode.Point;
        }

        if (lastPosition != terrainManagerScript.playerGridPosition)
        {
            int width = image.width;
            Color[] colorMap = image.GetPixels();
            colorMap[lastPosition.y * width + lastPosition.x] = colorUnderPlayerPos;
            lastPosition.x = terrainManagerScript.playerGridPosition.x;
            lastPosition.y = terrainManagerScript.playerGridPosition.y;
            colorUnderPlayerPos = image.GetPixel(lastPosition.x, lastPosition.y);
            colorMap[lastPosition.y * width + lastPosition.x] = Color.red;
            image.SetPixels(colorMap);
            image.Apply();
        }
    }
}
