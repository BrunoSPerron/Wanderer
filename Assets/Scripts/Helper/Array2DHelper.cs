using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Array2DHelper
{
    public static Vector2Int[] GetPositionsAround<T>(in Array2D<T> array, in int x, in int y)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (x > 0)
        {
            result.Add(new Vector2Int(x - 1, y));

            if (y > 0)
                result.Add(new Vector2Int(x - 1, y - 1));
            if (y < array.Height - 1)
                result.Add(new Vector2Int(x - 1, y + 1));

        }
        if (x < array.Width - 1)
        {
            result.Add(new Vector2Int(x + 1, y));

            if (y > 0)
                result.Add(new Vector2Int(x + 1, y - 1));
            if (y < array.Height - 1)
                result.Add(new Vector2Int(x + 1, y + 1));
        }
        if (y > 0)
            result.Add(new Vector2Int(x, y - 1));
        if (y < array.Height - 1)
            result.Add(new Vector2Int(x, y + 1));

        return result.ToArray();
    }
    public static Vector2Int[] GetPositionsAround<T>(in Array2D<T> array, in Vector2Int coord) => GetPositionsAround(array, coord.x, coord.y);

    public static Vector2Int[] GetCardinalPositions<T>(in Array2D<T> array, in int x, in int y)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (x > 0)
            result.Add(new Vector2Int(x - 1, y));
        if (x < array.Width - 1)
            result.Add(new Vector2Int(x + 1, y));
        if (y > 0)
            result.Add(new Vector2Int(x, y - 1));
        if (y < array.Height - 1)
            result.Add(new Vector2Int(x, y + 1));

        return result.ToArray();
    }
    public static Vector2Int[] GetCardinalPositions<T>(in Array2D<T> array, in Vector2Int coord) => GetCardinalPositions(array, coord.x, coord.y);

    public static class Mask
    {
        public static void Extend(ref Array2D<bool> mask, bool extendDiagonally = false)
        {
            if (extendDiagonally == true)
            {
                Array2D<bool> original = new Array2D<bool>(mask);
                for (int x = 0; x < original.Width; x++)
                    for (int y = 0; y < original.Height; y++)
                        if (original[x, y])
                            foreach (Vector2Int v2i in GetPositionsAround(original, x, y))
                                mask[v2i] = true;
            }
            else
            {
                Array2D<bool> original = new Array2D<bool>(mask);
                for (int x = 0; x < original.Width; x++)
                    for (int y = 0; y < original.Height; y++)
                        if (original[x, y])
                            foreach (Vector2Int v2i in GetCardinalPositions(original, x, y))
                                mask[v2i] = true;
            }
        }

        public static void RemoveBorder(ref Array2D<bool> mask)
        {
            int indexWidth = mask.Width - 1;
            int indexHeight = mask.Height - 1;
            for (int x = 0; x < mask.Width; x++)
            {
                mask[x, 0] = false;
                mask[x, indexHeight] = false;
            }
            for (int y = 0; y < mask.Width; y++)
            {
                mask[0, y] = false;
                mask[indexWidth, y] = false;
            }
        }

        public static void Substract(ref Array2D<bool> mask, in Array2D<bool> toSubstract)
        {
            for (int x = 0; x < toSubstract.Width; x++)
                for (int y = 0; y < toSubstract.Height; y++)
                    if (toSubstract[x, y])
                        mask[x, y] = false;
        }
    }
}
