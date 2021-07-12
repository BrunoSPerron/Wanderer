using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainHelper
{
    internal static Vector2 GridToLocal(Vector2 gridCoord)
    {
        return new Vector2((gridCoord.x - gridCoord.y) * 0.25f, (gridCoord.x + gridCoord.y) * .125f);
    }

    internal static Vector2 LocalToGrid(Vector2 localCoord)
    {
        float logicalX = localCoord.x / 0.5f;
        float logicalY = localCoord.y / 0.25f;
        return new Vector2(logicalX + logicalY, logicalY - logicalX);
    }

    internal static bool IsWithinDistance(Vector2 coord1, Vector2 coord2, float distance)
    {
        //TODO redo this correctly
        if (Mathf.Abs(coord1.x - coord2.x) + Mathf.Abs(coord1.y - coord2.y) < distance)
            return true;
        return false;
    }
}
