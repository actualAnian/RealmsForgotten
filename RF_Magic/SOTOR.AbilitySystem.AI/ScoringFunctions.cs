using System;

namespace SOTOR.AbilitySystem.AI;

public static class ScoringFunctions
{
	public static Func<float, float> Logistic(float mid = 0f, float L = 1f, float k = 10f, float m = 1f)
	{
		return (float x) => (float)((double)L / (1.0 + (double)m * Math.Pow(Math.E, (0f - k) * (x - mid))));
	}
}
