using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Homesteads;

public class HomesteadSettlementBehavior : CampaignBehaviorBase
{
	private List<HomesteadBuiltSettlement> _built = new List<HomesteadBuiltSettlement>();

	private List<string> _femaleCompatNotableIds = new List<string>();

	public static HomesteadSettlementBehavior? Instance { get; private set; }

	public HomesteadSettlementBehavior()
	{
		Instance = this;
	}

	public bool IsFemaleCompatNotable(Hero hero)
	{
		if (hero != null && hero.StringId != null)
		{
			return _femaleCompatNotableIds.Contains(hero.StringId);
		}
		return false;
	}

	public void MarkFemaleCompatNotable(Hero hero)
	{
		if (hero != null && hero.StringId != null && !_femaleCompatNotableIds.Contains(hero.StringId))
		{
			_femaleCompatNotableIds.Add(hero.StringId);
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		HomesteadSettlementBuilder.FinalizeReloadedSettlements();
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		try
		{
			Clan.PlayerClan?.ConsiderAndUpdateHomeSettlement();
		}
		catch
		{
		}
		HomesteadSettlementBuilder.ApplyOwnershipOnLoad();
		HomesteadSettlementBuilder.RestoreNotablesToSettlements();
		HomesteadSettlementBuilder.RestoreAlleyOwnersOnLoad();
		HomesteadSettlementBuilder.RestoreMilitiasOnSessionLaunched();
		HomesteadSettlementBuilder.RestoreVillageLandownersOnLoad();
	}

	public string? GetOwnerClanId(string settlementStringId)
	{
		return _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId)?.OwnerClanId;
	}

	public string? GetStoredXml(string settlementStringId)
	{
		return _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId)?.Xml;
	}

	public void UpdateStoredXml(string settlementStringId, string newXml)
	{
		HomesteadBuiltSettlement homesteadBuiltSettlement = _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId);
		if (homesteadBuiltSettlement != null && !string.IsNullOrEmpty(newXml))
		{
			homesteadBuiltSettlement.Xml = newXml;
		}
	}

	public int ClearBuilt()
	{
		int count = _built.Count;
		_built.Clear();
		return count;
	}

	public string? GetStoredDefaultBuildingId(string settlementStringId)
	{
		return _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId)?.DefaultBuildingId;
	}

	public float GetStoredProsperity(string settlementStringId)
	{
		return _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId)?.Prosperity ?? (-1f);
	}

	public float GetStoredMilitia(string settlementStringId)
	{
		return _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId)?.Militia ?? 0f;
	}

	public float GetStoredHearth(string settlementStringId)
	{
		return _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId)?.Hearth ?? 0f;
	}

	public HomesteadBuiltSettlement? GetRecord(string settlementStringId)
	{
		return _built.Find((HomesteadBuiltSettlement b) => b.StringId == settlementStringId);
	}

	private void CaptureRuntimeSettlementState()
	{
		if (_built == null)
		{
			return;
		}
		foreach (HomesteadBuiltSettlement item in _built)
		{
			try
			{
				if (string.IsNullOrEmpty(item.StringId))
				{
					continue;
				}
				Settlement settlement = MBObjectManager.Instance.GetObject<Settlement>(item.StringId);
				if (settlement == null)
				{
					continue;
				}
				item.Militia = settlement.Militia;
				item.SettlementHitPoints = settlement.SettlementHitPoints;
				item.HasVisited = settlement.HasVisited;
				item.BribePaid = settlement.BribePaid;
				MBReadOnlyList<float> settlementWallSectionHitPointsRatioList = settlement.SettlementWallSectionHitPointsRatioList;
				item.WallSectionHealth = ((settlementWallSectionHitPointsRatioList != null && settlementWallSectionHitPointsRatioList.Count > 0) ? new List<float>(settlementWallSectionHitPointsRatioList) : null);
				if (settlement.Stash != null)
				{
					item.StashItems = new List<string>();
					foreach (ItemRosterElement item2 in settlement.Stash)
					{
						if (item2.EquipmentElement.Item != null && item2.Amount > 0)
						{
							item.StashItems.Add(string.Format("{0}|{1}|{2}", item2.EquipmentElement.Item.StringId, item2.EquipmentElement.ItemModifier?.StringId ?? "", item2.Amount));
						}
					}
				}
				if (settlement.Village != null)
				{
					item.Hearth = settlement.Village.Hearth;
					item.VillageTradeTax = settlement.Village.TradeTaxAccumulated;
				}
				Town town = settlement.Town;
				if (town == null)
				{
					continue;
				}
				item.BuildingState = new List<string>();
				foreach (Building building in town.Buildings)
				{
					if (building?.BuildingType != null && !building.BuildingType.IsDailyProject)
					{
						item.BuildingState.Add(string.Format("{0}|{1}|{2}", building.BuildingType.StringId, building.CurrentLevel, building.BuildingProgress.ToString("R", CultureInfo.InvariantCulture)));
					}
				}
				item.BuildingQueue = ((town.BuildingsInProgress != null) ? (from q in town.BuildingsInProgress
					where q?.BuildingType?.StringId != null
					select q.BuildingType.StringId).ToList() : new List<string>());
				item.FoodStocks = town.FoodStocks;
				item.GarrisonAutoRecruitDisabled = !town.GarrisonAutoRecruitmentIsEnabled;
				item.GarrisonWagePaymentLimit = settlement.GarrisonWagePaymentLimit;
				item.InRebelliousState = town.InRebelliousState;
				string text = null;
				foreach (Building building2 in town.Buildings)
				{
					if (building2.IsCurrentlyDefault)
					{
						text = building2.BuildingType?.StringId;
						break;
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					item.DefaultBuildingId = text;
				}
				item.Prosperity = town.Prosperity;
				TraceLogger.Write("HomesteadSettlementBehavior", string.Format("DIAG CaptureRuntimeSettlementState '{0}': defId='{1}' -> rec.DefaultBuildingId='{2}', prosperity={3:0.#} -> rec.Prosperity={4:0.#}.", item.StringId, text ?? "NULL", item.DefaultBuildingId ?? "NULL", town.Prosperity, item.Prosperity));
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSettlementBehavior", "DIAG CaptureRuntimeSettlementState failed for '" + item?.StringId + "': " + ex.Message);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore.IsSaving)
		{
			CaptureRuntimeSettlementState();
		}
		dataStore.SyncData("hsr_built_settlements", ref _built);
		if (_built == null)
		{
			_built = new List<HomesteadBuiltSettlement>();
		}
		dataStore.SyncData("hsr_female_compat_notable_ids", ref _femaleCompatNotableIds);
		if (_femaleCompatNotableIds == null)
		{
			_femaleCompatNotableIds = new List<string>();
		}
	}

	public void Record(string stringId, string xml, string displayName, string ownerClanId)
	{
		if (!string.IsNullOrEmpty(stringId) && !string.IsNullOrEmpty(xml))
		{
			_built.RemoveAll((HomesteadBuiltSettlement b) => b.StringId == stringId);
			_built.Add(new HomesteadBuiltSettlement
			{
				StringId = stringId,
				Xml = xml,
				DisplayName = displayName,
				OwnerClanId = ownerClanId
			});
		}
	}
}
