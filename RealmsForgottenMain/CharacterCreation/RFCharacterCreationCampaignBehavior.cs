using HarmonyLib;
using RealmsForgotten.Career;
using RealmsForgotten.CustomSkills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using static RealmsForgotten.Globals;

namespace RealmsForgotten.CharacterCreation
{
    public class RFCharacterCreationCampaignBehavior : CampaignBehaviorBase, ICharacterCreationContentHandler
    {
        private string GetMotherEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId)
        {
            characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out string str);
            return "mother_char_creation_" + str + "_" + cultureId;
        }

        private string GetFatherEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId)
        {
            characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out string str);
            return "father_char_creation_" + str + "_" + cultureId;
        }

        private string GetPlayerChildhoodAgeEquipmentId(CharacterCreationManager characterCreationManager, string parentOccupationType, string cultureId, bool isFemale)
        {
            characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(parentOccupationType, out string text);
            return string.Concat(new string[]
            {
                "player_char_creation_childhood_age_",
                cultureId,
                "_",
                text,
                "_",
                isFemale ? "f" : "m"
            });
        }

        private string GetPlayerEducationAgeEquipmentId(CharacterCreationManager characterCreationManager, string parentOccupationType, string cultureId, bool isFemale)
        {
            characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(parentOccupationType, out string text);
            return string.Concat(new string[]
            {
                "player_char_creation_education_age_",
                cultureId,
                "_",
                text,
                "_",
                isFemale ? "f" : "m"
            });
        }

        private string GetPlayerEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId, bool isFemale)
        {
            characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out string text);
            return string.Concat(new string[]
            {
                "player_char_creation_",
                cultureId,
                "_",
                text,
                "_",
                isFemale ? "f" : "m"
            });
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(this, new Action<CharacterCreationManager>(OnCharacterCreationInitialized));
            CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCreationOver);
        }

        private void OnCreationOver()
        {
            CulturedStartAction.Apply(CurrentStartType, ChosenSettlement);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
        {
            _focusToAdd = characterCreationManager.CharacterCreationContent.FocusToAdd;
            _skillLevelToAdd = characterCreationManager.CharacterCreationContent.SkillLevelToAdd;
            _attributeLevelToAdd = characterCreationManager.CharacterCreationContent.AttributeLevelToAdd;
            characterCreationManager.CharacterCreationContent.DefaultSelectedTitleType = "guard";
            characterCreationManager.RegisterCharacterCreationContentHandler(this, 800);
        }
        void ICharacterCreationContentHandler.InitializeContent(CharacterCreationManager characterCreationManager)
        {

            characterCreationManager.CharacterCreationContent.AddEquipmentToUseGetter(delegate (string occupationId, out string equipmentId)
            {
                return _occupationToEquipmentMapping.TryGetValue(occupationId, out equipmentId);
            });
            InitializeCharacterCreationStages(characterCreationManager);
            InitializeCharacterCreationCultures(characterCreationManager);
            InitializeData(characterCreationManager);
        }

        void ICharacterCreationContentHandler.AfterInitializeContent(CharacterCreationManager characterCreationManager) { }

        void ICharacterCreationContentHandler.OnStageCompleted(CharacterCreationStageBase stage)
        {
            if (stage is CharacterCreationCultureStage)
            {
                BodyProperties.FromString(CharacterCreationConfig.GetBodyPropertiesFromCulture(Hero.MainHero.Culture.StringId), out BodyProperties properties);
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(properties, CharacterCreationConfig.GetRaceIdFromCulture(Hero.MainHero.Culture.StringId), CharacterObject.PlayerCharacter.IsFemale);
                MBTextManager.SetTextVariable("CULTURE", CharacterObject.PlayerCharacter.Culture.Name, false);
            }
            if (stage is CharacterCreationFaceGeneratorStage)
            {
                FaceGenUpdated();
            }
        }

        void ICharacterCreationContentHandler.OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager) { }

        public void InitializeCharacterCreationStages(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.AddStage(new CharacterCreationCultureStage());
            characterCreationManager.AddStage(new CharacterCreationFaceGeneratorStage());
            characterCreationManager.AddStage(new CharacterCreationNarrativeStage());
            characterCreationManager.AddStage(new CharacterCreationBannerEditorStage());
            characterCreationManager.AddStage(new CharacterCreationClanNamingStage());
            characterCreationManager.AddStage(new CharacterCreationReviewStage());
            characterCreationManager.AddStage(new CharacterCreationOptionsStage());
        }

        public void InitializeCharacterCreationCultures(CharacterCreationManager characterCreationManager)
        {
            foreach (CultureObject cultureObject in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>())
            {
                if (CharacterCreationConfig.PlayerSelectableCultures.Contains(cultureObject.StringId))
                    characterCreationManager.CharacterCreationContent.AddCharacterCreationCulture(cultureObject, 1, 10);
            }
        }

        public void InitializeData(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.ChangeReviewPageDescription(new TextObject("{=W6pKpEoT}You prepare to set off for a grand adventure in Calradia! Here is your character. Continue if you are ready, or go back to make changes.", null));
            AddParentsMenu(characterCreationManager);
            AddChildhoodMenu(characterCreationManager);
            AddEducationMenu(characterCreationManager);
            AddYouthMenu(characterCreationManager);
            AddAdulthoodMenu(characterCreationManager);
            AddAgeSelectionMenu(characterCreationManager);
            AddRFSpecialStartMenu(characterCreationManager);
            AddCultureLocationMenu(characterCreationManager);

            RFCulturalFeats culturalFeats = new();

            foreach (CultureObject cultureObject in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
            {
                string cultureId = cultureObject.StringId;
                FieldInfo _description = AccessTools.Field(typeof(PropertyObject), "_description");

                _description.SetValue(DefaultCulturalFeats.BattanianMilitiaFeat, new TextObject("Towns owned by rulers with the current culture have +1 militia production."));
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
                        if (cultureObject.CultureFeats.Contains(DefaultCulturalFeats.AseraiDesertFeat))
                            cultureObject.CultureFeats.Remove(DefaultCulturalFeats.AseraiDesertFeat);
                        if (cultureObject.CultureFeats.Contains(DefaultCulturalFeats.AseraiTraderFeat))
                            cultureObject.CultureFeats.Remove(DefaultCulturalFeats.AseraiTraderFeat);

                        cultureObject.CultureFeats.Add(culturalFeats.aqarunRecruitBandits);
                        break;
                    case "south_realm":
                        cultureObject.CultureFeats.Add(culturalFeats.empireAdittionalTier);
                        break;
                    case "west_realm":
                        cultureObject.CultureFeats.Add(culturalFeats.empireAdittionalTier);
                        break;
                    case "mage":
                        cultureObject.CultureFeats.Add(culturalFeats.empireAdittionalTier);
                        break;
                    case "dwarf":
                        cultureObject.CultureFeats.Add(culturalFeats.athasFasterConstructions);
                        break;
                    case "urkhai":
                        cultureObject.CultureFeats.Add(culturalFeats.xilantlacayRaidersBonus);
                        break;
                    case "wulf":
                        cultureObject.CultureFeats.Add(culturalFeats.allkhuurPrisonersJoinMilitia);
                        break;
                }
            }
        }
        public static readonly string IS_PLAYER_SETTLEMENT = "IS_PLAYER_SETTLEMENT";
        public void AddCultureLocationMenu(CharacterCreationManager characterCreationManager)
        {
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            bodyProperties = TaleWorlds.Core.FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, characterCreationManager.CharacterCreationContent.StartingAge);
            NarrativeMenuCharacter item = new("player_rf_menu_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            List<NarrativeMenuCharacter> list = new() { item };
            NarrativeMenu narrativeMenu = new("rf_start_location", "rf_start", "", new TextObject("{=CulturedStart29}Location Options", null), new TextObject("{=CulturedStart30}Beginning your new adventure...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetRFStartMenuNarrativeMenuCharacterArgs));

            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_0", new("{=CulturedStart31}Near your home in the city where your journey began"), new("{=CulturedStart32}Back to where you started"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement =  Settlement.All.Where(s => s.IsTown && s.Culture == Hero.MainHero.Culture).GetRandomElementInefficiently().StringId; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_1", new("{=CulturedStart33}In a strange new city (Random)"), new("{=CulturedStart34}Travelling far and wide you arrive at an unknown city"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = Settlement.All.GetRandomElement().StringId; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_2", new("{=CulturedStart35}In a caravan to the Athas city of Drakar"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_A8"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_3", new("{=CulturedStart37}In a caravan to the Elvean city of Cormanthor"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_B2"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_4", new("{=CulturedStart38}On a ship to the Realm city of Zehentil"), new("{=CulturedStart39}You leave the ship and arrive right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_EW2"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_5", new("{=CulturedStart40}In a caravan to the Dread city of Nippura"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_S2"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_6", new("{=CulturedStart41}In a caravan to the All Khuur city of Ortongard"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_K4"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_7", new("{=CulturedStart42}On a river boat to the Nasoria city of Valendia"), new("{=CulturedStart43}You leave the boat and arrive right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_V3"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_8", new("{=CulturedStart48}In a caravan to the Xilantlacay city of Uztlecot"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_G1"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_9", new("{=CulturedStart50}In a caravan to the free state of Balik"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_A5"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_10", new("{=CulturedStart51}In a caravan to the the city of Verbrund"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_ES1"; })));
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_11", new("{=CulturedStart51}In a caravan to the the city of Wulfgard"), new("{=CulturedStart36}You leave the caravan right at the gates"), null, null, null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = "town_dwarf_1"; })));
            List<StartType> typesForFiefStart = new() { StartType.King, StartType.Usurper, StartType.Knight };
            narrativeMenu.AddNarrativeMenuOption(new("rf_start_location_12", new("{=CulturedStart44}At your fief"), new("{=CulturedStart45}At your newly acquired settlement"), null, new NarrativeMenuOptionOnConditionDelegate(m => { return typesForFiefStart.Contains(CurrentStartType); }), null, new NarrativeMenuOptionOnConsequenceDelegate(m => { ChosenSettlement = IS_PLAYER_SETTLEMENT; })));

            //narrativeMenu.AddNarrativeMenuOption(new("rf_menu_mistic", new TextObject("{=CulturedStart27}A mistic of {CULTURE}"), new TextObject("{=CulturedStart28}A mystic peregrin in pursuit of arcane misteries."), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuMisticNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Mistic); }), new NarrativeMenuOptionOnConsequenceDelegate(MisticStartOnConsequence)));
            characterCreationManager.AddNewMenu(narrativeMenu);
        }
        private void AddRFSpecialStartMenu(CharacterCreationManager characterCreationManager)
        {
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            bodyProperties = TaleWorlds.Core.FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, characterCreationManager.CharacterCreationContent.StartingAge);
            NarrativeMenuCharacter item = new("player_rf_menu_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            List<NarrativeMenuCharacter> list = new() { item };
            NarrativeMenu narrativeMenu = new("rf_start", "narrative_age_selection_menu", "rf_start_location", new TextObject("Who are you in Auerth", null), new TextObject("Who are you in Auerth", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetRFStartMenuNarrativeMenuCharacterArgs));
            
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_commoner", new TextObject("{=CulturedStart09}A commoner (Default Start)", null), new TextObject("{=CulturedStart10}Setting off with your Father, Mother, Brother and your two younger siblings to a new town you'd heard was safer. But you did not make it.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuCommonerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => {ChooseCharacterEquipment(m, StartType.Default); }), new NarrativeMenuOptionOnConsequenceDelegate(CommonerStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_caravaneer", new TextObject("{=CulturedStart11}A budding caravaneer", null), new TextObject("{=CulturedStart12}With what savings you could muster you purchased some mules and mercenaries.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuCaravaneerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Merchant); }), new NarrativeMenuOptionOnConsequenceDelegate(MerchantStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_ranger", new TextObject("A ranger of {CULTURE} in exile", null), new TextObject("{=CulturedStart14}Forced into exile after your parents were executed for suspected treason. With only your family's bodyguard you set off. Should you return you'd be viewed as a criminal.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuRangerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Exiled); }), new NarrativeMenuOptionOnConsequenceDelegate(ExiledStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_mercenary", new TextObject("{=CulturedStart15}A leader of a failing mercenary company", null), new TextObject("{=CulturedStart16}With men deserting over lack of wages, your company leader was found dead, and you decided to take your chance and lead.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuMercenaryNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Mercenary); }), new NarrativeMenuOptionOnConsequenceDelegate(MercenaryStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_outlaw", new TextObject("{=CulturedStart17}A cheap outlaw", null), new TextObject("{=CulturedStart18}Left impoverished from war, you found a group of like-minded ruffians who were desperate to get by.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuOutlawNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Outlaw); }), new NarrativeMenuOptionOnConsequenceDelegate(OutlawStartOnConsequence)));
            //narrativeMenu.AddNarrativeMenuOption(new("rf_menu_cleric", new TextObject("{=CulturedStart19}An cleric of {CULTURE}", null), new TextObject("", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuClericNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(DoNothin), new NarrativeMenuOptionOnConsequenceDelegate(ClericStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_king", new TextObject("{=CulturedStart21}A King of {CULTURE}", null), new TextObject("{=CulturedStart22}A recognized ruler, you have a kingdom to rule.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuKingNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.King); }), new NarrativeMenuOptionOnConsequenceDelegate(KingStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_usurper", new TextObject("{=CulturedStart23}An ursurper of {CULTURE}", null), new TextObject("{=CulturedStart24}You acquired a castle through your own means and declared yourself a kingdom for better or worse.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuUsurperNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Usurper); }), new NarrativeMenuOptionOnConsequenceDelegate(UsurperStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_knight", new TextObject("{=CulturedStart25}A knight of {CULTURE}", null), new TextObject("{=CulturedStart26}Under the weight of an oath you came into an arrangement with the king for a chance at land.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuKnightNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Knight); }), new NarrativeMenuOptionOnConsequenceDelegate(KnightStartOnConsequence)));
            narrativeMenu.AddNarrativeMenuOption(new("rf_menu_mistic", new TextObject("{=CulturedStart27}A mistic of {CULTURE}", null), new TextObject("{=CulturedStart28}A mystic peregrin in pursuit of arcane misteries.", null), new GetNarrativeMenuOptionArgsDelegate(RFStartMenuMisticNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(m => { return true; }), new NarrativeMenuOptionOnSelectDelegate(m => { ChooseCharacterEquipment(m, StartType.Mistic); }), new NarrativeMenuOptionOnConsequenceDelegate(MisticStartOnConsequence)));
            characterCreationManager.AddNewMenu(narrativeMenu);
        }
        private List<NarrativeMenuCharacterArgs> GetRFStartMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
        {
            
            List<NarrativeMenuCharacterArgs> list = new();
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            list.Add(new NarrativeMenuCharacterArgs("player_rf_menu_character", characterCreationManager.CharacterCreationContent.StartingAge, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
            MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
            if (equipment == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ERROR, could not find {playerEquipmentId}!", new Color(1, 0, 0)));
                equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_empire_guard_m");
            }

            ItemObject item = equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
            list.Add(new NarrativeMenuCharacterArgs("narrative_character_horse", -1, "", "act_horse_stand_1", "spawnpoint_mount_1", equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, equipment.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed()), false, false));
            return list;
        }
        private void RFStartMenuCommonerNarrativeOptionArgs(NarrativeMenuOptionArgs args) {}
        private void RFStartMenuCaravaneerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(25);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        private void RFStartMenuRangerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Leadership,
                DefaultSkills.Scouting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(25);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        private void RFStartMenuMercenaryNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Tactics,
                DefaultSkills.Roguery
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(30);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
            //args.PositiveEffectText = new TextObject("{=CulturedStart46}Your subjects begrudgingly accept your rule.", null);
        }
        private void RFStartMenuOutlawNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Roguery,
                DefaultSkills.Scouting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(25);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        //private void RFStartMenuClericNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        //{
        //    SkillObject[] affectedSkills = new SkillObject[]
        //    {
        //        DefaultSkills.Steward,
        //        DefaultSkills.Charm,
        //        RFSkills.Faith
        //    };
        //    args.SetAffectedSkills(affectedSkills);
        //    args.SetFocusToSkills(_focusToAdd);
        //    args.SetLevelToSkills(20);
        //    args.SetLevelToAttribute(RFAttributes.Discipline, _attributeLevelToAdd);
        //}
        private void RFStartMenuKingNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
               DefaultSkills.Leadership,
               DefaultSkills.Steward,
               DefaultSkills.Riding,
               DefaultSkills.Charm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(20);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void RFStartMenuUsurperNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Leadership,
                DefaultSkills.Steward,
                DefaultSkills.Charm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(30);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        private void RFStartMenuKnightNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Steward,
                DefaultSkills.Riding
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(30);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void RFStartMenuMisticNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                RFSkills.Arcane,
                DefaultSkills.Scouting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(25);
            args.SetLevelToAttribute(RFAttributes.Discipline, _attributeLevelToAdd);
        }
        StartType CurrentStartType { get; set; }
        string ChosenSettlement { get; set; } = "";
        protected void CommonerStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Default;
        }

        protected void MerchantStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Merchant;
        }

        protected void ExiledStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Exiled;
            ChooseCharacterEquipment(characterCreation, CurrentStartType);
        }

        protected void MercenaryStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Mercenary;
            PlayerCareerExtension.AddCareer(RFCareers.Mercenary);
        }
        protected void OutlawStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Outlaw;
        }
        protected void KingStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.King;
        }

        protected void UsurperStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Usurper;
        }

        protected void KnightStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Knight;
            PlayerCareerExtension.AddCareer(RFCareers.Knight);
        }

        protected void MisticStartOnConsequence(CharacterCreationManager characterCreation)
        {
            CurrentStartType = StartType.Mistic;
            PlayerCareerExtension.AddCareer(RFCareers.Wizard);
        }
        public void FaceGenUpdated()
        {
            CharacterCreationManager characterCreationManager = (GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager;
            BodyProperties bodyProperties2;
            BodyProperties bodyProperties;
            TaleWorlds.Core.FaceGen.GenerateParentKey(bodyProperties = bodyProperties2 = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1), CharacterObject.PlayerCharacter.Race, ref bodyProperties2, ref bodyProperties);
            bodyProperties2 = new BodyProperties(new DynamicBodyProperties(33f, 0.3f, 0.2f), bodyProperties2.StaticProperties);
            bodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.5f, 0.5f), bodyProperties.StaticProperties);
            foreach (NarrativeMenu narrativeMenu in characterCreationManager.NarrativeMenus)
            {
                foreach (NarrativeMenuCharacter narrativeMenuCharacter in narrativeMenu.Characters)
                {
                    if (narrativeMenuCharacter.StringId.Equals("mother_character"))
                    {
                        narrativeMenuCharacter.UpdateBodyProperties(bodyProperties2, CharacterObject.PlayerCharacter.Race, true);
                    }
                    if (narrativeMenuCharacter.StringId.Equals("father_character"))
                    {
                        narrativeMenuCharacter.UpdateBodyProperties(bodyProperties, CharacterObject.PlayerCharacter.Race, false);
                    }
                    if (narrativeMenuCharacter.StringId.Equals("player_childhood_character") 
                        || narrativeMenuCharacter.StringId.Equals("player_education_character") 
                        || narrativeMenuCharacter.StringId.Equals("player_youth_character") 
                        || narrativeMenuCharacter.StringId.Equals("player_adulthood_character") 
                        || narrativeMenuCharacter.StringId.Equals("player_age_selection_character")
                        || narrativeMenuCharacter.StringId.Equals("player_rf_menu_character"))
                    {
                        narrativeMenuCharacter.UpdateBodyProperties(CharacterObject.PlayerCharacter.GetBodyProperties(null, -1), CharacterObject.PlayerCharacter.Race, false);
                    }
                }
            }
        }
        private List<NarrativeMenuCharacterArgs> GetParentMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
        {
            return new List<NarrativeMenuCharacterArgs>
            {
                new("mother_character", 33, "mother_char_creation_none_" + characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, "act_character_creation_female_default_standing", "spawnpoint_player_1", "", "", null, true, true),
                new("father_character", 33, "father_char_creation_none_" + characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, "act_character_creation_male_default_standing", "spawnpoint_player_1", "", "", null, true, false)
            };
        }
        private void AddParentsMenu(CharacterCreationManager characterCreationManager)
        {
            List<NarrativeMenuCharacter> list = new();
            BodyProperties bodyProperties2;
            BodyProperties bodyProperties;
            TaleWorlds.Core.FaceGen.GenerateParentKey(bodyProperties = bodyProperties2 = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1), CharacterObject.PlayerCharacter.Race, ref bodyProperties2, ref bodyProperties);
            bodyProperties2 = new BodyProperties(new DynamicBodyProperties(33f, 0.3f, 0.2f), bodyProperties2.StaticProperties);
            bodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.5f, 0.5f), bodyProperties.StaticProperties);
            NarrativeMenuCharacter item = new("mother_character", bodyProperties2, CharacterObject.PlayerCharacter.Race, true);
            list.Add(item);
            NarrativeMenuCharacter item2 = new("father_character", bodyProperties, CharacterObject.PlayerCharacter.Race, false);
            list.Add(item2);
            NarrativeMenu narrativeMenu = new("narrative_parent_menu", "start", "narrative_childhood_menu", new TextObject("{=b4lDDcli}Family", null), new TextObject("{=XgFU1pCx}You were born into a family of...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetParentMenuNarrativeMenuCharacterArgs));
            AddEmpireParentNarrativeMenuOptions(narrativeMenu);
            AddVlandianParentNarrativeMenuOptions(narrativeMenu);
            AddSturgianParentNarrativeMenuOptions(narrativeMenu);
            AddAseraiParentNarrativeMenuOptions(narrativeMenu);
            AddBattaniaNarrativeMenuOptions(narrativeMenu);
            AddKhuzaitNarrativeMenuOptions(narrativeMenu);
            AddXilanNarrativeMenuOptions(narrativeMenu);
            AddAquarunNarrativeMenuOptions(narrativeMenu);
            AddMageNarrativeMenuOptions(narrativeMenu);
            AddDwarfNarrativeMenuOptions(narrativeMenu);
            AddUrkhaiParentNarrativeMenuOptions(narrativeMenu);
            AddWulfenParentNarrativeMenuOptions(narrativeMenu);
            characterCreationManager.AddNewMenu(narrativeMenu);
        }

        private void AddEmpireParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption directDescendantsOption = new(
                "human_direct_descendants",
                new TextObject("Direct Descendants of the first people."),
                new TextObject("{=ivKl4mV2}Descending from the ruler´s bloodline of the First People - the ancestors that made the pilgrimage to Aeurth - your father was a leader among his village and the cousin of the King of his Realm. He rode with the lord´s cavalry, fighting as an armored lancer."),
                new GetNarrativeMenuOptionArgsDelegate(GetHumanDirectDescendantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(HumanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(directDescendantsOption);
            NarrativeMenuOption humanMerchantsOption = new(
                "human_merchants",
                new TextObject("{=651FhzdR}Urban merchants"),
                new TextObject("{=FQntPChs}Your family were merchants in one of the main cities of the Kingdoms of Man. They sometimes organized caravans to nearby towns, and discussed issues in the town council."),
                new GetNarrativeMenuOptionArgsDelegate(GetHumanMerchantNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(HumanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(humanMerchantsOption);
            NarrativeMenuOption humanFarmersOption = new(
                "human_farmers",
                new TextObject("Free Farmers"),
                new TextObject("{=09z8Q08f}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the realm rural economy, as well as the backbone of the levy."),
                new GetNarrativeMenuOptionArgsDelegate(GetHumanFarmerNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(HumanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(humanFarmersOption);
            NarrativeMenuOption humanArtisansOption = new(
                "human_artisans",
                new TextObject("{=v48N6h1t}Urban artisans"),
                new TextObject("{=ZKynvffv}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."),
                new GetNarrativeMenuOptionArgsDelegate(GetHumanArtisanNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(HumanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(humanArtisansOption);
            NarrativeMenuOption humanForestCaretakersOption = new(
                "human_forestcaretakers",
                new TextObject("Forestcaretakers"),
                new TextObject("Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."),
                new GetNarrativeMenuOptionArgsDelegate(GetHumanForestCaretakersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(HumanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(humanForestCaretakersOption);
            NarrativeMenuOption humanVagabondsOption = new(
                "human_vagabonds",
                new TextObject("{=aEke8dSb}Urban vagabonds"),
                new TextObject("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the many criminal gangs, and you had an early look at the dark side of life."),
                new GetNarrativeMenuOptionArgsDelegate(GetHumanVagabondsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(HumanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(VagabondNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(humanVagabondsOption);
        }

        private bool HumanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            string culture = characterCreationManager.CharacterCreationContent.SelectedCulture.StringId;
            return culture == "empire" || culture == "west_realm" || culture == "south_realm";
        }

        private void GetHumanDirectDescendantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetHumanMerchantNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private void GetHumanFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetHumanArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Crafting, DefaultSkills.Crossbow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private void GetHumanForestCaretakersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Bow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private void GetHumanVagabondsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }
        public void UpdateParentEquipment(CharacterCreationManager characterCreationManager, MBEquipmentRoster motherEquipment, MBEquipmentRoster fatherEquipment, string motherAnimation, string fatherAnimation)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId.Equals("mother_character"))
                {
                    narrativeMenuCharacter.SetEquipment(motherEquipment);
                    narrativeMenuCharacter.SetAnimationId(motherAnimation);
                }
                if (narrativeMenuCharacter.StringId.Equals("father_character"))
                {
                    narrativeMenuCharacter.SetEquipment(fatherEquipment);
                    narrativeMenuCharacter.SetAnimationId(fatherAnimation);
                }
            }
        }

        private void AddVlandianParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption retainersQairthOption = new(
                "nasorian_retainers_qairth",
                new TextObject("Retainers of the Qairth"),
                new TextObject("Your father was a bailiff for a local Qairth. He looked after his Qairth's estates, resolved disputes in the village, and helped train the village levy. He rode with the Qairth's cavalry, fighting as an armored knight."),
                new GetNarrativeMenuOptionArgsDelegate(GetNasorianRetainersQairthNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(NasorianNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(retainersQairthOption);
            NarrativeMenuOption guildMerchantsOption = new(
                "nasorian_guild_merchants",
                new TextObject("Guildmerchants"),
                new TextObject("{=qNZFkxJb}Your family were merchants in one of the main cities of the kingdom. They organized caravans to nearby towns and were active in the local merchant's guild."),
                new GetNarrativeMenuOptionArgsDelegate(GetNasorianGuildMerchantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(NasorianNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(guildMerchantsOption);
            NarrativeMenuOption aldenariOption = new(
                "nasorian_aldenari",
                new TextObject("Aldenari"),
                new TextObject("{=BLZ4mdhb}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."),
                new GetNarrativeMenuOptionArgsDelegate(GetNasorianAldenariNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(NasorianNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(aldenariOption);
            NarrativeMenuOption blacksmithOption = new(
            "nasorian_blacksmith",
            new TextObject("Urban blacksmith"),
            new TextObject("{=btsMpRcA}Your family owned a smithy in a city. Your father played an active if minor role in the town council, and also served in the militia."),
            new GetNarrativeMenuOptionArgsDelegate(GetNasorianBlacksmithNarrativeOptionArgs),
            new NarrativeMenuOptionOnConditionDelegate(NasorianNarrativeOptionOnCondition),
            new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
            null);
            narrativeMenu.AddNarrativeMenuOption(blacksmithOption);
            NarrativeMenuOption huntersOption = new(
            "nasorian_hunters",
            new TextObject("Hunters"),
            new TextObject("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."),
            new GetNarrativeMenuOptionArgsDelegate(GetNasorianHuntersNarrativeOptionArgs),
            new NarrativeMenuOptionOnConditionDelegate(NasorianNarrativeOptionOnCondition),
            new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
            null);
            narrativeMenu.AddNarrativeMenuOption(huntersOption);
            NarrativeMenuOption MercenariesOption = new(
                "nasorian_mercenaries",
                new TextObject("Mercenaries"),
                new TextObject("Your father joined one of the East many mercenary companies, composed of men who got such a taste for war in their clan's service that they never took well to peace. Their crossbowmen were much valued across the world. Your mother was a camp follower, taking you along in the wake of bloody campaigns."),
                new GetNarrativeMenuOptionArgsDelegate(GetNasorianMercenariesNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(NasorianNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MercenaryNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(MercenariesOption);
        }

        private bool NasorianNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
        }

        private void GetNasorianRetainersQairthNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetNasorianGuildMerchantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetNasorianAldenariNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Polearm,
                DefaultSkills.Crossbow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetNasorianHuntersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Scouting,
                DefaultSkills.Crossbow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetNasorianMercenariesNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Roguery,
                DefaultSkills.Crossbow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetNasorianBlacksmithNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Crafting,
                DefaultSkills.TwoHanded
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void AddSturgianParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption servantsUndeadOption = new(
                "undead_servants",
                new TextObject("Servants of the Undead"),
                new TextObject("Your family served the Undead."),
                new GetNarrativeMenuOptionArgsDelegate(GetUndeadServantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UndeadNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(servantsUndeadOption);
            NarrativeMenuOption undeadTradersOption = new(
                "undead_traders",
                new TextObject("{=HqzVBfpl}Urban traders"),
                new TextObject("Your family were merchants who lived in one of the land's great river ports, organizing the shipment of goods to faraway lands."),
                new GetNarrativeMenuOptionArgsDelegate(GetUndeadTradersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UndeadNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(undeadTradersOption);
            NarrativeMenuOption undeadFarmersOption = new(
                "undead_farmers",
                new TextObject("Farmers"),
                new TextObject("{=Mcd3ZyKq}Your family had just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."),
                new GetNarrativeMenuOptionArgsDelegate(GetUndeadFarmersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UndeadNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(undeadFarmersOption);
            NarrativeMenuOption undeadArtisansOption = new(
                "undead_artisans",
                new TextObject("{=v48N6h1t}Urban artisans"),
                new TextObject("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."),
                new GetNarrativeMenuOptionArgsDelegate(GetUndeadArtisansNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UndeadNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(undeadArtisansOption);
            NarrativeMenuOption forestfolkOption = new(
                "undead_forestfolk",
                new TextObject("Forestfolk"),
                new TextObject("Your family had no taste for authority of others. They made their living deep in the woods, slashing and burning fields which they tended for a year or two before moving on. They hunted and trapped fox, hare, ermine, and other fur-bearing animals."),
                new GetNarrativeMenuOptionArgsDelegate(GetUndeadForestfolkNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UndeadNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(forestfolkOption);
            NarrativeMenuOption undeadVagabondsOption = new(
                "undead_vagabonds",
                new TextObject("{=TPoK3GSj}Vagabonds"),
                new TextObject("{=2SDWhGmQ}Your family numbered among the poor migrants living in the slums that grow up outside the walls of the river cities, making whatever money they could from a variety of odd jobs. Sometimes they did services for one of the region's many criminal gangs."),
                new GetNarrativeMenuOptionArgsDelegate(GetUndeadVagabondsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UndeadNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(VagabondNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(undeadVagabondsOption);
        }

        private bool UndeadNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
        }

        private void GetUndeadServantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.TwoHanded });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private void GetUndeadTradersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Tactics });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private void GetUndeadFarmersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetUndeadArtisansNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Crafting, DefaultSkills.OneHanded });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private void GetUndeadForestfolkNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Bow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetUndeadVagabondsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }
        private void AddAseraiParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption AthasInnerCircleOption = new(
                "athas_inner_circle",
                new TextObject("The inner circle of Atha's rulers"),
                new TextObject("You were a family of some importance in the inner circle of Athas."),
                new GetNarrativeMenuOptionArgsDelegate(GetAthasInnerCircleNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AthasNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(AthasInnerCircleOption);
            // Warrior-slaves
            NarrativeMenuOption AthasWarriorSlavesOption = new(
                "athas_warrior_slaves",
                new TextObject("{=ngFVgwDD}Warrior-slaves"),
                new TextObject("{=GsPC2MgU}Your father was part of one of the slave-bodyguards maintained by the rulers. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother."),
                new GetNarrativeMenuOptionArgsDelegate(GetAthasWarriorSlavesNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AthasNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MercenaryNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(AthasWarriorSlavesOption);
            NarrativeMenuOption athasMerchantsOption = new(
                "athas_merchants",
                new TextObject("{=651FhzdR}Urban merchants"),
                new TextObject("{=1zXrlaav}Your family were respected traders in an oasis town. They ran caravans across the desert, and were experts in the finer points of negotiating passage through the desert tribes' territories."),
                new GetNarrativeMenuOptionArgsDelegate(GetAthasMerchantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AthasNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(athasMerchantsOption);
            NarrativeMenuOption athasSlaveFarmersOption = new(
                "athas_slave_farmers",
                new TextObject("Slave-farmers"),
                new TextObject("{=5P0KqBAw}Your family tilled the soil in one of the oases of the Kalikhr tribe and tended the palm orchards that produced the desert's famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under the emir's banner."),
                new GetNarrativeMenuOptionArgsDelegate(GetAthasSlaveFarmersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AthasNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(athasSlaveFarmersOption);
            NarrativeMenuOption athasFreeMenOption = new(
                "athas_free_men",
                new TextObject("Free men"),
                new TextObject("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Kalikhr."),
                new GetNarrativeMenuOptionArgsDelegate(GetAthasFreeMenNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AthasNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(athasFreeMenOption);
            NarrativeMenuOption athasOrfansOption = new(
                "athas_orfans",
                new TextObject("Urban Orfans"),
                new TextObject("{=6bUSbsKC}Your father was not your biological father, but took you under his protection to one day strenghten his army of thugs. He worked for a fitiwi , one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff."),
                new GetNarrativeMenuOptionArgsDelegate(GetAthasOrfansNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AthasNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(VagabondNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(athasOrfansOption);
        }
        private bool AthasNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
        }

        private void GetAthasInnerCircleNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetAthasWarriorSlavesNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetAthasMerchantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private void GetAthasSlaveFarmersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.OneHanded });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetAthasFreeMenNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Bow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private void GetAthasOrfansNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }
        private void AddBattaniaNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption elvean_highborn_option = new(
                "elvean_highborn",
                new TextObject("Elvean Highborn"),
                new TextObject("Your family were the trusted kinfolk of an Elvean lord, and sat at his table in his great hall. Your father assisted his chief in running the affairs and trained with the traditional weapons of the warrior elite, the two-handed sword or falx and the bow."),
                new GetNarrativeMenuOptionArgsDelegate(GetElveanHighbornNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(ElveanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(elvean_highborn_option);
            NarrativeMenuOption elvean_druids_option = new(
                "elvean_druids",
                new TextObject("Druids"),
                new TextObject("Your parents were healers who gathered herbs and treated the sick. As a living reservoir of elvean tradition, they were also asked to adjudicate many disputes between the clans."),
                new GetNarrativeMenuOptionArgsDelegate(GetElveanDruidsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(ElveanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HealerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(elvean_druids_option);
            NarrativeMenuOption elvean_folk_option = new(
                "elvean_folk",
                new TextObject("Elvean Folk"),
                new TextObject("Your family were middle-ranking members of a society, who tilled their own land. Your father fought with the kern, the main body of his people's warriors, joining in the screaming charges for which the Elveans were famous."),
                new GetNarrativeMenuOptionArgsDelegate(GetElveanFolkNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(ElveanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(elvean_folk_option);
            NarrativeMenuOption elvean_smiths_option = new(
                "elvean_smiths",
                new TextObject("{=BCU6RezA}Smiths"),
                new TextObject("Your family were smiths, a revered profession. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Elvean aristocracy."),
                new GetNarrativeMenuOptionArgsDelegate(GetElveanSmithsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(ElveanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(elvean_smiths_option);
            NarrativeMenuOption elvean_foresters_option = new(
                "elvean_foresters",
                new TextObject("{=7eWmU2mF}Foresters"),
                new TextObject("{=7jBroUUQ}Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation."),
                new GetNarrativeMenuOptionArgsDelegate(GetElveanForestersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(ElveanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(elvean_foresters_option);
            NarrativeMenuOption elvean_bards_option = new(
                "elvean_bards",
                new TextObject("{=SpJqhEEh}Bards"),
                new TextObject("Your Father was a Bard, a sacred duty for the Elvean Folk. Responsible to keep the Song alive, he went from halls to festivities, from rituals to war camps, to teach and inspire the people into the sacred ways. Your learned from him the cleverness of the tongue and the hability to tap into your people´s soul."),
                new GetNarrativeMenuOptionArgsDelegate(GetElveanBardsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(ElveanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(BardNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(elvean_bards_option);
        }
        private bool ElveanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
        }
        private void GetElveanHighbornNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.TwoHanded, DefaultSkills.Bow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetElveanDruidsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Medicine, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private void GetElveanFolkNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private void GetElveanSmithsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Crafting, DefaultSkills.TwoHanded });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetElveanForestersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Tactics });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private void GetElveanBardsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void AddKhuzaitNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption alkahuur_kinsfolk_option = new(
                "alkahuur_kinsfolk",
                new TextObject("Al-Kahuur Kinsfolk"),
                new TextObject("Your family were the trusted kinsfolk of a ruler, and shared his meals in the chieftain's yurt. Your father assisted his chief in running the affairs of the clan and fought in the core of armored lancers in the center of a battle line."),
                new GetNarrativeMenuOptionArgsDelegate(GetAlKahuurKinsfolkNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AlKahuurNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(alkahuur_kinsfolk_option);
            NarrativeMenuOption alkahuur_merchants_option = new(
                "alkahuur_merchants",
                new TextObject("{=TkgLEDRM}Merchants"),
                new TextObject("Your family came from one of the merchant clans that dominated the cities in the northwestern part of the world."),
                new GetNarrativeMenuOptionArgsDelegate(GetAlKahuurMerchantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AlKahuurNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(alkahuur_merchants_option);
            NarrativeMenuOption alkahuur_tribespeople_option = new(
                "alkahuur_tribespeople",
                new TextObject("{=tGEStbxb}Tribespeople"),
                new TextObject("Your family were middle-ranking members of one of the clans. They had some herds of thier own, but were not rich."),
                new GetNarrativeMenuOptionArgsDelegate(GetAlKahuurTribespeopleNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AlKahuurNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(alkahuur_tribespeople_option);
            NarrativeMenuOption alkahuur_farmers_option = new(
                "alkahuur_farmers",
                new TextObject("{=gQ2tAvCz}Farmers"),
                new TextObject("Your family tilled one of the small patches of arable land in the steppes for generations."),
                new GetNarrativeMenuOptionArgsDelegate(GetAlKahuurFarmersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AlKahuurNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(alkahuur_farmers_option);
            NarrativeMenuOption alkahuur_spiritcatchers_option = new(
                "alkahuur_spiritcatchers",
                new TextObject("Spirit-Catchers"),
                new TextObject("Your family were guardians of the sacred traditions, channelling the spirits of the wilderness and of the ancestors. They tended the sick and dispensed wisdom, resolving disputes and providing practical advice."),
                new GetNarrativeMenuOptionArgsDelegate(GetAlKahuurSpiritCatchersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AlKahuurNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HealerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(alkahuur_spiritcatchers_option);
            NarrativeMenuOption alkahuur_nomads_option = new(
                "alkahuur_nomads",
                new TextObject("{=Xqba1Obq}Nomads"),
                new TextObject("{=9aoQYpZs}Your family's clan never pledged its loyalty to the khan and never settled down, preferring to live out in the deep steppe away from his authority. They remain some of the finest trackers and scouts in the grasslands, as the ability to spot an enemy coming and move quickly is often all that protects their herds from their neighbors' predations."),
                new GetNarrativeMenuOptionArgsDelegate(GetAlKahuurNomadsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AlKahuurNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HerderNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(alkahuur_nomads_option);
        }
        private bool AlKahuurNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
        }
        private void GetAlKahuurKinsfolkNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetAlKahuurMerchantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private void GetAlKahuurTribespeopleNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Bow, DefaultSkills.Riding });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private void GetAlKahuurFarmersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Polearm, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetAlKahuurSpiritCatchersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Medicine, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private void GetAlKahuurNomadsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Riding });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }
        private void ArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_hugging";
            string fatherAnimation = "act_character_creation_male_default_hugging";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void HerderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("herder");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_side_to_side_3";
            string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void HealerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("healer_urban");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_side_to_side_3";
            string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void BardNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("bard_urban");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_hugging";
            string fatherAnimation = "act_character_creation_male_default_hugging";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void MerchantNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_mother_front";
            string fatherAnimation = "act_character_creation_male_default_mother_front";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void RetainerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_side_to_side_1";
            string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void HunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_side_to_side_3";
            string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void FarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_father_sitting";
            string fatherAnimation = "act_character_creation_male_default_father_sitting";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void VagabondNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("vagabond_urban");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_hugging";
            string fatherAnimation = "act_character_creation_male_default_hugging";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void MercenaryNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("mercenary_urban");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_mother_front";
            string fatherAnimation = "act_character_creation_male_default_mother_front";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void PhysicianNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SetParentOccupation("physician_urban");
            string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
            MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
            MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
            string motherAnimation = "act_character_creation_female_default_father_sitting";
            string fatherAnimation = "act_character_creation_male_default_father_sitting";
            UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
        }
        private void AddAquarunNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption ChampionsOption = new(
                "aqarun_champions", 
                new TextObject("Aqarun Champions"), 
                new TextObject("Your parents were chosen between the champions of Aqarun warriors. Your father filled up the warking's private army and your mother a shieldmaiden at the warlord bodyguard. The champions were the only ones that could speak directly to the warkings."), 
                new GetNarrativeMenuOptionArgsDelegate(GetAqarunChampionNarrativeOptionArgs), 
                new NarrativeMenuOptionOnConditionDelegate(AqarunNarrativeOptionOnCondition), 
                new NarrativeMenuOptionOnSelectDelegate(MercenaryNarrativeOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(ChampionsOption);
            NarrativeMenuOption warriorsOption = new(
                "aqarun_warriors",
                new TextObject("Warriors"),
                new TextObject("Your father was part of one of the slave-bodyguards maintained by the Aqarun warkings. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother."),
                new GetNarrativeMenuOptionArgsDelegate(GetAqarunWarriorNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AqarunNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(warriorsOption);
            NarrativeMenuOption merchantsOption = new(
                "aqarun_merchants",
                new TextObject("{=651FhzdR}Urban merchants"),
                new TextObject("{=1zXrlaav}Your family were respected traders in an oasis town. They ran caravans across the desert, and were experts in the finer points of negotiating passage through the desert tribes' territories."),
                new GetNarrativeMenuOptionArgsDelegate(GetAqarunMerchantNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AqarunNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(merchantsOption);
            NarrativeMenuOption farmersOption = new(
                "aqarun_farmers",
                new TextObject("Free farmers"),
                new TextObject("Your familly tilled the soil in one of the many oases under the aqarun territory and tended the palm orchards that produced the desert´s famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under his ruler´s banner."),
                new GetNarrativeMenuOptionArgsDelegate(GetAqarunFarmerNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AqarunNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(farmersOption);
            NarrativeMenuOption freemenOption = new(
                "aqarun_freemen",
                new TextObject("Free men"),
                new TextObject("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Kalikhr."),
                new GetNarrativeMenuOptionArgsDelegate(GetAqarunFreeMenNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AqarunNarrativeOptionOnCondition), 
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(freemenOption);
            NarrativeMenuOption orphansOption = new(
                "aqarun_orphans",
                new TextObject("Orphans"),
                new TextObject("Your father was not your biological father, but adopted you under his protection to one day strenghten his army of thugs. He worked for a fitiwi, one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff."),
                new GetNarrativeMenuOptionArgsDelegate(GetAqarunOrphansNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(AqarunNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(VagabondNarrativeOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(orphansOption);
        }
        private bool AqarunNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aqarun";
        }

        private void GetAqarunChampionNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetAqarunWarriorNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.OneHanded, DefaultSkills.TwoHanded });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetAqarunMerchantNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private void GetAqarunFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.OneHanded });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }
        private void GetAqarunFreeMenNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Bow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }
        private void GetAqarunOrphansNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }
        private void GetKhuzaitRetainerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void AddXilanNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption xilatlacay_highborn_option = new(
                "xilatlacay_highborn",
                new TextObject("Xilatlacay Highborn"),
                new TextObject("Your familly belonged to the noble bloodline of the Xilantlacay tribal leaders, descendent of the God Xilan. Your father assisted the Xilan king in running the affairs and trained with the sacred weapons of the warrior elite, the warclub and the crossbow."),
                new GetNarrativeMenuOptionArgsDelegate(GetXilatlacayHighbornNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(XilanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(xilatlacay_highborn_option);
            NarrativeMenuOption xilatlacay_shamans_option = new(
                "xilatlacay_shamans",
                new TextObject("Shamans"),
                new TextObject("Your parents were shamans, who spoke with the spirits of the forests and mastered the art of healing herbs. As a reservoir of Xilan knowledge, they adjudicated many disputes between the tribes."),
                new GetNarrativeMenuOptionArgsDelegate(GetXilatlacayShamansNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(XilanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HealerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(xilatlacay_shamans_option);
            NarrativeMenuOption xilatlacay_folk_option = new(
                "xilatlacay_folk",
                new TextObject("Xilan Folk"),
                new TextObject("Your family were of Xilantlacay folk, who tilled their own land. Your father fought with the Xtlacay, the main body of his people´s warriors, joining in the screaming charges for which the Xtlacay were famous."),
                new GetNarrativeMenuOptionArgsDelegate(GetXilatlacayFolkNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(XilanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(xilatlacay_folk_option);
            NarrativeMenuOption xilatlacay_smiths_option = new(
                "xilatlacay_smiths",
                new TextObject("{=BCU6RezA}Smiths"),
                new TextObject("Your family were smiths, a revered profession among the Xilantlacay. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Xilantlacay aristocracy."),
                new GetNarrativeMenuOptionArgsDelegate(GetXilatlacaySmithsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(XilanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(xilatlacay_smiths_option);
            NarrativeMenuOption xilatlacay_foresters_option = new(
                "xilatlacay_foresters",
                new TextObject("{=7eWmU2mF}Foresters"),
                new TextObject("Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation."),
                new GetNarrativeMenuOptionArgsDelegate(GetXilatlacayForestersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(XilanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(xilatlacay_foresters_option);
            NarrativeMenuOption xilatlacay_sages_option = new(
                "xilatlacay_sages",
                new TextObject("Sages"),
                new TextObject("Your Father was a Sage, a sacred duty for the Xilan Folk. Responsible to keep the history of their people alive, he went from halls to festivities, from rituals to war camps, to teach and inspire the people into the sacred ways. Your learned from him the cleverness of the tongue and the hability to tap into your people soul."),
                new GetNarrativeMenuOptionArgsDelegate(GetXilatlacaySagesNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(XilanNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(BardNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(xilatlacay_sages_option);
        }
        private bool XilanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "giant";
        }
        private void GetXilatlacayHighbornNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.TwoHanded, DefaultSkills.Crossbow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetXilatlacayShamansNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Medicine, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private void GetXilatlacayFolkNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private void GetXilatlacaySmithsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Crafting, DefaultSkills.OneHanded });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetXilatlacayForestersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Tactics });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private void GetXilatlacaySagesNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void AddMageNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption mageDirectDescendantsOption = new(
                "mage_direct_descendants",
                new TextObject("Direct Descendants of the first people"),
                new TextObject("Descending from the ruler´s bloodline of the First People - the ancestors that made the pilgrimage to Aeurth - your father was a leader among his village and the cousin of the King of his Realm. He rode with the lord´s cavalry, fighting as an armored lancer."),
                new GetNarrativeMenuOptionArgsDelegate(GetMageDirectDescendantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(MageNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(mageDirectDescendantsOption);
            NarrativeMenuOption mageUrbanMerchantsOption = new(
                "mage_urban_merchants",
                new TextObject("{=651FhzdR}Urban merchants"),
                new TextObject("{=FQntPChs}Your family were merchants in one of the main cities of the Kingdoms of Man. They sometimes organized caravans to nearby towns, and discussed issues in the town council."),
                new GetNarrativeMenuOptionArgsDelegate(GetMageUrbanMerchantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(MageNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(mageUrbanMerchantsOption);
            NarrativeMenuOption mageFreeFarmersOption = new(
                "mage_free_farmers",
                new TextObject("Free Farmers"),
                new TextObject("{=09z8Q08f}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the realm rural economy, as well as the backbone of the levy."),
                new GetNarrativeMenuOptionArgsDelegate(GetMageFreeFarmersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(MageNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(mageFreeFarmersOption);
            NarrativeMenuOption mageUrbanArtisansOption = new(
                "mage_urban_artisans",
                new TextObject("{=v48N6h1t}Urban artisans"),
                new TextObject("{=ZKynvffv}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."),
                new GetNarrativeMenuOptionArgsDelegate(GetMageUrbanArtisansNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(MageNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(mageUrbanArtisansOption);
            NarrativeMenuOption mageForestcaretakersOption = new(
                "mage_forestcaretakers",
                new TextObject("Forestcaretakers"),
                new TextObject("Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."),
                new GetNarrativeMenuOptionArgsDelegate(GetMageForestCaretakersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(MageNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(mageForestcaretakersOption);
            NarrativeMenuOption mageUrbanVagabondsOption = new(
                "mage_urban_vagabonds",
                new TextObject("{=aEke8dSb}Urban vagabonds"),
                new TextObject("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the many criminal gangs, and you had an early look at the dark side of life."),
                new GetNarrativeMenuOptionArgsDelegate(GetMageUrbanVagabondsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(MageNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(VagabondNarrativeOptionOnSelect),
                null
            );
            narrativeMenu.AddNarrativeMenuOption(mageUrbanVagabondsOption);
        }
        private bool MageNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "mage";
        }
        private void GetMageDirectDescendantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Riding, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private void GetMageUrbanMerchantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private void GetMageFreeFarmersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private void GetMageUrbanArtisansNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Crafting, DefaultSkills.Crossbow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private void GetMageForestCaretakersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Bow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private void GetMageUrbanVagabondsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private List<NarrativeMenuCharacterArgs> GetChildhoodMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
        {
            List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
            string playerChildhoodAgeEquipmentId = GetPlayerChildhoodAgeEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            list.Add(new NarrativeMenuCharacterArgs("player_childhood_character", 7, playerChildhoodAgeEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
            return list;
        }
        private void AddDwarfNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption dwarfNoblesOption = new(
                "dwarf_nobles",
                new TextObject("Dugrast Nobles"),
                new TextObject("Your family was part of the noble dwarven houses, renowned for their craftsmanship and warrior skills."),
                new GetNarrativeMenuOptionArgsDelegate(GetDwarfNoblesNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(DwarfNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(dwarfNoblesOption);
            NarrativeMenuOption dwarfMerchantsOption = new(
                "dwarf_merchants",
                new TextObject("{=651FhzdR}Urban merchants"),
                new TextObject("{=FQntPChs}Your family were merchants in one of the main cities of the Dugrast Kingdom. They sometimes organized caravans to nearby towns, and discussed issues in the town council."),
                new GetNarrativeMenuOptionArgsDelegate(GetDwarfMerchantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(DwarfNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(dwarfMerchantsOption);
            NarrativeMenuOption dwarfArtisansOption = new(
                "dwarf_artisans",
                new TextObject("Dugrast Artisans"),
                new TextObject("Your family were famous artisans, crafting weapons, armor, and items of unmatched quality."),
                new GetNarrativeMenuOptionArgsDelegate(GetDwarfArtisansNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(DwarfNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(dwarfArtisansOption);
            NarrativeMenuOption dwarfMinersOption = new(
                "dwarf_miners",
                new TextObject("Dugrast Miners"),
                new TextObject("Your family worked in the deep mines, extracting precious metals and gems from the earth."),
                new GetNarrativeMenuOptionArgsDelegate(GetDwarfMinersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(DwarfNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(FarmerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(dwarfMinersOption);
            NarrativeMenuOption dwarfWarriorsOption = new(
                "dwarf_warriors",
                new TextObject("Dugrast Warriors"),
                new TextObject("Your family were part of the dwarven military, renowned for their resilience and tactical brilliance in battle."),
                new GetNarrativeMenuOptionArgsDelegate(GetDwarfWarriorsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(DwarfNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(dwarfWarriorsOption);
            NarrativeMenuOption dwarfVagabondsOption = new(
                "dwarf_vagabonds",
                new TextObject("{=aEke8dSb}Urban vagabonds"),
                new TextObject("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the many criminal gangs, and you had an early look at the dark side of life."),
                new GetNarrativeMenuOptionArgsDelegate(GetDwarfVagabondsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(DwarfNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(VagabondNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(dwarfVagabondsOption);
        }
        private bool DwarfNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "dwarf";
        }
        private void GetDwarfNoblesNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.TwoHanded, DefaultSkills.Riding });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }
        private void GetDwarfMerchantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetDwarfArtisansNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Crafting, DefaultSkills.Trade });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        private void GetDwarfMinersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Engineering, DefaultSkills.Athletics });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }
        private void GetDwarfWarriorsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.OneHanded, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }
        private void GetDwarfVagabondsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private void AddUrkhaiParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption urkhaiCommandersOption = new(
                "urkhai_commanders",
                new TextObject("Urkhai Commanders"),
                new TextObject("You were born from a high rank Urkhai commander, renowned for his commanding skills and courage in battle."),
                new GetNarrativeMenuOptionArgsDelegate(GetUrkhaiCommandersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UrkhaiNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(urkhaiCommandersOption);
            NarrativeMenuOption urkhaiMerchantsOption = new(
                "urkhai_merchants",
                new TextObject("{=651FhzdR}Urban merchants"),
                new TextObject("{=FQntPChs}Your family were merchants in one of the main cities of the Urkhai Kingdom. They sometimes organized caravans to nearby towns, and discussed issues in the town council."),
                new GetNarrativeMenuOptionArgsDelegate(GetUrkhaiMerchantsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UrkhaiNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(MerchantNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(urkhaiMerchantsOption);
            NarrativeMenuOption urkhaiArtisansOption = new(
                "urkhai_artisans",
                new TextObject("Urkhai Artisans"),
                new TextObject("Your family were famous artisans, crafting weapons, armor, and items of unmatched quality."),
                new GetNarrativeMenuOptionArgsDelegate(GetUrkhaiArtisansNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UrkhaiNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(urkhaiArtisansOption);
            NarrativeMenuOption urkhaiMinersOption = new(
                "urkhai_miners",
                new TextObject("Urkhai Miners"),
                new TextObject("Your family worked in the deep mines, extracting precious metals and gems from the earth."),
                new GetNarrativeMenuOptionArgsDelegate(GetUrkhaiMinersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UrkhaiNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(urkhaiMinersOption);
            NarrativeMenuOption urkhaiTroopsOption = new(
                "urkhai_troops",
                new TextObject("Urkhai Troops"),
                new TextObject("Your family were part of the urkhaish military, renowned for their brutality and courage in battle."),
                new GetNarrativeMenuOptionArgsDelegate(GetUrkhaiTroopsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UrkhaiNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(urkhaiTroopsOption);
            NarrativeMenuOption urkhaiVagabondsOption = new(
                "urkhai_vagabonds",
                new TextObject("{=aEke8dSb}Urban vagabonds"),
                new TextObject("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the many criminal gangs, and you had an early look at the dark side of life."),
                new GetNarrativeMenuOptionArgsDelegate(GetUrkhaiVagabondsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(UrkhaiNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(VagabondNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(urkhaiVagabondsOption);
        }
        private bool UrkhaiNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "urkhai";
        }

        private void GetUrkhaiCommandersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.TwoHanded, DefaultSkills.Riding });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }
        private void GetUrkhaiMerchantsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Trade, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private void GetUrkhaiMinersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Engineering, DefaultSkills.Athletics });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }
        private void GetUrkhaiArtisansNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Crafting, DefaultSkills.Trade });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        private void GetUrkhaiTroopsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.OneHanded, DefaultSkills.Polearm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }
        private void GetUrkhaiVagabondsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }
        private void AddChildhoodMenu(CharacterCreationManager characterCreationManager)
        {
            List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            bodyProperties = TaleWorlds.Core.FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 7f);
            NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_childhood_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            list.Add(item);
            NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_childhood_menu", "narrative_parent_menu", "narrative_education_menu", new TextObject("{=8Yiwt1z6}Early Childhood", null), new TextObject("{=character_creation_content_16}As a child you were noted for...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetChildhoodMenuNarrativeMenuCharacterArgs));
            AddChildhoodNarrativeMenuOptions(narrativeMenu);
            characterCreationManager.AddNewMenu(narrativeMenu);
        }
        private void AddWulfenParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption wulfenHighbornOption = new(
                "wulfen_highborn",
                new TextObject("Wulfen Highborn"),
                new TextObject("Your family stood among the chieftain’s inner circle, sharing feasts at the mead-hall. They fought with fierce two-handed blades, and you learned woodland archery while training beside your clan’s champions."),
                new GetNarrativeMenuOptionArgsDelegate(GetWulfenHighbornNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(WulfenNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(RetainerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(wulfenHighbornOption);
            NarrativeMenuOption wulfenDruidsOption = new(
                "wulfen_druids",
                new TextObject("Wulfen Druids"),
                new TextObject("Your parents were wise druids, versed in sacred rites and herbal crafts. They ministered to the sick, mediated clan disputes, and kept alive the old Celtic-Germanic rituals that bound the tribe together."),
                new GetNarrativeMenuOptionArgsDelegate(GetWulfenDruidsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(WulfenNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HealerNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(wulfenDruidsOption);
            NarrativeMenuOption wulfenClansfolkOption = new(
                "wulfen_clansfolk",
                new TextObject("Wulfen Clansfolk"),
                new TextObject("Your family were stalwart freemen, tending their own fields in the shadow of deep forests. Your father joined the clan’s main warband, loosing furious charges echoing with battle cries of old."),
                new GetNarrativeMenuOptionArgsDelegate(GetWulfenClansfolkNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(WulfenNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HerderNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(wulfenClansfolkOption);
            NarrativeMenuOption wulfenSmithsOption = new(
                "wulfen_smiths",
                new TextObject("Wulfen Smiths"),
                new TextObject("Your kin were famed for forging stout iron blades and intricate jewelry. In smoky forges, they hammered steel into deadly axes and swords prized by chieftains across the land."),
                new GetNarrativeMenuOptionArgsDelegate(GetWulfenSmithsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(WulfenNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(ArtisanNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(wulfenSmithsOption);
            NarrativeMenuOption wulfenForestersOption = new(
                "wulfen_foresters",
                new TextObject("Wulfen Foresters"),
                new TextObject("Your family survived off thick woodlands, hunting and trapping game among ancient oaks and firs. They taught you to move silently and live off the land—a skill that could save your life in enemy territory."),
                new GetNarrativeMenuOptionArgsDelegate(GetWulfenForestersNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(WulfenNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(HunterNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(wulfenForestersOption);
            NarrativeMenuOption wulfenSkaldsOption = new(
                "wulfen_skalds",
                new TextObject("Wulfen Skalds"),
                new TextObject("Your father was a traveling skald, reciting heroic sagas and preserving clan lore. Through stirring verses at feasts and gatherings, you learned the power of the spoken word and the secrets of influencing men’s hearts."),
                new GetNarrativeMenuOptionArgsDelegate(GetWulfenSkaldsNarrativeOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(WulfenNarrativeOptionOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(BardNarrativeOptionOnSelect),
                null);
            narrativeMenu.AddNarrativeMenuOption(wulfenSkaldsOption);
        }
        private void GetWulfenHighbornNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.TwoHanded, DefaultSkills.Bow });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }
        private void GetWulfenDruidsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Medicine, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        private void GetWulfenClansfolkNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Athletics, DefaultSkills.Throwing });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }
        private void GetWulfenSmithsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Engineering, DefaultSkills.Athletics });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }
        private void GetWulfenForestersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Scouting, DefaultSkills.Tactics });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }
        private void GetWulfenSkaldsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetAffectedSkills(new SkillObject[] { DefaultSkills.Roguery, DefaultSkills.Charm });
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private bool WulfenNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "wulf";
        }
        private void AddChildhoodNarrativeMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("childhood_leadership_option", new TextObject("{=kmM68Qx4}your leadership skills.", null), new TextObject("{=FfNwXtii}If the wolf pup gang of your early childhood had an alpha, it was definitely you. All the other kids followed your lead as you decided what to play and where to play, and led them in games and mischief.", null), new GetNarrativeMenuOptionArgsDelegate(GetChildhoodLeadershipOptionArgs), new NarrativeMenuOptionOnConditionDelegate(ChildhoodLeadershipOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(ChildhoodLeadershipOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
            NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("childhood_brawn_option", new TextObject("{=5HXS8HEY}your brawn.", null), new TextObject("{=YKzuGc54}You were big, and other children looked to have you around in any scrap with children from a neighboring village. You pushed a plough and threw an axe like an adult.", null), new GetNarrativeMenuOptionArgsDelegate(GetChildhoodBrawnOptionArgs), new NarrativeMenuOptionOnConditionDelegate(ChildhoodBrawnOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(ChildhoodBrawnOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
            NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("childhood_detail_option", new TextObject("{=QrYjPUEf}your attention to detail.", null), new TextObject("{=JUSHAPnu}You were quick on your feet and attentive to what was going on around you. Usually you could run away from trouble, though you could give a good account of yourself in a fight with other children if cornered.", null), new GetNarrativeMenuOptionArgsDelegate(GetChildhoodDetailOptionArgs), new NarrativeMenuOptionOnConditionDelegate(ChildhoodDetailOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(ChildhoodDetailOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
            NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("childhood_smart_option", new TextObject("{=Y3UcaX74}your aptitude for numbers.", null), new TextObject("{=DFidSjIf}Most children around you had only the most rudimentary education, but you lingered after class to study letters and mathematics. You were fascinated by the marketplace - weights and measures, tallies and accounts, the chatter about profits and losses.", null), new GetNarrativeMenuOptionArgsDelegate(GetChildhoodSmartOptionArgs), new NarrativeMenuOptionOnConditionDelegate(ChildhoodSmartOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(ChildhoodSmartOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
            NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("childhood_leader_option", new TextObject("{=GEYzLuwb}your way with people.", null), new TextObject("{=w2TEQq26}You were always attentive to other people, good at guessing their motivations. You studied how individuals were swayed, and tried out what you learned from adults on your friends.", null), new GetNarrativeMenuOptionArgsDelegate(GetChildhoodLeaderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(ChildhoodLeaderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(ChildhoodLeaderOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
            NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("childhood_horse_option", new TextObject("{=MEgLE2kj}your skill with horses.", null), new TextObject("{=ngazFofr}You were always drawn to animals, and spent as much time as possible hanging out in the village stables. You could calm horses, and were sometimes called upon to break in new colts. You learned the basics of veterinary arts, much of which is applicable to humans as well.", null), new GetNarrativeMenuOptionArgsDelegate(GetChildhoodHorseOptionArgs), new NarrativeMenuOptionOnConditionDelegate(ChildhoodHorseOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(ChildhoodHorseOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
        }

        private void GetChildhoodLeadershipOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Leadership,
                DefaultSkills.Tactics
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private bool ChildhoodLeadershipOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }
        private void ChildhoodLeadershipOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_childhood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_leader");
                }
            }
        }

        private void GetChildhoodBrawnOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.TwoHanded,
                DefaultSkills.Throwing
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private bool ChildhoodBrawnOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void ChildhoodBrawnOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_childhood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
                }
            }
        }

        private void GetChildhoodDetailOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Athletics,
                DefaultSkills.Bow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private bool ChildhoodDetailOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void ChildhoodDetailOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_childhood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_memory");
                }
            }
        }

        private void GetChildhoodSmartOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Engineering,
                DefaultSkills.Trade
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private bool ChildhoodSmartOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void ChildhoodSmartOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_childhood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_numbers");
                }
            }
        }

        private void GetChildhoodLeaderOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Charm,
                DefaultSkills.Leadership
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private bool ChildhoodLeaderOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void ChildhoodLeaderOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_childhood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_manners");
                }
            }
        }

        private void GetChildhoodHorseOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Medicine
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private bool ChildhoodHorseOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void ChildhoodHorseOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_childhood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_animals");
                }
            }
        }

        private List<NarrativeMenuCharacterArgs> GetEducationMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
        {
            List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
            string playerEducationAgeEquipmentId = GetPlayerEducationAgeEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            list.Add(new NarrativeMenuCharacterArgs("player_education_character", 12, playerEducationAgeEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
            return list;
        }

        public void AddEducationMenu(CharacterCreationManager characterCreationManager)
        {
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            bodyProperties = TaleWorlds.Core.FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 12f);
            List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
            NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_education_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            list.Add(item);
            NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_education_menu", "narrative_childhood_menu", "narrative_youth_menu", new TextObject("{=rcoueCmk}Adolescence", null), new TextObject("{=WYvnWcXQ}Like all village children you helped out in the fields. You also...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetEducationMenuNarrativeMenuCharacterArgs));
            AddEducationMenuOptions(narrativeMenu);
            characterCreationManager.AddNewMenu(narrativeMenu);
        }

        private void AddEducationMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("education_herder_option", new TextObject("{=RKVNvimC}herded the sheep.", null), new TextObject("{=KfaqPpbK}You went with other fleet-footed youths to take the villages' sheep, goats or cattle to graze in pastures near the village. You were in charge of chasing down stray beasts, and always kept a big stone on hand to be hurled at lurking predators if necessary.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationHerderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationHerderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationHerderOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
            NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("education_smith_option", new TextObject("{=bTKiN0hr}worked in the village smithy.", null), new TextObject("{=y6j1bJTH}You were apprenticed to the local smith. You learned how to heat and forge metal, hammering for hours at a time until your muscles ached.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationSmithOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationSmithOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationSmithOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
            NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("education_engineer_option", new TextObject("{=tI8ZLtoA}repaired projects.", null), new TextObject("{=6LFj919J}You helped dig wells, rethatch houses, and fix broken plows. You learned about the basics of construction, as well as what it takes to keep a farming community prosperous.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationEngineerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationEngineerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationEngineerOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
            NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("education_doctor_option", new TextObject("{=TRwgSLD2}gathered herbs in the wild.", null), new TextObject("{=9ks4u5cH}You were sent by the village healer up into the hills to look for useful medicinal plants. You learned which herbs healed wounds or brought down a fever, and how to find them.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationDoctorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationDoctorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationDoctorOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
            NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("education_hunter_option", new TextObject("{=T7m7ReTq}hunted small game.", null), new TextObject("{=RuvSk3QT}You accompanied a local hunter as he went into the wilderness, helping him set up traps and catch small animals.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationHunterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationHunterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationHunterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
            NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("education_merchant_option", new TextObject("{=qAbMagWq}sold product at the market.", null), new TextObject("{=DIgsfYfz}You took your family's goods to the nearest town to sell your produce and buy supplies. It was hard work, but you enjoyed the hubbub of the marketplace.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationMerchantOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationMerchantOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationMerchantOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
            NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("education_watcher_option", new TextObject("{=go7Yu7KS}watched the militia training.", null), new TextObject("{=qnqdEJOv}You watched the town's watch practice shooting and perfect their plans to defend the walls in case of a siege.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationWatcherOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationWatcherOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationWatcherOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
            NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("education_ganger_option", new TextObject("{=gAjvAGTa}hung out with the gangs in the alleys.", null), new TextObject("{=1SUTcF0J}The gang leaders who kept watch over the slums of Calradian cities were always in need of poor youth to run messages and back them up in turf wars, while thrill-seeking merchants' sons and daughters sometimes slummed it in their company as well.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationGangerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationGangerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationGangerOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
            NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("education_docker_option", new TextObject("{=QVVCgajg}helped at building sites.", null), new TextObject("{=bhdkegZ4}All towns had their share of projects that were constantly in need of both skilled and unskilled labor. You learned how hoists and scaffolds were constructed, how planks and stones were hewn and fitted, and other skills.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationDockerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationDockerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationDockerOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
            NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("education_marketer_option", new TextObject("{=JTsv6PFe}worked in the markets and caravanserais.", null), new TextObject("{=rmMcwSn8}You helped your family handle their business affairs, going down to the marketplace to make purchases and oversee the arrival of caravans.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationMarketerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationMarketerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationMarketerOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
            NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("education_tutor_option", new TextObject("{=EMVojYzW}studied with your private tutor.", null), new TextObject("{=hXl25avg}Your family arranged for a private tutor and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationTutorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationTutorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationTutorOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
            NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("education_horser_option", new TextObject("{=hin3iA2D}cared for the horses.", null), new TextObject("{=Ghz90npw}Your family owned a few horses at the town stables and you took charge of their care. Many evenings you would take them out beyond the walls and gallup through the fields, racing other youth.", null), new GetNarrativeMenuOptionArgsDelegate(GetEducationPoorHorserOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EducationPoorHorserOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EducationPoorHorserOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
        }

        private void GetEducationHerderOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Athletics,
                DefaultSkills.Throwing
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private bool EducationHerderOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationHerderOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_streets");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("carry_bostaff_rogue1");
                    break;
                }
            }
        }

        private void GetEducationSmithOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.TwoHanded,
                DefaultSkills.Crafting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private bool EducationSmithOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationSmithOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_militia");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("peasant_hammer_1_t1");
                    break;
                }
            }
        }

        private void GetEducationEngineerOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Crafting,
                DefaultSkills.Engineering
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private bool EducationEngineerOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationEngineerOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_grit");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("carry_hammer");
                    break;
                }
            }
        }

        private void GetEducationDoctorOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Medicine,
                DefaultSkills.Scouting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private bool EducationDoctorOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationDoctorOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_basket_a");
                    break;
                }
            }
        }

        private void GetEducationHunterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Bow,
                DefaultSkills.Tactics
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private bool EducationHunterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationHunterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("composite_bow");
                    break;
                }
            }
        }

        private void GetEducationMerchantOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private bool EducationMerchantOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationMerchantOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers_2");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_fabric_c");
                    break;
                }
            }
        }
        private void GetEducationWatcherOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Polearm,
                DefaultSkills.Tactics
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }
        private bool EducationWatcherOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }
        private void EducationWatcherOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_fox");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("");
                    break;
                }
            }
        }
        private void GetEducationGangerOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Roguery,
                DefaultSkills.OneHanded
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }
        private bool EducationGangerOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }
        private void EducationGangerOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("");
                    break;
                }
            }
        }
        private void GetEducationDockerOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Athletics,
                DefaultSkills.Crafting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }
        private bool EducationDockerOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }
        private void EducationDockerOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_basket_a");
                    break;
                }
            }
        }
        private void GetEducationMarketerOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Trade,
                DefaultSkills.Charm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }
        private bool EducationMarketerOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }
        private void EducationMarketerOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_manners");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("");
                    break;
                }
            }
        }
        private void GetEducationTutorOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Engineering,
                DefaultSkills.Leadership
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }
        private bool EducationTutorOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationTutorOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_book");
                    narrativeMenuCharacter.SetLeftHandItem("character_creation_notebook");
                    narrativeMenuCharacter.SetRightHandItem("");
                    break;
                }
            }
        }

        private void GetEducationPoorHorserOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Steward
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private bool EducationPoorHorserOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void EducationPoorHorserOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_education_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers_2");
                    narrativeMenuCharacter.SetLeftHandItem("");
                    narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_fabric_c");
                    break;
                }
            }
        }

        private List<NarrativeMenuCharacterArgs> GetYouthMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
        {
            if (string.IsNullOrEmpty(characterCreationManager.CharacterCreationContent.SelectedTitleType))
            {
                characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
            }
            List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            list.Add(new NarrativeMenuCharacterArgs("player_youth_character", 17, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
            MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
            if (equipment == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ERROR, could not find {playerEquipmentId}!", new Color(1,0,0)));
                equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_empire_guard_m");
            }
            ItemObject item = equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
            list.Add(new NarrativeMenuCharacterArgs("narrative_character_horse", -1, "", "act_inventory_idle_start", "spawnpoint_mount_1", equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, equipment.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed()), false, false));
            return list;
        }

        private void AddYouthMenu(CharacterCreationManager characterCreationManager)
        {
            TextObject description = CharacterObject.PlayerCharacter.IsFemale ? new TextObject("{=5kbeAC7k}In wartorn Calradia, especially in frontier or tribal areas, some women as well as men learn to fight from an early age. You...", null) : new TextObject("{=F7OO5SAa}As a youngster growing up in Calradia, war was never too far away. You...", null);
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            bodyProperties = TaleWorlds.Core.FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 17f);
            NarrativeMenuCharacter item = new("player_youth_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            NarrativeMenuCharacter item2 = new("narrative_character_horse");
            List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>
            {
                item,
                item2
            };
            NarrativeMenu narrativeMenu = new("narrative_youth_menu", "narrative_education_menu", "narrative_adulthood_menu", new TextObject("{=ok8lSW6M}Youth", null), description, list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetYouthMenuNarrativeMenuCharacterArgs));
            AddYouthMenuOptions(narrativeMenu);
            characterCreationManager.AddNewMenu(narrativeMenu);
        }

        private void AddYouthMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("youth_staff_first_option", new TextObject("{=CITG915d}joined a commander's staff.", null), new TextObject("{=wNHqFlDL}You were chosen by your superior officer to serve an imperial strategos as a courier. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthStaffOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthStaffOneOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthStaffOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
            NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("youth_staff_second_option", new TextObject("{=CITG915d}joined a commander's staff.", null), new TextObject("{=ANbNblaH}You were picked as the courier of the commander of the local forces. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthStaffOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthStaffTwoOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthStaffOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
            NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("youth_groom_option", new TextObject("{=bhE2i6OU}served as a baron's groom.", null), new TextObject("{=i3k7YtA8}You were chosen by a knight to accompany a minor baron of the Vlandian kingdom. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthGroomOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthGroomOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthGroomOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
            NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("youth_servant_first_option", new TextObject("{=F2bgujPo}were a chieftain's servant.", null), new TextObject("{=AXWO4C69}Your were choosen among others to accompany a chieftain of your people. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthServantOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthServantOneOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthServantOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
            NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("youth_servant_second_option", new TextObject("{=F2bgujPo}were a chieftain's servant.", null), new TextObject("{=neMCgMZM}Local wise man picked you to become the messenger of a chieftain of your people. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthServantOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthServantTwoOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthServantOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
            NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("youth_cavalry_option", new TextObject("{=h2KnarLL}trained with the cavalry.", null), new TextObject("{=7cHsIMLP}You could never have bought the equipment on your own, but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthCavalryOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
            NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("youth_hearth_option", new TextObject("{=zsC2t5Hb}trained with the hearth guard.", null), new TextObject("{=RmbWW6Bm}You were a big and imposing enough youth that the chief's guard allowed you to train alongside them, in preparation to join them some day.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthHearthOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthHearthOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthHearthOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
            NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("youth_guard_high_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthGuardHighRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
            NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("youth_guard_low_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=oR58iNDz}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardLowRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthGuardLowRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthGuardLowRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
            NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("youth_guard_garrisons_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=e6lINjFg}The garrisons spent most of their time guarding the town walls, and their training focused largely on missile weapons.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardGarrisonRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthGuardGarrisonRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthGuardGarrisonRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
            NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("youth_guard_empire_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=oR58iNDz}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardEmpireRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthGuardEmpireRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthGuardEmpireRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
            NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("youth_rider_high_register_option", new TextObject("{=VlXOgIX6}rode with the scouts.", null), new TextObject("{=888lmJqs}All of Calradia's kingdoms recognize the value of good light cavalry and horse archers, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a good bow so that you could fulfill their levy obligations.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthRiderHighRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
            NarrativeMenuOption narrativeMenuOption13 = new NarrativeMenuOption("youth_rider_low_register_option", new TextObject("{=VlXOgIX6}rode with the scouts.", null), new TextObject("{=sYuN6hPD}All of Calradia's kingdoms recognize the value of good light cavalry, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a sheaf of javelins so that you could fulfill their levy obligations.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderLowRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthRiderLowRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthRiderLowRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption13);
            NarrativeMenuOption narrativeMenuOption14 = new NarrativeMenuOption("youth_infantry_option", new TextObject("{=a8arFSra}trained with the infantry.", null), new TextObject("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Calradia.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthInfantryOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption14);
            NarrativeMenuOption narrativeMenuOption15 = new NarrativeMenuOption("youth_skirmisher_option", new TextObject("{=oMbOIPc9}joined the skirmishers.", null), new TextObject("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthSkirmisherOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthSkirmisherOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthSkirmisherOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption15);
            NarrativeMenuOption narrativeMenuOption16 = new NarrativeMenuOption("youth_kern_option", new TextObject("{=cDWbwBwI}joined the Arakhora.", null), new TextObject("{=tTb28jyU}Many young Elveans dream to fight as Arakhora, versatile troops who could both harass the enemy line with their javelins or join in the final screaming charge once it weakened. Masters of the wild, their higher ranks are famoues to ride dire wolves into battle.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthKernOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthKernOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthKernOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption16);
            NarrativeMenuOption narrativeMenuOption17 = new NarrativeMenuOption("youth_camp_option", new TextObject("{=GFUggps8}marched with the camp followers.", null), new TextObject("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs), new NarrativeMenuOptionOnConditionDelegate(YouthCampOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption17);
            NarrativeMenuOption narrativeMenuOption18 = new NarrativeMenuOption("youth_envoys_guard_first_option", new TextObject("{=YmPlLGXb}served as an envoy's guard", null), new TextObject("{=qPamcCkA}Your family arranged for you to accompany an envoy. You were not given major responsibilities - mostly carrying arms and trying to look imposing. - but it did give you a chance to travel a lot and socialise and see the world.", null), new GetNarrativeMenuOptionArgsDelegate(GetEnvoysGuardFirstOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EnvoysGuardFirstOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EnvoysGuardFirstOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption18);
            NarrativeMenuOption narrativeMenuOption19 = new NarrativeMenuOption("youth_envoys_guard_second_option", new TextObject("{=YmPlLGXb}served as an envoy's guard", null), new TextObject("{=VYU1nEHP}Your family arranged for you to accompany an envoy. You were not given major responsibilities but it did give you a chance to travel and socialise and see a bit of the world.", null), new GetNarrativeMenuOptionArgsDelegate(GetEnvoysGuardSecondOptionArgs), new NarrativeMenuOptionOnConditionDelegate(EnvoysGuardSecondOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(EnvoysGuardSecondOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption19);

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "wulf_youth_noble_guard_option",
                new TextObject("{rf_trained_with_noble_guard}trained with the noble guard.", null),
                new TextObject("{=wulf_noble_guard_desc}You served as part of a chieftain's noble guard. You learned mounted shock tactics and the discipline of heavy lances.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWulfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "wulf_youth_folks_guard_option",
                new TextObject("{rf_joined_folks_guard}joined the folks' guard", null),
                new TextObject("{=wulf_folks_guard_desc}You served in the people's guard of your clanhold. You trained with spears and short swords and learned to hold the line.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardGarrisonRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWulfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardGarrisonRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "wulf_youth_scouts_option",
                new TextObject("{rf_rode_with_the_scouts}rode with the scouts.", null),
                new TextObject("{=wulf_scouts_desc}You rode ahead of the host as a scout, learning to read terrain and strike quickly from cover.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWulfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "wulf_youth_fenhild_option",
                new TextObject("{rf_trained_with_fenhild}trained with the Fenhild.", null),
                new TextObject("{=wulf_fenhild_desc}Armed with massive two-handed swords, the Fenhild are the backbone of the Wulfen host, hailing from mist-shrouded marsh clans.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWulfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "wulf_youth_dunharth_wardens_option",
                new TextObject("{rf_joined_dunharth_wardens}joined the Dunharth Wardens.", null),
                new TextObject("{=wulf_wardens_desc}Raised in the shadow of the deepwood forts, the Dunharth Wardens patrol the ancient forest trails with bow and blade. Masters of terrain and ambush, they serve as the Wulfen’s eyes in the wild—unseen until the first arrow strikes.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWulfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));

            // Urkhai
            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "urkhai_youth_commander_army_option",
                new TextObject("{rf_trained_with_commanders_army}trained with the commander's army.", null),
                new TextObject("{=urkhai_commander_desc}You served in the warlord's host, drilling with heavy weapons and learning battlefield discipline. You rode with the heavy troop and learned to fight as part of a formed unit.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuUrkhaiOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "urkhai_youth_city_patrol_option",
                new TextObject("{rf_patrolled_cities}patrolled the cities.", null),
                new TextObject("{=urkhai_patrol_desc}You were assigned to patrol and defend the city walls and barracks. Most of your training focused on missile weapons and fortification upkeep.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuUrkhaiOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "urkhai_youth_scouts_option",
                new TextObject("{rf_joined_scouts}joined the scouts.", null),
                new TextObject("{=urkhai_scouts_desc}You rode out ahead of the host as a scout, learning to read terrain, ride light mounts, and strike quickly from range.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuUrkhaiOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "urkhai_youth_infantry_option",
                new TextObject("{=a8arFSra}trained with the infantry.", null),
                new TextObject("{=urkhai_infantry_desc}You trained as a tribal spearman — drilled in shield and spear or short sword — serving the host as a reliable footman.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuUrkhaiOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "urkhai_youth_healers_option",
                new TextObject("{rf_joined_healers}joined the healers.", null),
                new TextObject("{=urkhai_healers_desc}You apprenticed with the healers and learned to mend wounds and tend the sick, combining practical medicine and traditional remedies.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthSkirmisherOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuUrkhaiOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthSkirmisherOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "urkhai_youth_free_people_option",
                new TextObject("{=GFUggps8}marched with the free people.", null),
                new TextObject("{=urkhai_free_desc}You avoided formal enlistment and instead marched with free bands — followers, foragers, and irregulars of the host — learning guerrilla skills and survival.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuUrkhaiOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));

            // -- Giant-specific narrative options (localized short labels) --
            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "giant_youth_noble_guard_option",
                new TextObject("{rf_trained_with_noble_guard}trained with the noble guard.", null),
                new TextObject("{=giant_noble_guard_desc}You served as part of the noble guard, learning mounted tactics and the discipline of heavy lances.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuGiantOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "giant_youth_folks_guard_option",
                new TextObject("{rf_joined_folks_guard}joined the folks guard", null),
                new TextObject("{=giant_folks_guard_desc}You served in the people's guard and trained with missile and engineering skills useful for garrison duty.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuGiantOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "giant_youth_scouts_option",
                new TextObject("{rf_rode_with_the_scouts}rode with the scouts.", null),
                new TextObject("{=giant_scouts_desc}You scouted ahead of the host learning to read terrain and report enemy positions.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuGiantOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "giant_youth_lanzalith_option",
                new TextObject("{rf_trained_with_lanzalith}trained with the Lanzalith.", null),
                new TextObject("{=giant_lanzalith_desc}You drilled as part of spear-and-shield infantry, forming the backbone of the host.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuGiantOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "giant_youth_tzalquendlan_option",
                new TextObject("{rf_joined_tzalquendlan}joined the Tzalquendlan.", null),
                new TextObject("{=giant_tzalquendlan_desc}You learned ambush, disguise and irregular warfare among the border scouts.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuGiantOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));

            // -- Aqarun-specific narrative options (city patrol label localized) --
            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "aqarun_youth_cavalry_option",
                new TextObject("{=h2KnarLL}trained with the cavalry.", null),
                new TextObject("{=aqarun_cavalry_desc}You trained with the cavalry, learning mounted shock tactics and lance-work.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuAqarunOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "aqarun_youth_city_patrol_option",
                new TextObject("{rf_patrolled_cities}patrolled the cities.", null),
                new TextObject("{=aqarun_patrol_desc}You served on city patrols learning crossbow and engineering tasks for garrison duty.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuAqarunOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "aqarun_youth_desert_scouts_option",
                new TextObject("{rf_joined_desert_scouts}joined the desert scouts.", null),
                new TextObject("{=aqarun_scouts_desc}You scouted desert wastes and learned to strike quickly from range.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuAqarunOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "aqarun_youth_infantry_option",
                new TextObject("{=a8arFSra}trained with the infantry.", null),
                new TextObject("{=aqarun_infantry_desc}You trained as foot infantry, mastering spear-and-shield drills.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuAqarunOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "aqarun_youth_skirmishers_option",
                new TextObject("{=oMbOIPc9}joined the skirmishers.", null),
                new TextObject("{=aqarun_skirmishers_desc}You joined the skirmishers, fighting with javelin and light arms.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthSkirmisherOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuAqarunOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthSkirmisherOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "aqarun_youth_free_people_option",
                new TextObject("{=GFUggps8}marched with the free people.", null),
                new TextObject("{=aqarun_free_desc}You marched with irregular bands, learning raiding and survival skills.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuAqarunOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));

            // -- South Realm-specific narrative options --
            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "southrealm_youth_cavalry_option",
                new TextObject("{=h2KnarLL}trained with the cavalry.", null),
                new TextObject("{=southrealm_cavalry_desc}You trained with mounted troops, learning to fight as part of a mounted unit.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuSouthRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "southrealm_youth_city_patrol_option",
                new TextObject("{rf_patrolled_cities}patrolled the cities.", null),
                new TextObject("{=southrealm_patrol_desc}You performed city patrol duties and garrison tasks.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuSouthRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "southrealm_youth_scouts_option",
                new TextObject("{rf_joined_scouts}joined the scouts.", null),
                new TextObject("{=southrealm_scouts_desc}You scouted for the host, mastering terrain and ranged harassment.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuSouthRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "southrealm_youth_infantry_option",
                new TextObject("{=a8arFSra}trained with the infantry.", null),
                new TextObject("{=southrealm_infantry_desc}You trained as spearmen and foot troops in the host.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuSouthRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "southrealm_youth_skirmishers_option",
                new TextObject("{=oMbOIPc9}joined the skirmishers.", null),
                new TextObject("{=southrealm_skirmishers_desc}You fought as a skirmisher, relying on speed and missiles.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthSkirmisherOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuSouthRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthSkirmisherOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "southrealm_youth_free_people_option",
                new TextObject("{=GFUggps8}marched with the free people.", null),
                new TextObject("{=southrealm_free_desc}You marched with free bands and learned survival and raiding crafts.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuSouthRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));

            // -- West Realm-specific narrative options --
            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "westrealm_youth_cavalry_option",
                new TextObject("{=h2KnarLL}trained with the cavalry.", null),
                new TextObject("{=westrealm_cavalry_desc}You trained with mounted units, gaining lance and riding skill.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWestRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "westrealm_youth_city_patrol_option",
                new TextObject("{rf_patrolled_cities}patrolled the cities.", null),
                new TextObject("{=westrealm_patrol_desc}You patrolled towns, learning crossbow work and garrison duties.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWestRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "westrealm_youth_scouts_option",
                new TextObject("{rf_joined_scouts}joined the scouts.", null),
                new TextObject("{=westrealm_scouts_desc}You scouted the borderlands and learned to strike from cover.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWestRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "westrealm_youth_infantry_option",
                new TextObject("{=a8arFSra}trained with the infantry.", null),
                new TextObject("{=westrealm_infantry_desc}You were drilled as reliable infantry for the host.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWestRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "westrealm_youth_skirmishers_option",
                new TextObject("{=oMbOIPc9}joined the skirmishers.", null),
                new TextObject("{=westrealm_skirmishers_desc}You fought as a light skirmisher trained to harass and evade.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthSkirmisherOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWestRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthSkirmisherOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "westrealm_youth_free_people_option",
                new TextObject("{=GFUggps8}marched with the free people.", null),
                new TextObject("{=westrealm_free_desc}You learned irregular warfare and survival by marching with free bands.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuWestRealmOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));

            // -- Mage-specific narrative options --
            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "mage_youth_cavalry_option",
                new TextObject("{=h2KnarLL}trained with the cavalry.", null),
                new TextObject("{=mage_cavalry_desc}You trained with mounted troops and learned battlefield discipline.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuMageOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "mage_youth_patrol_option",
                new TextObject("{rf_patrolled_cities}patrolled the cities.", null),
                new TextObject("{=mage_patrol_desc}You performed city patrol and garrison duties.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuMageOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "mage_youth_scouts_option",
                new TextObject("{rf_joined_scouts}joined the scouts.", null),
                new TextObject("{=mage_scouts_desc}You served as a scout, learning to ride and shoot.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuMageOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "mage_youth_infantry_option",
                new TextObject("{=a8arFSra}trained with the infantry.", null),
                new TextObject("{=mage_infantry_desc}You trained as infantry and learned drill and cohesion.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuMageOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "mage_youth_scholars_option",
                new TextObject("{=oMbOIPc9}joined the scholars.", null),
                new TextObject("{=mage_scholars_desc}You studied scrolls and arcane lore among the kingdom's scholars.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthSkirmisherOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuMageOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthSkirmisherOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "mage_youth_free_people_option",
                new TextObject("{=GFUggps8}marched with the free people.", null),
                new TextObject("{=mage_free_desc}You learned to survive and scavenge by marching with irregular bands.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuMageOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));

            // -- Dwarf-specific narrative options --
            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "dwarf_youth_cavalry_option",
                new TextObject("{=h2KnarLL}trained with the cavalry.", null),
                new TextObject("{=dwarf_cavalry_desc}You trained with mounted troops and learned battlefield formation riding.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCavalryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuDwarfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCavalryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "dwarf_youth_city_patrol_option",
                new TextObject("{rf_patrolled_cities}patrolled the cities.", null),
                new TextObject("{=dwarf_patrol_desc}You served garrison and city patrol duties, specializing in crossbow and engineering tasks.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthGuardHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuDwarfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthGuardHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "dwarf_youth_scouts_option",
                new TextObject("{rf_joined_scouts}joined the scouts.", null),
                new TextObject("{=dwarf_scouts_desc}You scouted from fortified positions and learned to strike from range.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthRiderHighRegisterOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuDwarfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthRiderHighRegisterOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "dwarf_youth_infantry_option",
                new TextObject("{=a8arFSra}trained with the infantry.", null),
                new TextObject("{=dwarf_infantry_desc}You trained as spearmen and foot troops in the mountain host.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthInfantryOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuDwarfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthInfantryOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "dwarf_youth_scholars_option",
                new TextObject("{=oMbOIPc9}joined the scholars.", null),
                new TextObject("{=dwarf_scholars_desc}You apprenticed to scholars and craftsmen learning lore and craft.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthSkirmisherOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuDwarfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthSkirmisherOptionOnSelect),
                null));

            narrativeMenu.AddNarrativeMenuOption(new NarrativeMenuOption(
                "dwarf_youth_free_people_option",
                new TextObject("{=GFUggps8}marched with the free people.", null),
                new TextObject("{=dwarf_free_desc}You marched with free bands and learned practical survival and raiding skills.", null),
                new GetNarrativeMenuOptionArgsDelegate(GetYouthCampOptionArgs),
                new NarrativeMenuOptionOnConditionDelegate(YouthMenuDwarfOnCondition),
                new NarrativeMenuOptionOnSelectDelegate(YouthCampOptionOnSelect),
                null));
        }

        private void GetYouthStaffOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Steward,
                DefaultSkills.Tactics
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private bool YouthStaffOneOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
        }

        private bool YouthStaffTwoOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
        }

        private void YouthStaffOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_decisive");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthGroomOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Charm,
                DefaultSkills.Tactics
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private bool YouthGroomOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
        }

        private void YouthGroomOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthServantOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Steward,
                DefaultSkills.Tactics
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private bool YouthServantOneOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
        }

        private bool YouthServantTwoOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
        }

        private void YouthServantOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_ready");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthCavalryOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private bool YouthCavalryOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
        }

        private void YouthCavalryOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "mercenary";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_apprentice");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthHearthOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Polearm
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private bool YouthHearthOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
        }

        private void YouthHearthOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "mercenary";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthGuardHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private bool YouthGuardHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
        }

        private void YouthGuardHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_vibrant");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthGuardLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Bow,
                DefaultSkills.Engineering
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private bool YouthGuardLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
        }

        private void YouthGuardLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthGuardGarrisonRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Bow,
                DefaultSkills.Engineering
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private bool YouthGuardGarrisonRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
        }

        private void YouthGuardGarrisonRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthGuardEmpireRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Crossbow,
                DefaultSkills.Engineering
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
        }

        private bool YouthGuardEmpireRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
        }

        private void YouthGuardEmpireRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthRiderHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private bool YouthRiderHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            string id = characterCreationManager.CharacterCreationContent.SelectedCulture.StringId;
            return id == "empire" || id == "khuzait";
        }

        private void YouthRiderHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "hunter";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_sturgia_mp_warrior_axe");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthRiderLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Riding,
                DefaultSkills.Bow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
        }

        private bool YouthRiderLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
        }

        private void YouthRiderLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "hunter";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_sturgia_mp_huskarl_idle");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthInfantryOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Polearm,
                DefaultSkills.OneHanded
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
        }

        private bool YouthInfantryOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            string id = characterCreationManager.CharacterCreationContent.SelectedCulture.StringId;
            return id == "empire" || id == "vlandia" || id == "khuzait" || id == "aserai" || id == "battania" || id == "sturgia";
        }

        private void YouthInfantryOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "infantry";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_fierce");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthSkirmisherOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private bool YouthSkirmisherOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
        }

        private void YouthSkirmisherOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "skirmisher";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_fox");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthKernOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Throwing,
                DefaultSkills.OneHanded
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
        }

        private bool YouthKernOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
        }

        private void YouthKernOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "kern";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_apprentice");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetYouthCampOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Roguery,
                DefaultSkills.Throwing
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
        }

        private bool YouthCampOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            string id = characterCreationManager.CharacterCreationContent.SelectedCulture.StringId;
            return id == "vlandia" || id == "sturgia";
        }

        private void YouthCampOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "bard";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_militia");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void GetEnvoysGuardFirstOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Charm,
                DefaultSkills.Scouting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private void GetEnvoysGuardSecondOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Charm,
                DefaultSkills.Scouting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
        }

        private bool EnvoysGuardFirstOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
        }

        private bool EnvoysGuardSecondOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
        }

        private void EnvoysGuardFirstOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private void EnvoysGuardSecondOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_youth_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
                }
            }
        }

        private List<NarrativeMenuCharacterArgs> GetAdultMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
        {
            List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            list.Add(new NarrativeMenuCharacterArgs("player_adulthood_character", 20, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
            MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
            if (equipment == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ERROR, could not find {playerEquipmentId}!", new Color(1, 0, 0)));
                equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_empire_guard_m");
            }
            ItemObject item = equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
            list.Add(new NarrativeMenuCharacterArgs("narrative_character_horse", -1, "", "act_horse_stand_1", "spawnpoint_mount_1", equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, equipment.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed()), false, false));
            return list;
        }

        private void AddAdulthoodMenu(CharacterCreationManager characterCreationManager)
        {
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            bodyProperties = TaleWorlds.Core.FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 20f);
            NarrativeMenuCharacter item = new("player_adulthood_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            NarrativeMenuCharacter item2 = new("narrative_character_horse");
            List<NarrativeMenuCharacter> list = new()
            {
                item,
                item2
            };
            MBTextManager.SetTextVariable("EXP_VALUE", _skillLevelToAdd);
            NarrativeMenu narrativeMenu = new("narrative_adulthood_menu", "narrative_youth_menu", "narrative_age_selection_menu", new TextObject("{=MafIe9yI}Young Adulthood", null), new TextObject("{=4WYY0X59}Before you set out for a life of adventure, your biggest achievement was...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetAdultMenuNarrativeMenuCharacterArgs));
            AddAdulthoodMenuOptions(narrativeMenu);
            characterCreationManager.AddNewMenu(narrativeMenu);
        }

        private void AddAdulthoodMenuOptions(NarrativeMenu narrativeMenu)
        {
            NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("adulthood_defeated_enemy_option", new TextObject("{=8bwpVpgy}you defeated an enemy in battle.", null), new TextObject("{=1IEroJKs}Not everyone who musters for the levy marches to war, and not everyone who goes on campaign sees action. You did both, and you also took down an enemy warrior in direct one-to-one combat, in the full view of your comrades.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodDefeatedEnemyOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodDefeatedEnemyOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodDefeatedEnemyOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
            NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("adulthood_manhunt_option", new TextObject("{=mP3uFbcq}you led a successful manhunt.", null), new TextObject("{=4f5xwzX0}When your community needed to organize a posse to pursue horse thieves, you were the obvious choice. You hunted down the raiders, surrounded them and forced their surrender, and took back your stolen property.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodManhuntOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodManhuntOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodManhuntOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
            NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("adulthood_caravan_leader_option", new TextObject("{=wfbtS71d}you led a caravan.", null), new TextObject("{=joRHKCkm}Your family needed someone trustworthy to take a caravan to a neighboring town. You organized supplies, ensured a constant watch to keep away bandits, and brought it safely to its destination.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodCaravanLeaderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodCaravanLeaderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodCaravanLeaderOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
            NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("adulthood_saved_village_option", new TextObject("{=x1HTX5hq}you saved your village from a flood.", null), new TextObject("{=bWlmGDf3}When a sudden storm caused the local stream to rise suddenly, your neighbors needed quick-thinking leadership. You provided it, directing them to build levees to save their homes.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodSavedVillageOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodSavedVillageOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodSavedVillageOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
            NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("adulthood_saved_city_option", new TextObject("{=s8PNllPN}you saved your city quarter from a fire.", null), new TextObject("{=ZAGR6PYc}When a sudden blaze broke out in a back alley, your neighbors needed quick-thinking leadership and you provided it. You organized a bucket line to the nearest well, putting the fire out before any homes were lost.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodSavedCityOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodSavedCityOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodSavedCityOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
            NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("adulthood_workshop_option", new TextObject("{=xORjDTal}you invested some money in a workshop.", null), new TextObject("{=PyVqDLBu}Your parents didn't give you much money, but they did leave just enough for you to secure a loan against a larger amount to build a small workshop. You paid back what you borrowed, and sold your enterprise for a profit.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodWorkshopOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodWorkshopOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodWorkshopOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
            NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("adulthood_investor_option", new TextObject("{=xKXcqRJI}you invested some money in land.", null), new TextObject("{=cbF9jdQo}Your parents didn't give you much money, but they did leave just enough for you to purchase a plot of unused land at the edge of the village. You cleared away rocks and dug an irrigation ditch, raised a few seasons of crops, than sold it for a considerable profit.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodInvestorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodInvestorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodInvestorOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
            NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("adulthood_hunter_option", new TextObject("{=TbNRtUjb}you hunted a dangerous animal.", null), new TextObject("{=I3PcdaaL}Wolves, bears are a constant menace to the flocks of northern Calradia, while hyenas and leopards trouble the south. You went with a group of your fellow villagers and fired the missile that brought down the beast.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodHunterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodHunterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodHunterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
            NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("adulthood_siege_survivor_option", new TextObject("{=WbHfGCbd}you survived a siege.", null), new TextObject("{=FhZPjhli}Your hometown was briefly placed under siege, and you were called to defend the walls. Everyone did their part to repulse the enemy assault, and everyone is justly proud of what they endured.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodSiegeSurvivorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodSiegeSurvivorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodSiegeSurvivorOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
            NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("adulthood_escapade_high_register_option", new TextObject("{=kNXet6Um}you had a famous escapade in town.", null), new TextObject("{=DjeAJtix}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, on one of your trips into town you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodEscapadeHighRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodEscapadeHighRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodEscapadeHighRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
            NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("adulthood_escapade_low_register_option", new TextObject("{=qlOuiKXj}you had a famous escapade.", null), new TextObject("{=lD5Ob3R4}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodEscapadeLowRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodEscapadeLowRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodEscapadeLowRegisterOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
            NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("adulthood_nice_person_option", new TextObject("{=Yqm0Dics}you treated people well.", null), new TextObject("{=dDmcqTzb}Yours wasn't the kind of reputation that local legends are made of, but it was the kind that wins you respect among those around you. You were consistently fair and honest in your business dealings and helpful to those in trouble. In doing so, you got a sense of what made people tick.", null), new GetNarrativeMenuOptionArgsDelegate(GetAdulthoodNicePersonOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AdulthoodNicePersonOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AdulthoodNicePersonOptionOnSelect), null);
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
        }

        private void GetAdulthoodDefeatedEnemyOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.OneHanded,
                DefaultSkills.TwoHanded
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Valor
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(20);
        }

        private bool AdulthoodDefeatedEnemyOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void AdulthoodDefeatedEnemyOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
                }
            }
        }

        private void GetAdulthoodManhuntOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Calculating
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(10);
        }

        private bool AdulthoodManhuntOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && (characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait");
        }

        private void AdulthoodManhuntOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_battania_mp_clan_warrior_shieldperk_idle");
                }
            }
        }

        private void GetAdulthoodCaravanLeaderOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Trade,
                DefaultSkills.Leadership
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Calculating
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(10);
        }

        private bool AdulthoodCaravanLeaderOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && (characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord");
        }

        private void AdulthoodCaravanLeaderOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_ready_handshield");
                }
            }
        }

        private void GetAdulthoodSavedVillageOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Valor
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(10);
        }

        private bool AdulthoodSavedVillageOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && (characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord");
        }

        private void AdulthoodSavedVillageOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_drafted_to_war_pose");
                }
            }
        }

        private void GetAdulthoodSavedCityOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Tactics,
                DefaultSkills.Leadership
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Calculating
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(10);
        }

        private bool AdulthoodSavedCityOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
        }

        private void AdulthoodSavedCityOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_vibrant");
                }
            }
        }

        private void GetAdulthoodWorkshopOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Trade,
                DefaultSkills.Crafting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Calculating
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(10);
        }

        private bool AdulthoodWorkshopOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void AdulthoodWorkshopOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_decisive");
                }
            }
        }

        private void GetAdulthoodInvestorOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Trade,
                DefaultSkills.Crafting
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Calculating
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(10);
        }

        private bool AdulthoodInvestorOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void AdulthoodInvestorOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_decisive");
                }
            }
        }

        private void GetAdulthoodHunterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Polearm,
                DefaultSkills.Bow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Valor
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(5);
        }

        private bool AdulthoodHunterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void AdulthoodHunterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_tough");
                }
            }
        }

        private void GetAdulthoodSiegeSurvivorOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Bow,
                DefaultSkills.Crossbow
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
            args.SetRenownToAdd(5);
        }

        private bool AdulthoodSiegeSurvivorOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void AdulthoodSiegeSurvivorOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_tough");
                }
            }
        }

        private void GetAdulthoodEscapadeHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Athletics,
                DefaultSkills.Roguery
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Valor
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(5);
        }

        private bool AdulthoodEscapadeHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void AdulthoodEscapadeHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_clever");
                }
            }
        }

        private void GetAdulthoodEscapadeLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Athletics,
                DefaultSkills.Roguery
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Valor
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(5);
        }

        private bool AdulthoodEscapadeLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
        }

        private void AdulthoodEscapadeLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_clever");
                }
            }
        }

        private void GetAdulthoodNicePersonOptionArgs(NarrativeMenuOptionArgs args)
        {
            SkillObject[] affectedSkills = new SkillObject[]
            {
                DefaultSkills.Charm,
                DefaultSkills.Steward
            };
            args.SetAffectedSkills(affectedSkills);
            args.SetFocusToSkills(_focusToAdd);
            args.SetLevelToSkills(_skillLevelToAdd);
            args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
            TraitObject[] affectedTraits = new TraitObject[]
            {
                DefaultTraits.Mercy,
                DefaultTraits.Generosity,
                DefaultTraits.Honor
            };
            args.SetAffectedTraits(affectedTraits);
            args.SetLevelToTraits(1);
            args.SetRenownToAdd(5);
        }

        private bool AdulthoodNicePersonOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void AdulthoodNicePersonOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_adulthood_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_manners");
                }
            }
        }

        private List<NarrativeMenuCharacterArgs> GetAgeSelectionMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
        {
            List<NarrativeMenuCharacterArgs> list = new();
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            list.Add(new NarrativeMenuCharacterArgs("player_age_selection_character", characterCreationManager.CharacterCreationContent.StartingAge, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
            MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
            if (equipment == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ERROR, could not find {playerEquipmentId}!", new Color(1, 0, 0)));
                equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_empire_guard_m");
            }

            ItemObject item = equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
            list.Add(new NarrativeMenuCharacterArgs("narrative_character_horse", -1, "", "act_horse_stand_1", "spawnpoint_mount_1", equipment.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, equipment.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed()), false, false));
            return list;
        }

        private void AddAgeSelectionMenu(CharacterCreationManager characterCreationManager)
        {
            MBTextManager.SetTextVariable("EXP_VALUE", _skillLevelToAdd);
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            bodyProperties = TaleWorlds.Core.FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, characterCreationManager.CharacterCreationContent.StartingAge);
            NarrativeMenuCharacter item = new("player_age_selection_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            NarrativeMenuCharacter item2 = new("narrative_character_horse");
            List<NarrativeMenuCharacter> list = new()
            {
                item,
                item2
            };
            NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_age_selection_menu", "narrative_adulthood_menu", "rf_start", new TextObject("{=HDFEAYDk}Starting Age", null), new TextObject("{=VlOGrGSn}Your character started off on the adventuring path at the age of...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(GetAgeSelectionMenuNarrativeMenuCharacterArgs));
            NarrativeMenuOption narrativeMenuOption = new("age_selection_young_adult_option", new TextObject("{=!}20", null), new TextObject("{=2k7adlh7}While lacking experience a bit, you are full with youthful energy, you are fully eager, for the long years of adventuring ahead.", null), new GetNarrativeMenuOptionArgsDelegate(GetAgeSelectionYoungAdultAgeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AgeSelectionYoungAdultAgeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AgeSelectionYoungAdultAgeOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(AgeSelectionYoungAdultAgeOptionOnConsequence));
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
            NarrativeMenuOption narrativeMenuOption2 = new("age_selection_adult_option", new TextObject("{=!}30", null), new TextObject("{=NUlVFRtK}You are at your prime, You still have some youthful energy but also have a substantial amount of experience under your belt. ", null), new GetNarrativeMenuOptionArgsDelegate(GetAgeSelectionAdultOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AgeSelectionAdultOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AgeSelectionAdultOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(AgeSelectionAdultOptionOnConsequence));
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
            NarrativeMenuOption narrativeMenuOption3 = new("age_selection_middle_age_option", new TextObject("{=!}40", null), new TextObject("{=5MxTYApM}is the right age for starting off, you have years of experience, and you are old enough for people to respect you and gather under your banner.", null), new GetNarrativeMenuOptionArgsDelegate(GetAgeSelectionMiddleAgeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AgeSelectionMiddleAgeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AgeSelectionMiddleAgeOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(AgeSelectionMiddleAgeOptionOnConsequence));
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
            NarrativeMenuOption narrativeMenuOption4 = new("age_selection_elder_option", new TextObject("{=!}50", null), new TextObject("{=ePD5Afvy}While you are past your prime, there is still enough time to go on that last big adventure for you. And you have all the experience you need to overcome anything!", null), new GetNarrativeMenuOptionArgsDelegate(GetAgeSelectionElderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(AgeSelectionElderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(AgeSelectionElderOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(AgeSelectionElderOptionOnConsequence));
            narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
            characterCreationManager.AddNewMenu(narrativeMenu);
        }

        private void GetAgeSelectionYoungAdultAgeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetUnspentFocusToAdd(2);
            args.SetUnspentAttributeToAdd(1);
        }

        private bool AgeSelectionYoungAdultAgeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void AgeSelectionYoungAdultAgeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_age_selection_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_focus");
                    narrativeMenuCharacter.ChangeAge(20f);
                    MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
                    if (equipment == null)
                    {
                        Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionYoungAdultAgeOptionOnSelect", 4884);
                        equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
                    }
                    narrativeMenuCharacter.SetEquipment(equipment);
                    break;
                }
            }
            characterCreationManager.CharacterCreationContent.StartingAge = 20;
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-20f));
        }

        private void AgeSelectionYoungAdultAgeOptionOnConsequence(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.StartingAge = 20;
            ApplyMainHeroEquipment(characterCreationManager);
        }

        private void GetAgeSelectionAdultOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetUnspentFocusToAdd(4);
            args.SetUnspentAttributeToAdd(2);
        }

        private bool AgeSelectionAdultOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void AgeSelectionAdultOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_age_selection_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
                    narrativeMenuCharacter.ChangeAge(30f);
                    MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
                    if (equipment == null)
                    {
                        Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionAdultOptionOnSelect", 4934);
                        equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
                    }
                    narrativeMenuCharacter.SetEquipment(equipment);
                    break;
                }
            }
            characterCreationManager.CharacterCreationContent.StartingAge = 30;
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-30f));
        }

        private void AgeSelectionAdultOptionOnConsequence(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.StartingAge = 30;
            ApplyMainHeroEquipment(characterCreationManager);
        }

        private void GetAgeSelectionMiddleAgeOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetUnspentFocusToAdd(6);
            args.SetUnspentAttributeToAdd(3);
        }

        private bool AgeSelectionMiddleAgeOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void AgeSelectionMiddleAgeOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_age_selection_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
                    narrativeMenuCharacter.ChangeAge(30f);
                    MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
                    if (equipment == null)
                    {
                        Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionMiddleAgeOptionOnSelect", 4984);
                        equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
                    }
                    narrativeMenuCharacter.SetEquipment(equipment);
                    break;
                }
            }
            characterCreationManager.CharacterCreationContent.StartingAge = 40;
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-40f));
        }

        private void AgeSelectionMiddleAgeOptionOnConsequence(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.StartingAge = 40;
            ApplyMainHeroEquipment(characterCreationManager);
        }

        private void GetAgeSelectionElderOptionArgs(NarrativeMenuOptionArgs args)
        {
            args.SetUnspentFocusToAdd(8);
            args.SetUnspentAttributeToAdd(4);
        }

        private bool AgeSelectionElderOptionOnCondition(CharacterCreationManager characterCreationManager)
        {
            return true;
        }

        private void AgeSelectionElderOptionOnSelect(CharacterCreationManager characterCreationManager)
        {
            string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
            foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
            {
                if (narrativeMenuCharacter.StringId == "player_age_selection_character")
                {
                    narrativeMenuCharacter.SetAnimationId("act_childhood_tough");
                    narrativeMenuCharacter.ChangeAge(50f);
                    MBEquipmentRoster equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
                    if (equipment == null)
                    {
                        Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionElderOptionOnSelect", 5034);
                        equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
                    }
                    narrativeMenuCharacter.SetEquipment(equipment);
                    break;
                }
            }
            characterCreationManager.CharacterCreationContent.StartingAge = 50;
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-50f));
        }

        private void AgeSelectionElderOptionOnConsequence(CharacterCreationManager characterCreationManager)
        {
            characterCreationManager.CharacterCreationContent.StartingAge = 50;
            ApplyMainHeroEquipment(characterCreationManager);
        }

        private void ApplyMainHeroEquipment(CharacterCreationManager characterCreationManager)
        {
            NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_age_selection_menu");
            NarrativeMenuCharacter? narrativeMenuCharacter = null;
            foreach (NarrativeMenuCharacter narrativeMenuCharacter2 in narrativeMenuWithId.Characters)
            {
                if (narrativeMenuCharacter2.StringId.Equals("player_age_selection_character"))
                {
                    narrativeMenuCharacter = narrativeMenuCharacter2;
                    break;
                }
            }
            CharacterObject.PlayerCharacter.Equipment.FillFrom(narrativeMenuCharacter.Equipment.DefaultEquipment, true);
            CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(narrativeMenuCharacter.Equipment.GetRandomCivilianEquipment(), true);
        }

        public void SetHeroAge(float age)
        {
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-age));
        }

        private readonly IReadOnlyDictionary<string, string> _occupationToEquipmentMapping = new Dictionary<string, string>
        {
            { "retainer", "retainer" },
            { "bard", "bard" },
            { "hunter", "hunter" },
            { "farmer", "farmer" },
            { "herder", "herder" },
            { "healer", "healer" },
            { "mercenary", "mercenary" },
            { "infantry", "infantry" },
            { "skirmisher", "skirmisher" },
            { "kern", "kern" },
            { "guard", "guard" },
            { "retainer_urban", "retainer" },
            { "mercenary_urban", "mercenary" },
            { "merchant_urban", "merchant" },
            { "vagabond_urban", "vagabond" },
            { "artisan_urban", "artisan" },
            { "physician_urban", "physician" },
            { "healer_urban", "healer" },
            { "bard_urban", "bard" }
        };
        private const int ChildhoodAge = 7;

        private const int EducationAge = 12;

        private const int YouthAge = 17;

        private const int AccomplishmentAge = 20;

        private const int ParentAge = 33;

        private const int YoungAdultAge = 20;

        private const int AdultAge = 30;

        private const int MiddleAge = 40;

        private const int ElderAge = 50;

        public const int FocusToAddYouthStart = 2;

        public const int FocusToAddAdultStart = 4;

        public const int FocusToAddMiddleAgedStart = 6;

        public const int FocusToAddElderlyStart = 8;

        public const int AttributeToAddYouthStart = 1;

        public const int AttributeToAddAdultStart = 2;

        public const int AttributeToAddMiddleAgedStart = 3;

        public const int AttributeToAddElderlyStart = 4;

        public const string MotherNarrativeCharacterStringId = "mother_character";

        public const string FatherNarrativeCharacterStringId = "father_character";

        public const string PlayerChildhoodCharacterStringId = "player_childhood_character";

        public const string PlayerEducationCharacterStringId = "player_education_character";

        public const string PlayerYouthCharacterStringId = "player_youth_character";

        public const string PlayerAdulthoodCharacterStringId = "player_adulthood_character";

        public const string PlayerAgeSelectionCharacterStringId = "player_age_selection_character";

        public const string HorseNarrativeCharacterStringId = "narrative_character_horse";

        private int _focusToAdd = 1;

        private int _skillLevelToAdd = 10;

        private int _attributeLevelToAdd = 1;
        private Equipment GetMaleEquipment(IEnumerable<Equipment> eq) { return eq.FirstOrDefault(); }
        private Equipment GetFemaleEquipment(IEnumerable<Equipment> eq) { return eq.LastOrDefault(); }
        private string ResolveStartEquipmentId(string equipmentId)
        {
            return equipmentId.Replace("{sex}", CharacterObject.PlayerCharacter.IsFemale ? "f" : "m");
        }

        protected void ChooseCharacterEquipment(CharacterCreationManager characterCreationManager, StartType startType)
        {
            MBEquipmentRoster equipmentRoster;
            try
            {
                string equipmentId = ResolveStartEquipmentId(CharacterCreationConfig.mainHeroStartingEquipment[startType][Hero.MainHero.Culture.StringId]);
                equipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(equipmentId);
                if (equipmentRoster == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Missing start equipment roster: {equipmentId}", new Color(255, 0, 0)));
                    return;
                }

                IEnumerable<Equipment> battleEquipments = equipmentRoster.GetBattleEquipments();
                IEnumerable<Equipment> civillianEquipments = equipmentRoster.GetCivilianEquipments();
                Equipment battleEquipment = CharacterObject.PlayerCharacter.IsFemale ? GetFemaleEquipment(battleEquipments) : GetMaleEquipment(battleEquipments);
                Equipment civillianEquipment = CharacterObject.PlayerCharacter.IsFemale ? GetFemaleEquipment(civillianEquipments) : GetMaleEquipment(civillianEquipments);
                if (battleEquipment != null)
                {
                    CharacterObject.PlayerCharacter.Equipment.FillFrom(battleEquipment, true);
                    CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(civillianEquipment, true);
                    CharacterObject.PlayerCharacter.FirstBattleEquipment.FillFrom(battleEquipment);
                    characterCreationManager.CurrentMenu.Characters[0].SetEquipment(equipmentRoster);
                    //ChangePlayerMount(characterCreation, Hero.MainHero);
                }
                if (civillianEquipment != null) CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(civillianEquipment);
            }
            catch
            {
                InformationManager.DisplayMessage(new InformationMessage("Error while giving player the equipment", new Color(255, 0, 0)));
            }
        }

        private bool YouthMenuWulfOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "wulf";
        }
        private bool YouthMenuUrkhaiOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "urkhai";
        }
        private bool YouthMenuGiantOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "giant";
        }

        private bool YouthMenuAqarunOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aqarun";
        }

        private bool YouthMenuSouthRealmOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "south_realm";
        }

        private bool YouthMenuWestRealmOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "west_realm";
        }

        private bool YouthMenuMageOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "mage";
        }

        private bool YouthMenuDwarfOnCondition(CharacterCreationManager characterCreationManager)
        {
            return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "dwarf";
        }
        private static class CharacterOccupationTypes
        {
            public static bool IsUrbanOccupation(string occupation)
            {
                return occupation == "retainer_urban" || occupation == "mercenary_urban" || occupation == "merchant_urban" || occupation == "vagabond_urban" || occupation == "artisan_urban" || occupation == "physician_urban" || occupation == "healer_urban" || occupation == "bard_urban";
            }

            public const string Retainer = "retainer";

            public const string Bard = "bard";

            public const string Hunter = "hunter";

            public const string Farmer = "farmer";

            public const string Herder = "herder";

            public const string Healer = "healer";

            public const string Mercenary = "mercenary";

            public const string Infantry = "infantry";

            public const string Skirmisher = "skirmisher";

            public const string Kern = "kern";

            public const string Guard = "guard";

            public const string RetainerUrban = "retainer_urban";

            public const string MercenaryUrban = "mercenary_urban";

            public const string MerchantUrban = "merchant_urban";

            public const string VagabondUrban = "vagabond_urban";

            public const string ArtisanUrban = "artisan_urban";

            public const string PhysicianUrban = "physician_urban";

            public const string HealerUrban = "healer_urban";

            public const string BardUrban = "bard_urban";
        }
    }
}
