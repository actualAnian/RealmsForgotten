using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Homesteads;

/// <summary>
/// Ties the homestead into the living world of Realms Forgotten:
///  - REFUGEES: when war burns a nearby village, its people seek shelter at the
///    homestead. Sheltering costs housing space and food; refugees work (daily
///    gold) and can be recruited into the garrison.
///  - VETERANS: high-tier troops can be retired to the homestead — they join the
///    garrison and drill it (daily XP scaling with the number of veterans).
///  - RESOURCE ZONES: a homestead near an RF resource zone receives a daily
///    trickle of that resource (reflection into RF_ResourceZones; silent no-op
///    when the module is absent).
///  - CLIMATE: in the harsh climates (permafrost, deep winter, harsh desert)
///    refugees work at half yield and stashed food spoils unless a Field
///    Kitchen is built (reflection into RealmsForgotten's climate catalog).
///  - PILGRIMS: a pleasant homestead (high Leisure) draws travellers who leave
///    coin behind.
///  - NOMAD COURT: when the homestead is the seat of a fiefless nomad kingdom
///    and an Ambassador is staffed, the court generates daily influence.
/// </summary>
public class HomesteadWorldTiesBehavior : CampaignBehaviorBase
{
	public static HomesteadWorldTiesBehavior? Instance;

	private Dictionary<string, int> _refugees = new Dictionary<string, int>();

	private Dictionary<string, int> _veterans = new Dictionary<string, int>();

	private readonly HashSet<string> _sessionNotices = new HashSet<string>();

	private const float RefugeeVillageRadius = 60f;

	private const float ZoneSynergyRadius = 10f;

	private const int PilgrimMinLeisure = 5;

