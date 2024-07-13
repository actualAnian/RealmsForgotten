using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade
{
    public class StorytellerBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, TextObject> dialogueStrings;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            try
            {
                LoadDialogues("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Mount & Blade II Bannerlord\\Modules\\RealmsForgotten\\bin\\Win64_Shipping_Client\\storyteller_dialogues.xml");
                AddDialogs(campaignGameStarter);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in OnSessionLaunched: {ex.Message}"));
            }
        }

        private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            try
            {
                Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement;
                if (settlement != null && settlement.IsTown && CampaignMission.Current != null)
                {
                    Location location = CampaignMission.Current.Location;
                    if (location != null && location.StringId == "tavern")
                    {
                        LocationCharacter locationCharacter = CreateStoryteller(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                        location.AddCharacter(locationCharacter);
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LocationCharactersAreReadyToSpawn: {ex.Message}"));
            }
        }

        private static LocationCharacter CreateStoryteller(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            try
            {
                CharacterObject storyteller = MBObjectManager.Instance.GetObject<CharacterObject>("the_storyteller");
                int minValue, maxValue;
                Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(storyteller, out minValue, out maxValue, "");
                Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(storyteller.Race, "_settlement");
                AgentData agentData = new AgentData(new SimpleAgentOrigin(storyteller, -1, null, default(UniqueTroopDescriptor)))
                                      .Monster(monsterWithSuffix)
                                      .Age(MBRandom.RandomInt(minValue, maxValue));
                var locationCharacter = new LocationCharacter(agentData,
                                                              new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors),
                                                              "sp_tavern_townsman",
                                                              true,
                                                              relation,
                                                              null,
                                                              true,
                                                              false,
                                                              null,
                                                              false,
                                                              false,
                                                              true);
                return locationCharacter;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in CreateStoryteller: {ex.Message}"));
                return null;
            }
        }

        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddDialogLine("storyteller_greeting", "start", "storyteller_ask", dialogueStrings["storyteller_greeting"].ToString(),
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "the_storyteller",
                null);

            campaignGameStarter.AddPlayerLine("storyteller_ask_story", "storyteller_ask", "storyteller_response", dialogueStrings["storyteller_ask_story"].ToString(),
                null, null);

            campaignGameStarter.AddDialogLine("storyteller_response", "storyteller_response", "storyteller_options", dialogueStrings["storyteller_response"].ToString(),
                null, null);

            // Story 1
            campaignGameStarter.AddPlayerLine("storyteller_option_1", "storyteller_options", "storyteller_story_1_part_1", dialogueStrings["storyteller_option_1"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_1_part_1", "storyteller_story_1_part_1", "storyteller_story_1_part_2", dialogueStrings["storyteller_story_1_part_1"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_1_part_2", "storyteller_story_1_part_2", "storyteller_story_1_part_3", dialogueStrings["storyteller_story_1_part_2"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_1_part_3", "storyteller_story_1_part_3", "storyteller_story_1_part_4", dialogueStrings["storyteller_story_1_part_3"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_1_part_4", "storyteller_story_1_part_4", "storyteller_story_1_part_5", dialogueStrings["storyteller_story_1_part_4"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_1_part_5", "storyteller_story_1_part_5", "storyteller_story_end", dialogueStrings["storyteller_story_1_part_5"].ToString(),
                null, null);

            // Story 2
            campaignGameStarter.AddPlayerLine("storyteller_option_2", "storyteller_options", "storyteller_story_2_part_1", dialogueStrings["storyteller_option_2"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_2_part_1", "storyteller_story_2_part_1", "storyteller_story_2_part_2", dialogueStrings["storyteller_story_2_part_1"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_2_part_2", "storyteller_story_2_part_2", "storyteller_story_2_part_3", dialogueStrings["storyteller_story_2_part_2"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_2_part_3", "storyteller_story_2_part_3", "storyteller_story_2_part_4", dialogueStrings["storyteller_story_2_part_3"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_2_part_4", "storyteller_story_2_part_4", "storyteller_story_2_part_5", dialogueStrings["storyteller_story_2_part_4"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_2_part_5", "storyteller_story_2_part_5", "storyteller_story_2_part_6", dialogueStrings["storyteller_story_2_part_5"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_2_part_6", "storyteller_story_2_part_6", "storyteller_story_end", dialogueStrings["storyteller_story_2_part_6"].ToString(),
                null, null);

            // Story 3
            campaignGameStarter.AddPlayerLine("storyteller_option_3", "storyteller_options", "storyteller_story_3_part_1", dialogueStrings["storyteller_option_3"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_3_part_1", "storyteller_story_3_part_1", "storyteller_story_3_part_2", dialogueStrings["storyteller_story_3_part_1"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_3_part_2", "storyteller_story_3_part_2", "storyteller_story_3_part_3", dialogueStrings["storyteller_story_3_part_2"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_3_part_3", "storyteller_story_3_part_3", "storyteller_story_3_part_4", dialogueStrings["storyteller_story_3_part_3"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("storyteller_story_3_part_4", "storyteller_story_3_part_4", "storyteller_story_3_part_5", dialogueStrings["storyteller_story_3_part_4"].ToString(),
                null, null);
           campaignGameStarter.AddDialogLine("storyteller_story_3_part_5", "storyteller_story_3_part_5", "storyteller_story_end", dialogueStrings["storyteller_story_3_part_5"].ToString(),
                null, null);

            // Ending the story
            campaignGameStarter.AddDialogLine("storyteller_story_end", "storyteller_story_end", "storyteller_end", dialogueStrings["storyteller_story_end"].ToString(),
                null, null);
            campaignGameStarter.AddPlayerLine("storyteller_end", "storyteller_end", "close_window", dialogueStrings["storyteller_end"].ToString(),
                null, null);
            campaignGameStarter.AddDialogLine("close_window", "close_window", "end", "", null, null);
        }

        private void LoadDialogues(string filePath)
        {
            try
            {
                dialogueStrings = new Dictionary<string, TextObject>();

                if (!File.Exists(filePath))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"File not found: {filePath}"));
                    return;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlNodeList stringNodes = doc.SelectNodes("//String");

                foreach (XmlNode stringNode in stringNodes)
                {
                    string id = stringNode.Attributes["id"].Value;
                    string text = stringNode.InnerText;

                    dialogueStrings.Add(id, new TextObject(text));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LoadDialogues: {ex.Message}"));
            }
        }
    }
}
