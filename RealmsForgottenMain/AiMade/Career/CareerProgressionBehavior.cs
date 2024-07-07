using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Career
{
    public class CareerProgressionBehavior : CampaignBehaviorBase
    {
        private int playerProgress = 0;
        private int chivalryPoints = 0; // Track chivalry points
        private bool hasLegendaryBattleCry = false;
        private string currentTier = null; // Default to null to avoid wrongly applying any tier
        private string currentCareerId = null; // Default to null
        private bool isCareerAccepted = false; // Track if career is accepted
        private float nextNotificationTime = float.MaxValue; // Initialize to prevent immediate notifications
        private List<MercenaryContract> availableContracts = new List<MercenaryContract>();

        public CareerProgressionBehavior()
        {
            MercenaryContractManager.ContractCompleted += OnContractCompleted;
        }

        public void AddChivalryPoints(int points, string careerId)
        {
            if (careerId == currentCareerId)
            {
                chivalryPoints += points;
                InformationManager.DisplayMessage(new InformationMessage($"Added {points} chivalry points to the {careerId} career."));
                CheckAndApplyTierProgression("Chivalry Points");
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"Attempted to add {points} chivalry points, but the current career is {currentCareerId}. No points added."));
            }
        }

        public bool IsCurrentCareerMercenary()
        {
            return currentCareerId == "Mercenary";
        }

        public bool CanRecruitFromCulture(string cultureId, int requiredRelation)
        {
            int totalRelation = 0;

            foreach (var clan in Clan.All)
            {
                if (clan.Culture.StringId == cultureId)
                {
                    foreach (var lord in clan.Lords)
                    {
                        totalRelation += Hero.MainHero.GetRelation(lord);
                    }
                }
            }

            return totalRelation >= requiredRelation;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnHeroLeveledUp);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        private void OnMissionStarted(IMission imission)
        {
            if (imission is Mission mission && hasLegendaryBattleCry)
            {
                mission.AddMissionBehavior(new BattleCryMissionBehavior());
            }
        }

        private void OnDailyTick()
        {
            if (CampaignTime.Now.ToDays >= nextNotificationTime)
            {
                if (CheckCareerConditions())
                {
                    ApplyCareer();
                    InformationManager.DisplayMessage(new InformationMessage("Career Accepted"));
                    isCareerAccepted = true;
                    nextNotificationTime = float.MaxValue; // To prevent further notifications
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Conditions not met for assigning career."));
                }
            }
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            ApplyCareerIfConditionsMet();
        }

        private bool CheckCareerConditions()
        {
            // Implement your condition check logic here
            return true; // Return true if conditions are met
        }

        private void ApplyCareer()
        {
            // Logic to apply career goes here based on game conditions or player choice
            if (currentCareerId != null && !isCareerAccepted)
            {
                StartCareer(currentCareerId);
            }
        }

        private void ApplyCareerIfConditionsMet()
        {
            if (CheckCareerConditions())
            {
                ApplyCareer();
                isCareerAccepted = true;
                InformationManager.DisplayMessage(new InformationMessage("Career Applied After Load"));
            }
        }

        private void OnHeroLeveledUp(Hero hero, bool isPlayerCharacter)
        {
            if (hero == Hero.MainHero)
            {
                UpdateProgression("Reach Level", hero.Level);
                CheckAndApplyTierProgression("Reach Level");
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent.IsPlayerMapEvent && Hero.MainHero.PartyBelongedTo.IsActive)
            {
                MercenaryContractManager.CheckObjectives(Hero.MainHero);
                UpdateProgression("Complete Battles", 1);
            }
        }

        private void OnContractCompleted(object sender, ContractCompletionEvent e)
        {
            if (e.Hero == Hero.MainHero)
            {
                UpdateProgression("Complete Contracts", 1);
            }
        }

        private void UpdateProgression(string requirement, int value)
        {
            playerProgress += value;
            CheckAndApplyTierProgression(requirement);
        }

        private void NotifyTierProgression(CareerTier tier)
        {
            InformationManager.ShowInquiry(new InquiryData(
                "Tier Progression",
                $"You have progressed to the {tier.Name} tier! Benefits: {GetTierBenefitsDescription(tier)}",
                true,
                false,
                "OK",
                null,
                null,
                null
            ));
        }

        private string GetTierBenefitsDescription(CareerTier tier)
        {
            switch (tier.Name)
            {
                case "Recruit":
                    return "Increased party morale by 5.";
                case "Veteran":
                    return "Reduced troop wages by 10%.";
                case "Elite":
                    return "15% bonus in looting when raiding villages.";
                case "Commander":
                    return "Increased relations with all minor factions.";
                case "Legendary":
                    return "Unlocked special ability 'Battle Cry'.";
                case "Slayer":
                    return "Slayer benefits applied automatically.";
                case "Squire":
                    return "10% bonus XP in One-Handed, Two-Handed, and Polearm skills.";
                case "Cavalier":
                    return "20% bonus XP in Riding skill.";
                case "Paladin":
                    return "25% increase in party morale.";
                case "Crusader":
                    return "Activated Divine Shield, increased damage.";
                case "Champion":
                    return "Special Champion benefits.";
                default:
                    return "Unknown benefits.";
            }
        }

        private void CheckAndApplyTierProgression(string requirement)
        {
            var career = CareerManager.GetCareerById(currentCareerId);
            if (career == null) return;

            foreach (var tier in career.Tiers)
            {
                bool requirementMet = false;

                if (tier.ProgressionRequirement == requirement && tier.Name == GetNextTierName(currentTier, currentCareerId))
                {
                    if (requirement == "Reach Level" && playerProgress >= tier.RequiredValue)
                    {
                        requirementMet = true;
                    }
                    else if (requirement == "Reach Steward Level" && Hero.MainHero.GetSkillValue(DefaultSkills.Steward) >= tier.RequiredValue)
                    {
                        requirementMet = true;
                    }
                    else if (playerProgress >= tier.RequiredValue)
                    {
                        requirementMet = true;
                    }
                }

                if (requirementMet)
                {
                    ApplyTierBenefits(tier);
                    currentTier = tier.Name;
                    NotifyTierProgression(tier);
                    break;
                }
            }
        }

        private string GetNextTierName(string currentTier, string careerId)
        {
            var career = CareerManager.GetCareerById(careerId);
            if (career == null) return null;

            for (int i = 0; i < career.Tiers.Count; i++)
            {
                if (career.Tiers[i].Name == currentTier && i < career.Tiers.Count - 1)
                {
                    return career.Tiers[i + 1].Name;
                }
            }

            return null;
        }
        private void ApplyTierBenefits(CareerTier tier)
        {
            switch (tier.Name)
            {
                case "Recruit":
                    ApplyRecruitBenefits();
                    break;
                case "Veteran":
                    ApplyVeteranBenefits();
                    break;
                case "Elite":
                    ApplyEliteBenefits();
                    break;
                case "Commander":
                    ApplyCommanderBenefits();
                    break;
                case "Legendary":
                    ApplyLegendaryBenefits();
                    break;
                case "Slayer":
                    ApplySlayerBenefits();
                    break;
                case "Squire": // Knight-specific tiers
                    ApplySquireBenefits();
                    break;
                case "Cavalier":
                    ApplyCavalierBenefits();
                    break;
                case "Paladin":
                    ApplyPaladinBenefits();
                    break;
                case "Crusader":
                    ApplyCrusaderBenefits();
                    break;
                case "Champion":
                    ApplyChampionBenefits();
                    break;
            }
        }

        private void ApplyRecruitBenefits()
        {
            if (Hero.MainHero.PartyBelongedTo != null)
            {
                var moraleModel = Campaign.Current.Models.PartyMoraleModel;
                var morale = moraleModel.GetEffectivePartyMorale(Hero.MainHero.PartyBelongedTo, false);
                morale.Add(5, new TextObject("Recruit Tier Bonus"));
            }
        }

        private void ApplyVeteranBenefits()
        {
            if (Hero.MainHero.PartyBelongedTo != null)
            {
                var wageModel = Campaign.Current.Models.PartyWageModel;
                var originalWage = wageModel.GetTotalWage(Hero.MainHero.PartyBelongedTo);
                var reducedWage = originalWage.ResultNumber * 0.90f;
                Hero.MainHero.PartyBelongedTo.PartyTradeGold -= (int)(originalWage.ResultNumber - reducedWage);
                InformationManager.DisplayMessage(new InformationMessage($"Troop wages have been reduced by 10% for Veteran tier."));
            }
        }

        private void ApplyEliteBenefits()
        {
            if (Hero.MainHero.PartyBelongedTo != null)
            {
                Campaign.Current.GetCampaignBehavior<RaidLootBonusBehavior>().AdditionalVillageLootBonus = 0.15f;
                InformationManager.DisplayMessage(new InformationMessage("You have received a 15% bonus in looting when raiding villages for Elite tier."));
            }
        }

        private void ApplyCommanderBenefits()
        {
            // Increase relations with all minor factions
            foreach (Clan clan in Clan.All)
            {
                if (clan.IsMinorFaction && clan.Leader != null)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, clan.Leader, 10);
                }
            }
            InformationManager.DisplayMessage(new InformationMessage("You have increased relations with all minor factions for Commander tier."));
        }

        private void ApplyLegendaryBenefits()
        {
            hasLegendaryBattleCry = true; // Variable set correctly
            InformationManager.DisplayMessage(new InformationMessage("You have unlocked the special ability 'Battle Cry' for Legendary tier."));
        }

        private void ApplySlayerBenefits()
        {
            // Slayer benefits are automatically applied through the damage model
        }

        private void ApplySquireBenefits()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have received the Squire tier benefits."));
            Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, Hero.MainHero.GetSkillValue(DefaultSkills.OneHanded) * 0.10f);
            Hero.MainHero.AddSkillXp(DefaultSkills.TwoHanded, Hero.MainHero.GetSkillValue(DefaultSkills.TwoHanded) * 0.10f);
            Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, Hero.MainHero.GetSkillValue(DefaultSkills.Polearm) * 0.10f);
        }
        private void ApplyCavalierBenefits()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have received the Cavalier tier benefits."));
            Hero.MainHero.AddSkillXp(DefaultSkills.Riding, Hero.MainHero.GetSkillValue(DefaultSkills.Riding) * 0.20f);
        }

        private void ApplyPaladinBenefits()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have received the Paladin tier benefits."));
            Campaign.Current.Models.PartyMoraleModel.GetEffectivePartyMorale(Hero.MainHero.PartyBelongedTo, false).Add(25, new TextObject("Paladin Tier Bonus: +25% Morale"));
        }

        private void ApplyCrusaderBenefits()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have received the Crusader tier benefits."));
            // The Crusader damage bonus is applied through the CrusaderDamageModel

            // Activate Divine Shield
            var mission = Mission.Current;
            if (mission != null)
            {
                var divineShieldBehavior = mission.GetMissionBehavior<DivineShieldMissionBehavior>();
                if (divineShieldBehavior != null)
                {
                    divineShieldBehavior.ActivateDivineShield();
                }
            }
        }

        private void ApplyChampionBenefits()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have received the Champion tier benefits."));
            // Add specific benefits logic for the Champion tier here
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("playerProgress", ref playerProgress);
            dataStore.SyncData("hasLegendaryBattleCry", ref hasLegendaryBattleCry);
            dataStore.SyncData("currentTier", ref currentTier);
            dataStore.SyncData("currentCareerId", ref currentCareerId);
            dataStore.SyncData("chivalryPoints", ref chivalryPoints);
            ChivalryManager.SyncData(dataStore);
        }

        public bool IsAgentSlayer(Hero hero)
        {
            // Implement logic to check if the hero is a Slayer
            return hero != null && hero.CharacterObject.StringId == "slayer"; // Example check
        }

        public void SetCurrentCareer(string careerId)
        {
            currentCareerId = careerId;
        }

        // Add methods to handle gaining Chivalry points
        public void GainChivalry(Hero hero, float amount)
        {
            ChivalryManager.AddChivalry(hero, amount, true);
        }

        // Example of gaining Chivalry points on specific events
        private void OnQuestCompleted(QuestBase quest)
        {
            if (quest.QuestGiver == Hero.MainHero && currentCareerId == "Knight")
            {
                GainChivalry(Hero.MainHero, 10); // Gain 10 Chivalry points for completing a quest
            }
        }
        public void StartCareer(string careerId)
        {
            currentCareerId = careerId;
            isCareerAccepted = true;
            ApplyInitialCareerTier(careerId);
            InformationManager.DisplayMessage(new InformationMessage($"You started the {careerId} career. Initial tier applied: {currentTier}."));
        }

        private void ApplyInitialCareerTier(string careerId)
        {
            var career = CareerManager.GetCareerById(careerId);
            if (career != null)
            {
                CareerTier initialTier = null;

                // Determine the initial tier based on the career type
                if (careerId == "Mercenary" && career.Tiers.Count > 0)
                {
                    initialTier = career.Tiers.FirstOrDefault(t => t.Name == "Recruit");
                }
                else if (careerId == "Knight" && career.Tiers.Count > 0)
                {
                    initialTier = career.Tiers.FirstOrDefault(t => t.Name == "Squire");
                }

                if (initialTier != null)
                {
                    ApplyTierBenefits(initialTier);
                    currentTier = initialTier.Name;
                    NotifyTierProgression(initialTier);
                    InformationManager.DisplayMessage(new InformationMessage($"Initial career tier '{initialTier.Name}' applied for career '{careerId}'."));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Failed to apply initial career tier for career '{careerId}'."));
                }
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"Career '{careerId}' not found."));
            }
        }
    }
}
    



