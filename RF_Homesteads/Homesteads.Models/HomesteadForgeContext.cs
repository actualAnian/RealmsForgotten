using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Homesteads.Models;

internal static class HomesteadForgeContext
{
	private static Homestead? _session;

	private const int MaxOrders = 6;

	private const float RefillChancePerSlot = 0.5f;

	private const float ReplaceChancePerDay = 0.3f;

	private static readonly Dictionary<Homestead, List<CraftingOrder>> _board = new Dictionary<Homestead, List<CraftingOrder>>();

	private static readonly Dictionary<Homestead, int> _boardDay = new Dictionary<Homestead, int>();

	private const string OrderIdPrefix = "hs_order_";

	private static MethodInfo? _addCustomOrder;

	private static MethodInfo? _removeCustomOrder;

	public static bool IsActive { get; private set; }

	public static Town? BackingTown { get; private set; }

	public static Settlement? CurrentOrBackingSettlement()
	{
		Settlement settlement = Settlement.CurrentSettlement;
		if (settlement == null)
		{
			if (!IsActive)
			{
				return null;
			}
			Town? backingTown = BackingTown;
			if (backingTown == null)
			{
				return null;
			}
			settlement = backingTown.Settlement;
		}
		return settlement;
	}

	public static List<CraftingOrder> ActiveOrders()
	{
		if (!IsActive || BackingTown == null || _session == null)
		{
			return new List<CraftingOrder>();
		}
		CraftingCampaignBehavior.CraftingOrderSlots slots = GetSlots(BackingTown);
		if (slots == null || !_board.TryGetValue(_session, out List<CraftingOrder> value))
		{
			return new List<CraftingOrder>();
		}
		MBReadOnlyList<CraftingOrder> live = slots.CustomOrders;
		return value.Where((CraftingOrder o) => live.Contains(o)).ToList();
	}

	public static void Begin(Homestead homestead)
	{
		try
		{
			End();
			if (homestead == null)
			{
				return;
			}
			BackingTown = PickBackingTown(homestead);
			if (BackingTown == null)
			{
				TraceLogger.Write("HomesteadForgeContext", "Begin: no town found to back the order list — orders disabled this session.");
				return;
			}
			CraftingCampaignBehavior.CraftingOrderSlots slots = GetSlots(BackingTown);
			if (slots == null)
			{
				TraceLogger.Write("HomesteadForgeContext", "Begin: backing town has no crafting-order slots — orders disabled.");
				BackingTown = null;
				return;
			}
			_session = homestead;
			IsActive = true;
			List<CraftingOrder> list = RefreshBoardForToday(homestead);
			foreach (CraftingOrder item in list)
			{
				if (!slots.CustomOrders.Contains(item))
				{
					AddCustomOrder(slots, item);
				}
			}
			TraceLogger.Write("HomesteadForgeContext", $"Begin: '{homestead.Name}' forge — backing town '{BackingTown.Name}', board has {list.Count} order(s).");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadForgeContext", "Begin failed: " + ex.GetType().Name + ": " + ex.Message);
			IsActive = true;
		}
	}

