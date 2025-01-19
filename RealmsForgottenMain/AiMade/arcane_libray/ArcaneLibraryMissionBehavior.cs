using SandBox.Conversation.MissionLogics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade.ArcaneLibrary
{
    public class ArcaneLibraryMissionBehavior : MissionLogic
    {
        private Dictionary<string, TextObject> _dialogueStrings = new(); // Initialize to avoid null warning
        private bool _npcSpawned = false;
        private Agent _npcAgent;  // For NPC reference
        private Agent _playerAgent;  // For Player reference

        public override void AfterStart()
        {
            base.AfterStart();
            // Ensure both player and NPC are spawned after the mission starts
            SpawnPlayer();
            SpawnNPC();
        }

        // Spawning the player at the given position
        private void SpawnPlayer()
        {
            if (Mission.Current != null)
            {
                Vec3 spawnPosition = new Vec3(59.43f, 57.38f, -0.50f);  // Set player spawn position
                Vec2 spawnDirection = new Vec2(1.0f, 0.0f);  // Set player direction

                AgentBuildData playerBuildData = new AgentBuildData(Hero.MainHero.CharacterObject)
                    .InitialPosition(spawnPosition)
                    .InitialDirection(spawnDirection)
                    .Controller(Agent.ControllerType.Player)  // Ensure player control
                    .NoHorses(true);  // No horses for the player in this mission

                _playerAgent = Mission.Current.SpawnAgent(playerBuildData);

                if (_playerAgent != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Player has been spawned in the Arcane Library."));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Failed to spawn player in the Arcane Library."));
                }
            }
        }

        // Spawning the NPC in the mission
        private void SpawnNPC()
        {
            if (!_npcSpawned && Mission.Current != null)
            {
                string npcId = "anorite_monastery_priest";  // NPC's ID
                CharacterObject npcCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(npcId);

                if (npcCharacter != null)
                {
                    Vec3 spawnPosition = new Vec3(55.45f, 41.92f, 0.44f);  // Set NPC spawn position
                    Vec2 spawnDirection = new Vec2(0f, 1f);  // Set NPC direction

                    AgentBuildData npcBuildData = new AgentBuildData(npcCharacter)
                        .NoHorses(true)
                        .InitialPosition(spawnPosition)
                        .InitialDirection(spawnDirection);

                    _npcAgent = Mission.Current.SpawnAgent(npcBuildData);

                    if (_npcAgent != null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("NPC has been spawned in the Arcane Library."));
                        _npcSpawned = true;  // Mark the NPC as spawned
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Failed to find NPC with ID: " + npcId));
                }
            }
        }

      
        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            try
            {
                string path = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);
                LoadDialogues(Path.Combine(path, "arcane_library_dialogues.xml"));

                AddDialogs(campaignGameStarter);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in OnSessionLaunched: {ex.Message}"));
            }
        }
              

        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddDialogLine("arcane_library_greeting", "start", "arcane_library_ask",
                _dialogueStrings["arcane_library_greeting"].ToString(),
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "anorite_monastery_priest", null);

            campaignGameStarter.AddPlayerLine("arcane_library_ask_question", "arcane_library_ask", "arcane_library_response",
                _dialogueStrings["arcane_library_ask_question"].ToString(), null, null);

            campaignGameStarter.AddDialogLine("arcane_library_response", "arcane_library_response", "arcane_library_story_options",
                _dialogueStrings["arcane_library_response"].ToString(), null, null);

            campaignGameStarter.AddPlayerLine("arcane_library_story_1", "arcane_library_story_options", "arcane_library_story_1_part_1",
                _dialogueStrings["arcane_library_story_1"].ToString(), null, null);
            campaignGameStarter.AddDialogLine("arcane_library_story_1_part_1", "arcane_library_story_1_part_1", "arcane_library_story_1_part_2",
                _dialogueStrings["arcane_library_story_1_part_1"].ToString(), null, null);
            campaignGameStarter.AddDialogLine("arcane_library_story_1_part_2", "arcane_library_story_1_part_2", "arcane_library_story_end",
                _dialogueStrings["arcane_library_story_1_part_2"].ToString(), null, null);

            campaignGameStarter.AddPlayerLine("arcane_library_story_2", "arcane_library_story_options", "arcane_library_story_2_part_1",
                _dialogueStrings["arcane_library_story_2"].ToString(), null, null);
            campaignGameStarter.AddDialogLine("arcane_library_story_2_part_1", "arcane_library_story_2_part_1", "arcane_library_story_2_part_2",
                _dialogueStrings["arcane_library_story_2_part_1"].ToString(), null, null);
            campaignGameStarter.AddDialogLine("arcane_library_story_2_part_2", "arcane_library_story_2_part_2", "arcane_library_story_end",
                _dialogueStrings["arcane_library_story_2_part_2"].ToString(), null, null);

            campaignGameStarter.AddDialogLine("arcane_library_story_end", "arcane_library_story_end", "arcane_library_end",
                _dialogueStrings["arcane_library_story_end"].ToString(), null, null);

            campaignGameStarter.AddPlayerLine("arcane_library_end", "arcane_library_end", "close_window",
                _dialogueStrings["arcane_library_end"].ToString(), null, null);
        }

        private void LoadDialogues(string filePath)
        {
            try
            {
                _dialogueStrings = new Dictionary<string, TextObject>();

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
                    string id = stringNode.Attributes["id"]?.Value ?? throw new InvalidDataException("ID not found.");
                    string text = stringNode.InnerText;

                    _dialogueStrings.Add(id, new TextObject(text));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LoadDialogues: {ex.Message}"));
            }
        }

        public void SyncData(IDataStore dataStore) { }
    }
}