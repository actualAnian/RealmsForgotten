using System;
using System.Collections.Generic;
using System.Linq;
using SOTOR.AbilitySystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace SOTOR.Extensions.ExtendedInfoSystem;

public class HeroExtendedInfo
{
	public static float TestingMaxWindsOverride = -1f;

	public const float DefaultMaxWindsOfMagic = 100f;

	[SaveableField(0)]
	public List<string> AcquiredAbilities = new List<string>();

	[SaveableField(1)]
	public List<string> AcquiredAttributes = new List<string>();

	[SaveableField(2)]
	private List<string> _selectedAbilities;

	[SaveableField(3)]
	private CharacterObject _baseCharacter;

	[SaveableField(4)]
	private float _windsOfMagic;

	[SaveableField(5)]
	private bool _windsInitialized;

	[SaveableField(6)]
	public List<string> AcquiredLores = new List<string>();

	[SaveableField(7)]
	public List<string> AcquiredSpells = new List<string>();

	public float MaxWindsOfMagic
	{
		get
		{
			if (TestingMaxWindsOverride >= 0f)
			{
				return TestingMaxWindsOverride;
			}
			Hero hero = _baseCharacter?.HeroObject;
			if (hero == null)
			{
				return 100f;
			}
			return SotorSpellcraftHelper.GetMaxWinds(hero);
		}
	}

	public float WindsOfMagic
	{
		get
		{
			EnsureWindsInitialized();
			// [RF-B] o TETO agora depende do cajado empunhado, e o valor atual e
			// campo SALVO: trocar um cajado grande por uma varinha (ou guardar o
			// foco) baixa o maximo e deixaria o atual acima dele — barra passando
			// do fim, mana fantasma. Clampar na LEITURA cobre todo caminho de uma
			// vez, sem depender de tick nem de evento de troca de equipamento.
			float max = MaxWindsOfMagic;
			if (_windsOfMagic > max)
			{
				_windsOfMagic = max;
			}
			return _windsOfMagic;
		}
	}

	public List<string> AllAbilities
	{
		get
		{
			List<string> list = new List<string>();
			if (_baseCharacter != null)
			{
				list.AddRange(_baseCharacter.GetAbilities());
			}
			list.AddRange(AcquiredAbilities);
			return list.Distinct().ToList();
		}
	}

	public List<string> AllAttributes
	{
		get
		{
			List<string> list = new List<string>();
			if (_baseCharacter != null)
			{
				list.AddRange(_baseCharacter.GetAttributes());
			}
			list.AddRange(AcquiredAttributes);
			return list.Distinct().ToList();
		}
	}

	public List<string> SelectedAbilities => _selectedAbilities ?? new List<string>();

	private void EnsureLores()
	{
		if (AcquiredLores == null)
		{
			AcquiredLores = new List<string>();
		}
	}

	public bool HasLore(string loreId)
	{
		EnsureLores();
		if (loreId != null)
		{
			return AcquiredLores.Contains(loreId);
		}
		return false;
	}

	public void AddLore(string loreId)
	{
		EnsureLores();
		if (loreId != null && !AcquiredLores.Contains(loreId))
		{
			AcquiredLores.Add(loreId);
		}
	}

	public void RemoveLore(string loreId)
	{
		EnsureLores();
		AcquiredLores.Remove(loreId);
	}

	private void EnsureSpells()
	{
		if (AcquiredSpells == null)
		{
			AcquiredSpells = new List<string>();
		}
	}

	public bool HasSpell(string abilityId)
	{
		EnsureSpells();
		if (abilityId != null)
		{
			return AcquiredSpells.Contains(abilityId);
		}
		return false;
	}

	public void AddSpell(string abilityId)
	{
		EnsureSpells();
		if (abilityId != null && !AcquiredSpells.Contains(abilityId))
		{
			AcquiredSpells.Add(abilityId);
		}
	}

	public void RemoveSpell(string abilityId)
	{
		EnsureSpells();
		AcquiredSpells.Remove(abilityId);
	}

	private void EnsureWindsInitialized()
	{
		if (!_windsInitialized)
		{
			_windsInitialized = true;
			_windsOfMagic = MaxWindsOfMagic;
		}
	}

	public void AddWindsOfMagic(float amount, bool allowOverMax = false)
	{
		EnsureWindsInitialized();
		float num = Math.Max(0f, _windsOfMagic + amount);
		if (!allowOverMax)
		{
			num = Math.Min(MaxWindsOfMagic, num);
		}
		_windsOfMagic = num;
	}

	public void SetWindsOfMagic(float amount)
	{
		_windsInitialized = true;
		_windsOfMagic = Math.Max(0f, Math.Min(MaxWindsOfMagic, amount));
	}

	public HeroExtendedInfo(CharacterObject character)
	{
		_baseCharacter = character;
		_selectedAbilities = new List<string>();
	}

	public void AddSelectedAbility(string abilityId)
	{
		if (!_selectedAbilities.Contains(abilityId))
		{
			_selectedAbilities.Add(abilityId);
		}
	}

	public void RemoveSelectedAbility(string abilityId)
	{
		_selectedAbilities.Remove(abilityId);
	}

	public void ToggleSelectedAbility(string abilityId)
	{
		if (IsAbilitySelected(abilityId))
		{
			RemoveSelectedAbility(abilityId);
		}
		else if (AllAbilities.Contains(abilityId))
		{
			AddSelectedAbility(abilityId);
		}
	}

	public bool IsAbilitySelected(string abilityId)
	{
		return _selectedAbilities.Contains(abilityId);
	}
}
