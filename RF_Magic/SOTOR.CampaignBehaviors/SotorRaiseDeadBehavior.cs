using System;
using System.Collections.Generic;
using System.Linq;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace SOTOR.CampaignBehaviors;

public class SotorRaiseDeadBehavior : CampaignBehaviorBase
{
	private const string RaisedTroopId = "sotor_skeleton";

	public override void RegisterEvents()
	{
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnMapEventEnded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent == null || PlayerEncounter.Current == null || Hero.MainHero == null)
		{
			return;
		}
		if (mapEvent.PlayerSide != mapEvent.WinningSide)
		{
			SotorMindControlMissionLogic.PendingRecruits.Clear();
			return;
		}
		RecruitMindControlledSurvivors();
		if (!SotorSettings.EnableSkeletonArmies)
		{
			return;
		}
		Hero bestRaiser = GetBestRaiser();
		if (bestRaiser == null)
		{
			return;
		}
		float raiseDeadChance = GetRaiseDeadChance(bestRaiser);
		if (raiseDeadChance <= 0f)
		{
			return;
		}
		CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("sotor_skeleton");
		if (characterObject == null)
		{
			SotorLog.Warn("RaiseDead: troop 'sotor_skeleton' not found — skipping.");
			return;
		}
		int num = 0;
		foreach (MapEventParty item in mapEvent.PartiesOnSide(mapEvent.DefeatedSide))
		{
			foreach (FlattenedTroopRosterElement item2 in item.Troops.Where((FlattenedTroopRosterElement x) => x.IsKilled))
			{
				_ = item2;
				if (MBRandom.RandomFloat <= raiseDeadChance)
				{
					num++;
				}
			}
		}
		if (num > 0)
		{
			PlayerEncounter.Current.RosterToReceiveLootMembers.AddToCounts(characterObject, num);
			SotorLog.Info($"RaiseDead: raised {num} skeleton(s) (raiser '{bestRaiser.Name}', chance {raiseDeadChance:P0}).");
		}
	}

	private static void RecruitMindControlledSurvivors()
	{
		Dictionary<CharacterObject, int> pendingRecruits = SotorMindControlMissionLogic.PendingRecruits;
		if (pendingRecruits.Count == 0)
		{
			return;
		}
		try
		{
			if (!SotorSettings.EnableMindControlledArmies || PlayerEncounter.Current == null)
			{
				return;
			}
			foreach (KeyValuePair<CharacterObject, int> item in pendingRecruits)
			{
				CharacterObject key = item.Key;
				int value = item.Value;
				if (key != null && value > 0)
				{
					PlayerEncounter.Current.RosterToReceiveLootMembers.AddToCounts(key, value);
					PlayerEncounter.Current.RosterToReceiveLootPrisoners.AddToCounts(key, -value);
					SotorLog.Info($"MindControl recruit: {value}x '{key.Name}' joined (de-duped from prisoners).");
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MindControl recruit failed: " + ex.Message);
		}
		finally
		{
			pendingRecruits.Clear();
		}
	}

	private static Hero GetBestRaiser()
	{
		MobileParty partyBelongedTo = Hero.MainHero.PartyBelongedTo;
		if (partyBelongedTo == null)
		{
			return null;
		}
		Hero result = null;
		int num = -1;
		foreach (TroopRosterElement item in partyBelongedTo.MemberRoster.GetTroopRoster())
		{
			Hero hero = item.Character?.HeroObject;
			if (hero != null && CanRaiseDead(hero))
			{
				int num2 = SpellcraftOf(hero);
				if (num2 > num)
				{
					num = num2;
					result = hero;
				}
			}
		}
		return result;
	}

	private static bool CanRaiseDead(Hero hero)
	{
		HeroExtendedInfo heroExtendedInfo = hero?.GetExtendedInfo();
		if (heroExtendedInfo == null)
		{
			return false;
		}
		if (!heroExtendedInfo.HasLore("LoreOfNecromancy"))
		{
			return false;
		}
		if (!heroExtendedInfo.HasSpell("SummonSkeleton"))
		{
			return heroExtendedInfo.HasSpell("GraveCall");
		}
		return true;
	}

	private static float GetRaiseDeadChance(Hero hero)
	{
		if (!CanRaiseDead(hero))
		{
			return 0f;
		}
		return MBMath.ClampFloat((float)SpellcraftOf(hero) * 0.005f, 0.05f, 0.7f);
	}

	private static int SpellcraftOf(Hero hero)
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (spellcraft == null)
		{
			return 0;
		}
		return hero.GetSkillValue(spellcraft);
	}
}
