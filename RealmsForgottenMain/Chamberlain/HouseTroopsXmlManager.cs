using RealmsForgotten;
using RealmsForgotten.Chamberlain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

internal class HouseTroopsXmlManager
{
    public HouseTroopsXmlManager()
    {
        string path = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);
        string projectPath = Path.GetFullPath(Path.Combine(path, @"..\..\ModuleData\"));
        campaignTroopBackupFolderPath = Path.Combine(projectPath, ChamberlainConfig.ChamberlainFolder);
        troopFilePath = Path.Combine(campaignTroopBackupFolderPath, ChamberlainConfig.BackupXmlFile); 
        CreateFolderIfNeeded();
    }
    internal void CreateFolderIfNeeded()
    {
        if (!Directory.Exists(campaignTroopBackupFolderPath))
        {
            Directory.CreateDirectory(campaignTroopBackupFolderPath);
        }
    }
    internal bool IsCampaignFileExists()
    {
        if (File.Exists(campaignTroopFilePath))
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
        return IsCampaignFileExists() ? campaignTroopFilePath : "";
    }
    internal string GetTroopFilePath()
    {
        return troopFilePath;
    }
    internal void SetCampaignFilePath(Campaign campaign)
    {
        campaignTroopFilePath = Path.Combine(campaignTroopBackupFolderPath, campaign.UniqueGameId + "_house_troop.xml");
    }
    internal void SetCampaignHouseTroopXml()
    {
        if (!File.Exists(campaignTroopFilePath))
        {
            XDocument doc = XDocument.Load(troopFilePath);
            doc.Save(campaignTroopFilePath);
        }
    }
    internal XDocument GetCampaignXml()
    {
        return IsCampaignFileExists() ? XDocument.Load(campaignTroopFilePath) : null;
    }
    internal XDocument GetOriginXml()
    {
        return XDocument.Load(troopFilePath);
    }
    internal void CreateXmlIfNeeded()
    {
        if (!File.Exists(campaignTroopFilePath))
        {
            XDocument doc = XDocument.Load(troopFilePath);
            doc.Save(campaignTroopFilePath);
        }
    }
    internal void SaveXml(string filePath)
    {
        XDocument doc = XDocument.Load(filePath);

        doc = ClearAllEquipmentElement(doc);

        doc = ChangeAllEquipmentElement(doc);

        doc.Save(filePath);
    }
    internal static XDocument ClearAllEquipmentElement(XDocument doc)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            npcElement.Elements("Equipments").Remove();
            npcElement.Elements("equipmentSet").Remove();
            npcElement.Elements("equipment").Remove();
        }
        return doc;
    }
    internal static XDocument ChangeAllEquipmentElement(XDocument doc)
    {
        XElement npcs = doc.Element("NPCCharacters");
        foreach (XElement npcElement in npcs.Elements("NPCCharacter"))
        {
            CharacterObject unit = RFChamberlainsBehavior.GetCharacterObject(npcElement.Attribute("id").Value);
            if (unit != null)
            {
                npcElement.Elements("Equipments").Remove();

                XElement equipmentsElement = new XElement("Equipments");

                foreach (var equipment in unit.BattleEquipments)
                {
                    XElement battleRosterElement = new XElement("EquipmentRoster", new XAttribute("civilian", "false"));
                    for (int index = 0; index < 12; index++)
                    {
                        ItemObject item = equipment[index].Item;
                        if (item != null)
                        {
                            battleRosterElement.Add(new XElement("equipment",
                                new XAttribute("slot", equipmentSlot[index]),
                                new XAttribute("id", "Item." + item.StringId)));
                        }
                    }
                    equipmentsElement.Add(battleRosterElement);
                }

                // Add civilian equipment
                foreach (var equipment in unit.CivilianEquipments)
                {
                    XElement civilianRosterElement = new XElement("EquipmentRoster", new XAttribute("civilian", "true"));
                    for (int index = 0; index < 12; index++)
                    {
                        ItemObject item = equipment[index].Item;
                        if (item != null)
                        {
                            civilianRosterElement.Add(new XElement("equipment",
                                new XAttribute("slot", equipmentSlot[index]),
                                new XAttribute("id", "Item." + item.StringId)));
                        }
                    }
                    equipmentsElement.Add(civilianRosterElement);
                }
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
                skillsElement.Elements().Remove();
            }
        }
        return doc;
    }
    internal static XDocument ChangeAllUnitSkillElement(XDocument doc)
    {
        foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            XElement skillElement = npcElement.Element("skills");
            CharacterObject unit = RFChamberlainsBehavior.GetCharacterObject(npcElement.Attribute("id").Value);
            if (unit != null)
            {
                foreach (SkillObject skill in Skills.All.ToList())
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
                doc.Save(campaignTroopFilePath);
            }
        }
    }
    internal void ChangeTroopGender(XDocument doc, string unitId)
    {
        foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                bool currentGender = bool.Parse(npcElement.Attribute("is_female").Value);
                npcElement.Attribute("is_female").Value = (!currentGender).ToString().ToLower();
                doc.Save(campaignTroopFilePath);
            }
        }
    }
    internal void ChangeTroopRace(XDocument doc, string unitId, string raceName)
    {
        foreach (XElement npcElement in doc.Element("NPCCharacters").Elements("NPCCharacter"))
        {
            if (npcElement.Attribute("id").Value.Equals(unitId))
            {
                npcElement.Attribute("race").Value = raceName;
                doc.Save(campaignTroopFilePath);
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
                npcElement.Attribute("is_female").Value = currentGender.Equals("M") ? isFemale.ToString().ToLower() : (!isFemale).ToString().ToLower();
                doc.Save(campaignTroopFilePath);
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
                doc.Save(campaignTroopFilePath);
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
                doc.Save(campaignTroopFilePath);
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
        XDocument doc = XDocument.Load(campaignTroopFilePath);
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
                                int slot = Array.IndexOf(equipmentSlot, equipment.Attribute("slot").Value);
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
                                int slot2 = Array.IndexOf(equipmentSlot, equipment2.Attribute("slot").Value);
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
                            int slot3 = Array.IndexOf(equipmentSlot, equipment3.Attribute("slot").Value);
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
                        count = num + (xattribute != null && xattribute.Value.Equals("false") ? 1 : 0);
                    }
                }
                else
                {
                    foreach (XElement equipmentRosterElement in equipmentsElement.Elements("EquipmentRoster"))
                    {
                        int num2 = count;
                        XAttribute xattribute2 = equipmentRosterElement.Attribute("civilian");
                        count = num2 + (xattribute2 != null && xattribute2.Value.Equals("false") ? 1 : 0);
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
        templateXElement.Attribute("level").Value = (int.Parse(templateXElement.Attribute("level").Value) + 5).ToString() ?? "";
        previousXElement.Element("upgrade_targets").Add(new XElement("upgrade_target", new XAttribute("id", "NPCCharacter." + newUnit.StringId)));
        previousXElement.AddAfterSelf(templateXElement);
        doc.Save(GetTroopFilePath());
        doc.Save(GetCampaignFilePath());
    }
    internal XDocument RemoveUpgradePathFromXml(XDocument doc, CharacterObject unitDeleteFrom, CharacterObject unitDeleted)
    {
        XElement npcs = doc.Element("NPCCharacters");
        npcs.Elements("NPCCharacter").First((x) => x.Attribute("id").Value.Equals(unitDeleteFrom.StringId)).Element("upgrade_targets").Elements("upgrade_target").First((x) => x.Attribute("id").Value.Equals("NPCCharacter." + unitDeleted.StringId)).Remove();
        return doc;
    }
    internal XDocument RemoveUnitFromXml(XDocument doc, CharacterObject unitDeleted)
    {
        XElement npcs = doc.Element("NPCCharacters");
        npcs.Elements("NPCCharacter").First((x) => x.Attribute("id").Value.Equals(unitDeleted.StringId)).Remove();
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
