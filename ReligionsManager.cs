using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Actions;
using Bannerlord.Module1.Religions;
using TaleWorlds.ObjectSystem;


namespace Bannerlord.Module1.Religions
{
    public class ReligionsManager
    {
        public void InitializeReligions()
        {
            // Example religion initialization
            var empireCulture = MBObjectManager.Instance.GetObject<CultureObject>("empire");
            var sturgiaCulture = MBObjectManager.Instance.GetObject<CultureObject>("sturgia");
            var battaniaCulture = MBObjectManager.Instance.GetObject<CultureObject>("battania");
            var vlandiaCulture = MBObjectManager.Instance.GetObject<CultureObject>("vlandia");
            var khuzaitCulture = MBObjectManager.Instance.GetObject<CultureObject>("khuzait");
            var aseraiCulture = MBObjectManager.Instance.GetObject<CultureObject>("aserai");

            var anoriteReligion = new ReligionObject("Anorite Religion", "Faith of Anorite", empireCulture);
            var celestialChorus = new ReligionObject("Celestial Chorus", "Faith of Celestial Chorus", empireCulture);
            var sturgianFaith = new ReligionObject("SturgianFaith", "Faith of Sturgia", sturgiaCulture);
            var battanianFaith = new ReligionObject("BattanianFaith", "Faith of Battania", battaniaCulture);
            var vlandianFaith = new ReligionObject("VlandianFaith", "Faith of Vlandia", vlandiaCulture);
            var khuzaitFaith = new ReligionObject("KhuzaitFaith", "Faith of Khuzait", khuzaitCulture);
            var aseraiFaith = new ReligionObject("AseraiFaith", "Faith of Aserai", aseraiCulture);

            MBObjectManager.Instance.RegisterObject(anoriteReligion);
            MBObjectManager.Instance.RegisterObject(celestialChorus);
            MBObjectManager.Instance.RegisterObject(sturgianFaith);
            MBObjectManager.Instance.RegisterObject(battanianFaith);
            MBObjectManager.Instance.RegisterObject(vlandianFaith);
            MBObjectManager.Instance.RegisterObject(khuzaitFaith);
            MBObjectManager.Instance.RegisterObject(aseraiFaith);

            // Initialize heroes with their religions
            InitializeFaithfulHeroes();
        }

        private void InitializeFaithfulHeroes()
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                DetermineReligionForHero(hero);
            }
        }

        public void DetermineReligionForHero(Hero hero)
        {
            ReligionObject religion = null;

            // Assign religion to clan leader first if they don't have one
            if (hero.Clan != null && hero.Clan.Leader == hero && !hero.HasReligion())
            {
                religion = AssignReligionBasedOnCulture(hero);
                if (religion != null)
                {
                    hero.SetReligion(religion);
                }
            }

            // Follow father's religion
            if (hero.Father != null && hero.Father.HasReligion())
            {
                religion = hero.Father.GetReligion();
            }
            // Follow clan leader's religion
            else if (hero.Clan != null && hero.Clan.Leader != null && hero.Clan.Leader.HasReligion())
            {
                religion = hero.Clan.Leader.GetReligion();
            }
            // Follow culture's religion
            else if (hero.Culture != null && ReligionObject.All.Any(x => x.Culture == hero.Culture))
            {
                religion = ReligionObject.All.Where(x => x.Culture == hero.Culture).OrderBy(r => MBRandom.RandomFloat).FirstOrDefault();
            }

            if (religion != null)
            {
                hero.SetReligion(religion);
            }
        }

        private ReligionObject AssignReligionBasedOnCulture(Hero hero)
        {
            // Randomly assign one of the religions if there are multiple for the culture
            return ReligionObject.All.Where(x => x.Culture == hero.Culture).OrderBy(r => MBRandom.RandomFloat).FirstOrDefault();
        }

        public void AdjustRelationsBasedOnReligion(Hero hero)
        {
            foreach (var otherHero in Hero.AllAliveHeroes)
            {
                if (hero != otherHero)
                {
                    var heroReligion = hero.GetReligion();
                    var otherHeroReligion = otherHero.GetReligion();

                    if (heroReligion != null && otherHeroReligion != null && heroReligion != otherHeroReligion)
                    {
                        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, otherHero, -10);
                    }
                }
            }
        }
    }
}