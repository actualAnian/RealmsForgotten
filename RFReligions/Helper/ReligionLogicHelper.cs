using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFReligions.Helper;

public static class ReligionLogicHelper
{
    public static Dictionary<Core.RFReligions, Core.RFReligions> TolerableReligions = new()
    {
        { Core.RFReligions.Faelora, Core.RFReligions.Xochxinti },
        { Core.RFReligions.Anorites, Core.RFReligions.AeternaFide },
        { Core.RFReligions.KharazDrathar, Core.RFReligions.TengralorOrkhai },
        { Core.RFReligions.Xochxinti, Core.RFReligions.Faelora },
        { Core.RFReligions.PharunAegis, Core.RFReligions.None },
        { Core.RFReligions.TengralorOrkhai, Core.RFReligions.All },
        { Core.RFReligions.VyralethAmara, Core.RFReligions.None },
        { Core.RFReligions.AeternaFide, Core.RFReligions.Anorites }
    };
    public static bool ReligionSacrificeHaveItems(Core.RFReligions rel, int requiredItemCount, ItemRoster roster)
    {
        requiredItemCount = requiredItemCount > 0 ? requiredItemCount : 1;
        if (rel == Core.RFReligions.Anorites)
            return roster.ToList<ItemRosterElement>().Find((ItemRosterElement item) =>
                       item.EquipmentElement.Item.IsAnimal && item.EquipmentElement.Item.StringId == "cow").Amount >=
                   requiredItemCount;
        if (rel == Core.RFReligions.KharazDrathar)
            return roster.ToList<ItemRosterElement>().Find((ItemRosterElement item) =>
                       item.EquipmentElement.Item.IsAnimal && item.EquipmentElement.Item.StringId == "hog").Amount >=
                   requiredItemCount;
        if (rel == Core.RFReligions.Xochxinti)
            return roster.ToList<ItemRosterElement>().Find((ItemRosterElement item) =>
                    item.EquipmentElement.Item.IsMountable && item.EquipmentElement.Item.StringId.Contains("horse"))
                .Amount >= requiredItemCount;
        if (rel == Core.RFReligions.AeternaFide)
            return roster.ToList<ItemRosterElement>().Find((ItemRosterElement item) =>
                       item.EquipmentElement.Item.IsAnimal && item.EquipmentElement.Item.StringId == "sheep").Amount >=
                   requiredItemCount;
        return false;
    }


