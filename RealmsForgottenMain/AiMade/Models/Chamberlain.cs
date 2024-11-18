using RealmsForgotten;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using static ADODHouseTroopsStatsModel;

namespace RealmsForgotten.AiMade.Models
{


    internal class HouseTroopsConfig
    {
        public float UpgradeCostMultiplier { get; set; } = 1.5f;
    }
}
internal class HouseTroopsEquipmentRecord
{
    internal int index { get; set; }
    internal string itemId { get; set; }
    internal int equipmentSet { get; set; }

    internal bool isCivilan { get; set; }
    public HouseTroopsEquipmentRecord(int index, string itemId, int equipmentSet, bool isCivilan = false)
    {
        this.index = index;
        this.itemId = itemId;
        this.equipmentSet = equipmentSet;
        this.isCivilan = isCivilan;
    }
}
internal class HouseTroopsMarketData : IMarketData
{
    public HouseTroopsMarketData() { }

    public int GetPrice(ItemObject item, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
    {
        return 0;
    }

    public int GetPrice(EquipmentElement itemRosterElement, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
    {
        return 0;
    }
}

internal class HouseTroopsSkillRecord
{
    internal string skill { get; set; }
    internal int value { get; set; }
    public HouseTroopsSkillRecord(string skill, int value)
    {
        this.skill = skill;
        this.value = value;
    }
}
internal class ADODHouseTroopsStatsModel : DefaultCharacterStatsModel
{
    public override int MaxCharacterTier
    {
        get
        {
            return 10;
        }
    }
    internal class HouseTroopsUtil
    {
        internal static PropertyInfo GetInstanceProperty<T>(T instance, string propertyName)
        {
            return typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        internal static FieldInfo GetInstanceField<T>(T instance, string fieldName)
        {
            return typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        private const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    }
}
internal class HouseTroopsXmlManager
{
    public HouseTroopsXmlManager()
    {
        HouseTroopsXmlManager.troopFilePath = this.FormXmlPath("ADOD_CustomHouseTroops.xml");
        HouseTroopsXmlManager.campaignTroopBackupFolderPath = this.FormXmlPath("");
        HouseTroopsXmlManager.campaignTroopBackupFolderPath = Path.Combine(HouseTroopsXmlManager.campaignTroopBackupFolderPath, "ADODHouseTroopsSavedTroops\\");
        this.CreateFolderIfNeeded();
    }

    private string FormXmlPath(string xmlName)
    {
        string path = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

        string projectPath = Path.GetFullPath(Path.Combine(path, @"..\..\ModuleData\"));
        return Path.Combine(projectPath, xmlName);
    }
    internal void CreateFolderIfNeeded()
    {
        if (!Directory.Exists(HouseTroopsXmlManager.campaignTroopBackupFolderPath))
        {
            Directory.CreateDirectory(HouseTroopsXmlManager.campaignTroopBackupFolderPath);
        }
    }

    internal bool IsCampaignFileExists()
    {
        if (File.Exists(HouseTroopsXmlManager.campaignTroopFilePath))
        {
            return true;
        }
        else
        {
            string str = "Your Custom House Troops file does not exist. Tell the Developers.";
            TextObject text = new TextObject(str, null);
            InformationManager.DisplayMessage(new InformationMessage(text.ToString(), new Color(1f, 0f, 0f, 1f)));
            return false;
        }
    }

    internal string GetCampaignFilePath()
    {
        return this.IsCampaignFileExists() ? HouseTroopsXmlManager.campaignTroopFilePath : "";
    }

    internal string GetTroopFilePath()
    {
        return HouseTroopsXmlManager.troopFilePath;
    }

    internal void SetCampaignFilePath(Campaign campaign)
    {
        HouseTroopsXmlManager.campaignTroopFilePath = Path.Combine(HouseTroopsXmlManager.campaignTroopBackupFolderPath, campaign.UniqueGameId + "_house_troop.xml");
    }

    internal void SetCampaignHouseTroopXml()
    {
        if (!File.Exists(HouseTroopsXmlManager.campaignTroopFilePath))
        {
            XDocument doc = XDocument.Load(HouseTroopsXmlManager.troopFilePath);
            doc.Save(HouseTroopsXmlManager.campaignTroopFilePath);
        }
    }

    internal XDocument GetCampaignXml()
    {
        return this.IsCampaignFileExists() ? XDocument.Load(HouseTroopsXmlManager.campaignTroopFilePath) : null;
    }

    internal XDocument GetOriginXml()
    {
        return XDocument.Load(HouseTroopsXmlManager.troopFilePath);
    }

    internal void CreateXmlIfNeeded()
    {
        if (!File.Exists(HouseTroopsXmlManager.campaignTroopFilePath))
        {
            XDocument doc = XDocument.Load(HouseTroopsXmlManager.troopFilePath);
            doc.Save(HouseTroopsXmlManager.campaignTroopFilePath);
        }
    }

    internal void SaveXml(string filePath)
    {
        XDocument doc = XDocument.Load(filePath);

        doc = HouseTroopsXmlManager.ClearAllEquipmentElement(doc);

        doc = HouseTroopsXmlManager.ChangeAllEquipmentElement(doc);

        doc.Save(filePath);
    }


    internal static XDocument ClearAllEquipmentElement(XDocument doc)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            npcElement.Elements("Equipments").Remove<XElement>();
            npcElement.Elements("equipmentSet").Remove<XElement>();
            npcElement.Elements("equipment").Remove<XElement>();
        }
        return doc;
    }

    internal static XDocument ChangeAllEquipmentElement(XDocument doc)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            CharacterObject unit = ADODChamberlainsBehavior.GetCharacterObject(npcElement.Attribute("id").Value);
            if (unit != null)
            {
                npcElement.Elements("Equipments").Remove();

                XElement equipmentsElement = new XElement("Equipments");

                XElement battleRosterElement = new XElement("EquipmentRoster", new XAttribute("civilian", "false"));
                foreach (var equipment in unit.BattleEquipments)
                {
                    for (int index = 0; index < 12; index++)
                    {
                        ItemObject item = equipment[index].Item;
                        if (item != null)
                        {
                            battleRosterElement.Add(new XElement("equipment",
                                new XAttribute("slot", HouseTroopsXmlManager.equipmentSlot[index]),
                                new XAttribute("id", "Item." + item.StringId)));
                        }
                    }
                }
                equipmentsElement.Add(battleRosterElement);

                // Add civilian equipment
                XElement civilianRosterElement = new XElement("EquipmentRoster", new XAttribute("civilian", "true"));
                foreach (var equipment in unit.CivilianEquipments)
                {
                    for (int index = 0; index < 12; index++)
                    {
                        ItemObject item = equipment[index].Item;
                        if (item != null)
                        {
                            civilianRosterElement.Add(new XElement("equipment",
                                new XAttribute("slot", HouseTroopsXmlManager.equipmentSlot[index]),
                                new XAttribute("id", "Item." + item.StringId)));
                        }
                    }
                }
                equipmentsElement.Add(civilianRosterElement);

                npcElement.Add(equipmentsElement);
            }
        }

        return doc;
    }



    internal static XDocument ClearAllSkillElement(XDocument doc)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            XElement skillsElement = npcElement.Element("skills");
            if (skillsElement.HasElements)
            {
                skillsElement.Elements().Remove<XElement>();
            }
        }
        return doc;
    }

