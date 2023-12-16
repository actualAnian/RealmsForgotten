﻿using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using RealmsForgotten.Managers;
using static RealmsForgotten.Globals;
using TaleWorlds.ObjectSystem;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem.Extensions;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RealmsForgotten
{
    public class RFCharacterCreationContent : SandboxCharacterCreationContent
    {
        public CulturedStartManager Manager
        {
            get
            {
                return CulturedStartManager.Current;
            }
        }

        public override TextObject ReviewPageDescription
        {
            get
            {
                return new("{=W6pKpEoT}You prepare to set off for a grand adventure! Here is your character. Continue if you are ready, or go back to make changes.", null);
            }
        }

        public override IEnumerable<Type> CharacterCreationStages
        {
            get
            {
                yield return typeof(CharacterCreationCultureStage);
                yield return typeof(CharacterCreationFaceGeneratorStage);
                yield return typeof(CharacterCreationGenericStage);
                yield return typeof(CharacterCreationBannerEditorStage);
                yield return typeof(CharacterCreationClanNamingStage);
                yield return typeof(CharacterCreationReviewStage);
                yield return typeof(CharacterCreationOptionsStage);
                yield break;
            }
        }

        protected override void OnCultureSelected()
        {
            base.SelectedTitleType = 1;
            base.SelectedParentType = 0;
            TextObject textObject = FactionHelper.GenerateClanNameforPlayer();
            Clan.PlayerClan.ChangeClanName(textObject, textObject);
            CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
            string stringId = playerCharacter.Culture.StringId;
            string cultureId = stringId;
            string bodyPropString;
            switch (cultureId)
            {
                case "aserai":
                    bodyPropString = AthasBodyPropString;
                    break;
                case "battania":
                    bodyPropString = ElveanBodyPropString;
                    break;
                case "empire":
                    bodyPropString = HumanBodyPropString;
                    break;
                case "khuzait":
                    bodyPropString = AllKhuurBodyPropString;
                    break;
                case "sturgia":
                    bodyPropString = UndeadBodyPropString;
                    break;
                case "vlandia":
                    bodyPropString = NasoriaBodyPropString;
                    break;
                case "giant":
                    bodyPropString = XilantlacayBodyPropString;
                    break;
                case "aqarun":
                    bodyPropString = AqarunBodyPropString;
                    break;
                default:
                    Debug.FailedAssert("Selected culture is invalid!", "RFCharacterCreationContent.cs", "OnCultureSelected", 80);
                    bodyPropString = HumanBodyPropString;
                    break;
            }
            BodyProperties.FromString(bodyPropString, out BodyProperties properties);
            playerCharacter.UpdatePlayerCharacterBodyProperties(properties, playerCharacter.Race, playerCharacter.IsFemale);
        }

        public override int GetSelectedParentType()
        {
            return base.SelectedParentType;
        }

        public override void OnCharacterCreationFinalized()
        {
            if (this._startingPoints.TryGetValue(CharacterObject.PlayerCharacter.Culture.StringId, out Vec2 position2D))
            {
                MobileParty.MainParty.Position2D = position2D;
            }
            else
            {
                MobileParty.MainParty.Position2D = Campaign.Current.DefaultStartingPosition;
                Debug.FailedAssert("Selected culture is not in the dictionary!", "RFCharacterCreationContent.cs", "OnCharacterCreationFinalized", 102);
            }
            MapState mapState = GameStateManager.Current.ActiveState as MapState;
            mapState?.Handler.ResetCamera(true, true);
            mapState?.Handler.TeleportCameraToMainParty();
            base.SetHeroAge((float)this._startingAge);
        }

        protected override void OnInitialized(CharacterCreation characterCreation)
        {
            this.AddParentsMenu(characterCreation);
            this.AddChildhoodMenu(characterCreation);
            this.AddEducationMenu(characterCreation);
            this.AddYouthMenu(characterCreation);
            this.AddAdulthoodMenu(characterCreation);
            base.AddAgeSelectionMenu(characterCreation);
            this.AddCultureStartMenu(characterCreation);
            this.AddCultureLocationMenu(characterCreation);

            RFCulturalFeats culturalFeats = new();


            foreach (CultureObject cultureObject in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
            {
                string cultureId = cultureObject.StringId;

                FieldInfo _description = AccessTools.Field(typeof(PropertyObject), "_description");
                
                _description.SetValue(DefaultCulturalFeats.BattanianMilitiaFeat, new TextObject("Towns owned by Xilantlacay rulers have +1 militia production."));
                _description.SetValue(DefaultCulturalFeats.KhuzaitAnimalProductionFeat, new TextObject("25% production bonus to horse, mule, cow and sheep in villages owned by All Khuur rulers."));

                switch (cultureId)
                {
                    case "vlandia":
                        cultureObject.CultureFeats.Add(culturalFeats.nasoriaCheaperMercenaries);
                        break;
                    case "sturgia":
                        cultureObject.CultureFeats.Add(culturalFeats.dreadrealmSoldiersRevive);
                        break;
                    case "battania":
                        cultureObject.CultureFeats.Add(culturalFeats.elveanForestMorale);
                        break;
                    case "aserai":
                        cultureObject.CultureFeats.Add(culturalFeats.athasFasterConstructions);
                        break;
                    case "empire":
                        cultureObject.CultureFeats.Add(culturalFeats.empireAdittionalTier);
                        break;
                    case "khuzait":
                        cultureObject.CultureFeats.Add(culturalFeats.allkhuurPrisonersJoinMilitia);
                        break;
                    case "giant":
                        cultureObject.CultureFeats.Add(culturalFeats.xilantlacayRaidersBonus);
                        if (cultureObject.CultureFeats.Contains(DefaultCulturalFeats.AseraiDesertFeat))
                            cultureObject.CultureFeats.Remove(DefaultCulturalFeats.AseraiDesertFeat);
                        if (cultureObject.CultureFeats.Contains(DefaultCulturalFeats.AseraiTraderFeat))
                            cultureObject.CultureFeats.Remove(DefaultCulturalFeats.AseraiTraderFeat);
                        break;
                    case "aqarun":

                        if(cultureObject.CultureFeats.Contains(DefaultCulturalFeats.AseraiDesertFeat))
                            cultureObject.CultureFeats.Remove(DefaultCulturalFeats.AseraiDesertFeat);
                        if (cultureObject.CultureFeats.Contains(DefaultCulturalFeats.AseraiTraderFeat))
                            cultureObject.CultureFeats.Remove(DefaultCulturalFeats.AseraiTraderFeat);

                        cultureObject.CultureFeats.Add(culturalFeats.aqarunRecruitBandits);
                        break;
                }

            }

        }

        protected new void AddParentsMenu(CharacterCreation characterCreation)
        {
            // FAMILY MENU
            CharacterCreationMenu parentsMenu = new(new("{=b4lDDcli}Family", null), new("{=XgFU1pCx}You were born into a family of...", null), new CharacterCreationOnInit(base.ParentsOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);

            // FAMILY MENU -> HUMAN (EMPIRE)
            CharacterCreationCategory humanParentsCategory = parentsMenu.AddMenuCategory(base.EmpireParentsOnCondition);
            humanParentsCategory.AddCategoryOption(new("Direct Descendants of the first people."), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireLandlordsRetainerOnConsequence, base.EmpireLandlordsRetainerOnApply, new("{=ivKl4mV2}Descending from the ruler´s bloodline of the First People - the ancestors that made the pilgrimage to Aeurth - your father was a leader among his village and the cousin of the King of his Realm. He rode with the lord´s cavalry, fighting as an armored lancer."), null, 0, 0, 0, 0, 0);
            humanParentsCategory.AddCategoryOption(new("{=651FhzdR}Urban merchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireMerchantOnConsequence, base.EmpireMerchantOnApply, new("{=FQntPChs}Your family were merchants in one of the main cities of the Kingdoms of Man. They sometimes organized caravans to nearby towns, and discussed issues in the town council."), null, 0, 0, 0, 0, 0);
            humanParentsCategory.AddCategoryOption(new("Free Farmers"), new() { DefaultSkills.Athletics, DefaultSkills.Polearm }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireFreeholderOnConsequence, base.EmpireFreeholderOnApply, new("{=09z8Q08f}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the realm rural economy, as well as the backbone of the levy."), null, 0, 0, 0, 0, 0);
            humanParentsCategory.AddCategoryOption(new("{=v48N6h1t}Urban artisans"), new() { DefaultSkills.Crafting, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireArtisanOnConsequence, base.EmpireArtisanOnApply, new("{=ZKynvffv}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."), null, 0, 0, 0, 0, 0);
            humanParentsCategory.AddCategoryOption(new("Forestcaretakers"), new() { DefaultSkills.Scouting, DefaultSkills.Bow }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireWoodsmanOnConsequence, base.EmpireWoodsmanOnApply, new("Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."), null, 0, 0, 0, 0, 0);
            humanParentsCategory.AddCategoryOption(new("{=aEke8dSb}Urban vagabonds"), new() { DefaultSkills.Roguery, DefaultSkills.Throwing }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.EmpireVagabondOnConsequence, base.EmpireVagabondOnApply, new("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the many criminal gangs, and you had an early look at the dark side of life."), null, 0, 0, 0, 0, 0);

            // FAMILY MENU -> NASORIA (VLANDIAN)
            CharacterCreationCategory nasorianParentsCategory = parentsMenu.AddMenuCategory(base.VlandianParentsOnCondition);
            nasorianParentsCategory.AddCategoryOption(new("Retainers of the Qairth"), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaBaronsRetainerOnConsequence, base.VlandiaBaronsRetainerOnApply, new("Your father was a bailiff for a local Qairth. He looked after his Qairth's estates, resolved disputes in the village, and helped train the village levy. He rode with the Qairth's cavalry, fighting as an armored knight."), null, 0, 0, 0, 0, 0);
            nasorianParentsCategory.AddCategoryOption(new("Guildmerchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaMerchantOnConsequence, base.VlandiaMerchantOnApply, new("{=qNZFkxJb}Your family were merchants in one of the main cities of the kingdom. They organized caravans to nearby towns and were active in the local merchant's guild."), null, 0, 0, 0, 0, 0);
            nasorianParentsCategory.AddCategoryOption(new("Aldenari"), new MBList<SkillObject> { DefaultSkills.Polearm, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaYeomanOnConsequence, base.VlandiaYeomanOnApply, new("{=BLZ4mdhb}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."), null, 0, 0, 0, 0, 0);
            nasorianParentsCategory.AddCategoryOption(new("{=p2KIhGbE}Urban blacksmith"), new MBList<SkillObject> { DefaultSkills.Crafting, DefaultSkills.TwoHanded }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaBlacksmithOnConsequence, base.VlandiaBlacksmithOnApply, new("{=btsMpRcA}Your family owned a smithy in a city. Your father played an active if minor role in the town council, and also served in the militia."), null, 0, 0, 0, 0, 0);
            nasorianParentsCategory.AddCategoryOption(new("{=YcnK0Thk}Hunters"), new MBList<SkillObject> { DefaultSkills.Scouting, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaHunterOnConsequence, base.VlandiaHunterOnApply, new("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."), null, 0, 0, 0, 0, 0);
            nasorianParentsCategory.AddCategoryOption(new("{=ipQP6aVi}Mercenaries"), new MBList<SkillObject> { DefaultSkills.Roguery, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.VlandiaMercenaryOnConsequence, base.VlandiaMercenaryOnApply, new("Your father joined one of the East many mercenary companies, composed of men who got such a taste for war in their clan's service that they never took well to peace. Their crossbowmen were much valued across the world. Your mother was a camp follower, taking you along in the wake of bloody campaigns."), null, 0, 0, 0, 0, 0);

            // FAMILY MENU -> UNDEAD (STURGIAN)
            CharacterCreationCategory undeadParentsCategory = parentsMenu.AddMenuCategory(base.SturgianParentsOnCondition);
            undeadParentsCategory.AddCategoryOption(new("Servants of the Undead"), new() { DefaultSkills.Riding, DefaultSkills.TwoHanded }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaBoyarsCompanionOnConsequence, base.SturgiaBoyarsCompanionOnApply, new("Your family served the Undead."), null, 0, 0, 0, 0, 0);
            undeadParentsCategory.AddCategoryOption(new("{=HqzVBfpl}Urban traders"), new MBList<SkillObject> { DefaultSkills.Trade, DefaultSkills.Tactics }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaTraderOnConsequence, base.SturgiaTraderOnApply, new("Your family were merchants who lived in one of the land's great river ports, organizing the shipment of goods to faraway lands."), null, 0, 0, 0, 0, 0);
            undeadParentsCategory.AddCategoryOption(new("Farmers"), new() { DefaultSkills.Athletics, DefaultSkills.Polearm }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaFreemanOnConsequence, base.SturgiaFreemanOnApply, new("{=Mcd3ZyKq}Your family had just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."), null, 0, 0, 0, 0, 0);
            undeadParentsCategory.AddCategoryOption(new("{=v48N6h1t}Urban artisans"), new() { DefaultSkills.Crafting, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaArtisanOnConsequence, base.SturgiaArtisanOnApply, new("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."), null, 0, 0, 0, 0, 0);
            undeadParentsCategory.AddCategoryOption(new("Forestfolk"), new() { DefaultSkills.Scouting, DefaultSkills.Bow }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaHunterOnConsequence, base.SturgiaHunterOnApply, new("Your family had no taste for authority of others. They made their living deep in the woods, slashing and burning fields which they tended for a year or two before moving on. They hunted and trapped fox, hare, ermine, and other fur-bearing animals."), null, 0, 0, 0, 0, 0);
            undeadParentsCategory.AddCategoryOption(new("{=TPoK3GSj}Vagabonds"), new() { DefaultSkills.Roguery, DefaultSkills.Throwing }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.SturgiaVagabondOnConsequence, base.SturgiaVagabondOnApply, new("{=2SDWhGmQ}Your family numbered among the poor migrants living in the slums that grow up outside the walls of the river cities, making whatever money they could from a variety of odd jobs. Sometimes they did services for one of the region's many criminal gangs."), null, 0, 0, 0, 0, 0);

            // FAMILY MENU -> ATHAS (ASERAI)
            CharacterCreationCategory athassianParentsCategory = parentsMenu.AddMenuCategory(base.AseraiParentsOnCondition);
            athassianParentsCategory.AddCategoryOption(new("The inner circle of Atha's rulers"), new() { DefaultSkills.Riding, DefaultSkills.Throwing }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiTribesmanOnConsequence, base.AseraiTribesmanOnApply, new("You were a family of some importance in the inner circle of Athas."), null, 0, 0, 0, 0, 0);
            athassianParentsCategory.AddCategoryOption(new("{=ngFVgwDD}Warrior-slaves"), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiWariorSlaveOnConsequence, base.AseraiWariorSlaveOnApply, new("{=GsPC2MgU}Your father was part of one of the slave-bodyguards maintained by the rulers. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother."), null, 0, 0, 0, 0, 0);
            athassianParentsCategory.AddCategoryOption(new("{=651FhzdR}Urban merchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiMerchantOnConsequence, base.AseraiMerchantOnApply, new("{=1zXrlaav}Your family were respected traders in an oasis town. They ran caravans across the desert, and were experts in the finer points of negotiating passage through the desert tribes' territories."), null, 0, 0, 0, 0, 0);
            athassianParentsCategory.AddCategoryOption(new("Slave-farmers"), new() { DefaultSkills.Athletics, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiOasisFarmerOnConsequence, base.AseraiOasisFarmerOnApply, new("{=5P0KqBAw}Your family tilled the soil in one of the oases of the Kalikhr tribe and tended the palm orchards that produced the desert's famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under the emir's banner."), null, 0, 0, 0, 0, 0);
            athassianParentsCategory.AddCategoryOption(new("Free men"), new() { DefaultSkills.Scouting, DefaultSkills.Bow }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiBedouinOnConsequence, base.AseraiBedouinOnApply, new("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Kalikhr."), null, 0, 0, 0, 0, 0);
            athassianParentsCategory.AddCategoryOption(new("Urban Orfans"), new() { DefaultSkills.Roguery, DefaultSkills.Polearm }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.AseraiBackAlleyThugOnConsequence, base.AseraiBackAlleyThugOnApply, new("{=6bUSbsKC}Your father was not your biological father, but took you under his protection to one day strenghten his army of thugs. He worked for a fitiwi , one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff."), null, 0, 0, 0, 0, 0);

            // FAMILY MENU -> ELVEAN (BATTANIAN)
            CharacterCreationCategory elveanParentsCategory = parentsMenu.AddMenuCategory(new(base.BattanianParentsOnCondition));
            elveanParentsCategory.AddCategoryOption(new("Elvean Highborn"), new() { DefaultSkills.TwoHanded, DefaultSkills.Bow }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaChieftainsHearthguardOnConsequence, base.BattaniaChieftainsHearthguardOnApply, new("Your family were the trusted kinfolk of an Elvean lord, and sat at his table in his great hall. Your father assisted his chief in running the affairs and trained with the traditional weapons of the warrior elite, the two-handed sword or falx and the bow."), null, 0, 0, 0, 0, 0);
            elveanParentsCategory.AddCategoryOption(new("Druids"), new() { DefaultSkills.Medicine, DefaultSkills.Charm }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaHealerOnConsequence, base.BattaniaHealerOnApply, new("Your parents were healers who gathered herbs and treated the sick. As a living reservoir of elvean tradition, they were also asked to adjudicate many disputes between the clans."), null, 0, 0, 0, 0, 0);
            elveanParentsCategory.AddCategoryOption(new("Elvean Folk"), new() { DefaultSkills.Athletics, DefaultSkills.Throwing }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaTribesmanOnConsequence, base.BattaniaTribesmanOnApply, new("Your family were middle-ranking members of a society, who tilled their own land. Your father fought with the kern, the main body of his people's warriors, joining in the screaming charges for which the Elveans were famous."), null, 0, 0, 0, 0, 0);
            elveanParentsCategory.AddCategoryOption(new("{=BCU6RezA}Smiths"), new() { DefaultSkills.Crafting, DefaultSkills.TwoHanded }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaSmithOnConsequence, base.BattaniaSmithOnApply, new("Your family were smiths, a revered profession. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Elvean aristocracy."), null, 0, 0, 0, 0, 0);
            elveanParentsCategory.AddCategoryOption(new("{=7eWmU2mF}Foresters"), new() { DefaultSkills.Scouting, DefaultSkills.Tactics }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaWoodsmanOnConsequence, base.BattaniaWoodsmanOnApply, new("{=7jBroUUQ}Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation."), null, 0, 0, 0, 0, 0);
            elveanParentsCategory.AddCategoryOption(new("{=SpJqhEEh}Bards"), new() { DefaultSkills.Roguery, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.BattaniaBardOnConsequence, base.BattaniaBardOnApply, new("Your Father was a Bard, a sacred duty for the Elvean Folk. Responsible to keep the Song alive, he went from halls to festivities, from rituals to war camps, to teach and inspire the people into the sacred ways. Your learned from him the cleverness of the tongue and the hability to tap into your people´s soul."), null, 0, 0, 0, 0, 0);

            // FAMILY MENU -> ALL KHUUR (KHUZAIT)
            CharacterCreationCategory allKhuurParentsCategory = parentsMenu.AddMenuCategory(new(base.KhuzaitParentsOnCondition));
            allKhuurParentsCategory.AddCategoryOption(new("Al-Kahuur Kinsfolk"), new() { DefaultSkills.Riding, DefaultSkills.Polearm }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitNoyansKinsmanOnConsequence, base.KhuzaitNoyansKinsmanOnApply, new("Your family were the trusted kinsfolk of a ruler, and shared his meals in the chieftain's yurt. Your father assisted his chief in running the affairs of the clan and fought in the core of armored lancers in the center of a battle line."), null, 0, 0, 0, 0, 0);
            allKhuurParentsCategory.AddCategoryOption(new("{=TkgLEDRM}Merchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitMerchantOnConsequence, base.KhuzaitMerchantOnApply, new("Your family came from one of the merchant clans that dominated the cities in the northwestern part of the world."), null, 0, 0, 0, 0, 0);
            allKhuurParentsCategory.AddCategoryOption(new("{=tGEStbxb}Tribespeople"), new() { DefaultSkills.Bow, DefaultSkills.Riding }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitTribesmanOnConsequence, base.KhuzaitTribesmanOnApply, new("Your family were middle-ranking members of one of the clans. They had some herds of thier own, but were not rich. "), null, 0, 0, 0, 0, 0);
            allKhuurParentsCategory.AddCategoryOption(new("{=gQ2tAvCz}Farmers"), new() { DefaultSkills.Polearm, DefaultSkills.Throwing }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitFarmerOnConsequence, base.KhuzaitFarmerOnApply, new("Your family tilled one of the small patches of arable land in the steppes for generations."), null, 0, 0, 0, 0, 0);
            allKhuurParentsCategory.AddCategoryOption(new("Spirit-Chatchers"), new() { DefaultSkills.Medicine, DefaultSkills.Charm }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitShamanOnConsequence, base.KhuzaitShamanOnApply, new("Your family were guardians of the sacred traditions, channelling the spirits of the wilderness and of the ancestors. They tended the sick and dispensed wisdom, resolving disputes and providing practical advice."), null, 0, 0, 0, 0, 0);
            allKhuurParentsCategory.AddCategoryOption(new("{=Xqba1Obq}Nomads"), new() { DefaultSkills.Scouting, DefaultSkills.Riding }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, base.KhuzaitNomadOnConsequence, base.KhuzaitNomadOnApply, new("{=9aoQYpZs}Your family's clan never pledged its loyalty to the khan and never settled down, preferring to live out in the deep steppe away from his authority. They remain some of the finest trackers and scouts in the grasslands, as the ability to spot an enemy coming and move quickly is often all that protects their herds from their neighbors' predations."), null, 0, 0, 0, 0, 0);


            // Giants - Xilatlacay
            CharacterCreationCategory giantsParentsCategory = parentsMenu.AddMenuCategory(new(this.GiantParentsOnCondition));
            giantsParentsCategory.AddCategoryOption(new("Xilatlacay Highborn"), new() { DefaultSkills.TwoHanded, DefaultSkills.Crossbow }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, XilatlacayHighbornOnConsequence, base.BattaniaChieftainsHearthguardOnApply, new("Your familly belonged to the noble bloodline of the Xilantlacay tribal leaders, descendent of the God Xilan. Your father assisted the Xilan king in running the affairs and trained with the sacred weapons of the warrior elite, the warclub and the crossbow."), null, 0, 0, 0, 0, 0);
            giantsParentsCategory.AddCategoryOption(new("Shamans"), new() { DefaultSkills.Medicine, DefaultSkills.Charm }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, XilatlacayShamanOnConsequence, base.BattaniaHealerOnApply, new("Your parents were shamans, who spoke with the spirits of the forests and mastered the art of healing herbs. As a reservoir of Xilan knowledge, they adjudicated many disputes between the tribes."), null, 0, 0, 0, 0, 0);
            giantsParentsCategory.AddCategoryOption(new("Xilan Folk"), new() { DefaultSkills.Athletics, DefaultSkills.Throwing }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, XilatlacayFolkOnConsequence, base.BattaniaTribesmanOnApply, new("Your family were of Xilantlacay folk, who tilled their own land. Your father fought with the Xtlacay, the main body of his people´s warriors, joining in the screaming charges for which the Xtlacay were famous."), null, 0, 0, 0, 0, 0);
            giantsParentsCategory.AddCategoryOption(new("{=BCU6RezA}Smiths"), new() { DefaultSkills.Crafting, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, XilatlacaySmithOnConsequence, base.BattaniaSmithOnApply, new("Your family were smiths, a revered profession among the Xilantlacay. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Xilantlacay aristocracy."), null, 0, 0, 0, 0, 0);
            giantsParentsCategory.AddCategoryOption(new("{=7eWmU2mF}Foresters"), new() { DefaultSkills.Scouting, DefaultSkills.Tactics }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, XilatlacayForesterOnConsequence, base.BattaniaWoodsmanOnApply, new("Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation."), null, 0, 0, 0, 0, 0);
            giantsParentsCategory.AddCategoryOption(new("Sages"), new() { DefaultSkills.Roguery, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, XilatlacaySageOnConsequence, base.BattaniaBardOnApply, new("Your Father was a Sage, a sacred duty for the Xilan Folk. Responsible to keep the history of their people alive, he went from halls to festivities, from rituals to war camps, to teach and inspire the people into the sacred ways. Your learned from him the cleverness of the tongue and the hability to tap into your people soul.\r\n"), null, 0, 0, 0, 0, 0);
            
            // Aqarun
            CharacterCreationCategory aqarunParentsCategory = parentsMenu.AddMenuCategory(new(this.AqarunParentsOnCondition));
            aqarunParentsCategory.AddCategoryOption(new("Aqarun Champions"), new() { DefaultSkills.Riding, DefaultSkills.Throwing }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, AqarunChampionsOnConsequence, base.AseraiTribesmanOnApply, new("Your parents were chosen between the champions of Aqarun warriors. Your father filled up the warking's private army and your mother a shieldmaiden at the warlord bodyguard. The champions were the only ones that could speak directly to the warkings. "), null, 0, 0, 0, 0, 0);
            aqarunParentsCategory.AddCategoryOption(new("Warriors"), new() { DefaultSkills.OneHanded, DefaultSkills.TwoHanded }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, AqarunWariorSlaveOnConsequence, base.AseraiWariorSlaveOnApply, new("Your father was part of one of the slave-bodyguards maintained by the Aqarun warkings. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother."), null, 0, 0, 0, 0, 0);
            aqarunParentsCategory.AddCategoryOption(new("{=651FhzdR}Urban merchants"), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, AqarunMerchantOnConsequence, base.AseraiMerchantOnApply, new("{=1zXrlaav}Your family were respected traders in an oasis town. They ran caravans across the desert, and were experts in the finer points of negotiating passage through the desert tribes' territories."), null, 0, 0, 0, 0, 0);
            aqarunParentsCategory.AddCategoryOption(new("Free farmers"), new() { DefaultSkills.Athletics, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, AqarunFarmerOnConsequence, base.AseraiOasisFarmerOnApply, new("Your familly tilled the soil in one of the many oases under the aqarun territory and tended the palm orchards that produced the desert´s famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under his ruler´s banner."), null, 0, 0, 0, 0, 0);
            aqarunParentsCategory.AddCategoryOption(new("Free men"), new() { DefaultSkills.Scouting, DefaultSkills.Bow }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, AqarunFreeMenOnConsequence, base.AseraiBedouinOnApply, new("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Kalikhr."), null, 0, 0, 0, 0, 0);
            aqarunParentsCategory.AddCategoryOption(new("Orfans"), new() { DefaultSkills.Roguery, DefaultSkills.Polearm }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, AqarunOrphansOnConsequence, base.AseraiBackAlleyThugOnApply, new("Your father was not your biological father, but adopted you under his protection to one day strenghten his army of thugs. He worked for a fitiwi, one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff."), null, 0, 0, 0, 0, 0);

            characterCreation.AddNewMenu(parentsMenu);
        }

        private void AqarunChampionsOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 1, OccupationTypes.Retainer);
        }

        private void AqarunWariorSlaveOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 2, OccupationTypes.Mercenary);
        }

        private void AqarunMerchantOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 1, OccupationTypes.Merchant);
        }

        private void AqarunFarmerOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 1, OccupationTypes.Farmer);
        }

        private void AqarunFreeMenOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 1, OccupationTypes.Herder);
        }

        private void AqarunOrphansOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 1, OccupationTypes.Artisan);
        }

        private void XilatlacayHighbornOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 1, OccupationTypes.Retainer);
        }
        private void XilatlacayShamanOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 2, OccupationTypes.Healer);
        }
        private void XilatlacayFolkOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 3, OccupationTypes.Farmer);
        }
        private void XilatlacaySmithOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 4, OccupationTypes.Artisan);
        }
        private void XilatlacayForesterOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 5, OccupationTypes.Hunter);
        }
        private void XilatlacaySageOnConsequence(CharacterCreation characterCreation)
        {
            SetParentAndOccupationType(characterCreation, 6, OccupationTypes.Healer);
        }

        protected new void AddChildhoodMenu(CharacterCreation characterCreation)
        {
            // CHILDHOOD MENU
            CharacterCreationMenu childhoodMenu = new(new("{=8Yiwt1z6}Early Childhood", null), new("{=character_creation_content_16}As a child you were noted for...", null), new CharacterCreationOnInit(base.ChildhoodOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);

            CharacterCreationCategory childhoodCategory = childhoodMenu.AddMenuCategory(null);
            childhoodCategory.AddCategoryOption(new("{=kmM68Qx4}your leadership skills."), new() { DefaultSkills.Leadership, DefaultSkills.Tactics }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodYourLeadershipSkillsOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodLeadingOnApply, new("{=FfNwXtii}If the wolf pup gang of your early childhood had an alpha, it was definitely you. All the other kids followed your lead as you decided what to play and where to play, and led them in games and mischief."), null, 0, 0, 0, 0, 0);
            childhoodCategory.AddCategoryOption(new("{=5HXS8HEY}your brawn."), new() { DefaultSkills.TwoHanded, DefaultSkills.Throwing }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodYourBrawnOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodAthleticsOnApply, new("{=YKzuGc54}You were big, and other children looked to have you around in any scrap with children from a neighboring village. You pushed a plough and throw an axe like an adult."), null, 0, 0, 0, 0, 0);
            childhoodCategory.AddCategoryOption(new("{=QrYjPUEf}your attention to detail."), new() { DefaultSkills.Athletics, DefaultSkills.Bow }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodAttentionToDetailOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodMemoryOnApply, new("{=JUSHAPnu}You were quick on your feet and attentive to what was going on around you. Usually you could run away from trouble, though you could give a good account of yourself in a fight with other children if cornered."), null, 0, 0, 0, 0, 0);
            childhoodCategory.AddCategoryOption(new("{=Y3UcaX74}your aptitude for numbers."), new() { DefaultSkills.Engineering, DefaultSkills.Trade }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodAptitudeForNumbersOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodMathOnApply, new("{=DFidSjIf}Most children around you had only the most rudimentary education, but you lingered after class to study letters and mathematics. You were fascinated by the marketplace - weights and measures, tallies and accounts, the chatter about profits and losses."), null, 0, 0, 0, 0, 0);
            childhoodCategory.AddCategoryOption(new("{=GEYzLuwb}your way with people."), new() { DefaultSkills.Charm, DefaultSkills.Leadership }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodWayWithPeopleOnConsequence, SandboxCharacterCreationContent.ChildhoodGoodMannersOnApply, new("{=w2TEQq26}You were always attentive to other people, good at guessing their motivations. You studied how individuals were swayed, and tried out what you learned from adults on your friends."), null, 0, 0, 0, 0, 0);
            childhoodCategory.AddCategoryOption(new("{=MEgLE2kj}your skill with horses."), new() { DefaultSkills.Riding, DefaultSkills.Medicine }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, SandboxCharacterCreationContent.ChildhoodSkillsWithHorsesOnConsequence, SandboxCharacterCreationContent.ChildhoodAffinityWithAnimalsOnApply, new("{=ngazFofr}You were always drawn to animals, and spent as much time as possible hanging out in the village stables. You could calm horses, and were sometimes called upon to break in new colts. You learned the basics of veterinary arts, much of which is applicable to humans as well."), null, 0, 0, 0, 0, 0);

            characterCreation.AddNewMenu(childhoodMenu);
        }

        protected new void AddEducationMenu(CharacterCreation characterCreation)
        {
            // EDUCATION MENU
            CharacterCreationMenu characterCreationMenu = new(new("{=rcoueCmk}Adolescence", null), this._educationIntroductoryText, new CharacterCreationOnInit(base.EducationOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);

            // EDUCATION MENU -> RURAL
            CharacterCreationCategory educationCategory = characterCreationMenu.AddMenuCategory(null);
            educationCategory.AddCategoryOption(new("{=RKVNvimC}herded the sheep.", null), new() { DefaultSkills.Athletics, DefaultSkills.Throwing }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceHerderOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceHerderOnApply, new("{=KfaqPpbK}You went with other fleet-footed youths to take the villages' sheep, goats or cattle to graze in pastures near the village. You were in charge of chasing down stray beasts, and always kept a big stone on hand to be hurled at lurking predators if necessary."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("learned the elvean ways of smithing."), new() { DefaultSkills.TwoHanded, DefaultSkills.Crafting }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.BattanianParentsOnCondition, base.RuralAdolescenceSmithyOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceSmithyOnApply, new("{=y6j1bJTH}You were apprenticed to the local smith. You learned how to heat and forge metal, hammering for hours at a time until your muscles ached."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=tI8ZLtoA}repaired projects."), new() { DefaultSkills.Crafting, DefaultSkills.Engineering }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceRepairmanOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceRepairmanOnApply, new("{=6LFj919J}You helped dig wells, rethatch houses, and fix broken plows. You learned about the basics of construction, as well as what it takes to keep a farming community prosperous."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=TRwgSLD2}gathered herbs in the wild."), new() { DefaultSkills.Medicine, DefaultSkills.Scouting }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceGathererOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceGathererOnApply, new("{=9ks4u5cH}You were sent by the village healer up into the hills to look for useful medicinal plants. You learned which herbs healed wounds or brought down a fever, and how to find them."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=T7m7ReTq}hunted small game."), new() { DefaultSkills.Bow, DefaultSkills.Tactics }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceHunterOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceHunterOnApply, new("{=RuvSk3QT}You accompanied a local hunter as he went into the wilderness, helping him set up traps and catch small animals."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=qAbMagWq}sold produce at the market."), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.RuralAdolescenceOnCondition, base.RuralAdolescenceHelperOnConsequence, SandboxCharacterCreationContent.RuralAdolescenceHelperOnApply, new("{=DIgsfYfz}You took your family's goods to the nearest town to sell your produce and buy supplies. It was hard work, but you enjoyed the hubbub of the marketplace."), null, 0, 0, 0, 0, 0);

            // EDUCATION MENU -> URBAN
            educationCategory.AddCategoryOption(new("{=nOfSqRnI}at the town watch's training ground."), new() { DefaultSkills.Crossbow, DefaultSkills.Tactics }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanAdolescenceOnCondition, base.UrbanAdolescenceWatcherOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceWatcherOnApply, new("{=qnqdEJOv}You watched the town's watch practice shooting and perfect their plans to defend the walls in case of a siege."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=8a6dnLd2}with the alley gangs."), new() { DefaultSkills.Roguery, DefaultSkills.OneHanded }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanAdolescenceOnCondition, base.UrbanAdolescenceGangerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceGangerOnApply, new("{=1SUTcF0J}The gang leaders who kept watch over the slums of Athas' cities were always in need of poor youth to run messages and back them up in turf wars, while thrill-seeking merchants' sons and daughters sometimes slummed it in their company as well."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=7Hv984Sf}at docks and building sites."), new() { DefaultSkills.Athletics, DefaultSkills.Crafting }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanAdolescenceOnCondition, base.UrbanAdolescenceDockerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=bhdkegZ4}All towns had their share of projects that were constantly in need of both skilled and unskilled labor. You learned how hoists and scaffolds were constructed, how planks and stones were hewn and fitted, and other skills."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=kbcwb5TH}in the markets and merchant caravans."), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanPoorAdolescenceOnCondition, base.UrbanAdolescenceMarketerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceMarketerOnApply, new("{=lLJh7WAT}You worked in the marketplace, selling trinkets and drinks to busy shoppers."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=kbcwb5TH}in the markets and merchant caravans."), new() { DefaultSkills.Trade, DefaultSkills.Charm }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanRichAdolescenceOnCondition, base.UrbanAdolescenceMarketerOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceMarketerOnApply, new("{=rmMcwSn8}You helped your family handle their business affairs, going down to the marketplace to make purchases and oversee the arrival of caravans."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=mfRbx5KE}reading and studying."), new() { DefaultSkills.Engineering, DefaultSkills.Leadership }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanPoorAdolescenceOnCondition, base.UrbanAdolescenceTutorOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=elQnygal}Your family scraped up the money for a rudimentary schooling and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=etG87fB7}with your tutor."), new() { DefaultSkills.Engineering, DefaultSkills.Leadership }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanRichAdolescenceOnCondition, base.UrbanAdolescenceTutorOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=hXl25avg}Your family arranged for a private tutor and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=FKpLEamz}caring for horses."), new() { DefaultSkills.Riding, DefaultSkills.Steward }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanRichAdolescenceOnCondition, base.UrbanAdolescenceHorserOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=Ghz90npw}Your family owned a few horses at the town stables and you took charge of their care. Many evenings you would take them out beyond the walls and gallup through the fields, racing other youth."), null, 0, 0, 0, 0, 0);
            educationCategory.AddCategoryOption(new("{=vH7GtuuK}working at the stables."), new() { DefaultSkills.Riding, DefaultSkills.Steward }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, base.UrbanPoorAdolescenceOnCondition, base.UrbanAdolescenceHorserOnConsequence, SandboxCharacterCreationContent.UrbanAdolescenceDockerOnApply, new("{=csUq1RCC}You were employed as a hired hand at the town's stables. The overseers recognized that you had a knack for horses, and you were allowed to exercise them and sometimes even break in new steeds."), null, 0, 0, 0, 0, 0);

            characterCreation.AddNewMenu(characterCreationMenu);
        }

        protected new void AddYouthMenu(CharacterCreation characterCreation)
        {
            // CLEANUP FROM HERE
            CharacterCreationMenu characterCreationMenu = new(new("{=ok8lSW6M}Youth", null), this._youthIntroductoryText, new CharacterCreationOnInit(RFYouthOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(new(base.AseraiParentsOnCondition));
            characterCreationCategory.AddCategoryOption(new("{=h2KnarLL}trained with the cavalry.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("partrolled the cities.", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the desert scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=a8arFSra}trained with the infantry.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, null, new(base.YouthInfantryOnApply), new("{=afH90aNs}Young Tribesmen armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=oMbOIPc9}joined the skirmishers.", null), new()
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthSkirmisherOnConsequence), new(base.YouthSkirmisherOnApply), new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=GFUggps8}marched with the free people.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory = characterCreationMenu.AddMenuCategory(new(base.BattanianParentsOnCondition));

            characterCreationCategory.AddCategoryOption(new("trained with the noble guard.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the folks guard", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("rode with the scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("trained with the Akh'Velahr.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, null, new(base.YouthInfantryOnApply), new("{=afH90aNs}Armed with Spear and shield, the Akh´Velahr is the bulk of the Elvean forces, drawned from the smallholding farmers.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the Arakhora.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}Arakhor, an Elvean term that translates loosely as - one who protects the forest -, were the Scounts sent to the borders of the Realm to watch over possible treats. Often you needed  trick your way into foreign armies and cities, cheating, entertaining, whatever disguise was at your disposal.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory = characterCreationMenu.AddMenuCategory(new(base.EmpireParentsOnCondition));

            characterCreationCategory.AddCategoryOption(new("trained with the cavalry.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("served in the Hall of Men.", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("stood guard with the garrisons.", null), new()
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthSkirmisherOnConsequence), new(base.YouthSkirmisherOnApply), new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("rode with the scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the spear bearers.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, null, new(base.YouthInfantryOnApply), new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("trained with the infantry.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory = characterCreationMenu.AddMenuCategory(new(base.VlandianParentsOnCondition));

            characterCreationCategory.AddCategoryOption(new("trained with the Nasoria cavalry.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("patrolled the cities.", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("trained with the infantry.", null), new()
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthSkirmisherOnConsequence), new(base.YouthSkirmisherOnApply), new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("rode with the scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the spearman front.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthInfantryOnConsequence), new(base.YouthInfantryOnApply), new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("marched with the camp followers.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory = characterCreationMenu.AddMenuCategory(new(base.KhuzaitParentsOnCondition));

            characterCreationCategory.AddCategoryOption(new("served the Al-Khuur cavalry.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("patrolled the villages and cities.", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("trained with the infrantry.", null), new()
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthSkirmisherOnConsequence), new(base.YouthSkirmisherOnApply), new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("rode with the scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the spearmen.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, null, new(base.YouthInfantryOnApply), new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("marched with the campfollowers.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory = characterCreationMenu.AddMenuCategory(new(base.SturgianParentsOnCondition));

            characterCreationCategory.AddCategoryOption(new("served in the Dreadlords bodyguard.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("Protecting your dreadlord was your main duty.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("stood guard with the garrisons.", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("trained with the infrantry.", null), new()
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthSkirmisherOnConsequence), new(base.YouthSkirmisherOnApply), new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("rode with the scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the skirmishers.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthInfantryOnConsequence), new(base.YouthInfantryOnApply), new("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("marched with the army retinue.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), null, 0, 0, 0, 0, 0);


            // Giants
            characterCreationCategory = characterCreationMenu.AddMenuCategory(new(GiantParentsOnCondition));

            characterCreationCategory.AddCategoryOption(new("trained with the noble guard.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the folks guard", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("rode with the scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("trained with the Lanzalith.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, null, new(base.YouthInfantryOnApply), new("{=afH90aNs}Armed with Spear and shield, the Akh´Velahr is the bulk of the Elvean forces, drawned from the smallholding farmers.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the Tzalquendlan.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}Arakhor, an Elvean term that translates loosely as - one who protects the forest -, were the Scounts sent to the borders of the Realm to watch over possible treats. Often you needed  trick your way into foreign armies and cities, cheating, entertaining, whatever disguise was at your disposal.", null), null, 0, 0, 0, 0, 0);

            // Aqarun
            
            characterCreationCategory = characterCreationMenu.AddMenuCategory(new(AqarunParentsOnCondition));
            characterCreationCategory.AddCategoryOption(new("{=h2KnarLL}trained with the cavalry.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCavalryOnConsequence), new(base.YouthCavalryOnApply), new("{=7cHsIMLP}You could never have bought the equipment on your own but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("partrolled the cities.", null), new()
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthGarrisonOnConsequence), new(base.YouthGarrisonOnApply), new("{63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("joined the desert scouts.", null), new()
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthOtherOutridersOnConsequence), new(base.YouthOtherOutridersOnApply), new("You couted ahead of the army.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=a8arFSra}trained with the infantry.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, null, new(base.YouthInfantryOnApply), new("{=afH90aNs}Young Tribesmen armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Athas.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=oMbOIPc9}joined the skirmishers.", null), new()
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthSkirmisherOnConsequence), new(base.YouthSkirmisherOnApply), new("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), null, 0, 0, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=GFUggps8}marched with the free people.", null), new()
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.YouthCamperOnConsequence), new(base.YouthCamperOnApply), new("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), null, 0, 0, 0, 0, 0);


            characterCreation.AddNewMenu(characterCreationMenu);
        }

        private void RFYouthOnInit(CharacterCreation characterCreation)
        {
            characterCreation.IsPlayerAlone = true;
            characterCreation.HasSecondaryCharacter = false;
            characterCreation.ClearFaceGenPrefab();
            TextObject textObject = new TextObject("{=F7OO5SAa}As a youngster growing up in Aeurth, war was never too far away. You...", null);
            TextObject textObject2 = new TextObject("{=5kbeAC7k}In wartorn Aeurth, especially in frontier or tribal areas, some women as well as men learn to fight from an early age. You...", null);
            this._youthIntroductoryText.SetTextVariable("YOUTH_INTRO", CharacterObject.PlayerCharacter.IsFemale ? textObject2 : textObject);
            characterCreation.ChangeFaceGenChars(SandboxCharacterCreationContent.ChangePlayerFaceWithAge((float)this.YouthAge, "act_childhood_schooled"));
            characterCreation.ChangeCharsAnimation(new List<string>
            {
                "act_childhood_schooled"
            });
            if (base.SelectedTitleType < 1 || base.SelectedTitleType > 10)
            {
                base.SelectedTitleType = 1;
            }
            this.RefreshPlayerAppearance(characterCreation);

        }

        protected new void AddAdulthoodMenu(CharacterCreation characterCreation)
        {
            MBTextManager.SetTextVariable("EXP_VALUE", this.SkillLevelToAdd);
            CharacterCreationMenu characterCreationMenu = new(new("{=MafIe9yI}Young Adulthood", null), new("{=4WYY0X59}Before you set out for a life of adventure, your biggest achievement was...", null), new CharacterCreationOnInit(base.AccomplishmentOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(null);

            characterCreationCategory.AddCategoryOption(new("{=8bwpVpgy}you defeated an enemy in battle.", null), new()
            {
                DefaultSkills.OneHanded,
                DefaultSkills.TwoHanded
            }, DefaultCharacterAttributes.Vigor, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.AccomplishmentDefeatedEnemyOnConsequence), new(base.AccomplishmentDefeatedEnemyOnApply), new("{=1IEroJKs}Not everyone who musters for the levy marches to war, and not everyone who goes on campaign sees action. You did both, and you also took down an enemy warrior in direct one-to-one combat, in the full view of your comrades.", null), new()
            {
                DefaultTraits.Valor
            }, 1, 20, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=mP3uFbcq}you led a successful manhunt.", null), new()
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentPosseOnConditions), new(base.AccomplishmentExpeditionOnConsequence), new(base.AccomplishmentExpeditionOnApply), new("{=4f5xwzX0}When your community needed to organize a posse to pursue horse thieves, you were the obvious choice. You hunted down the raiders, surrounded them and forced their surrender, and took back your stolen property.", null), new()
            {
                DefaultTraits.Calculating
            }, 1, 10, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=wfbtS71d}you led a caravan.", null), new()
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentMerchantOnCondition), new(base.AccomplishmentMerchantOnConsequence), new(base.AccomplishmentExpeditionOnApply), new("{=joRHKCkm}Your family needed someone trustworthy to take a caravan to a neighboring town. You organized supplies, ensured a constant watch to keep away bandits, and brought it safely to its destination.", null), new()
            {
                DefaultTraits.Calculating
            }, 1, 10, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=x1HTX5hq}you saved your village from a flood.", null), new()
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentSavedVillageOnCondition), new(base.AccomplishmentSavedVillageOnConsequence), new(base.AccomplishmentExpeditionOnApply), new("{=bWlmGDf3}When a sudden storm caused the local stream to rise suddenly, your neighbors needed quick-thinking leadership. You provided it, directing them to build levees to save their homes.", null), new()
            {
                DefaultTraits.Calculating
            }, 1, 10, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=s8PNllPN}you saved your city quarter from a fire.", null), new()
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            }, DefaultCharacterAttributes.Cunning, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentSavedStreetOnCondition), new(base.AccomplishmentSavedStreetOnConsequence), new(base.AccomplishmentExpeditionOnApply), new("{=ZAGR6PYc}When a sudden blaze broke out in a back alley, your neighbors needed quick-thinking leadership and you provided it. You organized a bucket line to the nearest well, putting the fire out before any homes were lost.", null), new()
            {
                DefaultTraits.Calculating
            }, 1, 10, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=xORjDTal}you invested some money in a workshop.", null), new()
            {
                DefaultSkills.Trade,
                DefaultSkills.Crafting
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentUrbanOnCondition), new(base.AccomplishmentWorkshopOnConsequence), new(base.AccomplishmentWorkshopOnApply), new("{=PyVqDLBu}Your parents didn't give you much money, but they did leave just enough for you to secure a loan against a larger amount to build a small workshop. You paid back what you borrowed, and sold your enterprise for a profit.", null), new()
            {
                DefaultTraits.Calculating
            }, 1, 10, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=xKXcqRJI}you invested some money in land.", null), new()
            {
                DefaultSkills.Trade,
                DefaultSkills.Crafting
            }, DefaultCharacterAttributes.Intelligence, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentRuralOnCondition), new(base.AccomplishmentWorkshopOnConsequence), new(base.AccomplishmentWorkshopOnApply), new("{=cbF9jdQo}Your parents didn't give you much money, but they did leave just enough for you to purchase a plot of unused land at the edge of the village. You cleared away rocks and dug an irrigation ditch, raised a few seasons of crops, than sold it for a considerable profit.", null), new()
            {
                DefaultTraits.Calculating
            }, 1, 10, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=TbNRtUjb}you hunted a dangerous animal.", null), new()
            {
                DefaultSkills.Polearm,
                DefaultSkills.Crossbow
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentRuralOnCondition), new(base.AccomplishmentSiegeHunterOnConsequence), new(base.AccomplishmentSiegeHunterOnApply), new("{=I3PcdaaL}Wolves, bears are a constant menace to the flocks of northern Athas, while hyenas and leopards trouble the south. You went with a group of your fellow villagers and fired the missile that brought down the beast.", null), null, 0, 5, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=WbHfGCbd}you survived a siege.", null), new()
            {
                DefaultSkills.Bow,
                DefaultSkills.Crossbow
            }, DefaultCharacterAttributes.Control, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentUrbanOnCondition), new(base.AccomplishmentSiegeHunterOnConsequence), new(base.AccomplishmentSiegeHunterOnApply), new("{=FhZPjhli}Your hometown was briefly placed under siege, and you were called to defend the walls. Everyone did their part to repulse the enemy assault, and everyone is justly proud of what they endured.", null), null, 0, 5, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=kNXet6Um}you had a famous escapade in town.", null), new()
            {
                DefaultSkills.Athletics,
                DefaultSkills.Roguery
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentRuralOnCondition), new(base.AccomplishmentEscapadeOnConsequence), new(base.AccomplishmentEscapadeOnApply), new("{=DjeAJtix}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, on one of your trips into town you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null), new()
            {
                DefaultTraits.Valor
            }, 1, 5, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=qlOuiKXj}you had a famous escapade.", null), new()
            {
                DefaultSkills.Athletics,
                DefaultSkills.Roguery
            }, DefaultCharacterAttributes.Endurance, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, new(base.AccomplishmentUrbanOnCondition), new(base.AccomplishmentEscapadeOnConsequence), new(base.AccomplishmentEscapadeOnApply), new("{=lD5Ob3R4}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null), new()
            {
                DefaultTraits.Valor
            }, 1, 5, 0, 0, 0);

            characterCreationCategory.AddCategoryOption(new("{=Yqm0Dics}you treated people well.", null), new()
            {
                DefaultSkills.Charm,
                DefaultSkills.Steward
            }, DefaultCharacterAttributes.Social, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, null, new(base.AccomplishmentTreaterOnConsequence), new(base.AccomplishmentTreaterOnApply), new("{=dDmcqTzb}Yours wasn't the kind of reputation that local legends are made of, but it was the kind that wins you respect among those around you. You were consistently fair and honest in your business dealings and helpful to those in trouble. In doing so, you got a sense of what made people tick.", null), new()
            {
                DefaultTraits.Mercy,
                DefaultTraits.Generosity,
                DefaultTraits.Honor
            }, 1, 5, 0, 0, 0);

            characterCreation.AddNewMenu(characterCreationMenu);
        }

        public void AddCultureLocationMenu(CharacterCreation characterCreation)
        {
            CharacterCreationMenu characterCreationMenu = new(new("{=CulturedStart29}Location Options", null), new("{=CulturedStart30}Beginning your new adventure...", null), null, CharacterCreationMenu.MenuTypes.MultipleChoice);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(null);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart31}Near your home in the city where your journey began", null), new(), null, 0, 0, 0, null, new(this.HometownLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart32}Back to where you started", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart33}In a strange new city (Random)", null), new(), null, 0, 0, 0, null, new(this.RandomLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart34}Travelling far and wide you arrive at an unknown city", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart35}In a caravan to the Athas city of Drakar", null), new(), null, 0, 0, 0, null, new(this.QasariLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart37}In a caravan to the Elvean city of Cormanthor", null), new(), null, 0, 0, 0, null, new(this.DunglanysLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart38}On a ship to the Realm city of Zehentil", null), new(), null, 0, 0, 0, null, new(this.ZeonicaLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart39}You leave the ship and arrive right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart40}In a caravan to the Dread city of Nippura", null), new(), null, 0, 0, 0, null, new(this.BalgardLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart41}In a caravan to the All Khuur city of Ortongard", null), new(), null, 0, 0, 0, null, new(this.OrtongardLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart42}On a river boat to the Nasoria city of Valendia", null), new(), null, 0, 0, 0, null, new(this.PravendLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart43}You leave the boat and arrive right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart48}In a caravan to the Xilantlacay city of Uztlecot", null), new(), null, 0, 0, 0, null, new(UztlecotLocationOnConsequence), new(DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart50}In a caravan to the free state of Balik", null), new(), null, 0, 0, 0, null, new(this.BalikLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart36}You leave the caravan right at the gates", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart44}At your castle", null), new(), null, 0, 0, 0, new(this.CastleLocationOnCondition), new(this.CastleLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart45}At your newly acquired castle", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart46}Escaping from your captor", null), new(), null, 0, 0, 0, new(this.EscapingLocationOnCondition), new(this.EscapingLocationOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart47}Having just escaped", null), null, 0, 0, 0, 0, 0);
            characterCreation.AddNewMenu(characterCreationMenu);
        }
        public void AddCultureStartMenu(CharacterCreation characterCreation)
        {

            CharacterCreationMenu characterCreationMenu = new(new("{=CulturedStart07}Start Options", null), new("{=CulturedStart08}Who are you in Aeurth...", null), new CharacterCreationOnInit(this.StartOnInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
            CharacterCreationCategory characterCreationCategory = characterCreationMenu.AddMenuCategory(null);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart09}A commoner (Default Start)", null), new(), null, 0, 0, 0, null, new(this.DefaultStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart10}Setting off with your Father, Mother, Brother and your two younger siblings to a new town you'd heard was safer. But you did not make it.", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart11}A budding caravanner", null), new MBList<SkillObject>
            {
                DefaultSkills.Trade
            }, null, 1, 25, 0, null, new(this.MerchantStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart12}With what savings you could muster you purchased some mules and mercenaries." + $"\n{startingSkillMult[StartType.Merchant]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart13}A noble of {CULTURE} in exile", null), new MBList<SkillObject>
            {
                DefaultSkills.Leadership
            }, null, 1, 50, 0, null, new(this.ExiledStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart14}Forced into exile after your parents were executed for suspected treason. With only your family's bodyguard you set off. Should you return you'd be viewed as a criminal." + $"\n{startingSkillMult[StartType.Exiled]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 150, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart15}In a failing mercenary company", null), new MBList<SkillObject>
            {
                DefaultSkills.Tactics
            }, null, 1, 50, 0, null, new(this.MercenaryStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart16}With men deserting over lack of wages, your company leader was found dead, and you decided to take your chance and lead." + $"\n{startingSkillMult[StartType.Mercenary]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 50, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart17}A cheap outlaw", null), new MBList<SkillObject>
            {
                DefaultSkills.Roguery
            }, null, 1, 25, 0, null, new(this.LooterStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart18}Left impoverished from war, you found a group of like-minded ruffians who were desperate to get by." + $"\n{startingSkillMult[StartType.Looter]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 0, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart19}A new vassal of {CULTURE}", null), new MBList<SkillObject>
            {
                DefaultSkills.Steward
            }, null, 1, 50, 0, null, new(this.VassalStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart20}A young noble who came into an arrangement with the king for a chance at land." + $"\n{startingSkillMult[StartType.VassalNoFief]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 150, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart21}Leading part of {CULTURE}", null), new MBList<SkillObject>
            {
                DefaultSkills.Leadership,
                DefaultSkills.Steward
            }, DefaultCharacterAttributes.Social, 1, 50, 1, null, new(this.KingdomStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart22}With the support of companions you have gathered an army. With limited funds and food you decided it's time for action." + $"\n{startingSkillMult[StartType.KingdomRuler]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 900, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart23}You acquired a castle", null), new MBList<SkillObject>
            {
                DefaultSkills.Leadership,
                DefaultSkills.Steward
            }, DefaultCharacterAttributes.Social, 1, 25, 1, null, new(this.CastleRulerStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart24}You acquired a castle through your own means and declared yourself a kingdom for better or worse." + $"\n{startingSkillMult[StartType.CastleRuler]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 900, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart25}A landed vassal of {CULTURE}", null), new MBList<SkillObject>
            {
                DefaultSkills.Steward
            }, null, 1, 50, 0, null, new(this.LandedVassalStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart26}A young noble who came into an arrangement with the king for land." + $"\n{startingSkillMult[StartType.VassalFief]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 150, 0, 0, 0);
            characterCreationCategory.AddCategoryOption(new("{=CulturedStart27}A wanderer mystic of {CULTURE}", null), new MBList<SkillObject>
            {
                RFSkills.Arcane
            }, null, 1, 10, 0, null, new(this.EscapedStartOnConsequence), new(this.DoNothingOnApply), new("{=CulturedStart28}A mystic peregrin in pursuit of arcane misteries." + $"\n{startingSkillMult[StartType.EscapedPrisoner]} " + "{=rf_skill_change}times starting skill level multiplier", null), null, 0, 0, 0, 0, 0);
            characterCreation.AddNewMenu(characterCreationMenu);
        }
        protected bool GiantParentsOnCondition()
        {
            return base.GetSelectedCulture().StringId == "giant";
        }
        protected bool AqarunParentsOnCondition()
        {
            return base.GetSelectedCulture().StringId == "aqarun";
        }

        protected void StartOnInit(CharacterCreation characterCreation)
        {
            MBTextManager.SetTextVariable("CULTURE", CharacterCreationContentBase.Instance.GetSelectedCulture().Name, false);
        }

        protected void DefaultQuestOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetQuestOption(0);
        }

        protected void SkipQuestOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetQuestOption(1);
        }

        protected void DefaultStartOnConsequence(CharacterCreation characterCreation)
        {

            ChooseCharacterEquipment(characterCreation, StartType.Default);
            this.Manager.SetStoryOption(0);
        }

        protected void MerchantStartOnConsequence(CharacterCreation characterCreation)
        {

            ChooseCharacterEquipment(characterCreation, StartType.Merchant);
            this.Manager.SetStoryOption(1);
        }

        protected void ExiledStartOnConsequence(CharacterCreation characterCreation)
        {
            ChooseCharacterEquipment(characterCreation, StartType.Exiled);
            this.Manager.SetStoryOption(2);
        }

        protected void MercenaryStartOnConsequence(CharacterCreation characterCreation)
        {
            ChooseCharacterEquipment(characterCreation, StartType.Mercenary);
            this.Manager.SetStoryOption(3);
        }
        private Equipment getMaleEquipment(IEnumerable<Equipment> eq) { return eq.FirstOrDefault(); }
        private Equipment getFemaleEquipment(IEnumerable<Equipment> eq) { return eq.LastOrDefault(); }
        protected void ChooseCharacterEquipment(CharacterCreation characterCreation, StartType startType)
        {
            MBEquipmentRoster equipmentRoster;
            try
            { 
                equipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(CulturedStartAction.mainHeroStartingEquipment[startType][Hero.MainHero.Culture.StringId]);
                IEnumerable<Equipment> battleEquipments = equipmentRoster.GetBattleEquipments();
                IEnumerable<Equipment> civillianEquipments = equipmentRoster.GetCivilianEquipments();
                Equipment battleEquipment = CharacterObject.PlayerCharacter.IsFemale? getFemaleEquipment(battleEquipments) : getMaleEquipment(battleEquipments);
                Equipment civillianEquipment = CharacterObject.PlayerCharacter.IsFemale ? getFemaleEquipment(civillianEquipments) : getMaleEquipment(civillianEquipments);
                if (battleEquipment != null)
                {
                    var a = new List<int> { 1 };
                    characterCreation.ChangeCharactersEquipment(new List<Equipment>{ battleEquipment });
                    CharacterObject.PlayerCharacter.FirstBattleEquipment.FillFrom(battleEquipment);
                    ChangePlayerMount(characterCreation, Hero.MainHero);
                }
                if (civillianEquipment != null) CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(civillianEquipment);
            }
            catch
            {
                equipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("rf_looter");
                Equipment backupEquipment = equipmentRoster.GetBattleEquipments().GetRandomElementInefficiently();
                characterCreation.ChangeCharactersEquipment(new List<Equipment> { backupEquipment });
                CharacterObject.PlayerCharacter.FirstBattleEquipment.FillFrom(backupEquipment);
                CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(backupEquipment);

                InformationManager.DisplayMessage(new InformationMessage("Error while giving player the equipment", new Color(255, 0, 0)));
            }
        }
        protected void LooterStartOnConsequence(CharacterCreation characterCreation)
        {

            ChooseCharacterEquipment(characterCreation, StartType.Looter);
            this.Manager.SetStoryOption(4);
        }

        protected void VassalStartOnConsequence(CharacterCreation characterCreation)
        {

            ChooseCharacterEquipment(characterCreation, StartType.VassalFief);
            this.Manager.SetStoryOption(5);
        }

        protected void KingdomStartOnConsequence(CharacterCreation characterCreation)
        {
            ChooseCharacterEquipment(characterCreation, StartType.KingdomRuler);
            this.Manager.SetStoryOption(6);
        }

        protected void CastleRulerStartOnConsequence(CharacterCreation characterCreation)
        {

            ChooseCharacterEquipment(characterCreation, StartType.CastleRuler);
            this.Manager.SetStoryOption(7);
        }

        protected void LandedVassalStartOnConsequence(CharacterCreation characterCreation)
        {
            ChooseCharacterEquipment(characterCreation, StartType.VassalFief);
            this.Manager.SetStoryOption(8);
        }

        protected void EscapedStartOnConsequence(CharacterCreation characterCreation)
        {

            ChooseCharacterEquipment(characterCreation, StartType.EscapedPrisoner);
            this.Manager.SetStoryOption(9);
        }

        protected void HometownLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(0);
        }

        protected void RandomLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(1);
        }

        protected void QasariLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(2);
        }

        protected void DunglanysLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(3);
        }

        protected void ZeonicaLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(4);
        }

        protected void BalgardLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(5);
        }

        protected void OrtongardLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(6);
        }

        protected void PravendLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(7);
        }

        protected void CastleLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(8);
        }

        protected void EscapingLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(9);
        }
        
        protected void UztlecotLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(10);
        }
        protected void BalikLocationOnConsequence(CharacterCreation characterCreation)
        {
            this.Manager.SetLocationOption(11);
        }
        protected void DoNothingOnApply(CharacterCreation characterCreation)
        {
        }

        protected bool CastleLocationOnCondition()
        {
            return this.Manager.StoryOption == 7 || this.Manager.StoryOption == 8;
        }

        protected bool EscapingLocationOnCondition()
        {
            return this.Manager.StoryOption == 9;
        }

        protected new readonly Dictionary<string, Vec2> _startingPoints = new()
        {
            {
                "aserai",
                new Vec2(642.16f, 240.77f)
            },
            {
                "battania",
                new Vec2(446.58f, 460.19f)
            },
            {
                "empire",
                new Vec2(296.03f, 646.27f)
            },
            {
                "khuzait",
                new Vec2(43.49f, 733.3f)
            },
            {
                "sturgia",
                new Vec2(697.85f, 721.67f)
            },
            {
                "vlandia",
                new Vec2(667.2f, 442.22f)
            }
        };

        private const string AthasBodyPropString = "<BodyProperties version=\"4\" age=\"22.23\" weight=\"0.0448\" build=\"0.6065\"  key=\"003FB40FCE001016AF9E6DFC6B0756871FF2FD9D8031BB1327CCC0244CAB9C060069160306EC96D8000000000000000000000000000000000000000010CC1004\"  />";
        private const string NasoriaBodyPropString = "<BodyProperties version=\"4\" age=\"40\" weight=\"0.8288\" build=\"0.4213\"  key=\"001EAC0B80000004FFC53FE76E83CCEA36A3EC6D8174DF4070129ADF3E13E54B0366C6350684B8A7000000000000000000000000000000000000000026CC7002\"  />";
        private const string AllKhuurBodyPropString = "<BodyProperties version=\"4\" age=\"22.49\" weight=\"0.9599\" build=\"0.3611\"  key=\"001EF80D8000200AB8708BB6CDC85229D3698B3ABDFE344CD22D3DD5388988680355E6350596723B0000000000000000000000000000000000000000609C1005\"  />";
        private const string ElveanBodyPropString = "<BodyProperties version=\"4\" age=\"22.49\" weight=\"0.0262\" build=\"0.5108\"  key=\"00000400000000038788080F07757777F0F887F8F88008888E068A89808D80060078060307883F10000000000000000000000000000000000000000052F47145\"  />";
        private const string HumanBodyPropString = "<BodyProperties version=\"4\" age=\"22.35\" weight=\"0.5417\" build=\"0.5231\"  key=\"000DF00FC00033CD8771188F38770F8801F188778888888888888888546AF0F90088860308888888000000000000000000000000000000000000000043044144\"  />";
        private const string UndeadBodyPropString = "<BodyProperties version=\"4\" age=\"40\" weight=\"0.2978\" build=\"0.9522\"  key=\"000004001900178D18E0788057F760886F8707E84EA8E18174414A490D1100E803BE46350BA7B7A50000000000000000000000000000000000000000016430C6\"  />";
        private const string AqarunBodyPropString = "<BodyProperties version=\"4\" age=\"22.2\" weight=\"0.3272\" build=\"0.6343\"  key=\"003FF00997001019BFEBEF53ADA8CB8B1FFDFD063C34C704EEFCE0BD50AF939F009A560309FCF9B80000000000000000000000000000000000000000112C9002\"  />";
        private const string XilantlacayBodyPropString = "<BodyProperties version=\"4\" age=\"22.2\" weight=\"0.3272\" build=\"0.6343\"  key=\"003458078000200AFDAECE6F0BB44F0EF5F1DEFEDAA6B1818E66E1EE818DF07A007A560307E84F31000000000000000000000000000000000000000052F43142\"  />";
    }
}
