using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SOTOR.AbilitySystem;

public class TransformationMissionLogic : MissionLogic
{
	private sealed class ActiveTransformation
	{
		public Equipment OriginalEquipment;

		public float EndTime;
	}

	private readonly Dictionary<Agent, ActiveTransformation> _activeTransformations = new Dictionary<Agent, ActiveTransformation>();

	public bool TryTransform(Agent agent, string formTroopId, float duration)
	{
		if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsFadingOut() || agent.Health < 1f || string.IsNullOrWhiteSpace(formTroopId))
		{
			return false;
		}
		BasicCharacterObject formCharacter = MBObjectManager.Instance.GetObject<BasicCharacterObject>(formTroopId);
		Equipment formEquipment = formCharacter?.RandomBattleEquipment;
		if (formEquipment == null)
		{
			SotorLog.Warn("Transformation: form troop '" + formTroopId + "' was not found or has no battle equipment.");
			return false;
		}
		if (!_activeTransformations.TryGetValue(agent, out ActiveTransformation value))
		{
			value = new ActiveTransformation
			{
				OriginalEquipment = new Equipment(agent.SpawnEquipment)
			};
			_activeTransformations.Add(agent, value);
		}
		Equipment equipment = new Equipment(value.OriginalEquipment);
		for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.ArmorItemEndSlot; i++)
		{
			equipment[(EquipmentIndex)i] = new EquipmentElement(formEquipment[(EquipmentIndex)i]);
		}
		float health = agent.Health;
		agent.UpdateSpawnEquipmentAndRefreshVisuals(equipment);
		agent.Health = Math.Min(health, agent.HealthLimit);
		value.EndTime = Mission.Current.CurrentTime + Math.Max(1f, duration);
		SotorLog.Info($"Transformation: '{agent.Name}' assumed '{formTroopId}' for {duration:0.#}s.");
		return true;
	}

	public override void OnMissionTick(float dt)
	{
		if (_activeTransformations.Count == 0)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		foreach (KeyValuePair<Agent, ActiveTransformation> item in new List<KeyValuePair<Agent, ActiveTransformation>>(_activeTransformations))
		{
			Agent key = item.Key;
			if (key == null || !key.IsActive() || key.IsFadingOut() || key.Health < 1f)
			{
				_activeTransformations.Remove(key);
			}
			else if (currentTime >= item.Value.EndTime)
			{
				Restore(key, item.Value);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		foreach (KeyValuePair<Agent, ActiveTransformation> item in new List<KeyValuePair<Agent, ActiveTransformation>>(_activeTransformations))
		{
			if (item.Key != null && item.Key.IsActive() && !item.Key.IsFadingOut())
			{
				Restore(item.Key, item.Value);
			}
		}
		_activeTransformations.Clear();
		base.OnRemoveBehavior();
	}

	private void Restore(Agent agent, ActiveTransformation transformation)
	{
		float health = agent.Health;
		agent.UpdateSpawnEquipmentAndRefreshVisuals(new Equipment(transformation.OriginalEquipment));
		agent.Health = Math.Min(health, agent.HealthLimit);
		_activeTransformations.Remove(agent);
		SotorLog.Info($"Transformation: '{agent.Name}' returned to the original form.");
	}
}
