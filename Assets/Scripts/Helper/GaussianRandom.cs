using UnityEngine;

public static class GaussianRandom
{
    private static System.Random rand = new System.Random();

    /// <returns>unclamped random value from a normal distribution</returns>
    public static float generateNormalRandom(float mean = 0, float deviation = 1)
    {
        float rand1 = (float)rand.NextDouble();
        float rand2 = (float)rand.NextDouble();

        float n = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Cos(2.0f * Mathf.PI * rand2);

        return mean + deviation * n;
    }
}
