using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BiomeData/Forest")]
public class BiomeData_Forest : ScriptableObject
{
    public int GroundCohesion = 80;

    public float TreesSparcity = 4;
    public float BushesSparcity = 1.2f;
    public float SmallBushesSparcity = 0.6f;

    public int MinAmountOfRock = 0;
    public int MaxAmountOfRock = 3;
    public float FlowerSparcity = 1.2f;
    public int FlowerChance = 15;

    public NoiseSettings BushesNoiseSettings;
    public AnimationCurve BushesDistributionCurve;
    public AnimationCurve SmallBushesDistributionCurve;
}
