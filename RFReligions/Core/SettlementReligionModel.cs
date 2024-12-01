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

        (from x in _religiousValues
            orderby x.Value
            select x).Reverse<KeyValuePair<RFReligions, float>>();
    }


    public void DailyReligionDevotion(float val)
    {
        try
        {
            var num = val / (float)_religiousValues.Count;
            var key = (from x in _religiousValues
                orderby x.Value
                select x).Reverse().FirstOrDefault().Key;
            var religiousValues = _religiousValues;
            var key2 = key;
            religiousValues[key2] += val * (float)(MBRandom.RandomInt(0, 100) > 80 ? -1 : 1);
            foreach (var keyValuePair in _religiousValues)
                if (keyValuePair.Key != key)
                {
                    religiousValues = _religiousValues;
                    key2 = keyValuePair.Key;
                    religiousValues[key2] += num * (float)(MBRandom.RandomInt(0, 100) > 80 ? -1 : 1);
                }

            (from x in _religiousValues
                orderby x.Value
                select x).Reverse();
        }
        catch
        {
        }
    }


    public float GetMainReligionRatio()
    {
        var num = 0f;
        var key = (from x in _religiousValues
            orderby x.Value
            select x).Reverse().FirstOrDefault().Key;
        foreach (var keyValuePair in _religiousValues)
            if (keyValuePair.Key != key)
                num += keyValuePair.Value;
        return _religiousValues[key] / (num + _religiousValues[key]);
    }


    public void ReCalculateDevotion(Town town)
    {
        var mainReligionRatio = GetMainReligionRatio();
        var num = subject.Town.Prosperity * mainReligionRatio;
        var num2 = subject.Town.Prosperity - num;
        var key = (from x in _religiousValues
            orderby x.Value
            select x).Reverse().FirstOrDefault().Key;
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

        (from x in _religiousValues
            orderby x.Value
            select x).Reverse();
    }

    public void ResetReligionDevotion(RFReligions rel)
    {
        if (_religiousValues.ContainsKey(rel)) _religiousValues[rel] = 0f;
        (from x in _religiousValues
            orderby x.Value
            select x).Reverse();
    }

    [SaveableField(1)] public Dictionary<RFReligions, float> _religiousValues;


    [SaveableField(2)] public Settlement subject;
}