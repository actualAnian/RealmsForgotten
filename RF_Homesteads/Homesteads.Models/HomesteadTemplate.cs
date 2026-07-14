using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace Homesteads.Models;

public class HomesteadTemplate
{
	public string Name { get; set; } = "";

	public int Version { get; set; } = 1;

	public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

	public int TotalBuildPoints { get; set; }

	public Dictionary<string, int> TotalCost { get; set; } = new Dictionary<string, int>();

	public string SourceHomesteadName { get; set; } = "";

	public string TargetMap { get; set; } = "";

	public List<TemplateEntity> Entities { get; set; } = new List<TemplateEntity>();

	public bool HasAnchor { get; set; }

	public float AnchorX { get; set; }

	public float AnchorY { get; set; }

	public float AnchorZ { get; set; }

	public static HomesteadTemplate CreateFromEntities(string name, string sourceHomesteadName, string targetMap, List<HomesteadSceneSavedEntity> entities)
	{
		if (entities == null || entities.Count == 0)
		{
			return null;
		}
		HomesteadTemplate homesteadTemplate = new HomesteadTemplate
		{
			Name = name,
			SourceHomesteadName = sourceHomesteadName,
			TargetMap = targetMap,
			CreatedDate = DateTime.UtcNow,
			Version = 1,
			Entities = new List<TemplateEntity>(),
			TotalBuildPoints = 0,
			TotalCost = new Dictionary<string, int>()
		};
		Vec3 centerPoint = CalculateCenterPoint(entities);
		foreach (HomesteadSceneSavedEntity entity in entities)
		{
			TemplateEntity templateEntity = new TemplateEntity(entity, centerPoint);
			homesteadTemplate.Entities.Add(templateEntity);
			homesteadTemplate.TotalBuildPoints += templateEntity.BuildPoints;
			foreach (KeyValuePair<string, int> itemCost in templateEntity.ItemCosts)
			{
				if (!homesteadTemplate.TotalCost.ContainsKey(itemCost.Key))
				{
					homesteadTemplate.TotalCost[itemCost.Key] = 0;
				}
				homesteadTemplate.TotalCost[itemCost.Key] += itemCost.Value;
			}
		}
		return homesteadTemplate;
	}

	private static Vec3 CalculateCenterPoint(List<HomesteadSceneSavedEntity> entities)
	{
		if (entities.Count == 0)
		{
			return Vec3.Zero;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		float num5 = float.MaxValue;
		float num6 = float.MinValue;
		foreach (HomesteadSceneSavedEntity entity in entities)
		{
			Vec3 vec = new Vec3(entity.posX, entity.posY, entity.posZ);
			num = Math.Min(num, vec.x);
			num2 = Math.Max(num2, vec.x);
			num3 = Math.Min(num3, vec.y);
			num4 = Math.Max(num4, vec.y);
			num5 = Math.Min(num5, vec.z);
			num6 = Math.Max(num6, vec.z);
		}
		return new Vec3((num + num2) / 2f, (num3 + num4) / 2f, (num5 + num6) / 2f);
	}

	public string GetFormattedCost()
	{
		if (TotalCost == null || TotalCost.Count == 0)
		{
			return "Free";
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int> item in TotalCost)
		{
			list.Add($"{item.Value} {item.Key}");
		}
		return string.Join(", ", list);
	}

	public int GetMinTierRequired()
	{
		if (Entities == null || Entities.Count == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (TemplateEntity entity in Entities)
		{
			num = Math.Max(num, entity.TierRequired);
		}
		return num;
	}

	public Dictionary<string, int> GetCostSummary()
	{
		return TotalCost;
	}
}
