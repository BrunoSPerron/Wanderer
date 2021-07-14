using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BiomeData/Swamp")]
public class BiomeData_Swamp : ScriptableObject
{

    public float TreeSparcity = 3;
    public float ShrubSparcity = 3;
    public float CattailSparcity = 1;
    public float WaterlilySparcity = 1;
    public int GroundCohesion = 70;
    public float WaterThreshold = 0.4f;

    public NoiseSettings ShrubsNoiseSettings;
    public NoiseSettings WaterNoiseSettings;
    public NoiseSettings CattailNoiseSettings;
    public AnimationCurve BushesDistributionCurve;
    public AnimationCurve CattailDistributionCurve;
    public AnimationCurve WaterlilyDistributionCurve;
}