	public HomesteadWorldTiesBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_htWorldRefugees", ref _refugees);
		dataStore.SyncData("_htWorldVeterans", ref _veterans);
		_refugees ??= new Dictionary<string, int>();
		_veterans ??= new Dictionary<string, int>();
	}

	public int GetRefugeeCount(Homestead? hs)
	{
		string? key = hs?.MobileParty?.StringId;
		if (key != null && _refugees.TryGetValue(key, out var value))
		{
			return value;
		}
		return 0;
	}

	public int GetVeteranCount(Homestead? hs)
	{
		string? key = hs?.MobileParty?.StringId;
		if (key != null && _veterans.TryGetValue(key, out var value))
		{
			return value;
		}
		return 0;
	}

	// ── Refugees ─────────────────────────────────────────────────────────

	private void OnVillageLooted(Village village)
	{
		try
		{
			Settlement? settlement = village?.Settlement;
			if (settlement == null || HomesteadBehavior.Instance == null)
			{
				return;
			}
			Vec2 villagePos = settlement.GetPosition2D;
			Homestead? homestead = null;
			float num = RefugeeVillageRadius;
			foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadBehavior.Instance.HomesteadMobileParties.ToList())
			{
				if (item.Key != null && item.Key.IsActive && item.Value != null && !item.Value.IsRetiredOrDestroyed && !item.Value.IsMoving)
				{
					float num2 = villagePos.Distance(item.Key.GetPosition2D);
					if (num2 < num)
					{
						num = num2;
						homestead = item.Value;
					}
				}
			}
			if (homestead?.MobileParty == null)
			{
				return;
			}
			string key = homestead.MobileParty.StringId;
			int current = GetRefugeeCount(homestead);
			int cap = GetRefugeeCap(homestead);
			if (current >= cap)
			{
				return;
			}
			int wave = Math.Min(cap - current, 3 + MBRandom.RandomInt(6));
			Homestead hsRef = homestead;
			TextObject title = new TextObject("{=ht_refugees_title}Refugees at the gate");
			TextObject body = new TextObject("{=ht_refugees_body}{COUNT} refugees from the sack of {VILLAGE} have reached {HOMESTEAD}, begging for shelter. Taking them in will cost food, but willing hands are willing hands.");
			body.SetTextVariable("COUNT", wave);
			body.SetTextVariable("VILLAGE", settlement.Name);
			body.SetTextVariable("HOMESTEAD", homestead.Name);
			InformationManager.ShowInquiry(new InquiryData(title.ToString(), body.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=ht_refugees_accept}Shelter them").ToString(), new TextObject("{=ht_refugees_reject}Turn them away").ToString(), delegate
			{
				_refugees[key] = GetRefugeeCount(hsRef) + wave;
				InformationManager.DisplayMessage(new InformationMessage($"{wave} refugees settle in at {hsRef.Name}. ({_refugees[key]} sheltering)", new Color(0.55f, 0.8f, 1f)));
				HomesteadChronicle.Record($"{wave} refugees from the sack of {settlement.Name} found shelter at the homestead of {hsRef.Name}.");
			}, null));
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadWorldTies", "OnVillageLooted failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static int GetRefugeeCap(Homestead hs)
	{
		int num = 5 + hs.Tier * 5;
		try
		{
			num += hs.GetHomesteadScene().TotalSpace;
		}
		catch
		{
		}
		return num;
	}

	// ── Daily tick: workers, food, zones, climate, pilgrims, court ───────

	private void OnDailyTick()
	{
		if (HomesteadBehavior.Instance == null)
		{
			return;
		}
		foreach (KeyValuePair<MobileParty, Homestead> item in HomesteadBehavior.Instance.HomesteadMobileParties.ToList())
		{
			MobileParty key = item.Key;
			Homestead value = item.Value;
			if (key == null || !key.IsActive || value == null || value.IsRetiredOrDestroyed)
			{
				continue;
			}
			bool harshClimate = IsHarshClimate(value);
			try
			{
				TickRefugees(key, value, harshClimate);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadWorldTies", "TickRefugees failed: " + ex.Message);
			}
			try
			{
				TickVeteranTraining(value);
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadWorldTies", "TickVeteranTraining failed: " + ex2.Message);
			}
			try
			{
				TickZoneSynergy(key, value);
			}
			catch (Exception ex3)
			{
				TraceLogger.Write("HomesteadWorldTies", "TickZoneSynergy failed: " + ex3.Message);
			}
			try
			{
				TickClimateSpoilage(value, harshClimate);
			}
			catch (Exception ex4)
			{
				TraceLogger.Write("HomesteadWorldTies", "TickClimateSpoilage failed: " + ex4.Message);
			}
			try
			{
				TickPilgrims(value);
			}
			catch (Exception ex5)
			{
				TraceLogger.Write("HomesteadWorldTies", "TickPilgrims failed: " + ex5.Message);
			}
			try
			{
				TickNomadCourt(value);
			}
			catch (Exception ex6)
			{
				TraceLogger.Write("HomesteadWorldTies", "TickNomadCourt failed: " + ex6.Message);
			}
		}
	}

	private void TickRefugees(MobileParty party, Homestead hs, bool harshClimate)
	{
		int count = GetRefugeeCount(hs);
		if (count <= 0)
		{
			return;
		}
		// Workers: 2 gold per refugee per day; harsh climates halve the yield
		// unless a Field Kitchen keeps the camp running.
		int perHead = ((harshClimate && !hs.HasFieldKitchen) ? 1 : 2);
		hs.GoldStored += count * perHead;
		// Mouths to feed: one food per four refugees per day, from the stash.
		int need = (count + 3) / 4;
		int eaten = ConsumeFoodFromStash(hs, need);
		if (eaten < need)
		{
			int leaving = Math.Max(1, count / 4);
			_refugees[party.StringId] = count - leaving;
			InformationManager.DisplayMessage(new InformationMessage($"There is not enough food at {hs.Name} — {leaving} refugees moved on. ({_refugees[party.StringId]} remain)", new Color(1f, 0.65f, 0.3f)));
		}
	}

	private static int ConsumeFoodFromStash(Homestead hs, int amount)
	{
		int num = 0;
		if (amount <= 0)
		{
			return 0;
		}
		try
		{
			foreach (ItemRosterElement item in hs.Stash.ToList())
			{
				ItemObject item2 = item.EquipmentElement.Item;
				if (item2 != null && item2.IsFood && item.Amount > 0)
				{
					int num2 = Math.Min(item.Amount, amount - num);
					hs.Stash.AddToCounts(item2, -num2);
					num += num2;
					if (num >= amount)
					{
						break;
					}
				}
			}
		}
		catch
		{
		}
		return num;
	}

	// ── Veterans ─────────────────────────────────────────────────────────

	private void TickVeteranTraining(Homestead hs)
	{
		int count = GetVeteranCount(hs);
		if (count <= 0)
		{
			return;
		}
		TroopRoster troops = hs.Troops;
		if (troops == null || troops.Count == 0)
		{
			return;
		}
		int xp = Math.Min(150, count * 5);
		for (int i = 0; i < troops.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = troops.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Character != null && !elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Number > 0)
			{
				troops.AddXpToTroopAtIndex(i, xp * elementCopyAtIndex.Number);
			}
		}
	}

	private void RetireVeteransFromPlayerParty(Homestead hs)
	{
		TroopRoster? roster = MobileParty.MainParty?.MemberRoster;
		if (roster == null || hs.MobileParty == null)
		{
			return;
		}
		int moved = 0;
		foreach (TroopRosterElement item in roster.GetTroopRoster().ToList())
		{
			CharacterObject character = item.Character;
			if (character != null && !character.IsHero && character.Tier >= 5 && item.Number > 0)
			{
				roster.AddToCounts(character, -item.Number);
				hs.Troops.AddToCounts(character, item.Number);
				moved += item.Number;
			}
		}
		if (moved > 0)
		{
			string key = hs.MobileParty.StringId;
			_veterans[key] = GetVeteranCount(hs) + moved;
			InformationManager.DisplayMessage(new InformationMessage($"{moved} hardened veterans hang up their marching boots at {hs.Name}. They will drill the garrison. ({_veterans[key]} veterans)", new Color(0.55f, 0.9f, 0.55f)));
			HomesteadChronicle.Record($"{moved} war veterans retired to the homestead of {hs.Name}, and took to drilling its garrison.");
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("No tier 5+ troops in your party to retire.", new Color(1f, 0.65f, 0.3f)));
		}
	}

	// ── Resource-zone synergy (reflection into RF_ResourceZones) ─────────

	private static bool _zonesResolved;

	private static PropertyInfo? _zonesInstanceProp;

	private static MethodInfo? _zonesGetLive;

	private static FieldInfo? _zonesDefinitions;

	private static void ResolveZonesApi()
	{
		if (_zonesResolved)
		{
			return;
		}
		_zonesResolved = true;
		try
		{
			Type? type = FindLoadedType("ResourceZonesCampaignBehavior", "RF_ResourceZones");
			if (type != null)
			{
				_zonesInstanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
				_zonesGetLive = type.GetMethod("GetLiveZones", BindingFlags.Public | BindingFlags.Instance);
				_zonesDefinitions = type.GetField("_definitions", BindingFlags.NonPublic | BindingFlags.Instance);
			}
			TraceLogger.Write("HomesteadWorldTies", (_zonesGetLive != null) ? "Resource-zone bridge resolved." : "RF_ResourceZones not available — zone synergy off.");
		}
		catch
		{
		}
	}

	private void TickZoneSynergy(MobileParty party, Homestead hs)
	{
		ResolveZonesApi();
		object? behavior = _zonesInstanceProp?.GetValue(null);
		if (behavior == null || _zonesGetLive == null)
		{
			return;
		}
		if (!(_zonesGetLive.Invoke(behavior, null) is IEnumerable liveZones))
		{
			return;
		}
		IDictionary? definitions = _zonesDefinitions?.GetValue(behavior) as IDictionary;
		Vec2 hsPos = party.GetPosition2D;
		foreach (object tuple in liveZones)
		{
			Type tupleType = tuple.GetType();
			if (!(tupleType.GetField("Item2")?.GetValue(tuple) is MobileParty zoneParty) || !zoneParty.IsActive)
			{
				continue;
			}
			if (hsPos.Distance(zoneParty.GetPosition2D) > ZoneSynergyRadius)
			{
				continue;
			}
			object? record = tupleType.GetField("Item1")?.GetValue(tuple);
			string zoneId = record?.GetType().GetField("ZoneId")?.GetValue(record) as string ?? "";
			string zoneType = "";
			if (definitions != null && zoneId.Length > 0 && definitions.Contains(zoneId))
			{
				object? definition = definitions[zoneId];
				zoneType = definition?.GetType().GetField("Type")?.GetValue(definition)?.ToString() ?? "";
			}
			if (ApplyZoneYield(hs, zoneType) && _sessionNotices.Add("zone_" + party.StringId))
			{
				InformationManager.DisplayMessage(new InformationMessage($"Living beside the {zoneType} works, {hs.Name} receives a daily share of its yield.", new Color(0.55f, 0.8f, 1f)));
			}
			break;
		}
	}

	private static bool ApplyZoneYield(Homestead hs, string zoneType)
	{
		switch (zoneType)
		{
		case "Gold":
			hs.GoldStored += 15;
			return true;
		case "Silver":
			hs.GoldStored += 10;
			return true;
		case "Karthradium":
			hs.GoldStored += 25;
			return true;
		case "Iron":
			return AddStashItem(hs, "iron", 2);
		case "Wood":
			return AddStashItem(hs, "hardwood", 2);
		case "Charcoal":
			return AddStashItem(hs, "charcoal", 2);
		case "Salt":
			return AddStashItem(hs, "salt", 2);
		case "Clay":
			return AddStashItem(hs, "clay", 2);
		default:
			return false;
		}
	}

	private static bool AddStashItem(Homestead hs, string itemId, int amount)
	{
		try
		{
			ItemObject itemObject = Campaign.Current.ObjectManager.GetObject<ItemObject>(itemId);
			if (itemObject != null)
			{
				hs.Stash.AddToCounts(itemObject, amount);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	// ── Climate (reflection into RealmsForgotten's climate catalog) ──────

	private static bool _climateResolved;

	private static MethodInfo? _climateForSettlement;

	private readonly Dictionary<string, string> _climateCache = new Dictionary<string, string>();

	private bool IsHarshClimate(Homestead hs)
	{
		try
		{
			MobileParty? party = hs.MobileParty;
			if (party == null)
			{
				return false;
			}
			if (_climateCache.TryGetValue(party.StringId, out var cached))
			{
				return IsHarshClimateName(cached);
			}
			if (!_climateResolved)
			{
				_climateResolved = true;
				Type? type = FindLoadedType("WeatherClimateCatalog", "RealmsForgotten");
				_climateForSettlement = type?.GetMethod("GetClimateForSettlement", BindingFlags.Public | BindingFlags.Static);
				TraceLogger.Write("HomesteadWorldTies", (_climateForSettlement != null) ? "Climate bridge resolved." : "Climate catalog not available — climate effects off.");
			}
			if (_climateForSettlement == null)
			{
				return false;
			}
			Vec2 pos = party.GetPosition2D;
			Settlement? nearest = Settlement.All?.Where((Settlement s) => s != null && (s.IsTown || s.IsVillage)).OrderBy((Settlement s) => s.GetPosition2D.DistanceSquared(pos)).FirstOrDefault();
			if (nearest == null)
			{
				return false;
			}
			string climate = _climateForSettlement.Invoke(null, new object[1] { nearest })?.ToString() ?? "";
			_climateCache[party.StringId] = climate;
			return IsHarshClimateName(climate);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHarshClimateName(string climate)
	{
		return climate == "PermaWinter" || climate == "WinterNord" || climate == "HarshDesert";
	}

	private void TickClimateSpoilage(Homestead hs, bool harshClimate)
	{
		if (harshClimate && !hs.HasFieldKitchen && ConsumeFoodFromStash(hs, 1) > 0 && _sessionNotices.Add("spoil_" + hs.MobileParty?.StringId))
		{
			InformationManager.DisplayMessage(new InformationMessage($"The harsh climate at {hs.Name} is spoiling stored food — a Field Kitchen would preserve it.", new Color(1f, 0.65f, 0.3f)));
		}
	}

	// ── Pilgrims & travellers ────────────────────────────────────────────

	private void TickPilgrims(Homestead hs)
	{
		int leisure;
		try
		{
			leisure = hs.GetHomesteadScene().TotalLeisure;
		}
		catch
		{
			return;
		}
		if (leisure >= PilgrimMinLeisure)
		{
			int gold = Math.Min(150, leisure * 5);
			hs.GoldStored += gold;
			if (_sessionNotices.Add("pilgrims_" + hs.MobileParty?.StringId))
			{
				InformationManager.DisplayMessage(new InformationMessage($"Travellers and pilgrims rest at {hs.Name} (Leisure {leisure}) and leave coin behind: +{gold} gold/day.", new Color(0.55f, 0.8f, 1f)));
			}
		}
	}

	// ── Nomad court ──────────────────────────────────────────────────────

	private void TickNomadCourt(Homestead hs)
	{
		Kingdom? kingdom = Clan.PlayerClan?.Kingdom;
		if (kingdom != null && kingdom.RulingClan == Clan.PlayerClan && kingdom.Fiefs != null && kingdom.Fiefs.Count == 0 && hs.AmbassadorHero != null && hs.AmbassadorHero.IsAlive)
		{
			float influence = 1f + (float)hs.Tier * 0.5f;
			ChangeClanInfluenceAction.Apply(Clan.PlayerClan, influence);
			if (_sessionNotices.Add("court_" + hs.MobileParty?.StringId))
			{
				InformationManager.DisplayMessage(new InformationMessage($"The nomad court at {hs.Name} is in session — {hs.AmbassadorHero.Name} receives envoys (+{influence:0.#} influence/day).", new Color(0.55f, 0.8f, 1f)));
			}
		}
	}

	// ── Menu options ─────────────────────────────────────────────────────

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		starter.AddGameMenuOption("homestead_menu_manage_main", "ht_worldties_refugees", "{=ht_menu_refugees}Recruit sheltering refugees into the garrison", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
			Homestead? hs2 = HomesteadBehavior.Instance?.CurrentHomestead;
			return GetRefugeeCount(hs2) > 0;
		}, delegate
		{
			Homestead? hs3 = HomesteadBehavior.Instance?.CurrentHomestead;
			if (hs3?.MobileParty != null)
			{
				int count = GetRefugeeCount(hs3);
				CharacterObject? basicTroop = (hs3.MobileParty.LeaderHero?.Culture ?? Hero.MainHero.Culture)?.BasicTroop;
				if (count > 0 && basicTroop != null)
				{
					hs3.Troops.AddToCounts(basicTroop, count);
					_refugees[hs3.MobileParty.StringId] = 0;
					InformationManager.DisplayMessage(new InformationMessage($"{count} refugees take up arms for {hs3.Name} and join the garrison as {basicTroop.Name}.", new Color(0.55f, 0.9f, 0.55f)));
					HomesteadChronicle.Record($"{count} refugees sheltering at {hs3.Name} took up arms for its garrison.");
				}
			}
		});
		starter.AddGameMenuOption("homestead_menu_manage_main", "ht_worldties_veterans", "{=ht_menu_veterans}Retire your hardened veterans here (tier 5+ troops)", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.ManageGarrison;
			if (HomesteadBehavior.Instance?.CurrentHomestead == null)
			{
				return false;
			}
			TroopRoster? roster = MobileParty.MainParty?.MemberRoster;
			return roster != null && roster.GetTroopRoster().Any((TroopRosterElement e) => e.Character != null && !e.Character.IsHero && e.Character.Tier >= 5 && e.Number > 0);
		}, delegate
		{
			Homestead? hs4 = HomesteadBehavior.Instance?.CurrentHomestead;
			if (hs4 != null)
			{
				RetireVeteransFromPlayerParty(hs4);
			}
		});
	}

	// ── Shared reflection helper ─────────────────────────────────────────

	private static Type? FindLoadedType(string simpleName, string assemblyHint)
	{
		try
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				string name = assembly.GetName().Name ?? "";
				if (!name.StartsWith(assemblyHint, StringComparison.OrdinalIgnoreCase) && !name.StartsWith("RealmsForgotten", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				try
				{
					Type? type = assembly.GetTypes().FirstOrDefault((Type t) => t.Name == simpleName);
					if (type != null)
					{
						return type;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}
}
