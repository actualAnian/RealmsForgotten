using System.Collections.Generic;
using SOTOR.Extensions;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class AbilityComponent : AgentComponent
{
	private Ability _currentAbility;

	private readonly List<Ability> _knownAbilitySystem = new List<Ability>();

	public bool LastCastWasQuickCast { get; set; }

	public List<Ability> KnownAbilitySystem => _knownAbilitySystem;

	public Ability CurrentAbility
	{
		get
		{
			return _currentAbility;
		}
		set
		{
			_currentAbility = value;
		}
	}

	public List<AbilityTemplate> GetKnownAbilityTemplates()
	{
		List<AbilityTemplate> list = new List<AbilityTemplate>(_knownAbilitySystem.Count);
		foreach (Ability item in _knownAbilitySystem)
		{
			list.Add(item.Template);
		}
		return list;
	}

	public AbilityComponent(Agent agent)
		: base(agent)
	{
		foreach (string selectedAbility in agent.GetSelectedAbilities())
		{
			Ability ability = AbilityFactory.CreateNew(selectedAbility, agent);
			if (ability != null)
			{
				_knownAbilitySystem.Add(ability);
			}
			else
			{
				SotorLog.Warn("Failed to create ability '" + selectedAbility + "' for agent.");
			}
		}
		if (_knownAbilitySystem.Count > 0)
		{
			SelectAbility(0);
		}
		string text = agent.GetHero()?.GetInfoKey() ?? "no-hero";
		SotorLog.Info(string.Format("AbilityComponent created. hero={0} main={1} known={2} current={3}", text, agent.IsMainAgent, _knownAbilitySystem.Count, CurrentAbility?.StringID ?? "none"));
	}

	public void SelectAbility(Ability ability)
	{
		if (_knownAbilitySystem.Contains(ability))
		{
			CurrentAbility = ability;
		}
	}

	public void SelectAbility(int index)
	{
		if (_knownAbilitySystem.Count > 0 && index >= 0 && index < _knownAbilitySystem.Count)
		{
			CurrentAbility = _knownAbilitySystem[index];
		}
	}

	public override void OnTick(float dt)
	{
		foreach (Ability item in _knownAbilitySystem)
		{
			item.TickCastingState();
		}
	}
}
