using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


[CreateAssetMenu(menuName = "SerializableTilesCollection")]
public class SerializableTilesCollection : ScriptableObject
{
    public List<TileType> Keys;
    public List<TileBase> Values;
}
    
