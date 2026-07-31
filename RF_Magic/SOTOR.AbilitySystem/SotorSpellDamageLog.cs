using System;
using System.Collections.Generic;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public static class SotorSpellDamageLog
{
	private sealed class Session
	{
		public Agent Caster;

		public string SpellName;

		public DamageType PrimaryDamageType = DamageType.Physical;

		public float LastBookTime;

		public int TotalDamage;

		public readonly HashSet<int> DamagedTargets = new HashSet<int>();

		public int Kills;

		public int TotalHealing;

		public readonly HashSet<int> HealedTargets = new HashSet<int>();

		public int TotalFriendlyFire;

		public readonly HashSet<int> FriendlyTargets = new HashSet<int>();

		public int FriendlyKills;
	}

	private const float FlushDelay = 2f;

	private static readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>();

	private static Mission _boundMission;

	private const float ShipEventThrottle = 3f;

	private static readonly Dictionary<string, float> _shipEventTimes = new Dictionary<string, float>();

	private static string SessionKey(Agent caster, string spellName)
	{
		return caster.Index + "|" + (string.IsNullOrEmpty(spellName) ? "(unnamed)" : spellName);
	}

	public static void BookHit(Agent caster, Agent victim, DamageType damageType, int amount, bool killed, string spellName)
	{
		if (amount <= 0 || caster == null || victim == null || caster == victim || !SotorSettings.EnableSpellDamageLog || !IsPlayerOrMainParty(caster))
		{
			return;
		}
		Session orCreate = GetOrCreate(caster, spellName);
		if (victim.IsEnemyOf(caster))
		{
			orCreate.PrimaryDamageType = damageType;
			orCreate.TotalDamage += amount;
			orCreate.DamagedTargets.Add(victim.Index);
			if (killed)
			{
				orCreate.Kills++;
			}
		}
		else
		{
			orCreate.TotalFriendlyFire += amount;
			orCreate.FriendlyTargets.Add(victim.Index);
			if (killed)
			{
				orCreate.FriendlyKills++;
			}
		}
	}

	public static void BookHeal(Agent caster, Agent target, int amount, string spellName)
	{
		if (amount > 0 && caster != null && target != null && SotorSettings.EnableSpellDamageLog && IsPlayerOrMainParty(caster))
		{
			Session orCreate = GetOrCreate(caster, spellName);
			orCreate.TotalHealing += amount;
			orCreate.HealedTargets.Add(target.Index);
		}
	}

	public static void BookShipEvent(Agent caster, DamageType damageType, string spellName, string message)
	{
		if (caster != null && !string.IsNullOrEmpty(message) && SotorSettings.EnableSpellDamageLog && IsPlayerOrMainParty(caster))
		{
			RebindIfMissionChanged();
			string text = (string.IsNullOrEmpty(spellName) ? "Spell" : spellName);
			string key = caster.Index + "|" + text + "|" + message;
			float num = MissionTime();
			if (!_shipEventTimes.TryGetValue(key, out var value) || !(num - value < 3f))
			{
				_shipEventTimes[key] = num;
				string damageTypeIcon = GetDamageTypeIcon(damageType);
				Post(damageTypeIcon + " " + text + " " + message, GetDamageTypeColor(damageType));
			}
		}
	}

	private static Session GetOrCreate(Agent caster, string spellName)
	{
		RebindIfMissionChanged();
		string key = SessionKey(caster, spellName);
		if (!_sessions.TryGetValue(key, out var value))
		{
			value = new Session
			{
				Caster = caster,
				SpellName = spellName
			};
			_sessions[key] = value;
		}
		value.LastBookTime = MissionTime();
		return value;
	}

	public static void FlushExpired(Mission mission)
	{
		if (mission == null)
		{
			return;
		}
		RebindIfMissionChanged();
		if (_sessions.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<string> list = null;
		foreach (KeyValuePair<string, Session> session in _sessions)
		{
			if (currentTime - session.Value.LastBookTime >= 2f)
			{
				(list ?? (list = new List<string>())).Add(session.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (string item in list)
		{
			if (_sessions.TryGetValue(item, out var value))
			{
				Emit(value);
				_sessions.Remove(item);
			}
		}
	}

	public static void Reset()
	{
		_sessions.Clear();
		_shipEventTimes.Clear();
		_boundMission = null;
	}

	private static void RebindIfMissionChanged()
	{
		Mission current = Mission.Current;
		if (current != _boundMission)
		{
			_sessions.Clear();
			_shipEventTimes.Clear();
			_boundMission = current;
		}
	}

	private static float MissionTime()
	{
		return Mission.Current?.CurrentTime ?? 0f;
	}

	private static bool IsPlayerOrMainParty(Agent caster)
	{
		if (caster == null)
		{
			return false;
		}
		if (caster == Agent.Main)
		{
			return true;
		}
		Hero hero = caster.GetHero();
		if (hero != null)
		{
			return hero.PartyBelongedTo == MobileParty.MainParty;
		}
		return false;
	}

	private static void Emit(Session session)
	{
		string spellName = (string.IsNullOrEmpty(session.SpellName) ? "Spell" : session.SpellName);
		if (session.TotalDamage > 0 && session.DamagedTargets.Count > 0)
		{
			DisplayAggregateSpellDamage(session.PrimaryDamageType, session.TotalDamage, session.DamagedTargets.Count, session.Kills, spellName);
		}
		if (session.TotalHealing > 0 && session.HealedTargets.Count > 0)
		{
			DisplayAggregateSpellHealing(session.TotalHealing, session.HealedTargets.Count, spellName);
		}
		if (session.TotalFriendlyFire > 0 && session.FriendlyTargets.Count > 0)
		{
			DisplayAggregateSpellFriendlyFire(session.TotalFriendlyFire, session.FriendlyTargets.Count, session.FriendlyKills, spellName);
		}
	}

	private static void DisplayAggregateSpellDamage(DamageType damageType, int totalDamage, int agentsAffected, int agentsKilled, string spellName)
	{
		string damageTypeIcon = GetDamageTypeIcon(damageType);
		string damageTypeText = GetDamageTypeText(damageType);
		string text = ((agentsAffected == 1) ? "target" : "targets");
		Post((agentsKilled > 0) ? $"{damageTypeIcon} {spellName} dealt {totalDamage} {damageTypeText} damage to {agentsAffected} {text}, {agentsKilled} eliminated" : $"{damageTypeIcon} {spellName} dealt {totalDamage} {damageTypeText} damage to {agentsAffected} {text}", GetDamageTypeColor(damageType));
	}

	private static void DisplayAggregateSpellHealing(int totalHealing, int agentsAffected, string spellName)
	{
		string text = ((agentsAffected == 1) ? "target" : "targets");
		Post($"<img src=\"heart_icon\"/> {spellName} healed {totalHealing} health to {agentsAffected} {text}", Colors.Green);
	}

	private static void DisplayAggregateSpellFriendlyFire(int totalDamage, int agentsAffected, int agentsKilled, string spellName)
	{
		string text = ((agentsAffected == 1) ? "ally" : "allies");
		Post((agentsKilled > 0) ? $"<img src=\"screamingskull_icon\"/> {spellName} hit {agentsAffected} {text} for {totalDamage} friendly fire damage, {agentsKilled} killed" : $"<img src=\"screamingskull_icon\"/> {spellName} hit {agentsAffected} {text} for {totalDamage} friendly fire damage", Colors.Magenta);
	}

	private static void Post(string message, Color color)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(message.TrimStart(), color));
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorSpellDamageLog.Post failed: " + ex.Message);
		}
	}

	private static Color GetDamageTypeColor(DamageType damageType)
	{
		return damageType switch
		{
			DamageType.Fire => Colors.Red, 
			DamageType.Holy => Colors.Yellow, 
			DamageType.Lightning => Color.FromUint(5745663u), 
			DamageType.Magical => Colors.Cyan, 
			DamageType.Frost => Color.FromUint(8909823u), 
			_ => Colors.White, 
		};
	}

	private static string GetDamageTypeIcon(DamageType damageType)
	{
		string text = damageType switch
		{
			DamageType.Fire => "traits_fire_icon", 
			DamageType.Holy => "traits_holy_icon", 
			DamageType.Lightning => "traits_lightning_icon", 
			DamageType.Magical => "traits_magic_icon", 
			DamageType.Frost => "traits_frost_icon", 
			_ => null, 
		};
		if (!string.IsNullOrEmpty(text))
		{
			return "<img src=\"" + text + "\"/>";
		}
		return "";
	}

	private static string GetDamageTypeText(DamageType damageType)
	{
		return damageType switch
		{
			DamageType.Fire => "Fire", 
			DamageType.Holy => "Holy", 
			DamageType.Lightning => "Lightning", 
			DamageType.Magical => "Magical", 
			DamageType.Frost => "Frost", 
			_ => "Physical", 
		};
	}
}