    internal static XDocument ChangeAllUnitSkillElement(XDocument doc)
    {
        foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            XElement skillElement = npcElement.Element("skills");
            CharacterObject unit = ADODChamberlainsBehavior.GetCharacterObject(npcElement.Attribute("id").Value);
            if (unit != null)
            {
                foreach (SkillObject skill in Skills.All.ToList<SkillObject>())
                {
                    int skillValue = unit.GetSkillValue(skill);
                    if (skillValue > 0)
                    {
                        skillElement.Add(new XElement("skill", new object[]
                        {
                                new XAttribute("id", skill.StringId),
                                new XAttribute("value", skillValue)
                        }));
                    }
                }
            }
        }
        return doc;
    }

    internal static List<HouseTroopsSkillRecord> GetUnitSkill(XDocument doc, string unitId)
    {
        List<HouseTroopsSkillRecord> list = new List<HouseTroopsSkillRecord>();
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId) && npcElement.Element("skills") != null)
            {
                foreach (XElement skill in npcElement.Element("skills").Elements("skill"))
                {
                    int value;
                    int.TryParse(skill.Attribute("value").Value, out value);
                    list.Add(new HouseTroopsSkillRecord(skill.Attribute("id").Value, value));
                }
            }
        }
        return list;
    }

    internal void RenameUnit(XDocument doc, string unitId, string name)
    {
        foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                npcElement.Attribute("name").Value = name;
                doc.Save(HouseTroopsXmlManager.campaignTroopFilePath);
            }
        }
    }

    internal static string GetUnitName(XDocument doc, string unitId)
    {
        foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                return npcElement.Attribute("name").Value;
            }
        }
        return "";
    }

    internal void ChangeUnitGender(XDocument doc, string unitId, string currentGender)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                bool isFemale = false;
                if (npcElement.Attribute("is_female") == null)
                {
                    npcElement.Add(new XAttribute("is_female", "false"));
                }
                npcElement.Attribute("is_female").Value = (currentGender.Equals("M") ? isFemale.ToString().ToLower() : (!isFemale).ToString().ToLower());
                doc.Save(HouseTroopsXmlManager.campaignTroopFilePath);
            }
        }
    }

    internal static bool GetIsUnitFemale(XDocument doc, string unitId)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                if (npcElement.Attribute("is_female") == null)
                {
                    npcElement.Add(new XAttribute("is_female", "false"));
                }
                return !npcElement.Attribute("is_female").Value.Equals("false");
            }
        }
        return false;
    }

    internal void ChangeUnitFace(XDocument doc, string unitId, string newFace)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                XElement face = npcElement.Element("face").Element("face_key_template");
                if (face != null)
                {
                    face.Attribute("value").Value = "BodyProperty." + newFace;
                }
                doc.Save(HouseTroopsXmlManager.campaignTroopFilePath);
            }
        }
    }

    internal static string GetUnitFace(XDocument doc, string unitId)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId) && npcElement.Element("face") != null)
            {
                XElement faces = npcElement.Element("face");
                using (IEnumerator<XElement> enumerator2 = faces.Elements("face_key_template").GetEnumerator())
                {
                    if (enumerator2.MoveNext())
                    {
                        XElement face = enumerator2.Current;
                        return face.Attribute("value").Value.Split(new char[]
                        {
                                '.'
                        })[1];
                    }
                }
            }
        }
        return "";
    }

    internal void ChangeUnitGroup(XDocument doc, string unitId, string newGroup)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                npcElement.Attribute("default_group").Value = newGroup;
                doc.Save(HouseTroopsXmlManager.campaignTroopFilePath);
            }
        }
    }

    internal static string GetUnitGroup(XDocument doc, string unitId)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId) && npcElement.Attribute("default_group") != null)
            {
                return npcElement.Attribute("default_group").Value;
            }
        }
        return "";
    }

    internal List<string> GetAllHouseTroopId()
    {
        List<string> customTroopId = new List<string>();
        XDocument doc = XDocument.Load(HouseTroopsXmlManager.campaignTroopFilePath);
        foreach (XElement npc in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            customTroopId.Add(npc.Attribute("id").Value);
        }
        return customTroopId;
    }

    internal static List<HouseTroopsEquipmentRecord> GetUnitEquipmentId(XDocument doc, string unitId)
    {
        List<HouseTroopsEquipmentRecord> list = new List<HouseTroopsEquipmentRecord>();
        if (doc == null)
        {
            return list;
        }
        else
        {
            foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
            {
                int set = 0;
                if (npcElement.Attribute("id").Value.Equals(unitId))
                {
                    XElement equipmentsElement = npcElement.Element("Equipments");
                    if (equipmentsElement == null)
                    {
                        foreach (XElement equipmentSetElement in npcElement.Elements("equipmentSet"))
                        {
                            bool isCivilian = false;
                            if (equipmentSetElement.Attribute("civilian") != null && equipmentSetElement.Attribute("civilian").Value.Equals("true"))
                            {
                                isCivilian = true;
                                set--;
                            }
                            foreach (XElement equipment in equipmentSetElement.Elements())
                            {
                                int slot = Array.IndexOf<string>(HouseTroopsXmlManager.equipmentSlot, equipment.Attribute("slot").Value);
                                string item = equipment.Attribute("id").Value.Split(new char[]
                                {
                                        '.'
                                })[1];
                                if (slot > -1 && !string.IsNullOrEmpty(item))
                                {
                                    list.Add(new HouseTroopsEquipmentRecord(slot, item, set, isCivilian));
                                }
                            }
                            foreach (XElement equipment2 in npcElement.Elements("equipment"))
                            {
                                int slot2 = Array.IndexOf<string>(HouseTroopsXmlManager.equipmentSlot, equipment2.Attribute("slot").Value);
                                string item2 = equipment2.Attribute("id").Value.Split(new char[]
                                {
                                        '.'
                                })[1];
                                if (slot2 > -1 && !string.IsNullOrEmpty(item2))
                                {
                                    list.Add(new HouseTroopsEquipmentRecord(slot2, item2, set, false));
                                }
                            }
                            set++;
                        }
                        break;
                    }
                    foreach (XElement equipmentRosterElement in equipmentsElement.Elements("EquipmentRoster"))
                    {
                        bool isCivilian2 = false;
                        if (equipmentRosterElement.Attribute("civilian") != null && equipmentRosterElement.Attribute("civilian").Value.Equals("true"))
                        {
                            isCivilian2 = true;
                            set--;
                        }
                        foreach (XElement equipment3 in equipmentRosterElement.Elements())
                        {
                            int slot3 = Array.IndexOf<string>(HouseTroopsXmlManager.equipmentSlot, equipment3.Attribute("slot").Value);
                            string item3 = equipment3.Attribute("id").Value.Split(new char[]
                            {
                                    '.'
                            })[1];
                            if (slot3 > -1 && !string.IsNullOrEmpty(item3))
                            {
                                list.Add(new HouseTroopsEquipmentRecord(slot3, item3, set, isCivilian2));
                            }
                        }
                        set++;
                    }
                    break;
                }
            }
            return list;
        }
    }

    internal static int GetEquipmentSetNumber(XDocument doc, string unitId)
    {
        int count = 0;
        foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                XElement equipmentsElement = npcElement.Element("Equipments");
                if (equipmentsElement == null)
                {
                    foreach (XElement setElement in npcElement.Elements("equipmentSet"))
                    {
                        int num = count;
                        XAttribute xattribute = setElement.Attribute("civilian");
                        count = num + ((xattribute != null && xattribute.Value.Equals("false")) ? 1 : 0);
                    }
                }
                else
                {
                    foreach (XElement equipmentRosterElement in equipmentsElement.Elements("EquipmentRoster"))
                    {
                        int num2 = count;
                        XAttribute xattribute2 = equipmentRosterElement.Attribute("civilian");
                        count = num2 + ((xattribute2 != null && xattribute2.Value.Equals("false")) ? 1 : 0);
                    }
                }
            }
        }
        return count;
    }

    internal void AddUnitToXml(XDocument doc, string previousUnit, CharacterObject newUnit)
    {
        XElement npcs = doc.Element("NPCCharacters");
        XElement templateXElement = null;
        XElement previousXElement = null;
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(previousUnit))
            {
                templateXElement = new XElement(npcElement);
                previousXElement = npcElement;
            }
            if (npcElement.Attribute("id").Value.Equals(newUnit.StringId))
            {
                return;
            }
        }
        templateXElement.Element("upgrade_targets").RemoveNodes();
        templateXElement.Attribute("id").Value = newUnit.StringId;
        templateXElement.Attribute("name").Value = newUnit.Name.ToString();
        templateXElement.Attribute("level").Value = ((int.Parse(templateXElement.Attribute("level").Value) + 5).ToString() ?? "");
        previousXElement.Element("upgrade_targets").Add(new XElement("upgrade_target", new XAttribute("id", "NPCCharacter." + newUnit.StringId)));
        previousXElement.AddAfterSelf(templateXElement);
        doc.Save(this.GetTroopFilePath());
        doc.Save(this.GetCampaignFilePath());
    }

    internal XDocument RemoveUpgradePathFromXml(XDocument doc, CharacterObject unitDeleteFrom, CharacterObject unitDeleted)
    {
        XElement npcs = doc.Element("NPCCharacters");
        npcs.Elements("NPCCharacter").First((XElement x) => x.Attribute("id").Value.Equals(unitDeleteFrom.StringId)).Element("upgrade_targets").Elements("upgrade_target").First((XElement x) => x.Attribute("id").Value.Equals("NPCCharacter." + unitDeleted.StringId)).Remove();
        return doc;
    }

    internal XDocument RemoveUnitFromXml(XDocument doc, CharacterObject unitDeleted)
    {
        XElement npcs = doc.Element("NPCCharacters");
        npcs.Elements("NPCCharacter").First((XElement x) => x.Attribute("id").Value.Equals(unitDeleted.StringId)).Remove();
        return doc;
    }

    internal static string troopFilePath = "";
    internal static string campaignTroopFilePath = "";
    internal static string campaignTroopBackupFolderPath = "";

    internal static string[] equipmentSlot = new string[]
    {
            "Item0",
            "Item1",
            "Item2",
            "Item3",
            "",
            "Head",
            "Body",
            "Leg",
            "Gloves",
            "Cape",
            "Horse",
            "HorseHarness"
    };
}
internal class ADODChamberlainsBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        try
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.AddHouseTroopMenu));
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, new Action(this.SaveChangeToXml));
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage("House Troop Error, tell the Dev Team: " + ex.Message + ex.InnerException?.Message));
        }
    }

    private void InitXml()
    {
        ADODChamberlainsBehavior.xmlManager = new HouseTroopsXmlManager();
        ADODChamberlainsBehavior.xmlManager.SetCampaignFilePath(Campaign.Current);
        ADODChamberlainsBehavior.xmlManager.SetCampaignHouseTroopXml();
        ADODChamberlainsBehavior.xmlManager.CreateXmlIfNeeded();
        this.InitAllUnitSetting();
    }

    private void InitAllUnitSetting()
    {
        List<string> troopIds = ADODChamberlainsBehavior.xmlManager.GetAllHouseTroopId();
        foreach (string troopId in troopIds)
        {
            CharacterObject unit = ADODChamberlainsBehavior.GetCharacterObject(troopId);
            if (unit != null)
            {
                ADODChamberlainsBehavior.selectedUnit = unit;

                // Update the unit's culture to the player's culture
                unit.Culture = Hero.MainHero.Culture;

                this.InitUnitName(unit);
                this.InitUnitSkill(unit);
                this.InitUnitEquipment(unit);
                ADODChamberlainsBehavior.selectedUnit = null;
            }
        }
    }


    private void InitUnitName(CharacterObject unit)
    {
        this.ChangeUnitName(unit, HouseTroopsXmlManager.GetUnitName(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), unit.StringId));
    }



    private void InitUnitSkill(CharacterObject unit)
    {
        List<HouseTroopsSkillRecord> skillRecord = HouseTroopsXmlManager.GetUnitSkill(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), unit.StringId);
        foreach (SkillObject skill in Skills.All.ToList())
        {
            HouseTroopsSkillRecord record = skillRecord.FirstOrDefault(x => x.skill.Equals(skill.StringId));
            if (record != null)
            {
                this.SetSkillValue(unit, skill, record.value);
            }
        }
    }

    private void InitUnitEquipment(CharacterObject unit)
    {
        List<HouseTroopsEquipmentRecord> records = HouseTroopsXmlManager.GetUnitEquipmentId(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), unit.StringId);
        this.CreateEmptyEquipmentForInit(unit);
        for (int i = 0; i < records.Count; i++)
        {
            ItemObject item = ADODChamberlainsBehavior.GetItemObject(records[i].itemId);
            if (item != null)
            {
                this.ChangeUnitEquipment(ADODChamberlainsBehavior.selectedUnit, records[i].index, item, records[i].equipmentSet, records[i].isCivilan);
            }
        }
    }

    private void SaveChangeToXml()
    {
        string path = ADODChamberlainsBehavior.xmlManager.GetCampaignFilePath();
        if (!string.IsNullOrEmpty(path))
        {
            ADODChamberlainsBehavior.xmlManager.SaveXml(path);
        }
    }

    private bool isUnitSelected()
    {
        return ADODChamberlainsBehavior.selectedUnit != null;
    }

    private void AddHouseTroopMenu(CampaignGameStarter campaignGameStarter)
    {
        this.InitXml();
        campaignGameStarter.AddGameMenu("house_troop_menu", "You approach the Chamberlain's chambers. You see him going over upkeep costs for your party.", null, GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
        campaignGameStarter.AddGameMenuOption("town_keep", "house_troop_keep", "Visit the Chamberlain's Chambers", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            return this.IsInKingdom() && this.IsTown() && this.IsOwnedSettlement();
        }, delegate (MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("house_troop_menu");
        }, false, 1, false, null);

        campaignGameStarter.AddGameMenu("house_troop_menu", "You approach the Chamberlain's chambers. You see him going over upkeep costs for your party.", null, GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
        campaignGameStarter.AddGameMenuOption("castle", "house_troop_keep", "Visit the Chamberlain's Chambers", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
            return this.IsInKingdom() && this.IsCastle() && this.IsOwnedSettlement();
        }, delegate (MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("house_troop_menu");
        }, false, 1, false, null);

        campaignGameStarter.AddGameMenu("house_troop_menu", "{=house_troop_menu}Chamberlain's Roster\n{unit_info}", delegate (MenuCallbackArgs args)
        {
            this.UpdateTextVariables();
        }, GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);

        campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_select", "Edit House Army", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return true;
        }, delegate (MenuCallbackArgs args)
        {
            this.DisplayUnitList();
        }, false, -1, false, null);

        campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_rename_unit", "Rename House Troop", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return this.isUnitSelected();
        }, delegate (MenuCallbackArgs args)
        {
            this.GetInputName();
        }, false, -1, false, null);


        campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_recruit", "Recruit House Troops",
            args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                return true;
            },
            args => RecruitHouseTroops(),
            false, -1, false, null);


        AddExportImportButtons(campaignGameStarter);

        campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_leave", "Leave chambers", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, delegate (MenuCallbackArgs args)
        {
            ADODChamberlainsBehavior.selectedUnit = null;
            if (this.IsTown())
            {
                GameMenu.SwitchToMenu("town_keep");
            }
            else if (this.IsCastle())
            {
                GameMenu.SwitchToMenu("castle");
            }
        }, false, 0, false, null);

        this.ShowManageEquipmentOption(campaignGameStarter);
    }

    private void ShowManageEquipmentOption(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_select_equipment_set", "{=house_troop_menu_select_equipment_set}Select Equipment Set", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return this.isUnitSelected();
        }, delegate (MenuCallbackArgs args)
        {
            this.SelectEquipmentSet();
        }, false, -1, false, null);

        campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_manage_equipment", "{=house_troop_menu_manage_equipment}Manage Equipment", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return this.isUnitSelected();
        }, delegate (MenuCallbackArgs args)
        {
            if (ADODChamberlainsBehavior.set > -1)
            {
                this.InventoryScreenForEquipmentSelection();
            }
            else
            {
                this.DisplayMessage("{=house_troop_menu_invalid_set}Choose or create an Equipment Set for your House Troop first.");
            }
        }, false, -1, false, null);

        campaignGameStarter.AddGameMenuOption("house_troop_menu", "house_troop_menu_delete_equipment_set", "{=house_troop_menu_delete_equipment_set}Delete Equipment Set", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return this.isUnitSelected() && ADODChamberlainsBehavior.set > -1;
        }, delegate (MenuCallbackArgs args)
        {
            this.DeleteEquipmentSet(ADODChamberlainsBehavior.selectedUnit);
            this.UpdateTextVariables();
        }, false, -1, false, null);
    }

    private void RecruitHouseTroops()
    {
        ShowHouseTroopPurchaseDialog();
    }

    private void ShowHouseTroopPurchaseDialog()
    {
        var troops = ADODChamberlainsBehavior.CORE_UNIT_LIST.Select(MBObjectManager.Instance.GetObject<CharacterObject>).ToList();

        string title = new TextObject("Recruit House Troops", null).ToString();

        List<InquiryElement> options = troops.Select(troop =>
        {
            int troopCost = CalculateHouseTroopCost(troop);

            return new InquiryElement(troop, troop.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(troop)),
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

        // Calculate troop cost dynamically based on the troop's index in the list
        int troopCost = CalculateHouseTroopCost(selectedTroop);

        int totalCost = quantity * troopCost;

        if (Hero.MainHero.Gold >= totalCost)
        {
            // Deduct gold
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -totalCost);

            // Assign the player's culture to the recruited troops
            selectedTroop.Culture = Hero.MainHero.Culture;

            // Add the selected troops to the party
            TroopRoster roster = MobileParty.MainParty.MemberRoster;
            roster.AddToCounts(selectedTroop, quantity);

            // Display confirmation message
            InformationManager.DisplayMessage(new InformationMessage($"{quantity} {selectedTroop.Name} recruited to your party for {totalCost} Gold Dragons."));
        }
        else
        {
            // Display error message if the player cannot afford the troops
            InformationManager.DisplayMessage(new InformationMessage("You cannot afford to recruit your troops."));
        }
    }


    private int CalculateHouseTroopCost(CharacterObject troop)
    {
        int baseCost = 100;
        int index = ADODChamberlainsBehavior.CORE_UNIT_LIST.IndexOf(troop.StringId);

      
        if (index == -1)
        {
            // Log a warning or provide a fallback cost in case the troop is not in the list
            InformationManager.DisplayMessage(new InformationMessage($"Warning: Troop '{troop.StringId}' not found in CORE_UNIT_LIST."));
            return baseCost;
        }

        return baseCost + (index * 100);
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
            this.RenameUnit(name);
            this.AfterChange();
        }, delegate ()
        {
            InformationManager.HideInquiry();
        }, false, null, "", ""), false, false);
    }

    private void RenameUnit(string name)
    {
        this.ChangeUnitName(ADODChamberlainsBehavior.selectedUnit, name);
        ADODChamberlainsBehavior.xmlManager.RenameUnit(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), ADODChamberlainsBehavior.selectedUnit.StringId, name);
        this.SaveChangeToXml();
    }

    private bool IsEliteBasicTroop(CharacterObject unit)
    {
        List<CultureObject> list = new List<CultureObject>();
        foreach (CultureObject cultureObject in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
        {
            if (cultureObject.IsMainCulture && !list.Contains(cultureObject))
            {
                list.Add(cultureObject);
            }
        }
        return list.FirstOrDefault(x => x.EliteBasicTroop == unit) != null;
    }

    private void AfterChange()
    {
        this.UpdateTextVariables();
        this.RefreshMenu();
    }

    private void UpdateTextVariables()
    {
        MBTextManager.SetTextVariable("unit_info", this.DisplayUnitInfo(), false);
    }

    private string DisplayUnitInfo()
    {
        string info = "";
        TextObject text = new TextObject("", null);
        if (this.isUnitSelected())
        {
            info += "Name: {name}\n";
            info += "Level: {level}\n";
            info += "Group: {group}\n";
            info += "Equipment Set: {set}\n";
            if (ADODChamberlainsBehavior.set > -1)
            {
                Equipment equipment = null;
                if (ADODChamberlainsBehavior.selectedUnit.BattleEquipments.ToList().Count > 0)
                {
                    equipment = ADODChamberlainsBehavior.selectedUnit.BattleEquipments.ToList()[ADODChamberlainsBehavior.set];
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
            text.SetTextVariable("name", ADODChamberlainsBehavior.selectedUnit.Name);
            text.SetTextVariable("level", ADODChamberlainsBehavior.selectedUnit.Level);
            text.SetTextVariable("gender", ADODChamberlainsBehavior.selectedUnit.IsFemale ? "F" : "M");
            text.SetTextVariable("group", (ADODChamberlainsBehavior.selectedUnit.DefaultFormationGroup == 4) ? "Skirmisher" : ((FormationClass)ADODChamberlainsBehavior.selectedUnit.DefaultFormationGroup).ToString());
            text.SetTextVariable("set", ADODChamberlainsBehavior.set);
        }
        return text.ToString();
    }

    private void DisplayUnitList()
    {
        TextObject title = new TextObject("Select", null);
        TextObject info = new TextObject("Select the House Troop you want to modify", null);
        TextObject confirm = new TextObject("Confirm", null);
        TextObject cancel = new TextObject("Cancel", null);
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title.ToString(), info.ToString(), this.GetHouseTroop(), true, 1, 1, confirm.ToString(), cancel.ToString(), delegate (List<InquiryElement> unit)
        {
            ADODChamberlainsBehavior.selectedUnit = (CharacterObject)this.GetSelectedItem(unit);
            ADODChamberlainsBehavior.set = -1;
            this.AfterChange();
        }, delegate (List<InquiryElement> noItems)
        {
            InformationManager.HideInquiry();
        }, "", false), false, false);
    }

    private List<InquiryElement> GetHouseTroop()
    {
        List<InquiryElement> list = new List<InquiryElement>();
        List<string> houseTroopId = ADODChamberlainsBehavior.xmlManager.GetAllHouseTroopId();
        foreach (string unitId in houseTroopId)
        {
            CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>(unitId);
            ImageIdentifier image = (character == null) ? null : new ImageIdentifier(CharacterCode.CreateFrom(character));
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
                return (sort != 0) ? sort : string.Compare(e2.DefaultFormationClass.ToString(), e.DefaultFormationClass.ToString());
            });
        }
        catch { }
        return list;
    }

    private List<ItemObject> GetItemPool()
    {
        List<ItemObject> items = new List<ItemObject>();

        foreach (ItemRosterElement item in ADODChamberlainsBehavior.inventoryBackUp)
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
            List<ItemObject> items = this.GetItemPool();
            foreach (ItemObject item in items)
            {
                left.AddToCounts(item, 3);
            }
        }
        InventoryLogic inventoryLogic = new InventoryLogic(null);
        CharacterObject hero = Hero.MainHero.CharacterObject;
        MobileParty party = MobileParty.MainParty;
        HouseTroopsMarketData data = new HouseTroopsMarketData();
        InventoryManager.InventoryCategoryType type = InventoryManager.InventoryCategoryType.None;
        TextObject name = ADODChamberlainsBehavior.selectedUnit.Name;
        TextObject text = new TextObject($"{name} : {ADODChamberlainsBehavior.selectedUnit.DefaultFormationClass}", null);
        inventoryLogic.Initialize(left, party, true, true, hero, type, data, false, text, null, null);
        HouseTroopsUtil.GetInstanceField<InventoryManager>(InventoryManager.Instance, "_currentMode").SetValue(InventoryManager.Instance, InventoryMode.Trade);
        HouseTroopsUtil.GetInstanceField<InventoryManager>(InventoryManager.Instance, "_inventoryLogic").SetValue(InventoryManager.Instance, inventoryLogic);
        HouseTroopsUtil.GetInstanceField<InventoryManager>(InventoryManager.Instance, "_doneLogicExtrasDelegate").SetValue(InventoryManager.Instance, new InventoryManager.DoneLogicExtrasDelegate(this.InventoryDoneLogic));
        return inventoryLogic;
    }

    private void InventoryScreenForEquipmentSelection()
    {
        this.ClearPlayerInventory();
        this.ChangePlayerSkillToUnit();
        this.ChangePlayerGender();
        InventoryLogic inventoryLogic = this.newCustomInventoryLogic(true);
        InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
        inventoryState.InitializeLogic(inventoryLogic);
        Game.Current.GameStateManager.PushState(inventoryState, 0);
        this.ChangePlayerEquipmentToUnit(inventoryLogic);
    }

    private void ChangePlayerEquipmentToUnit(InventoryLogic inventoryLogic)
    {
        object partyInitialEquipment = this.GetPartyInitialEquipment(inventoryLogic);
        PropertyInfo property = partyInitialEquipment.GetType().GetProperties().FirstOrDefault();
        Dictionary<CharacterObject, Equipment[]> equipments = new Dictionary<CharacterObject, Equipment[]>();
        if (property != null)
        {
            equipments = (Dictionary<CharacterObject, Equipment[]>)property.GetValue(partyInitialEquipment);
            ADODChamberlainsBehavior.playerEquipment = new Dictionary<CharacterObject, Equipment[]>(equipments);
        }
        Dictionary<CharacterObject, Equipment[]> unitEquipments = new Dictionary<CharacterObject, Equipment[]>();
        foreach (KeyValuePair<CharacterObject, Equipment[]> equipment in equipments)
        {
            if (equipment.Key.IsPlayerCharacter)
            {
                unitEquipments.Add(equipment.Key, new Equipment[]
                {
                        new Equipment(ADODChamberlainsBehavior.selectedUnit.BattleEquipments.ToList()[ADODChamberlainsBehavior.set]),
                        new Equipment(ADODChamberlainsBehavior.selectedUnit.CivilianEquipments.ToList()[ADODChamberlainsBehavior.set])
                });
            }
        }
        property.SetValue(partyInitialEquipment, unitEquipments);
        inventoryLogic.Reset(false);
    }

    private void ChangePlayerGender()
    {
        Hero player = Hero.MainHero;
        ADODChamberlainsBehavior.playerIsFemale = player.IsFemale;
        if (ADODChamberlainsBehavior.playerIsFemale != ADODChamberlainsBehavior.selectedUnit.IsFemale)
        {
            player.UpdatePlayerGender(ADODChamberlainsBehavior.selectedUnit.IsFemale);
        }
    }

    private void ResetPlayerGender()
    {
        Hero player = Hero.MainHero;
        if (ADODChamberlainsBehavior.playerIsFemale != player.IsFemale)
        {
            player.UpdatePlayerGender(ADODChamberlainsBehavior.playerIsFemale);
        }
    }

    private object GetPartyInitialEquipment(InventoryLogic inventoryLogic)
    {
        return HouseTroopsUtil.GetInstanceField<InventoryLogic>(inventoryLogic, "_partyInitialEquipment").GetValue(inventoryLogic);
    }

    private void ResetPlayerEquipment(InventoryLogic inventoryLogic)
    {
        object partyInitialEquipment = this.GetPartyInitialEquipment(inventoryLogic);
        PropertyInfo property = partyInitialEquipment.GetType().GetProperties().FirstOrDefault();
        if (property != null)
        {
            property.SetValue(partyInitialEquipment, ADODChamberlainsBehavior.playerEquipment);
        }
    }

    private void ChangePlayerSkillToUnit()
    {
        ADODChamberlainsBehavior.playerSkill.Clear();
        Hero player = Hero.MainHero;
        foreach (SkillObject skill in Skills.All.ToList())
        {
            ADODChamberlainsBehavior.playerSkill.Add(new HouseTroopsSkillRecord(skill.StringId, player.GetSkillValue(skill)));
            player.SetSkillValue(skill, ADODChamberlainsBehavior.selectedUnit.GetSkillValue(skill));
        }
    }

    private void ResetPlayerSkill()
    {
        List<SkillObject> skills = Skills.All.ToList();
        Hero player = Hero.MainHero;
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].StringId.Equals(ADODChamberlainsBehavior.playerSkill[i].skill))
            {
                player.SetSkillValue(skills[i], ADODChamberlainsBehavior.playerSkill[i].value);
            }
        }
    }

    private void ClearPlayerInventory()
    {
        PartyBase partyBase = PartyBase.MainParty;
        ADODChamberlainsBehavior.inventoryBackUp = partyBase.ItemRoster;
        HouseTroopsUtil.GetInstanceProperty<PartyBase>(partyBase, "ItemRoster").SetValue(partyBase, new ItemRoster());
    }

    private void ResetPlayerInventory()
    {
        PartyBase partyBase = PartyBase.MainParty;
        HouseTroopsUtil.GetInstanceProperty<PartyBase>(partyBase, "ItemRoster").SetValue(partyBase, ADODChamberlainsBehavior.inventoryBackUp);
    }

    private void InventoryDoneLogic()
    {
        InventoryLogic inventoryLogic = this.newCustomInventoryLogic(false);
        List<HouseTroopsEquipmentRecord> records = this.GetNewEquipment(inventoryLogic);
        foreach (HouseTroopsEquipmentRecord record in records)
        {
            this.ChangeUnitEquipment(ADODChamberlainsBehavior.selectedUnit, record.index, ADODChamberlainsBehavior.GetItemObject(record.itemId), record.equipmentSet, record.isCivilan);
        }
        this.ResetPlayerEquipment(inventoryLogic);
        this.ResetPlayerSkill();
        this.ResetPlayerGender();
        this.ResetPlayerInventory();
        inventoryLogic.Reset(false);
        this.AfterChange();
    }

    private List<HouseTroopsEquipmentRecord> GetNewEquipment(InventoryLogic inventoryLogic)
    {
        bool isCivilian = false;
        List<Equipment> equipmentList = inventoryLogic.InitialEquipmentCharacter.AllEquipments.ToList();
        List<HouseTroopsEquipmentRecord> records = new List<HouseTroopsEquipmentRecord>();
        foreach (Equipment equipmentSet in equipmentList)
        {
            if (equipmentSet.IsCivilian)
            {
                isCivilian = true;
            }
            for (int i = 0; i < 12; i++)
            {
                if (equipmentSet[i].Item != null)
                {
                    records.Add(new HouseTroopsEquipmentRecord(i, equipmentSet[i].Item.StringId, ADODChamberlainsBehavior.set, isCivilian));
                }
                else
                {
                    records.Add(new HouseTroopsEquipmentRecord(i, "", ADODChamberlainsBehavior.set, isCivilian));
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
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title.ToString(), info.ToString(), this.GetEquipmentSetNumberOption(), true, 1, 1, confirm.ToString(), cancel.ToString(), delegate (List<InquiryElement> selectedSet)
        {
            ADODChamberlainsBehavior.set = (int)this.GetSelectedItem(selectedSet);
            if (ADODChamberlainsBehavior.set == -100)
            {
                this.CloneUnitEquipment(ADODChamberlainsBehavior.selectedUnit);
            }
            this.AfterChange();
        }, delegate (List<InquiryElement> noItems)
        {
            InformationManager.HideInquiry();
        }, "", false), false, false);
    }

    private void DeleteEquipmentSet(CharacterObject unit)
    {
        if (ADODChamberlainsBehavior.set == 0)
        {
            InformationManager.DisplayMessage(new InformationMessage("{=house_troop_menu_delete_equipment_set_error}Cannot delete equipment set 0"));
        }
        else
        {
            List<Equipment> equipments = new List<Equipment>();
            Equipment deleteBattle = ADODChamberlainsBehavior.selectedUnit.BattleEquipments.ToList()[ADODChamberlainsBehavior.set];
            Equipment deleteCiv = ADODChamberlainsBehavior.selectedUnit.CivilianEquipments.ToList()[ADODChamberlainsBehavior.set];
            foreach (Equipment equipment in ADODChamberlainsBehavior.selectedUnit.AllEquipments)
            {
                if (equipment != deleteBattle && equipment != deleteCiv)
                {
                    equipments.Add(equipment);
                }
            }
            this.UpdateSelectedUnitEquipment(unit, equipments);
            ADODChamberlainsBehavior.set = -1;
            this.SaveChangeToXml();
        }
    }

    private void CloneUnitEquipment(CharacterObject unit)
    {
        Equipment civilian = ADODChamberlainsBehavior.selectedUnit.AllEquipments.First(x => x.IsCivilian).Clone(false);
        Equipment battle = ADODChamberlainsBehavior.selectedUnit.AllEquipments.First(x => !x.IsCivilian).Clone(false);
        List<Equipment> equipments = ADODChamberlainsBehavior.selectedUnit.AllEquipments.ToList();
        equipments.Add(battle);
        equipments.Add(civilian);
        this.UpdateSelectedUnitEquipment(unit, equipments);
        this.SaveChangeToXml();
    }

    private void CreateEmptyEquipmentForInit(CharacterObject unit)
    {
        List<Equipment> equipments = new List<Equipment>();
        int count = HouseTroopsXmlManager.GetEquipmentSetNumber(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), unit.StringId);
        if (count == 0)
        {
            count = 1;
        }
        for (int i = 0; i < count; i++)
        {
            equipments.Add(new Equipment(false));
            equipments.Add(new Equipment(true));
        }
        this.UpdateSelectedUnitEquipment(unit, equipments);
    }

    private List<InquiryElement> GetEquipmentSetNumberOption()
    {
        List<InquiryElement> list = new List<InquiryElement>();
        int setNumber = HouseTroopsXmlManager.GetEquipmentSetNumber(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), ADODChamberlainsBehavior.selectedUnit.StringId);
        for (int i = 0; i < setNumber; i++)
        {
            list.Add(new InquiryElement(i, i.ToString(), null));
        }
        list.Add(new InquiryElement(-100, "+", null));
        return list;
    }

    private void ChangeUnitEquipment(CharacterObject unit, int slot, ItemObject item, int set, bool isCivilian = false)
    {
        List<Equipment> civilian = (from x in unit.AllEquipments
                                    where x.IsCivilian
                                    select x).ToList();
        List<Equipment> battle = (from x in unit.AllEquipments
                                  where !x.IsCivilian
                                  select x).ToList();
        EquipmentElement equipmentElement = (item != null) ? new EquipmentElement(item, null, null, false) : default;
        if (isCivilian)
        {
            civilian[set][slot] = equipmentElement;
        }
        else
        {
            battle[set][slot] = equipmentElement;
        }
        battle.AddRange(civilian);
        this.UpdateSelectedUnitEquipment(unit, battle);
    }

    private void UpdateSelectedUnitEquipment(CharacterObject unit, List<Equipment> equipments)
    {
        MBEquipmentRoster roster = new MBEquipmentRoster();
        HouseTroopsUtil.GetInstanceField<MBEquipmentRoster>(roster, "_equipments").SetValue(roster, new MBList<Equipment>(equipments));
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
            this.ManageUpgradePath(name);
        }, delegate
        {
            InformationManager.HideInquiry();
        }, false, null, "", ""), false, false);
    }

    private CharacterObject CreateCharacterObject(string unitName)
    {
        string unitId = "adod_house_troop_" + unitName.Replace(' ', '_').ToLower();
        CharacterObject newUnit = MBObjectManager.Instance.CreateObject<CharacterObject>(unitId);

        // Assign the player's culture to the new troop
        newUnit.Culture = Hero.MainHero.Culture;

        HouseTroopsUtil.GetInstanceProperty<CharacterObject>(newUnit, "UpgradeTargets").SetValue(newUnit, new CharacterObject[0]);
        HouseTroopsUtil.GetInstanceProperty<CharacterObject>(newUnit, "BodyPropertyRange").SetValue(newUnit, MBObjectManager.Instance.GetObject<MBBodyProperty>("fighter_custom"));
        newUnit.StringId = unitId;
        this.ChangeUnitName(newUnit, unitName);
        newUnit.Level = ADODChamberlainsBehavior.selectedUnit.Level + 5;

        HouseTroopsUtil.GetInstanceField<CharacterObject>(newUnit, "CharacterSkills").SetValue(newUnit, new MBCharacterSkills());
        foreach (SkillObject skill in Skills.All.ToList())
        {
            if (ADODChamberlainsBehavior.selectedUnit.GetSkillValue(skill) > 0)
            {
                this.SetSkillValue(newUnit, skill, ADODChamberlainsBehavior.selectedUnit.GetSkillValue(skill) + 50);
            }
        }
        this.CreateEmptyEquipmentForInit(newUnit);

        return newUnit;
    }


    private void ManageUpgradePath(string newUnitName)
    {
        CharacterObject unit = this.CreateCharacterObject(newUnitName);
        ADODChamberlainsBehavior.xmlManager.AddUnitToXml(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), ADODChamberlainsBehavior.selectedUnit.StringId, unit);
        this.AddUpgradePath(unit);
    }

    private void AddUpgradePath(CharacterObject newUnit)
    {
        List<CharacterObject> newUpgradePath = new List<CharacterObject>();
        PropertyInfo property2 = HouseTroopsUtil.GetInstanceProperty<CharacterObject>(ADODChamberlainsBehavior.selectedUnit, "UpgradeTargets");
        CharacterObject current = ADODChamberlainsBehavior.selectedUnit.UpgradeTargets.Length > 0 ? ADODChamberlainsBehavior.selectedUnit.UpgradeTargets[0] : null;
        if (current != null)
        {
            newUpgradePath.Add(current);
        }
        newUpgradePath.Add(newUnit);
        property2.SetValue(ADODChamberlainsBehavior.selectedUnit, newUpgradePath.ToArray());
    }

    private void ConfirmRemoveTroop(CharacterObject unit)
    {
        TextObject name = unit.Name;
        InformationManager.ShowInquiry(new InquiryData("Confirm", $"Delete {name}", true, true, "Confirm", "Cancel", delegate
        {
            this.RemoveTroop(unit);
        }, null, "", 0f, null, null, null), true, false);
    }

    private void RemoveTroop(CharacterObject deleteUnit)
    {
        List<CharacterObject> unitRelated = new List<CharacterObject>();
        foreach (string troopId in ADODChamberlainsBehavior.xmlManager.GetAllHouseTroopId())
        {
            CharacterObject troop = ADODChamberlainsBehavior.GetCharacterObject(troopId);
            if (troop != null && troop.UpgradeTargets.Length > 0 && troop.UpgradeTargets.Contains(deleteUnit))
            {
                unitRelated.Add(troop);
                PropertyInfo property = HouseTroopsUtil.GetInstanceProperty<CharacterObject>(troop, "UpgradeTargets");
                property.SetValue(troop, troop.UpgradeTargets.Where(x => !x.StringId.Equals(deleteUnit.StringId)).ToArray());
            }
        }
        if (unitRelated.Count > 0)
        {
            foreach (CharacterObject related in unitRelated)
            {
                XDocument doc = ADODChamberlainsBehavior.xmlManager.RemoveUpgradePathFromXml(ADODChamberlainsBehavior.xmlManager.GetOriginXml(), related, deleteUnit);
                doc.Save(ADODChamberlainsBehavior.xmlManager.GetTroopFilePath());
                doc = ADODChamberlainsBehavior.xmlManager.RemoveUpgradePathFromXml(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), related, deleteUnit);
                doc.Save(ADODChamberlainsBehavior.xmlManager.GetCampaignFilePath());
            }
        }
        this.RemoveUnitFromMap(deleteUnit);
        XDocument doc2 = ADODChamberlainsBehavior.xmlManager.RemoveUnitFromXml(ADODChamberlainsBehavior.xmlManager.GetOriginXml(), deleteUnit);
        doc2.Save(ADODChamberlainsBehavior.xmlManager.GetTroopFilePath());
        doc2 = ADODChamberlainsBehavior.xmlManager.RemoveUnitFromXml(ADODChamberlainsBehavior.xmlManager.GetCampaignXml(), deleteUnit);
        doc2.Save(ADODChamberlainsBehavior.xmlManager.GetCampaignFilePath());
        ADODChamberlainsBehavior.selectedUnit = null;
        this.AfterChange();
    }

    private void RemoveUnitFromMap(CharacterObject unit)
    {
        FieldInfo field = HouseTroopsUtil.GetInstanceField<CampaignObjectManager>(Campaign.Current.CampaignObjectManager, "_mobileParties");
        List<MobileParty> partyBaseList = (List<MobileParty>)field.GetValue(Campaign.Current.CampaignObjectManager);
        foreach (MobileParty party in partyBaseList)
        {
            PartyBase partyBase = party.Party;
            if (partyBase.MemberRoster.Contains(unit))
            {
                partyBase.MemberRoster.RemoveTroop(unit, partyBase.MemberRoster.GetTroopCount(unit), default(UniqueTroopDescriptor), 0);
            }
            if (partyBase.PrisonRoster.Contains(unit))
            {
                partyBase.PrisonRoster.RemoveTroop(unit, partyBase.PrisonRoster.GetTroopCount(unit), default(UniqueTroopDescriptor), 0);
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

        XDocument currentTroopsXml = ADODChamberlainsBehavior.xmlManager.GetCampaignXml();

        if (currentTroopsXml != null)
        {
            foreach (XElement npcElement in currentTroopsXml.Element("NPCCharacters").Elements("NPCCharacter"))
            {
                XElement newNpcElement = new XElement(npcElement);

                string troopId = npcElement.Attribute("id").Value;
                CharacterObject unit = ADODChamberlainsBehavior.GetCharacterObject(troopId);

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

            ADODChamberlainsBehavior.xmlManager.SetCampaignFilePath(Campaign.Current);
            doc.Save(ADODChamberlainsBehavior.xmlManager.GetCampaignFilePath());

            foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
            {
                string troopId = npcElement.Attribute("id").Value;
                CharacterObject unit = ADODChamberlainsBehavior.GetCharacterObject(troopId);

                if (unit != null)
                {
                    if (npcElement.Attribute("name") != null)
                    {
                        this.ChangeUnitName(unit, npcElement.Attribute("name").Value);
                    }

                    if (npcElement.Element("Equipments") != null)
                    {
                        List<HouseTroopsEquipmentRecord> equipmentRecords = HouseTroopsXmlManager.GetUnitEquipmentId(doc, troopId);
                        foreach (HouseTroopsEquipmentRecord record in equipmentRecords)
                        {
                            ItemObject item = ADODChamberlainsBehavior.GetItemObject(record.itemId);
                            if (item != null)
                            {
                                this.ChangeUnitEquipment(unit, record.index, item, record.equipmentSet, record.isCivilan);
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
        return unit.AllEquipments.Where(x => !x.IsCivilian).ToList();
    }

    internal static List<Equipment> GetTroopCivEquipmentSet(CharacterObject unit)
    {
        return unit.AllEquipments.Where(x => x.IsCivilian).ToList();
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

    public override void SyncData(IDataStore dataStore)
    {
    }

    private static List<string> CORE_UNIT_LIST = new List<string>
        {
            "adod_house_troop_recruit",
            "adod_house_troop_infantry",
            "adod_house_troop_archer",
            "adod_house_troop_trained_infantry",
            "adod_house_troop_veteran_infantry",
            "adod_house_troop_heavy_infantry",
            "adod_house_troop_spearman",
            "adod_house_troop_cavalry",
            "adod_house_troop_heavy_cavalry",
            "adod_house_troop_bowman",
            "adod_house_troop_heavy_bowman",
            "adod_house_troop_marksman",
            "adod_house_troop_banner_bearer",
        };


    private static HouseTroopsXmlManager xmlManager;
    private static CharacterObject selectedUnit;
    private static int set = -1;
    private static Dictionary<CharacterObject, Equipment[]> playerEquipment;
    private static bool playerIsFemale;
    private static List<HouseTroopsSkillRecord> playerSkill = new List<HouseTroopsSkillRecord>();
    private static ItemRoster inventoryBackUp;

}
