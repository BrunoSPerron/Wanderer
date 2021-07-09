using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BiomeData/Forest")]
public class BiomeData_Forest : ScriptableObject
{
    public float TreesSparcity = 4;
    public float BushesSparcity = 1.2f;
    public float SmallBushesSparcity = 0.6f;

    public int RockChance = 15;
    public int FlowerChance = 15;

    public NoiseSettings BushesNoiseSettings;
    public AnimationCurve BushesDistributionCurve;
}
