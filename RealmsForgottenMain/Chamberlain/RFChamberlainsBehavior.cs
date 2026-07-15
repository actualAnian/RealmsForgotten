using HarmonyLib;
using Helpers;
using RealmsForgotten.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Chamberlain
{
    internal class RFChamberlainsBehavior : CampaignBehaviorBase
    {
        static readonly MethodInfo AllEquipmentsGetter = AccessTools.PropertyGetter(typeof(BasicCharacterObject), "AllEquipments");
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(AddHouseTroopMenu));
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, new Action(SaveChangeToXml));
        }
        private void InitXml()
        {
            xmlManager = new HouseTroopsXmlManager();
            xmlManager.SetCampaignFilePath(Campaign.Current);
            xmlManager.SetCampaignHouseTroopXml();
            xmlManager.CreateXmlIfNeeded();
            InitAllUnitSetting();
        }
        private void InitAllUnitSetting()
        {
            List<string> troopIds = xmlManager.GetAllHouseTroopId();
            foreach (string troopId in troopIds)
            {
                CharacterObject unit = GetCharacterObject(troopId);
                if (unit != null)
                {
                    selectedUnit = unit;
                    // Update the unit's culture to the player's culture
                    ((BasicCharacterObject)unit).Culture = Hero.MainHero.Culture;
                    InitUnitName(unit);
                    InitUnitSkill(unit);
                    InitUnitEquipment(unit);
                    selectedUnit = null;
                }
            }
        }
        private void InitUnitName(CharacterObject unit)
        {
            ChangeUnitName(unit, HouseTroopsXmlManager.GetUnitName(xmlManager.GetCampaignXml(), unit.StringId));
        }
        private void InitUnitSkill(CharacterObject unit)
        {
            List<HouseTroopsSkillRecord> skillRecord = HouseTroopsXmlManager.GetUnitSkill(xmlManager.GetCampaignXml(), unit.StringId);
            foreach (SkillObject skill in Skills.All.ToList())
            {
                HouseTroopsSkillRecord record = skillRecord.FirstOrDefault(x => x.skill.Equals(skill.StringId));
                if (record != null)
                {
                    SetSkillValue(unit, skill, record.value);
                }
            }
        }
        private void InitUnitEquipment(CharacterObject unit)
        {
            List<HouseTroopsEquipmentRecord> records = HouseTroopsXmlManager.GetUnitEquipmentId(xmlManager.GetCampaignXml(), unit.StringId);
            CreateEmptyEquipmentForInit(unit);
            for (int i = 0; i < records.Count; i++)
            {
                ItemObject item = GetItemObject(records[i].itemId);
                if (item != null)
                {
                    ChangeUnitEquipment(selectedUnit, records[i].index, item, records[i].equipmentSet, records[i].isCivilan);
                }
            }
        }
        private void SaveChangeToXml()
        {
            string path = xmlManager.GetCampaignFilePath();
            if (!string.IsNullOrEmpty(path))
            {
                xmlManager.SaveXml(path);
            }
        }
        private bool isUnitSelected()
        {
            return selectedUnit != null;
        }
        private void AddHouseTroopMenu(CampaignGameStarter campaignGameStarter)
        {
            InitXml();
            campaignGameStarter.AddGameMenuOption("town_keep", "house_troop_keep", "Visit the Chamberlain's Chambers", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                return IsInKingdom() && IsTown() && IsOwnedSettlement();
            }, delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("house_troop_menu");
            }, false, 1, false, null);
            campaignGameStarter.AddGameMenuOption("castle", "house_troop_keep", "Visit the Chamberlain's Chambers", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                return IsInKingdom() && IsCastle() && IsOwnedSettlement();
            }, delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("house_troop_menu");
            }, false, 1, false, null);

            campaignGameStarter.AddGameMenu("house_troop_menu", "{=house_troop_menu}Chamberlain's Roster\n{unit_info}", delegate (MenuCallbackArgs args)
            {
                UpdateTextVariables();
            }, GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);

            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_recruit", "Recruit House Troops",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    return true;
                },
                args => RecruitHouseTroops(),
                false, -1, false, null);

            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_select", "Edit House Army", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                DisplayUnitList();
            }, false, -1, false, null);

            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_rename_unit", "Rename House Troop", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return isUnitSelected();
            }, delegate (MenuCallbackArgs args)
            {
                GetInputName();
            }, false, -1, false, null);
            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_change_gender", "Change troop gender", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return isUnitSelected();
            }, delegate (MenuCallbackArgs args)
            {
                ChangeTroopGender();
                AfterChange();
            }, false, -1, false, null);
            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_change_race", "Change troop race", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return isUnitSelected();
            }, delegate (MenuCallbackArgs args)
            {
                ChangeTroopRace();
            }, false, -1, false, null);
            AddExportImportButtons(campaignGameStarter);

            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_leave", "Leave chambers", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                selectedUnit = null;
                if (IsTown())
                {
                    GameMenu.SwitchToMenu("town_keep");
                }
                else if (IsCastle())
                {
                    GameMenu.SwitchToMenu("castle");
                }
            }, false, 0, false, null);

            ShowManageEquipmentOption(campaignGameStarter);
        }
        private void ShowManageEquipmentOption(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_select_equipment_set", "{=house_troop_menu_select_equipment_set}Select Equipment Set", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return isUnitSelected();
            }, delegate (MenuCallbackArgs args)
            {
                SelectEquipmentSet();
            }, false, -1, false, null);

            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_manage_equipment", "{=house_troop_menu_manage_equipment}Manage Equipment", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return isUnitSelected();
            }, delegate (MenuCallbackArgs args)
            {
                if (set > -1)
                {
                    InventoryScreenForEquipmentSelection();
                }
                else
                {
                    DisplayMessage("{=house_troop_menu_invalid_set}Choose or create an Equipment Set for your House Troop first.");
                }
            }, false, -1, false, null);

            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_delete_equipment_set", "{=house_troop_menu_delete_equipment_set}Delete Equipment Set", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                return isUnitSelected() && set > -1;
            }, delegate (MenuCallbackArgs args)
            {
                DeleteEquipmentSet(selectedUnit);
                UpdateTextVariables();
            }, false, -1, false, null);
        }
        private void RecruitHouseTroops()
        {
            List<CharacterObject> troops = ChamberlainConfig.PurchasableUnitList.Select(MBObjectManager.Instance.GetObject<CharacterObject>).ToList();

            string title = new TextObject("Recruit Troops", null).ToString();

            List<InquiryElement> options = troops.Select(troop =>
            {
                int troopCost = CalculateHouseTroopCost(troop);
                return new InquiryElement(troop, troop.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(troop)),
                    Hero.MainHero.Gold >= troopCost, // Can afford condition
                    new TextObject("{=!}{GOLD_ICON}" + troopCost).ToString() // Tooltip showing cost
                );
            }).ToList();

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    title,
                    string.Empty,
                    options,
                    true,
                    1,
                    1,
                    GameTexts.FindText("str_done", null).ToString(),
                    GameTexts.FindText("str_cancel", null).ToString(),
                    elements => OnHouseTroopTypeSelected(elements),
                    null,
                    string.Empty,
                    false
                ),
                false,
                false
            );
        }
        private void OnHouseTroopQuantitySelected(List<InquiryElement> selectedElements, CharacterObject selectedTroop)
        {
            int quantity = (int)selectedElements.First().Identifier;
            int troopCost = CalculateHouseTroopCost(selectedTroop);
            int totalCost = quantity * troopCost;

            if (Hero.MainHero.Gold >= totalCost)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -totalCost);

                TroopRoster roster = MobileParty.MainParty.MemberRoster;
                roster.AddToCounts(selectedTroop, quantity);

                InformationManager.DisplayMessage(new InformationMessage($"{quantity} {selectedTroop.Name} recruited to your party for {totalCost} Gold Coins."));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("You cannot afford to recruit your troops."));
            }
        }
        private int CalculateHouseTroopCost(CharacterObject troop)
        {
            int baseCost = 100;
            List<string> houseTroopId = xmlManager.GetAllHouseTroopId();
            int index = houseTroopId.IndexOf(troop.StringId);

            if (index == -1)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Warning: Troop '{troop.StringId}' not found in CORE_UNIT_LIST."));
                return baseCost;
            }

            return baseCost + index * 100;
        }
        private void OnHouseTroopTypeSelected(List<InquiryElement> selectedElements)
        {
            CharacterObject selectedTroop = selectedElements.First().Identifier as CharacterObject;

            if (selectedTroop == null)
            {
                return;
            }

            string inquiryText = new TextObject("How many of your {TROOP}'s do you want to recruit?", null)
                .SetTextVariable("TROOP", selectedTroop.Name)
                .ToString();

            var inquiryElements = new int[] { 1, 5, 10, 25, 50 }
                .Select(i => new InquiryElement(i, i.ToString(), null))
                .ToList();

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                inquiryText,
                string.Empty,
                inquiryElements,
                true,
                1,
                1,
                GameTexts.FindText("str_done", null).ToString(),
                GameTexts.FindText("str_cancel", null).ToString(),
                l => OnHouseTroopQuantitySelected(l, selectedTroop),
                null,
                string.Empty,
                false
            ));
        }
        private void GetInputName()
        {
            TextObject title = new TextObject("Select", null);
            TextObject info = new TextObject("Choose your new House Troop's name", null);
            TextObject confirm = new TextObject("Confirm", null);
            TextObject cancel = new TextObject("Cancel", null);
            InformationManager.ShowTextInquiry(new TextInquiryData(title.ToString(), info.ToString(), true, true, confirm.ToString(), cancel.ToString(), delegate (string name)
            {
                RenameUnit(name);
                AfterChange();
            }, delegate ()
            {
                InformationManager.HideInquiry();
            }, false, null, "", ""), false, false);
        }

        private void ChangeTroopRace()
        {
            List<string> races = ChamberlainConfig.PossibleTroopRaces;
            string title = new TextObject("Change race", null).ToString();
            List<InquiryElement> options = races.Select(race => {return new InquiryElement(race, race, new EmptyImageIdentifier(), true, null);}).ToList();
            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    title,
                    string.Empty,
                    options,
                    true,
                    1,
                    1,
                    GameTexts.FindText("str_done", null).ToString(),
                    GameTexts.FindText("str_cancel", null).ToString(),
                    elements => OnRaceSelected(elements),
                    null,
                    string.Empty,
                    false
                ),
                false,
                false
            );
        }

        private void OnRaceSelected(List<InquiryElement> elements)
        {
            string raceName = (string)elements[0].Identifier;
            selectedUnit.Race = RaceManager.Instance.GetRaceIdFromName(raceName);
            xmlManager.ChangeTroopRace(xmlManager.GetCampaignXml(), selectedUnit.StringId, raceName);
            SaveChangeToXml();
            AfterChange();
        }

        private void ChangeTroopGender()
        {
            selectedUnit.IsFemale = !selectedUnit.IsFemale;
            xmlManager.ChangeTroopGender(xmlManager.GetCampaignXml(), selectedUnit.StringId);
            SaveChangeToXml();
        }
        private void RenameUnit(string name)
        {
            ChangeUnitName(selectedUnit, name);
            xmlManager.RenameUnit(xmlManager.GetCampaignXml(), selectedUnit.StringId, name);
            SaveChangeToXml();
        }
        private void AfterChange()
        {
            UpdateTextVariables();
            RefreshMenu();
        }
        private void UpdateTextVariables()
        {
            MBTextManager.SetTextVariable("unit_info", DisplayUnitInfo(), false);
        }
        private string DisplayUnitInfo()
        {
            string info = "";
            TextObject text = new TextObject("", null);
            if (isUnitSelected())
            {
                info += "Name: {name}\n";
                info += "Level: {level}\n";
                info += "Gender: {gender}\n";
                info += "Race: {race}\n";
                info += "Group: {group}\n";
                info += "Equipment Set: {set}\n";
                if (set > -1)
                {
                    Equipment equipment = null;
                    if (selectedUnit.BattleEquipments.ToList().Count > 0)
                    {
                        equipment = selectedUnit.BattleEquipments.ToList()[set];
                    }
                    if (equipment != null)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            if (equipment[i].Item != null)
                            {
                                info += $"{HouseTroopsXmlManager.equipmentSlot[i]} : {equipment[i].Item.Name}\n";
                            }
                        }
                    }
                }
                text = new TextObject(info, null);
                text.SetTextVariable("name", selectedUnit.Name);
                text.SetTextVariable("level", selectedUnit.Level);
                text.SetTextVariable("gender", selectedUnit.IsFemale ? "F" : "M");
                text.SetTextVariable("race", RaceManager.Instance.GetRaceNameFromId(selectedUnit.Race));
                text.SetTextVariable("group", selectedUnit.DefaultFormationGroup == 4 ? "Skirmisher" : ((FormationClass)selectedUnit.DefaultFormationGroup).ToString());
                text.SetTextVariable("set", set);
            }
            return text.ToString();
        }
        private void DisplayUnitList()
        {
            TextObject title = new TextObject("Select", null);
            TextObject info = new TextObject("Select the House Troop you want to modify", null);
            TextObject confirm = new TextObject("Confirm", null);
            TextObject cancel = new TextObject("Cancel", null);
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title.ToString(), info.ToString(), GetHouseTroop(), true, 1, 1, confirm.ToString(), cancel.ToString(), delegate (List<InquiryElement> unit)
            {
                selectedUnit = (CharacterObject)GetSelectedItem(unit);
                set = -1;
                AfterChange();
            }, delegate (List<InquiryElement> noItems)
            {
                InformationManager.HideInquiry();
            }, "", false), false, false);
        }
        private List<InquiryElement> GetHouseTroop()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            List<string> houseTroopId = xmlManager.GetAllHouseTroopId();
            foreach (string unitId in houseTroopId)
            {
                CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>(unitId);
                ImageIdentifier image = character == null ? null : new CharacterImageIdentifier(CharacterCode.CreateFrom(character));
                if (character != null)
                {
                    list.Add(new InquiryElement(character, character.Name.ToString() + " T" + character.Tier.ToString(), image));
                }
            }
            try
            {
                list.Sort(delegate (InquiryElement x, InquiryElement y)
                {
                    CharacterObject e = (CharacterObject)x.Identifier;
                    CharacterObject e2 = (CharacterObject)y.Identifier;
                    int sort = string.Compare(e2.Tier.ToString(), e.Tier.ToString());
                    return sort != 0 ? sort : string.Compare(e2.DefaultFormationClass.ToString(), e.DefaultFormationClass.ToString());
                });
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[RF] Chamberlain: troop list sort failed, showing unsorted: {ex.Message}");
            }
            return list;
        }
        private List<ItemObject> GetItemPool()
        {
            List<ItemObject> items = new List<ItemObject>();

            foreach (ItemRosterElement item in inventoryBackUp)
            {
                items.Add(item.EquipmentElement.Item);
            }
            return items;
        }
        private InventoryLogic newCustomInventoryLogic(bool isManagingEquipment = false)
        {
            ItemRoster left = new ItemRoster();
            ItemRoster right = new ItemRoster();
            if (isManagingEquipment)
            {
                List<ItemObject> items = GetItemPool();
                foreach (ItemObject item in items)
                {
                    left.AddToCounts(item, 3);
                }
            }
            InventoryLogic inventoryLogic = new(null);
            CharacterObject hero = Hero.MainHero.CharacterObject;
            MobileParty party = MobileParty.MainParty;
            HouseTroopsMarketData data = new();
            InventoryScreenHelper.InventoryCategoryType type = InventoryScreenHelper.InventoryCategoryType.None;
            var mode = InventoryScreenHelper.InventoryMode.Trade;
            TextObject name = selectedUnit.Name;
            TextObject text = new($"{name} : {selectedUnit.DefaultFormationClass}", null);
            inventoryLogic.Initialize(left, party, true, true, hero, type, data, false, mode, text, null, null);
            return inventoryLogic;
        }
        private void InventoryScreenForEquipmentSelection()
        {
            ClearPlayerInventory();
            ChangePlayerSkillToUnit();
            ChangePlayerGender();
            ChangePlayerRace();
            InventoryLogic inventoryLogic = newCustomInventoryLogic(true);
            InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
            inventoryState.InventoryLogic = inventoryLogic;
            inventoryState.DoneLogicExtrasDelegate = InventoryDoneLogic;
            Game.Current.GameStateManager.PushState(inventoryState, 0);
            ChangePlayerEquipmentToUnit(inventoryLogic);
        }
        private void ChangePlayerEquipmentToUnit(InventoryLogic inventoryLogic)
        {
            object partyInitialEquipment = GetPartyInitialEquipment(inventoryLogic);
            PropertyInfo property = partyInitialEquipment.GetType().GetProperties().FirstOrDefault();
            Dictionary<CharacterObject, Equipment[]> equipments = new Dictionary<CharacterObject, Equipment[]>();
            if (property != null)
            {
                equipments = (Dictionary<CharacterObject, Equipment[]>)property.GetValue(partyInitialEquipment);
                playerEquipment = new Dictionary<CharacterObject, Equipment[]>(equipments);
            }
            Dictionary<CharacterObject, Equipment[]> unitEquipments = new();
            foreach (KeyValuePair<CharacterObject, Equipment[]> equipment in equipments)
            {
                if (equipment.Key.IsPlayerCharacter)
                {
                    unitEquipments.Add(equipment.Key, new Equipment[]
                    {
                        new Equipment(selectedUnit.BattleEquipments.ToList()[set]),
                        new Equipment(selectedUnit.CivilianEquipments.ToList()[set])
                    });
                }
            }
            property.SetValue(partyInitialEquipment, unitEquipments);
            inventoryLogic.Reset(false);
        }
        private void ChangePlayerGender()
        {
            Hero player = Hero.MainHero;
            playerIsFemale = player.IsFemale;
            if (playerIsFemale != selectedUnit.IsFemale)
                player.IsFemale = selectedUnit.IsFemale;
        }
        private void ResetPlayerGender()
        {
            Hero player = Hero.MainHero;
            if (playerIsFemale != player.IsFemale)
                player.IsFemale = playerIsFemale;
        }
        private void ChangePlayerRace()
        {
            Hero player = Hero.MainHero;
            playerRaceId = player.CharacterObject.Race;
            if (playerRaceId != selectedUnit.Race)
                player.CharacterObject.Race = selectedUnit.Race;
        }
        private void ResetPlayerRace()
        {
            Hero player = Hero.MainHero;
            if (player.CharacterObject.Race != playerRaceId)
                player.CharacterObject.Race = playerRaceId;
        }


        private object GetPartyInitialEquipment(InventoryLogic inventoryLogic)
        {
            return HouseTroopsUtil.GetInstanceField(inventoryLogic, "_partyInitialEquipment").GetValue(inventoryLogic);
        }

        private void ResetPlayerEquipment(InventoryLogic inventoryLogic)
        {
            object partyInitialEquipment = GetPartyInitialEquipment(inventoryLogic);
            PropertyInfo property = partyInitialEquipment.GetType().GetProperties().FirstOrDefault();
            property?.SetValue(partyInitialEquipment, playerEquipment);
        }
        private void ChangePlayerSkillToUnit()
        {
            playerSkill.Clear();
            Hero player = Hero.MainHero;
            foreach (SkillObject skill in Skills.All.ToList())
            {
                playerSkill.Add(new HouseTroopsSkillRecord(skill.StringId, player.GetSkillValue(skill)));
                player.SetSkillValue(skill, selectedUnit.GetSkillValue(skill));
            }
        }
        private void ResetPlayerSkill()
        {
            List<SkillObject> skills = Skills.All.ToList();
            Hero player = Hero.MainHero;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].StringId.Equals(playerSkill[i].skill))
                {
                    player.SetSkillValue(skills[i], playerSkill[i].value);
                }
            }
        }
        private void ClearPlayerInventory()
        {
            PartyBase partyBase = PartyBase.MainParty;
            inventoryBackUp = partyBase.ItemRoster;
            HouseTroopsUtil.GetInstanceProperty(partyBase, "ItemRoster").SetValue(partyBase, new ItemRoster());
        }
        private void ResetPlayerInventory()
        {
            PartyBase partyBase = PartyBase.MainParty;
            HouseTroopsUtil.GetInstanceProperty(partyBase, "ItemRoster").SetValue(partyBase, inventoryBackUp);
        }
        private void InventoryDoneLogic()
        {
            InventoryState state = (InventoryState)Game.Current.GameStateManager.ActiveState;
            InventoryLogic inventoryLogic = state.InventoryLogic;   // newCustomInventoryLogic(false);
            List<HouseTroopsEquipmentRecord> records = GetNewEquipment(inventoryLogic);
            foreach (HouseTroopsEquipmentRecord record in records)
                ChangeUnitEquipment(selectedUnit, record.index, GetItemObject(record.itemId), record.equipmentSet, record.isCivilan);
            ResetPlayerEquipment(inventoryLogic);
            ResetPlayerSkill();
            ResetPlayerGender();
            ResetPlayerRace();
            ResetPlayerInventory();
            inventoryLogic.Reset(false);
            AfterChange();
        }
        private List<HouseTroopsEquipmentRecord> GetNewEquipment(InventoryLogic inventoryLogic)
        {
            bool isCivilian = false;
            object partyInitialEquipment = GetPartyInitialEquipment(inventoryLogic);
            Type nestedType = typeof(InventoryLogic).GetNestedType("PartyEquipment", BindingFlags.NonPublic);
            MethodInfo getter = AccessTools.PropertyGetter(nestedType, "CharacterEquipments");
            var troopEquipment = (Dictionary<CharacterObject, Equipment[]>)getter.Invoke(partyInitialEquipment, null);
            List<HouseTroopsEquipmentRecord> records = new();
            foreach (Equipment equipmentSet in troopEquipment[CharacterObject.PlayerCharacter])
            {
                if (equipmentSet.IsStealth) continue;
                if (equipmentSet.IsCivilian)
                {
                    isCivilian = true;
                }
                for (int i = 0; i < 12; i++)
                {
                    if (equipmentSet[i].Item != null)
                    {
                        records.Add(new HouseTroopsEquipmentRecord(i, equipmentSet[i].Item.StringId, set, isCivilian));
                    }
                    else
                    {
                        records.Add(new HouseTroopsEquipmentRecord(i, "", set, isCivilian));
                    }
                }
            }
            return records;
        }
        private void SelectEquipmentSet()
        {
            TextObject title = new TextObject("{=house_troop_select}Select", null);
            TextObject info = new TextObject("{=house_troop_select}Select", null);
            TextObject confirm = new TextObject("{=house_troop_confirm}Confirm", null);
            TextObject cancel = new TextObject("{=house_troop_cancel}Cancel", null);
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title.ToString(), info.ToString(), GetEquipmentSetNumberOption(), true, 1, 1, confirm.ToString(), cancel.ToString(), delegate (List<InquiryElement> selectedSet)
            {
                set = (int)GetSelectedItem(selectedSet);
                if (set == -100)
                {
                    CloneUnitEquipment(selectedUnit);
                    set = selectedUnit.BattleEquipments.Count() - 1;
                }
                AfterChange();
            }, delegate (List<InquiryElement> noItems)
            {
                InformationManager.HideInquiry();
            }, "", false), false, false);
        }
        private void DeleteEquipmentSet(CharacterObject unit)
        {
            if (set == 0)
            {
                // Was falling through to the delete block below and deleting set 0
                // anyway (the else only bound to the set == -1 check).
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=house_troop_menu_delete_equipment_set_error}Cannot delete equipment set 0").ToString()));
                return;
            }
            if (set == -1)
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=house_troop_menu_invalid_set}Choose or create an Equipment Set for your House Troop first.").ToString()));
            }
            else
            {
                List<Equipment> equipments = new List<Equipment>();
                Equipment deleteBattle = selectedUnit.BattleEquipments.ToList()[set];
                Equipment deleteCiv = selectedUnit.CivilianEquipments.ToList()[set];
                BasicCharacterObject basicCharacter = selectedUnit;
                var allEquipments = (MBReadOnlyList<Equipment>)AllEquipmentsGetter.Invoke(basicCharacter, null);
                foreach (Equipment equipment in allEquipments)
                {
                    if (equipment != deleteBattle && equipment != deleteCiv)
                    {
                        equipments.Add(equipment);
                    }
                }
                UpdateSelectedUnitEquipment(unit, equipments);
                set = -1;
                SaveChangeToXml();
            }
        }
        private void CloneUnitEquipment(CharacterObject unit)
        {
            BasicCharacterObject basicCharacter = selectedUnit;
            var allEquipments = (MBReadOnlyList<Equipment>)AllEquipmentsGetter.Invoke(basicCharacter, null);
            List<Equipment> equipments = allEquipments.ToList();
            Equipment civilian = selectedUnit.FirstCivilianEquipment.Clone(false);
            Equipment battle = selectedUnit.FirstBattleEquipment.Clone(false);
            equipments.Add(battle);
            equipments.Add(civilian);
            UpdateSelectedUnitEquipment(unit, equipments);
            SaveChangeToXml();
        }
        private void CreateEmptyEquipmentForInit(CharacterObject unit)
        {
            List<Equipment> equipments = new List<Equipment>();
            int count = HouseTroopsXmlManager.GetEquipmentSetNumber(xmlManager.GetCampaignXml(), unit.StringId);
            if (count == 0)
            {
                count = 1;
            }
            for (int i = 0; i < count; i++)
            {
                equipments.Add(new Equipment(Equipment.EquipmentType.Battle));
                equipments.Add(new Equipment(Equipment.EquipmentType.Civilian));
            }
            UpdateSelectedUnitEquipment(unit, equipments);
        }
        private List<InquiryElement> GetEquipmentSetNumberOption()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            int setNumber = HouseTroopsXmlManager.GetEquipmentSetNumber(xmlManager.GetCampaignXml(), selectedUnit.StringId);
            for (int i = 0; i < setNumber; i++)
            {
                list.Add(new InquiryElement(i, i.ToString(), null));
            }
            list.Add(new InquiryElement(-100, "+", null));
            return list;
        }
        private void ChangeUnitEquipment(CharacterObject unit, int slot, ItemObject item, int set, bool isCivilian = false)
        {
            List<Equipment> civilian = unit.CivilianEquipments.ToList();
            List<Equipment> battle = unit.BattleEquipments.ToList();
            EquipmentElement equipmentElement = item != null ? new EquipmentElement(item, null, null, false) : default;
            if (isCivilian)
            {
                civilian[set][slot] = equipmentElement;
            }
            else
            {
                battle[set][slot] = equipmentElement;
            }
            battle.AddRange(civilian);
            UpdateSelectedUnitEquipment(unit, battle);
        }
        private void UpdateSelectedUnitEquipment(CharacterObject unit, List<Equipment> equipments)
        {
            MBEquipmentRoster roster = new MBEquipmentRoster();
            HouseTroopsUtil.GetInstanceField(roster, "_equipments").SetValue(roster, new MBList<Equipment>(equipments));
            HouseTroopsUtil.GetInstanceField<BasicCharacterObject>(unit, "_equipmentRoster").SetValue(unit, roster);
            unit.InitializeEquipmentsOnLoad(unit);
        }
        private void SetSkillValue(CharacterObject unit, SkillObject skill, int value)
        {
            if (unit != null && unit.IsHero)
            {
                Hero hero = unit.HeroObject;
                if (hero != null)
                {
                    hero.HeroDeveloper.ChangeSkillLevel(skill, value);
                }
            }
        }
        private void GetInputNewUnitName()
        {
            TextObject title = new TextObject("{=house_troop_select}Select", null);
            TextObject info = new TextObject("{=house_troop_new_unit_name}New House Troop Name", null);
            TextObject confirm = new TextObject("{=house_troop_confirm}Confirm", null);
            TextObject cancel = new TextObject("{=house_troop_cancel}Cancel", null);
            InformationManager.ShowTextInquiry(new TextInquiryData(title.ToString(), info.ToString(), true, true, confirm.ToString(), cancel.ToString(), delegate (string name)
            {
                ManageUpgradePath(name);
            }, delegate
            {
                InformationManager.HideInquiry();
            }, false, null, "", ""), false, false);
        }
        private CharacterObject CreateCharacterObject(string unitName)
        {
            string unitId = "adod_house_troop_" + unitName.Replace(' ', '_').ToLower();
            CharacterObject newUnit = MBObjectManager.Instance.CreateObject<CharacterObject>(unitId);

            ((BasicCharacterObject)newUnit).Culture = Hero.MainHero.Culture;

            HouseTroopsUtil.GetInstanceProperty(newUnit, "UpgradeTargets").SetValue(newUnit, new CharacterObject[0]);
            HouseTroopsUtil.GetInstanceProperty(newUnit, "BodyPropertyRange").SetValue(newUnit, MBObjectManager.Instance.GetObject<MBBodyProperty>("fighter_custom"));
            newUnit.StringId = unitId;
            ChangeUnitName(newUnit, unitName);
            newUnit.Level = selectedUnit.Level + 5;

            HouseTroopsUtil.GetInstanceField(newUnit, "CharacterSkills").SetValue(newUnit, new MBCharacterSkills());
            foreach (SkillObject skill in Skills.All.ToList())
            {
                if (selectedUnit.GetSkillValue(skill) > 0)
                {
                    SetSkillValue(newUnit, skill, selectedUnit.GetSkillValue(skill) + 50);
                }
            }
            CreateEmptyEquipmentForInit(newUnit);

            return newUnit;
        }
        private void ManageUpgradePath(string newUnitName)
        {
            CharacterObject unit = CreateCharacterObject(newUnitName);
            xmlManager.AddUnitToXml(xmlManager.GetCampaignXml(), selectedUnit.StringId, unit);
            AddUpgradePath(unit);
        }

        private void AddUpgradePath(CharacterObject newUnit)
        {
            List<CharacterObject> newUpgradePath = new List<CharacterObject>();
            PropertyInfo property2 = HouseTroopsUtil.GetInstanceProperty(selectedUnit, "UpgradeTargets");
            CharacterObject current = selectedUnit.UpgradeTargets.Length > 0 ? selectedUnit.UpgradeTargets[0] : null;
            if (current != null)
            {
                newUpgradePath.Add(current);
            }
            newUpgradePath.Add(newUnit);
            property2.SetValue(selectedUnit, newUpgradePath.ToArray());
        }
        private void ConfirmRemoveTroop(CharacterObject unit)
        {
            TextObject name = unit.Name;
            InformationManager.ShowInquiry(new InquiryData("Confirm", $"Delete {name}", true, true, "Confirm", "Cancel", delegate
            {
                RemoveTroop(unit);
            }, null, "", 0f, null, null, null), true, false);
        }
        private void RemoveTroop(CharacterObject deleteUnit)
        {
            List<CharacterObject> unitRelated = new List<CharacterObject>();
            foreach (string troopId in xmlManager.GetAllHouseTroopId())
            {
                CharacterObject troop = GetCharacterObject(troopId);
                if (troop != null && troop.UpgradeTargets.Length > 0 && troop.UpgradeTargets.Contains(deleteUnit))
                {
                    unitRelated.Add(troop);
                    PropertyInfo property = HouseTroopsUtil.GetInstanceProperty(troop, "UpgradeTargets");
                    property.SetValue(troop, troop.UpgradeTargets.Where(x => !x.StringId.Equals(deleteUnit.StringId)).ToArray());
                }
            }
            if (unitRelated.Count > 0)
            {
                foreach (CharacterObject related in unitRelated)
                {
                    XDocument doc = xmlManager.RemoveUpgradePathFromXml(xmlManager.GetOriginXml(), related, deleteUnit);
                    doc.Save(xmlManager.GetTroopFilePath());
                    doc = xmlManager.RemoveUpgradePathFromXml(xmlManager.GetCampaignXml(), related, deleteUnit);
                    doc.Save(xmlManager.GetCampaignFilePath());
                }
            }
            RemoveUnitFromMap(deleteUnit);
            XDocument doc2 = xmlManager.RemoveUnitFromXml(xmlManager.GetOriginXml(), deleteUnit);
            doc2.Save(xmlManager.GetTroopFilePath());
            doc2 = xmlManager.RemoveUnitFromXml(xmlManager.GetCampaignXml(), deleteUnit);
            doc2.Save(xmlManager.GetCampaignFilePath());
            selectedUnit = null;
            AfterChange();
        }
        private void RemoveUnitFromMap(CharacterObject unit)
        {
            FieldInfo field = HouseTroopsUtil.GetInstanceField(Campaign.Current.CampaignObjectManager, "_mobileParties");
            List<MobileParty> partyBaseList = (List<MobileParty>)field.GetValue(Campaign.Current.CampaignObjectManager);
            foreach (MobileParty party in partyBaseList)
            {
                PartyBase partyBase = party.Party;
                if (partyBase.MemberRoster.Contains(unit))
                {
                    partyBase.MemberRoster.RemoveTroop(unit, partyBase.MemberRoster.GetTroopCount(unit), default, 0);
                }
                if (partyBase.PrisonRoster.Contains(unit))
                {
                    partyBase.PrisonRoster.RemoveTroop(unit, partyBase.PrisonRoster.GetTroopCount(unit), default, 0);
                }
            }
        }
        private void AddExportImportButtons(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_export", "Export House Troops",
                delegate (MenuCallbackArgs args)
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return true;
                },
                delegate (MenuCallbackArgs args)
                {
                    GetInputExportFileName();
                }, false, -1, false, null);

            campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_import", "Import House Troops",
                delegate (MenuCallbackArgs args)
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return true;
                },
                delegate (MenuCallbackArgs args)
                {
                    ShowImportOptions();
                }, false, -1, false, null);
        }
        private void GetInputExportFileName()
        {
            TextObject title = new TextObject("Export Troops", null);
            TextObject info = new TextObject("Enter the name for the export file:", null);
            TextObject confirm = new TextObject("Confirm", null);
            TextObject cancel = new TextObject("Cancel", null);

            InformationManager.ShowTextInquiry(new TextInquiryData(title.ToString(), info.ToString(), true, true, confirm.ToString(), cancel.ToString(), delegate (string fileName)
            {
                ExportTroopsXml(fileName);
            }, delegate ()
            {
                InformationManager.HideInquiry();
            }, false, null, "", ""), false, false);
        }
        private void ExportTroopsXml(string fileName)
        {
            string filePath = Path.Combine(HouseTroopsXmlManager.campaignTroopBackupFolderPath, fileName + ".xml");

            XDocument exportDoc = new XDocument(new XElement("NPCCharacters"));

            XDocument currentTroopsXml = xmlManager.GetCampaignXml();

            if (currentTroopsXml != null)
            {
                foreach (XElement npcElement in currentTroopsXml.Element("NPCCharacters").Elements("NPCCharacter"))
                {
                    XElement newNpcElement = new XElement(npcElement);

                    string troopId = npcElement.Attribute("id").Value;
                    CharacterObject unit = GetCharacterObject(troopId);

                    if (unit != null)
                    {
                        newNpcElement.SetAttributeValue("name", unit.Name.ToString());

                        List<HouseTroopsEquipmentRecord> equipmentRecords = HouseTroopsXmlManager.GetUnitEquipmentId(currentTroopsXml, unit.StringId);
                        XElement equipmentsElement = new XElement("Equipments");

                        foreach (HouseTroopsEquipmentRecord record in equipmentRecords)
                        {
                            XElement equipmentSetElement = new XElement("EquipmentRoster", new XAttribute("civilian", record.isCivilan));
                            equipmentSetElement.Add(new XElement("equipment", new object[]
                            {
                        new XAttribute("slot", HouseTroopsXmlManager.equipmentSlot[record.index]),
                        new XAttribute("id", "Item." + record.itemId)
                            }));
                            equipmentsElement.Add(equipmentSetElement);
                        }
                        newNpcElement.Add(equipmentsElement);
                    }
                    exportDoc.Element("NPCCharacters").Add(newNpcElement);
                }
                exportDoc.Save(filePath);
                InformationManager.DisplayMessage(new InformationMessage($"House Troops exported successfully to {filePath}"));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Could not load current troop data for export."));
            }
        }
        private void ShowImportOptions()
        {
            string[] xmlFiles = Directory.GetFiles(HouseTroopsXmlManager.campaignTroopBackupFolderPath, "*.xml");

            List<InquiryElement> options = xmlFiles.Select(filePath =>
                new InquiryElement(filePath, Path.GetFileName(filePath), null)).ToList();

            if (options.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No XML files available for import."));
                return;
            }

            string title = new TextObject("Import Troops", null).ToString();

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    title,
                    "Select the file to import",
                    options,
                    true,
                    1,
                    1,
                    GameTexts.FindText("str_done", null).ToString(),
                    GameTexts.FindText("str_cancel", null).ToString(),
                    elements => ImportTroopsXml(elements.First().Identifier as string),
                    null,
                    string.Empty,
                    false
                ),
                false,
                false
            );
        }
        private void ImportTroopsXml(string filePath)
        {
            if (File.Exists(filePath))
            {
                XDocument doc = XDocument.Load(filePath);

                xmlManager.SetCampaignFilePath(Campaign.Current);
                doc.Save(xmlManager.GetCampaignFilePath());

                foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
                {
                    string troopId = npcElement.Attribute("id").Value;
                    CharacterObject unit = GetCharacterObject(troopId);

                    if (unit != null)
                    {
                        if (npcElement.Attribute("name") != null)
                        {
                            ChangeUnitName(unit, npcElement.Attribute("name").Value);
                        }

                        if (npcElement.Element("Equipments") != null)
                        {
                            List<HouseTroopsEquipmentRecord> equipmentRecords = HouseTroopsXmlManager.GetUnitEquipmentId(doc, troopId);
                            foreach (HouseTroopsEquipmentRecord record in equipmentRecords)
                            {
                                ItemObject item = GetItemObject(record.itemId);
                                if (item != null)
                                {
                                    ChangeUnitEquipment(unit, record.index, item, record.equipmentSet, record.isCivilan);
                                }
                            }
                        }
                    }
                }
                InformationManager.DisplayMessage(new InformationMessage("House Troops imported successfully from " + filePath));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("File not found: " + filePath));
            }
        }
        private object GetSelectedItem(List<InquiryElement> list)
        {
            return list[0].Identifier;
        }

        internal static List<Equipment> GetTroopEquipmentSet(CharacterObject unit)
        {
            return unit.BattleEquipments.ToList();
        }
        internal static List<Equipment> GetTroopCivEquipmentSet(CharacterObject unit)
        {
            return unit.CivilianEquipments.ToList();
        }
        internal static List<EquipmentElement> GetTroopEquipment(Equipment equipment)
        {
            List<EquipmentElement> list = new List<EquipmentElement>();
            for (int i = 0; i < 12; i++)
            {
                list.Add(equipment[i]);
            }
            return list;
        }
        internal static CharacterObject GetCharacterObject(string unitId)
        {
            return MBObjectManager.Instance.GetObject<CharacterObject>(unitId) ?? null;
        }
        internal static ItemObject GetItemObject(string itemId)
        {
            return MBObjectManager.Instance.GetObject<ItemObject>(itemId) ?? null;
        }
        private void DisplayMessage(string str)
        {
            TextObject text = new TextObject(str, null);
            InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
        }
        private bool IsInKingdom()
        {
            Kingdom kingdom = Clan.PlayerClan.Kingdom;
            return kingdom != null;
        }
        private bool IsOwnedSettlement()
        {
            return Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan;
        }
        private bool IsTown()
        {
            return Settlement.CurrentSettlement.IsTown;
        }
        private bool IsCastle()
        {
            return Settlement.CurrentSettlement.IsCastle;
        }
        private void RefreshMenu()
        {
            GameMenu.SwitchToMenu("house_troop_menu");
        }
        private void ChangeUnitName(CharacterObject unit, string name)
        {
            typeof(BasicCharacterObject).GetMethod("SetName", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unit, new object[] { new TextObject(name, null) });
        }
        public override void SyncData(IDataStore dataStore) { }


        private static HouseTroopsXmlManager xmlManager;
        private static CharacterObject selectedUnit;
        private static int set = -1;
        private static Dictionary<CharacterObject, Equipment[]> playerEquipment;
        private static bool playerIsFemale;
        private static int playerRaceId;
        private static List<HouseTroopsSkillRecord> playerSkill = new();
        private static ItemRoster inventoryBackUp;
    }
}
