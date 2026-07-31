using System;
using System.Collections.Generic;
using System.Linq;

namespace SOTOR.AbilitySystem.AI;

public static class AxisExtensions
{
	public static float GeometricMean(this List<Axis> axes, Target target)
	{
		List<Axis> list = axes.FindAll((Axis axis) => axis.IsActive(target));
		List<float> source = list.Select((Axis axis) => axis.Evaluate(target)).ToList();
		return target.UtilityValue = ((!source.Any()) ? 0f : ((float)Math.Pow(source.Aggregate((float a, float x) => a * x), 1.0 / (double)list.Count)));
	}
}