	public static void End()
	{
		try
		{
			if (BackingTown != null && _session != null && _board.TryGetValue(_session, out List<CraftingOrder> value))
			{
				CraftingCampaignBehavior.CraftingOrderSlots slots = GetSlots(BackingTown);
				if (slots != null)
				{
					foreach (CraftingOrder item in value.ToList())
					{
						if (slots.CustomOrders.Contains(item))
						{
							RemoveCustomOrder(slots, item);
						}
						else
						{
							value.Remove(item);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadForgeContext", "End cleanup failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		BackingTown = null;
		_session = null;
		IsActive = false;
	}

	private static List<CraftingOrder> RefreshBoardForToday(Homestead homestead)
	{
		if (!_board.TryGetValue(homestead, out List<CraftingOrder> value))
		{
			value = new List<CraftingOrder>();
			_board[homestead] = value;
			_boardDay[homestead] = int.MinValue;
		}
		List<Hero> list = CollectOrderOwners(homestead);
		if (list.Count == 0)
		{
			return value;
		}
		int num = (int)CampaignTime.Now.ToDays;
		int value2;
		int num2 = (_boardDay.TryGetValue(homestead, out value2) ? value2 : int.MinValue);
		if (num2 == int.MinValue)
		{
			while (value.Count < 6)
			{
				CraftingOrder craftingOrder = TryCreateOrder(list.GetRandomElement());
				if (craftingOrder == null)
				{
					break;
				}
				value.Add(craftingOrder);
			}
		}
		else if (num != num2)
		{
			int num3 = Math.Min(Math.Max(1, num - num2), 7);
			for (int i = 0; i < num3; i++)
			{
				if (value.Count > 0 && MBRandom.RandomFloat < 0.3f)
				{
					value.RemoveAt(MBRandom.RandomInt(value.Count));
				}
				while (value.Count < 6 && MBRandom.RandomFloat < 0.5f)
				{
					CraftingOrder craftingOrder2 = TryCreateOrder(list.GetRandomElement());
					if (craftingOrder2 == null)
					{
						break;
					}
					value.Add(craftingOrder2);
				}
			}
		}
		_boardDay[homestead] = num;
		return value;
	}

	private static List<Hero> CollectOrderOwners(Homestead hs)
	{
		List<Hero> owners = new List<Hero>();
		Add(hs.MasterSmithHero);
		Add(hs.TavernKeeperHero);
		Add(hs.HoundMasterHero);
		Add(hs.MarketLadyHero);
		Add(hs.AmbassadorHero);
		Add(hs.ArmsMasterHero);
		Add(hs.TroubadourHero);
		Add(hs.Leader);
		if (hs.ResidentHeroes != null)
		{
			foreach (Hero residentHero in hs.ResidentHeroes)
			{
				Add(residentHero);
			}
		}
		return owners;
		void Add(Hero? h)
		{
			if (h != null && h.IsAlive && h.Culture != null && !owners.Contains(h))
			{
				owners.Add(h);
			}
		}
	}

	private static CraftingOrder? TryCreateOrder(Hero owner)
	{
		try
		{
			CraftingTemplate randomElement = CraftingTemplate.All.GetRandomElement();
			CultureObject culture = owner.Culture ?? Hero.MainHero?.Culture;
			Crafting crafting = new Crafting(randomElement, culture, new TextObject(string.Empty));
			crafting.Init();
			WeaponDesign currentWeaponDesign = crafting.CurrentWeaponDesign;
			float orderDifficulty = MBRandom.RandomInt(40, 160);
			string customId = "hs_order_" + Guid.NewGuid().ToString("N");
			return new CraftingOrder(owner, orderDifficulty, currentWeaponDesign, randomElement, -1, customId);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadForgeContext", "TryCreateOrder failed for '" + owner?.StringId + "': " + ex.GetType().Name + ": " + ex.Message);
			return null;
		}
	}

	private static Town? PickBackingTown(Homestead hs)
	{
		Vec2 home = hs.MobileParty?.GetPosition2D ?? Vec2.Zero;
		return (from s in Settlement.All
			where s.IsTown && s.Town != null
			orderby (s.GetPosition2D - home).LengthSquared
			select s.Town).FirstOrDefault();
	}

	private static CraftingCampaignBehavior.CraftingOrderSlots? GetSlots(Town town)
	{
		ICraftingCampaignBehavior craftingCampaignBehavior = Campaign.Current?.GetCampaignBehavior<ICraftingCampaignBehavior>();
		if (craftingCampaignBehavior?.CraftingOrders == null)
		{
			return null;
		}
		if (!craftingCampaignBehavior.CraftingOrders.TryGetValue(town, out var value))
		{
			return null;
		}
		return value;
	}

	private static void AddCustomOrder(CraftingCampaignBehavior.CraftingOrderSlots slots, CraftingOrder order)
	{
		if ((object)_addCustomOrder == null)
		{
			_addCustomOrder = typeof(CraftingCampaignBehavior.CraftingOrderSlots).GetMethod("AddCustomOrder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		_addCustomOrder?.Invoke(slots, new object[1] { order });
	}

	private static void RemoveCustomOrder(CraftingCampaignBehavior.CraftingOrderSlots slots, CraftingOrder order)
	{
		if ((object)_removeCustomOrder == null)
		{
			_removeCustomOrder = typeof(CraftingCampaignBehavior.CraftingOrderSlots).GetMethod("RemoveCustomOrder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		_removeCustomOrder?.Invoke(slots, new object[1] { order });
	}
}
