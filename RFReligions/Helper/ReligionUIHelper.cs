using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.RFReligions.Behavior;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFReligions.Helper;

public static class ReligionUIHelper
{
    public static List<TooltipProperty> GetTownReligion(Town town)
    {
        return GetTooltipForAccumulatingProperty(_religionStr.ToString(), town.FoodStocks, GetExplainedReligions(town));
    }

    public static int GetTownReligionLbl()
    {
        var campaignBehavior = ReligionBehavior.Instance;
        var settlementReligionModel = campaignBehavior._settlements[Settlement.CurrentSettlement];
        var mainReligion = settlementReligionModel.GetMainReligion();
        return (int)settlementReligionModel._religiousValues.Sum(keyValuePair => keyValuePair.Value *
            (keyValuePair.Key == mainReligion ? 1 : -1));
    }

    public static StringPairItemVM GetHeroReligion(Hero hero)
    {
        var campaignBehavior = ReligionBehavior.Instance;
        if (campaignBehavior._heroes.ContainsKey(hero))
        {
            var religion = campaignBehavior._heroes[hero].Religion;
            return new StringPairItemVM(_religionStr.ToString(), GetReligionName(religion).ToString(), null);
        }

        return new StringPairItemVM(_religionStr.ToString(), _atheist.ToString(), null);
    }


    public static StringPairItemVM GetHeroReligionDevotion(Hero hero)
    {
        var campaignBehavior = ReligionBehavior.Instance;
        if (!campaignBehavior._heroes.ContainsKey(hero))
            return new StringPairItemVM(_devotion.ToString(), _atheist.ToString(), null);
        var religion = campaignBehavior._heroes[hero].Religion;
        var devotionToCurrentReligion = campaignBehavior._heroes[hero].GetDevotionToCurrentReligion();
        return new StringPairItemVM(_devotion.ToString(), GetHeroReligionToolTip(religion, devotionToCurrentReligion),
            null);
    }


    private static string GetHeroReligionToolTip(Core.RFReligions relType, float devotion)
    {
        TextObject textObject;
        if (devotion > 90f)
            textObject = GameTexts.FindText("RFRDZoX88").SetTextVariable("VALUE", (int)devotion);
        else if (devotion <= 50f)
            textObject = GameTexts.FindText("RFRmlFcWU").SetTextVariable("VALUE", (int)devotion);
        else
            textObject = GameTexts.FindText("RFRwvhCGA").SetTextVariable("VALUE", (int)devotion);
        return textObject.ToString();
    }


    private static ExplainedNumber GetExplainedReligions(Town town)
    {
        var result = new ExplainedNumber(0f, true, null);
        var campaignBehavior = ReligionBehavior.Instance;
        if (campaignBehavior._settlements.ContainsKey(town.Settlement))
        {
            var settlementReligionModel = campaignBehavior._settlements[town.Settlement];
            var mainReligion = settlementReligionModel.GetMainReligion();
            foreach (var keyValuePair in settlementReligionModel._religiousValues)
            {
                var religionName = GetReligionName(keyValuePair.Key);
                result.Add(keyValuePair.Value * (float)(keyValuePair.Key == mainReligion ? 1 : -1), religionName, null);
            }
        }

        return result;
    }


    public static TextObject GetReligionName(Core.RFReligions rel, Hero hero)
    {
        switch (rel)
        {
            case Core.RFReligions.Faelora:
                return GameTexts.FindText("RFRJL9DFy").SetTextVariable("HERO", hero.Name);
            case Core.RFReligions.AeternaFide:
                return GameTexts.FindText("RFRmVFuaP").SetTextVariable("HERO", hero.Name);
            case Core.RFReligions.Anorites:
                return GameTexts.FindText("RFRmJQL1W").SetTextVariable("HERO", hero.Name);
            case Core.RFReligions.Xochxinti:
                return GameTexts.FindText("RFRSXf5jC").SetTextVariable("HERO", hero.Name);
            case Core.RFReligions.KharazDrathar:
                return GameTexts.FindText("RFRJ2sQmB").SetTextVariable("HERO", hero.Name);
            default:
                return GameTexts.FindText("RFRZSJNWb").SetTextVariable("HERO", hero.Name);
        }
    }


    public static TextObject GetReligionName(Core.RFReligions rel)
    {
        switch (rel)
        {
            case Core.RFReligions.Faelora:
                return GameTexts.FindText("str_title_religion_faelora");
            case Core.RFReligions.AeternaFide:
                return GameTexts.FindText("str_title_religion_aeternafide");
            case Core.RFReligions.Anorites:
                return GameTexts.FindText("str_title_religion_anorites");
            case Core.RFReligions.Xochxinti:
                return GameTexts.FindText("str_title_religion_xochxinti");
            case Core.RFReligions.KharazDrathar:
                return GameTexts.FindText("str_title_religion_kharazdrathar");
            case Core.RFReligions.PharunAegis:
                return GameTexts.FindText("str_title_religion_pharunaegis");
            case Core.RFReligions.TengralorOrkhai:
                return GameTexts.FindText("str_title_religion_tengralororkhai");
            case Core.RFReligions.VyralethAmara:
                return GameTexts.FindText("str_title_religion_vyralethamara");
            default:
                return GameTexts.FindText("str_title_religion_aeternafide");
        }
    }


    private static List<TooltipProperty> GetTooltipForAccumulatingProperty(string propertyName, float currentValue,
        ExplainedNumber explainedNumber)
    {
        List<TooltipProperty> list = new();
        TooltipAddPropertyTitleWithValue(list, propertyName, currentValue);
        TooltipAddExplanedChange(list, ref explainedNumber);
        return list;
    }


    private static void TooltipAddExplanedChange(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
    {
        TooltipAddExplanation(properties, ref explainedNumber);
        TooltipAddDoubleSeperator(properties, false);
    }

    private static void TooltipAddDoubleSeperator(List<TooltipProperty> properties, bool onlyShowOnExtend = false)
    {
        properties.Add(new TooltipProperty("", string.Empty, 0, onlyShowOnExtend,
            TooltipProperty.TooltipPropertyFlags.RundownSeperator));
    }

    private static void TooltipAddExplanation(List<TooltipProperty> properties, ref ExplainedNumber explainedNumber)
    {
        List<ValueTuple<string, float>> lines = explainedNumber.GetLines();
        if (lines.Count <= 0) return;
        foreach (ValueTuple<string, float> valueTuple in lines)
        {
            var text = string.Format("{0}{1:0.##}", (double)valueTuple.Item2 > 0.001 ? _plusStr.ToString() : "",
                valueTuple.Item2);
            properties.Add(new TooltipProperty(valueTuple.Item1, text, 0));
        }
    }


    private static void TooltipAddPropertyTitleWithValue(List<TooltipProperty> properties, string propertyName,
        float currentValue)
    {
        properties.Add(new TooltipProperty(propertyName, "", 0, modifier: TooltipProperty.TooltipPropertyFlags.Title));
    }


    private static readonly TextObject _plusStr = new("{=eTw2aNV5}+", null);
    private static readonly TextObject _religionStr = GameTexts.FindText("RFRxjxR1z");
    private static readonly TextObject _devotion = GameTexts.FindText("RFRYKBtm0");
    private static readonly TextObject _atheist = GameTexts.FindText("RFRQZpjaP");
}