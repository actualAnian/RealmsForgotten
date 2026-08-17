using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.MagicAccessories;

public static class MagicRuneCombatFeedback
{
	private sealed class WeaponModifier
	{
		public MagicRuneData Rune;
		public float RecordedAt;
	}

	private const float DisplayThrottleSeconds = 0.8f;
	private const float PendingWeaponModifierSeconds = 1f;

	private static readonly Dictionary<string, float> _lastDisplayTimes = new Dictionary<string, float>();
	private static readonly Dictionary<string, WeaponModifier> _pendingWeaponModifiers = new Dictionary<string, WeaponModifier>();
	private static Mission _boundMission;

	public static void Reset()
	{
		_lastDisplayTimes.Clear();
		_pendingWeaponModifiers.Clear();
		_boundMission = Mission.Current;
	}

	public static void RecordWeaponModifier(Agent attacker, Agent victim, MagicRuneData rune)
	{
		if (attacker == null || victim == null || rune == null)
		{
			return;
		}
		EnsureMission();
		_pendingWeaponModifiers[WeaponModifierKey(attacker, victim)] = new WeaponModifier
		{
			Rune = rune,
			RecordedAt = Mission.Current?.CurrentTime ?? 0f
		};
	}

	public static bool TryConsumeWeaponModifier(Agent attacker, Agent victim, out MagicRuneData rune)
	{
		rune = null;
		if (attacker == null || victim == null)
		{
			return false;
		}
		EnsureMission();
		string key = WeaponModifierKey(attacker, victim);
		if (!_pendingWeaponModifiers.TryGetValue(key, out WeaponModifier pending))
		{
			return false;
		}
		_pendingWeaponModifiers.Remove(key);
		if ((Mission.Current?.CurrentTime ?? 0f) - pending.RecordedAt > PendingWeaponModifierSeconds)
		{
			return false;
		}
		rune = pending.Rune;
		return true;
	}

	public static void Report(Agent owner, MagicRuneEffect effect, string eventName, string message, Color color)
	{
		SotorLog.Debug($"Rune proc: effect={effect} event={eventName} owner='{owner?.Name}' {message}");
		if (owner == null || !owner.IsPlayerControlled)
		{
			return;
		}

		EnsureMission();
		float now = Mission.Current?.CurrentTime ?? 0f;
		string key = owner.Index + "|" + effect + "|" + eventName;
		if (_lastDisplayTimes.TryGetValue(key, out float lastTime) && now - lastTime < DisplayThrottleSeconds)
		{
			return;
		}
		_lastDisplayTimes[key] = now;

		try
		{
			InformationManager.DisplayMessage(new InformationMessage(message, color));
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicRuneCombatFeedback failed: " + ex.Message);
		}
	}

	private static void EnsureMission()
	{
		if (Mission.Current != _boundMission)
		{
			_lastDisplayTimes.Clear();
			_pendingWeaponModifiers.Clear();
			_boundMission = Mission.Current;
		}
	}

	private static string WeaponModifierKey(Agent attacker, Agent victim)
	{
		return attacker.Index + "|" + victim.Index;
	}
}