    public static bool ReligionOfferHaveItems(Core.RFReligions rel, ItemRoster roster)
    {
        var num = 5;
        var num2 = 3;
        if (rel == Core.RFReligions.Anorites)
        {
            var amount = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "wood").Amount;
            var amount2 = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "charcoal").Amount;
            return amount >= num && amount2 >= num2;
        }

        if (rel == Core.RFReligions.KharazDrathar)
        {
            var amount3 = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "cheese").Amount;
            var amount4 = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "butter").Amount;
            return amount3 >= num && amount4 >= num2;
        }

        if (rel == Core.RFReligions.Xochxinti)
        {
            var amount5 = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "iron").Amount;
            var amount6 = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "charcoal").Amount;
            return amount5 >= num && amount6 >= num2;
        }

        if (rel == Core.RFReligions.AeternaFide)
        {
            var amount7 = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "wood").Amount;
            var amount8 = roster.ToList<ItemRosterElement>()
                .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "iron").Amount;
            return amount7 >= num && amount8 >= num2;
        }

        if (rel != Core.RFReligions.Faelora) return false;
        var amount9 = roster.ToList<ItemRosterElement>()
            .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "wine").Amount;
        var amount10 = roster.ToList<ItemRosterElement>()
            .Find((ItemRosterElement item) => item.EquipmentElement.Item.StringId == "cheese").Amount;
        return amount9 >= num && amount10 >= num2;
    }


    public static bool ReligionOfferItems(Core.RFReligions rel, ItemRoster roster)
    {
        try
        {
            var num = 5;
            var num2 = 3;
            if (rel == Core.RFReligions.Anorites)
            {
                foreach (var itemRosterElement in roster.ToList<ItemRosterElement>())
                {
                    if (num == 0 && num2 == 0) return true;
                    if (num != 0 && itemRosterElement.EquipmentElement.Item.StringId == "wood")
                    {
                        num--;
                        roster.Remove(itemRosterElement);
                    }

                    if (num2 != 0 && itemRosterElement.EquipmentElement.Item.StringId == "charcoal")
                    {
                        num2--;
                        roster.Remove(itemRosterElement);
                    }
                }

                return true;
            }

            if (rel == Core.RFReligions.KharazDrathar)
            {
                foreach (var itemRosterElement2 in roster.ToList<ItemRosterElement>())
                {
                    if (num == 0 && num2 == 0) return true;
                    if (num != 0 && itemRosterElement2.EquipmentElement.Item.StringId == "cheese")
                    {
                        num--;
                        roster.Remove(itemRosterElement2);
                    }

                    if (num2 != 0 && itemRosterElement2.EquipmentElement.Item.StringId == "butter")
                    {
                        num2--;
                        roster.Remove(itemRosterElement2);
                    }
                }

                return true;
            }

            if (rel == Core.RFReligions.Xochxinti)
            {
                foreach (var itemRosterElement3 in roster.ToList<ItemRosterElement>())
                {
                    if (num == 0 && num2 == 0) return true;
                    if (num != 0 && itemRosterElement3.EquipmentElement.Item.StringId == "iron")
                    {
                        num--;
                        roster.Remove(itemRosterElement3);
                    }

                    if (num2 != 0 && itemRosterElement3.EquipmentElement.Item.StringId == "charcoal")
                    {
                        num2--;
                        roster.Remove(itemRosterElement3);
                    }
                }

                return true;
            }

            if (rel == Core.RFReligions.AeternaFide)
            {
                foreach (var itemRosterElement4 in roster.ToList<ItemRosterElement>())
                {
                    if (num == 0 && num2 == 0) return true;
                    if (num != 0 && itemRosterElement4.EquipmentElement.Item.StringId == "wood")
                    {
                        num--;
                        roster.Remove(itemRosterElement4);
                    }

                    if (num2 != 0 && itemRosterElement4.EquipmentElement.Item.StringId == "iron")
                    {
                        num2--;
                        roster.Remove(itemRosterElement4);
                    }
                }

                return true;
            }

            if (rel == Core.RFReligions.Faelora)
            {
                foreach (var itemRosterElement5 in roster.ToList<ItemRosterElement>())
                {
                    if (num == 0 && num2 == 0) return true;
                    if (num != 0 && itemRosterElement5.EquipmentElement.Item.StringId == "wine")
                    {
                        num--;
                        roster.Remove(itemRosterElement5);
                    }

                    if (num2 != 0 && itemRosterElement5.EquipmentElement.Item.StringId == "cheese")
                    {
                        num2--;
                        roster.Remove(itemRosterElement5);
                    }
                }

                return true;
            }
        }
        catch
        {
        }

        return false;
    }


    public static bool ReligionSacrificeItems(Core.RFReligions rel, int requiredItemCount, ItemRoster roster)
    {
        try
        {
            requiredItemCount = requiredItemCount > 0 ? requiredItemCount : 1;
            if (rel == Core.RFReligions.Anorites)
            {
                foreach (var itemRosterElement in roster.ToList<ItemRosterElement>())
                {
                    if (requiredItemCount == 0) return true;
                    if (itemRosterElement.EquipmentElement.Item.IsAnimal &&
                        itemRosterElement.EquipmentElement.Item.StringId == "cow")
                        roster.AddToCounts(itemRosterElement.EquipmentElement, -1 * requiredItemCount);
                }

                return true;
            }

            if (rel == Core.RFReligions.KharazDrathar)
            {
                foreach (var itemRosterElement2 in roster.ToList<ItemRosterElement>())
                {
                    if (requiredItemCount == 0) return true;
                    if (itemRosterElement2.EquipmentElement.Item.IsAnimal &&
                        itemRosterElement2.EquipmentElement.Item.StringId == "hog")
                        roster.AddToCounts(itemRosterElement2.EquipmentElement, -1 * requiredItemCount);
                }

                return true;
            }

            if (rel == Core.RFReligions.Xochxinti)
            {
                foreach (var itemRosterElement3 in roster.ToList<ItemRosterElement>())
                {
                    if (requiredItemCount == 0) return true;
                    if (itemRosterElement3.EquipmentElement.Item.IsMountable &&
                        itemRosterElement3.EquipmentElement.Item.StringId.Contains("horse"))
                        roster.AddToCounts(itemRosterElement3.EquipmentElement, -1 * requiredItemCount);
                }

                return true;
            }

            if (rel == Core.RFReligions.AeternaFide)
            {
                foreach (var itemRosterElement4 in roster.ToList<ItemRosterElement>())
                {
                    if (requiredItemCount == 0) return true;
                    if (itemRosterElement4.EquipmentElement.Item.IsAnimal &&
                        itemRosterElement4.EquipmentElement.Item.StringId == "sheep")
                        roster.AddToCounts(itemRosterElement4.EquipmentElement, -1 * requiredItemCount);
                }

                return true;
            }
        }
        catch
        {
        }

        return false;
    }


    public static TextObject ReligionTempleSacrificeText(Core.RFReligions rel)
    {
        switch (rel)
        {
            case Core.RFReligions.Anorites:
            {
                var itemObject = Items.All.FirstOrDefault((ItemObject item) => item.IsAnimal && item.StringId == "cow");
                return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", 5)
                    .SetTextVariable("ANIMAL", itemObject.Name);
            }
            case Core.RFReligions.KharazDrathar:
            {
                var itemObject2 = Items.All.FirstOrDefault((ItemObject item) => item.IsAnimal && item.StringId == "hog");
                return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", 5)
                    .SetTextVariable("ANIMAL", itemObject2.Name);
            }
            case Core.RFReligions.Xochxinti:
                Items.All.FirstOrDefault((ItemObject item) =>
                    item.IsMountable && item.ItemCategory.StringId.Contains("horse"));
                return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", 3)
                    .SetTextVariable("ANIMAL", new TextObject("{=LwfILaRH}Horse", null));
            case Core.RFReligions.AeternaFide:
            {
                var itemObject3 =
                    Items.All.FirstOrDefault((ItemObject item) => item.IsAnimal && item.StringId.Contains("sheep"));
                return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", 5)
                    .SetTextVariable("ANIMAL", itemObject3.Name);
            }
            default:
                var itemObject4 =
                    Items.All.FirstOrDefault((ItemObject item) => item.IsAnimal && item.StringId.Contains("sheep"));
                return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", 5)
                    .SetTextVariable("ANIMAL", itemObject4.Name);
        }
    }


    public static TextObject ReligionTempleOfferItems(Core.RFReligions rel)
    {
        if (rel == Core.RFReligions.Anorites)
        {
            var itemObject = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("wood"));
            var itemObject2 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("charcoal"));
            if (itemObject2 == null || itemObject2 == null) return TextObject.Empty;
            return new TextObject(primSecond, null).SetTextVariable("PRIMARY_COUNT", 5)
                .SetTextVariable("PRIMARY_NAME", itemObject.Name).SetTextVariable("SECONDARY_COUNT", 3)
                .SetTextVariable("SECONDARY_NAME", itemObject2.Name);
        }
        else if (rel == Core.RFReligions.KharazDrathar)
        {
            var itemObject3 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("cheese"));
            var itemObject4 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("butter"));
            if (itemObject4 != null && itemObject4 != null)
                return new TextObject(primSecond, null).SetTextVariable("PRIMARY_COUNT", 5)
                    .SetTextVariable("PRIMARY_NAME", itemObject3.Name).SetTextVariable("SECONDARY_COUNT", 3)
                    .SetTextVariable("SECONDARY_NAME", itemObject4.Name);
            return TextObject.Empty;
        }
        else if (rel != Core.RFReligions.Xochxinti)
        {
            if (rel != Core.RFReligions.AeternaFide)
            {
                if (rel != Core.RFReligions.Faelora) return TextObject.Empty;
                var itemObject5 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("wine"));
                var itemObject6 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("cheese"));
                if (itemObject6 != null && itemObject6 != null)
                    return new TextObject(primSecond, null).SetTextVariable("PRIMARY_COUNT", 5)
                        .SetTextVariable("PRIMARY_NAME", itemObject5.Name).SetTextVariable("SECONDARY_COUNT", 3)
                        .SetTextVariable("SECONDARY_NAME", itemObject6.Name);
                return TextObject.Empty;
            }
            else
            {
                var itemObject7 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("wood"));
                var itemObject8 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("iron"));
                if (itemObject8 == null || itemObject8 == null) return TextObject.Empty;
                return new TextObject(primSecond, null).SetTextVariable("PRIMARY_COUNT", 5)
                    .SetTextVariable("PRIMARY_NAME", itemObject7.Name).SetTextVariable("SECONDARY_COUNT", 3)
                    .SetTextVariable("SECONDARY_NAME", itemObject8.Name);
            }
        }
        else
        {
            var itemObject9 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("iron"));
            var itemObject10 = Items.All.FirstOrDefault((ItemObject item) => item.StringId.Contains("charcoal"));
            if (itemObject10 != null && itemObject10 != null)
                return new TextObject(primSecond, null).SetTextVariable("PRIMARY_COUNT", 5)
                    .SetTextVariable("PRIMARY_NAME", itemObject9.Name).SetTextVariable("SECONDARY_COUNT", 3)
                    .SetTextVariable("SECONDARY_NAME", itemObject10.Name);
            return TextObject.Empty;
        }
    }


    public static TextObject ReligionSacrificeText(Core.RFReligions rel, int soldierCount)
    {
        if (rel == Core.RFReligions.Anorites)
        {
            var itemObject = Items.All.FirstOrDefault((ItemObject item) => item.IsAnimal && item.StringId == "cow");
            var num = soldierCount / 15;
            return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", num == 0 ? 1 : num)
                .SetTextVariable("ANIMAL", itemObject.Name);
        }

        if (rel == Core.RFReligions.KharazDrathar)
        {
            var itemObject2 = Items.All.FirstOrDefault((ItemObject item) => item.IsAnimal && item.StringId == "hog");
            var num2 = soldierCount / 10;
            return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", num2 == 0 ? 1 : num2)
                .SetTextVariable("ANIMAL", itemObject2.Name);
        }

        if (rel == Core.RFReligions.Xochxinti)
        {
            Items.All.FirstOrDefault((ItemObject item) =>
                item.IsMountable && item.ItemCategory.StringId.Contains("horse"));
            var num3 = soldierCount / 20;
            return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", num3 == 0 ? 1 : num3)
                .SetTextVariable("ANIMAL", new TextObject("{=LwfILaRH}Horse", null));
        }

        if (rel == Core.RFReligions.AeternaFide)
        {
            var itemObject3 =
                Items.All.FirstOrDefault((ItemObject item) => item.IsAnimal && item.StringId.Contains("sheep"));
            var num4 = soldierCount / 10;
            return new TextObject("({COUNT} {ANIMAL})", null).SetTextVariable("COUNT", num4 == 0 ? 1 : num4)
                .SetTextVariable("ANIMAL", itemObject3.Name);
        }

        return TextObject.Empty;
    }


    public static bool ReligionCanSacrifice(Core.RFReligions rel)
    {
        return rel - Core.RFReligions.AeternaFide <= 3;
    }


    public static bool ReligionCanItemSacrifice(Core.RFReligions rel)
    {
        return rel <= Core.RFReligions.KharazDrathar;
    }


    private static string primSecond = GameTexts.FindText("RFRAJQJbd").Value;
}