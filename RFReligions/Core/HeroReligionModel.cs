using System;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFReligions.Core;

public class HeroReligionModel
{
    public HeroReligionModel(RFReligions mainReligion, float devotion)
    {
        _mainReligion = mainReligion;
        _devotion = devotion;
    }


    // (get) Token: 0x060000FE RID: 254 RVA: 0x00002CE4 File Offset: 0x00000EE4
    public RFReligions Religion => _mainReligion;


    public void ConvertReligion(RFReligions newReligion)
    {
        if (_mainReligion == newReligion) return;
        _devotion = 0f;
        _mainReligion = newReligion;
    }


    public float GetDevotionToCurrentReligion()
    {
        return _devotion;
    }


    public float AddDevotion(float value, Hero? hero)
    {
        return AddDevotionInternal(value, hero);
    }


    public float AddDevotion(float value, RFReligions type, Hero? hero)
    {
        if (type == _mainReligion)
        {
            return AddDevotionInternal(value, hero);
        }

        return 0f;
    }

    private float AddDevotionInternal(float value, Hero? hero)
    {
        _devotion += value;
        _devotion = Math.Max(_devotion, 0f);
        _devotion = Math.Min(_devotion, 100f);
        hero?.AddSkillXp(RFSkills.Faith, value * 2);
        return _devotion;
    }

    [SaveableField(1)] private RFReligions _mainReligion;


    [SaveableField(2)] private float _devotion;
}