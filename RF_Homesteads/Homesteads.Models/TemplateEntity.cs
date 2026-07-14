using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace Homesteads.Models;

public class TemplateEntity
{
	public string PrefabName { get; set; } = "";

	public string DisplayName { get; set; } = "";

	public float RelativePosX { get; set; }

	public float RelativePosY { get; set; }

	public float RelativePosZ { get; set; }

	public float RotFx { get; set; }

	public float RotFy { get; set; }

	public float RotFz { get; set; }

	public float RotUx { get; set; }

	public float RotUy { get; set; }

	public float RotUz { get; set; }

	public float RotSx { get; set; }

	public float RotSy { get; set; }

	public float RotSz { get; set; }

	public int BuildPoints { get; set; }

	public Dictionary<string, int> ItemCosts { get; set; } = new Dictionary<string, int>();

	public int TierRequired { get; set; }

	public TemplateEntity()
	{
	}

	public TemplateEntity(HomesteadSceneSavedEntity entity, Vec3 centerPoint)
	{
		if (entity?.Placeable == null)
		{
			return;
		}
		PrefabName = entity.Placeable.PrefabName ?? "";
		DisplayName = entity.Placeable.DisplayName?.ToString() ?? "";
		TierRequired = 0;
		BuildPoints = entity.Placeable.BuildPointsRequired;
		RelativePosX = entity.posX - centerPoint.x;
		RelativePosY = entity.posY - centerPoint.y;
		RelativePosZ = entity.posZ - centerPoint.z;
		RotFx = entity.rotFx;
		RotFy = entity.rotFy;
		RotFz = entity.rotFz;
		RotUx = entity.rotUx;
		RotUy = entity.rotUy;
		RotUz = entity.rotUz;
		RotSx = entity.rotSx;
		RotSy = entity.rotSy;
		RotSz = entity.rotSz;
		ItemCosts = new Dictionary<string, int>();
		if (entity.Placeable.ItemRequirements == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> itemRequirement in entity.Placeable.ItemRequirements)
		{
			if (!string.IsNullOrEmpty(itemRequirement.Key) && itemRequirement.Value > 0)
			{
				ItemCosts[itemRequirement.Key] = itemRequirement.Value;
			}
		}
	}

	public Vec3 GetWorldPosition(Vec3 templateOrigin, float templateRotationY, float templateOffsetZ = 0f)
	{
		float num = (float)Math.Cos((double)templateRotationY * Math.PI / 180.0);
		float num2 = (float)Math.Sin((double)templateRotationY * Math.PI / 180.0);
		float num3 = RelativePosX * num - RelativePosY * num2;
		float num4 = RelativePosX * num2 + RelativePosY * num;
		return new Vec3(templateOrigin.x + num3, templateOrigin.y + num4, templateOrigin.z + RelativePosZ + templateOffsetZ);
	}

	public Mat3 GetWorldRotation(float templateRotationY)
	{
		Mat3 mat = new Mat3(new Vec3(RotSx, RotSy, RotSz), new Vec3(RotFx, RotFy, RotFz), new Vec3(RotUx, RotUy, RotUz));
		float num = templateRotationY * (TaleWorlds.Library.MathF.PI / 180f);
		float num2 = (float)Math.Cos(num);
		float num3 = (float)Math.Sin(num);
		return new Mat3(new Vec3(mat.s.X * num2 - mat.s.Y * num3, mat.s.X * num3 + mat.s.Y * num2, mat.s.Z), new Vec3(mat.f.X * num2 - mat.f.Y * num3, mat.f.X * num3 + mat.f.Y * num2, mat.f.Z), new Vec3(mat.u.X * num2 - mat.u.Y * num3, mat.u.X * num3 + mat.u.Y * num2, mat.u.Z));
	}
}
