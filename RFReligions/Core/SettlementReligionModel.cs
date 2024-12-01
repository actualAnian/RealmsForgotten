using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFReligions.Core;

public class SettlementReligionModel
{
    public SettlementReligionModel(Settlement subjectParam)
    {
        subject = subjectParam;
        _religiousValues = new Dictionary<RFReligions, float>();
        foreach (var obj in Enum.GetValues(typeof(RFReligions)))
        {
            var key = (RFReligions)obj;
            _religiousValues.Add(key, 0f);
        }
    }


    public RFReligions GetMainReligion()
    {
        if (_religiousValues.Count <= 0) return RFReligions.Faelora;
        return (from x in _religiousValues
            orderby x.Value
            select x).Reverse().FirstOrDefault().Key;
    }


    public void AddDevotionToReligion(RFReligions rel, float val)
    {
        if (_religiousValues.ContainsKey(rel))
        {
            var religiousValues = _religiousValues;
            religiousValues[rel] += val;
        }

        //(from x in _religiousValues
        //    orderby x.Value
        //    select x).Reverse<KeyValuePair<RFReligions, float>>();
    }


    public void DailyReligionDevotion(float val)
    {
        float num = val / _religiousValues.Count;
        RFReligions key = CalculateMainReligion();
        RFReligions key2 = key;
        _religiousValues[key2] += val * (MBRandom.RandomInt(0, 100) > 80 ? -1 : 1);
        List<RFReligions> keys = _religiousValues.Keys.ToList();
        foreach (RFReligions religion in keys)
            if (religion != key)
            {
                _religiousValues[religion] += num * (MBRandom.RandomInt(0, 100) > 80 ? -1 : 1);
            }

            //(from x in _religiousValues
            // orderby x.Value
            // select x).Reverse();
    }


    public float GetMainReligionRatio()
    {
        RFReligions mainReligion = CalculateMainReligion();
        var num = 0f;
        foreach (var keyValuePair in _religiousValues)
            if (keyValuePair.Key != mainReligion)
                num += keyValuePair.Value;
        return _religiousValues[mainReligion] / (num + _religiousValues[mainReligion]);
    }

    private RFReligions CalculateMainReligion()
    {
        float maxValue = 0;
        RFReligions maxKey = default;
        foreach (KeyValuePair<RFReligions, float> keyValuePair in _religiousValues)
        {
            if (keyValuePair.Value > maxValue)
            {
                maxValue = keyValuePair.Value;
                maxKey = keyValuePair.Key;
            }
        }
        return maxKey;
    }
    public void ReCalculateDevotion(Town town)
    {
        var mainReligionRatio = GetMainReligionRatio();
        var num = subject.Town.Prosperity * mainReligionRatio;
        var num2 = subject.Town.Prosperity - num;
        RFReligions key = CalculateMainReligion();
        _religiousValues[key] = num;
        foreach (var keyValuePair in _religiousValues.ToList())
        {
            if (num2 < 1f) break;
            var num3 = MBRandom.RandomFloatRanged(num2);
            if (keyValuePair.Key != key)
            {
                num2 -= num3;
                _religiousValues[keyValuePair.Key] = num3;
            }
        }

        //(from x in _religiousValues
        //    orderby x.Value
        //    select x).Reverse();
    }

    public void ResetReligionDevotion(RFReligions rel)
    {
        if (_religiousValues.ContainsKey(rel)) _religiousValues[rel] = 0f;
        //(from x in _religiousValues
        //    orderby x.Value
        //    select x).Reverse();
    }

    [SaveableField(1)] public Dictionary<RFReligions, float> _religiousValues;


    [SaveableField(2)] public Settlement subject;
}