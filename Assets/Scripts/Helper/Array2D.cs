using UnityEngine;

public struct Array2D<T>
{
    private T[,] array;
    public int Width => array.GetLength(0);
    public int Height => array.GetLength(1);
    public Array2D(int x, int y)
    {
        array = new T[x, y];
    }
    public Array2D(Vector2 vector)
    {
        array = new T[(int)vector.x, (int)vector.y];
    }
    public Array2D(Vector2Int vector)
    {
        array = new T[vector.x, vector.y];
    }
    public Array2D(Array2D<T> a)
    {
        array = new T[a.Width, a.Height];
        System.Array.Copy(a.array, array, array.Length);
    }


    public T this[int x, int y]
    {
        get => array[x, y]; 
        set { array[x, y] = value; }
    }

    public T this[Vector2Int vector]
    {
        get { return array[vector.x, vector.y]; }
        set { array[vector.x, vector.y] = value; }
    }

    public T this[Vector2 vector]
    {
        get { return array[(int)vector.x, (int)vector.y]; }
        set { array[(int)vector.x, (int)vector.y] = value; }
    }
}
