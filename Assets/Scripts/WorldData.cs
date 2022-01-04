using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldData
{
    public static int Seed;

    public static void Initiate()
    {
        
        Seed = System.Guid.NewGuid().GetHashCode();
    }
}
