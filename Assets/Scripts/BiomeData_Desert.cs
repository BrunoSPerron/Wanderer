using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BiomeData/Desert")]
public class BiomeData_Desert : ScriptableObject
{
    public float CactiSparcity = 4;

    public int MinAmountOfRock = 0;
    public int MaxAmountOfRock = 5;

    public int maxBones = 2;
    public int boneChance = 20;

    public float TreeSparcity = 2;
    public float ShrubSparcity = 0.5f;
    public int TreeChance = 80;
    public int ShrubChance = 80;

    public NoiseSettings NoiseSettings;
    public AnimationCurve TreesDistributionCurve;
    public AnimationCurve ShrubsDistributionCurve;
}
