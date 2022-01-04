using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IsoGridHelper
{
    public static Vector2 GridToLocal(in Vector2 gridCoord)
    {
        return new Vector2((gridCoord.x - gridCoord.y) * 0.25f, (gridCoord.x + gridCoord.y) * .125f);
    }

    public static Vector2 LocalToGrid(in Vector2 localCoord)
    {
        float logicalX = localCoord.x / 0.5f;
        float logicalY = localCoord.y / 0.25f;
        return new Vector2(logicalX + logicalY, logicalY - logicalX);
    }

    public static bool IsWithinDistance(in Vector2 coord1, in Vector2 coord2, in float distance)
    {
        if ((coord2 - coord1).sqrMagnitude < distance * distance)
            return true;
        return false;
    }
}
