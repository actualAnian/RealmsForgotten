using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade.Career
{
    public class ContractorBehavior : CampaignBehaviorBase
    {
        private object isMercenaryCareerOffered;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("isMercenaryCareerOffered", ref isMercenaryCareerOffered);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddDialogs(campaignGameStarter);
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
                        LocationCharacter contractorCharacter = CreateContractor(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                        location.AddCharacter(contractorCharacter);
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LocationCharactersAreReadyToSpawn: {ex.Message}"));
            }
        }

        private static LocationCharacter CreateContractor(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            try
            {
                CharacterObject contractor = MBObjectManager.Instance.GetObject<CharacterObject>("the_contractor");
                int minValue, maxValue;
                Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(contractor, out minValue, out maxValue, "");
                Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(contractor.Race, "_settlement");
                AgentData agentData = new AgentData(new SimpleAgentOrigin(contractor, -1, null, default(UniqueTroopDescriptor)))
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
                InformationManager.DisplayMessage(new InformationMessage($"Error in CreateContractor: {ex.Message}"));
                return null;
            }
        }

        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddDialogLine("contractor_greeting", "start", "contractor_task", "I have an opportunity for you, if you're interested.",
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "the_contractor",
                null);

            campaignGameStarter.AddPlayerLine("contractor_ask_task", "contractor_task", "contractor_career_offer", "Tell me more.",
                null, null);

            campaignGameStarter.AddDialogLine("contractor_career_offer", "contractor_career_offer", "career_final_confirmation", "The Mercenary Guild is looking for associates. Signing with the guild will allow you to receive contracts from time to time from unknown sources, tasks you need to fulfill representing the guild. You will be paid and being successful will raise your ranks among us. Do you want to sign?",
                null, null);

            campaignGameStarter.AddPlayerLine("contractor_final_accept", "career_final_confirmation", "close_window", "Yes, I want to sign.",
                null,
                () =>
                {
                    // Trigger career selection directly
                    var careerSelectionBehavior = Campaign.Current.GetCampaignBehavior<CareerSelectionBehavior>();
                    careerSelectionBehavior?.ApplyMercenaryCareer();
                });

            campaignGameStarter.AddPlayerLine("contractor_final_decline", "career_final_confirmation", "close_window", "No, thank you.",
                null, null);
        }
    }
}