using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SOTOR.MagicAccessories;

public sealed class MagicPowerRingCampaignBehavior : CampaignBehaviorBase
{
	private sealed class WaterState
	{
		public float EligibleAt = -1f;
		public float NextHealAt;
	}

	private readonly Dictionary<Hero, WaterState> _waterStates = new Dictionary<Hero, WaterState>();

	private float _campaignTime;

	public override void RegisterEvents()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, OnCanHeroDie);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private static bool MainHeroWears(MagicPowerRingEffect effect)
	{
		return Hero.MainHero != null && MagicAccessoryService.GetEquippedPowerRing(Hero.MainHero, effect) != null;
	}

	private static bool IsActivePlayerCompanion(Hero hero)
	{
		return hero != null && hero.IsPlayerCompanion && hero.IsActive && !hero.IsPrisoner &&
			hero.PartyBelongedTo == MobileParty.MainParty;
	}

	private static MapEventParty FindMainParty(MapEvent mapEvent, BattleSideEnum side)
	{
		foreach (MapEventParty partyEvent in mapEvent.PartiesOnSide(side))
		{
			if (partyEvent.Party == PartyBase.MainParty)
			{
				return partyEvent;
			}
		}
		return null;
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent == null || !MainHeroWears(MagicPowerRingEffect.Death))
		{
			return;
		}

		MapEventParty mainPartyEvent = FindMainParty(mapEvent, BattleSideEnum.Attacker) ??
			FindMainParty(mapEvent, BattleSideEnum.Defender);
		if (mainPartyEvent?.DiedInBattle == null)
		{
			return;
		}

		foreach (TroopRosterElement casualty in mainPartyEvent.DiedInBattle.GetTroopRoster())
		{
			CharacterObject troop = casualty.Character;
			if (troop == null || troop.IsHero || casualty.Number <= 0)
			{
				continue;
			}

			int restored = 0;
			for (int i = 0; i < casualty.Number; i++)
			{
				if (MBRandom.RandomFloat < 0.7f)
				{
					restored++;
				}
			}
			if (restored > 0)
			{
				MobileParty.MainParty.MemberRoster.AddToCounts(troop, restored, false, restored, 0, true, -1);
			}
		}
	}

	private void OnCanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail detail, ref bool canDie)
	{
		if (!canDie || detail != KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
		{
			return;
		}
		if (hero == Hero.MainHero)
		{
			if (MainHeroWears(MagicPowerRingEffect.Death) && MBRandom.RandomFloat < 0.8f)
			{
				canDie = false;
			}
			return;
		}
		if (!IsActivePlayerCompanion(hero) || MagicAccessoryService.GetEquippedPowerRing(hero, MagicPowerRingEffect.Death) == null)
		{
			return;
		}
		if (MBRandom.RandomFloat < 0.6f)
		{
			canDie = false;
		}
	}

	private void OnCampaignTick(float dt)
	{
		if (dt <= 0f || Hero.MainHero == null)
		{
			return;
		}
		_campaignTime += dt;
		HashSet<Hero> candidates = new HashSet<Hero>();
		if (IsCampaignWaterCandidate(Hero.MainHero))
		{
			candidates.Add(Hero.MainHero);
		}
		Clan playerClan = Clan.PlayerClan;
		if (playerClan != null)
		{
			foreach (Hero companion in playerClan.Companions)
			{
				if (IsCampaignWaterCandidate(companion))
				{
					candidates.Add(companion);
				}
			}
		}
		foreach (Hero hero in candidates)
		{
			TickWater(hero);
		}
		foreach (Hero tracked in new List<Hero>(_waterStates.Keys))
		{
			if (!candidates.Contains(tracked))
			{
				_waterStates.Remove(tracked);
			}
		}
	}

	private static bool IsCampaignWaterCandidate(Hero hero)
	{
		return hero != null && hero.IsAlive && hero.IsActive && !hero.IsPrisoner &&
			MagicAccessoryService.GetEquippedPowerRing(hero, MagicPowerRingEffect.Water) != null;
	}

	private void TickWater(Hero hero)
	{
		if (MagicAccessoryService.GetEquippedPowerRing(hero, MagicPowerRingEffect.Water) == null || hero.MaxHitPoints <= 0)
		{
			_waterStates.Remove(hero);
			return;
		}

		int threshold = (int)Math.Ceiling(hero.MaxHitPoints * 0.6f);
		if (hero.HitPoints >= threshold)
		{
			_waterStates.Remove(hero);
			return;
		}
		if (!_waterStates.TryGetValue(hero, out WaterState state))
		{
			state = new WaterState { EligibleAt = _campaignTime };
			_waterStates[hero] = state;
			return;
		}
		if (_campaignTime - state.EligibleAt < 5f || _campaignTime < state.NextHealAt)
		{
			return;
		}

		state.NextHealAt = _campaignTime + 5f;
		hero.HitPoints = Math.Min(threshold, hero.HitPoints + 2);
	}
}
