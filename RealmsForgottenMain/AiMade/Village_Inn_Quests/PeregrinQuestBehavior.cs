using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    public class PeregrinQuestBehavior : CampaignBehaviorBase
    {
        private CampaignTime _nextQuestAvailableTime;
        private int _currentQuestIndex;

        private readonly List<QuestDefinition> _questQueue = new List<QuestDefinition>();

        public PeregrinQuestBehavior()
        {
            // Fila de quests do Peregrin (adicione mais conforme precisar)
            _questQueue.Add(new QuestDefinition(
                "rf_werewolf_quest",
                () =>
                {
                    Village targetVillage = null;
                    if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsVillage)
                    {
                        targetVillage = Settlement.CurrentSettlement.Village;
                    }

                    if (targetVillage == null)
                    {
                        // fallback: pega a vila mais próxima do jogador
                        var nearest = Settlement.All
                            .Where(s => s.IsVillage)
                            .OrderBy(s => s.GatePosition.DistanceSquared(MobileParty.MainParty.Position2D))
                            .FirstOrDefault();
                        targetVillage = nearest?.Village;
                    }

                    if (targetVillage != null)
                    {
                        return new WerewolfQuest(targetVillage);
                    }

                    return null;
                },
                7
            ));

            _questQueue.Add(new QuestDefinition(
                "rf_ruins_quest",
                () =>
                {
                    Village targetVillage = null;
                    if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsVillage)
                    {
                        targetVillage = Settlement.CurrentSettlement.Village;
                    }

                    if (targetVillage == null)
                    {
                        var nearestVillage = Settlement.All
                            .Where(s => s.IsVillage)
                            .OrderBy(s => s.GatePosition.DistanceSquared(MobileParty.MainParty.Position2D))
                            .FirstOrDefault();
                        targetVillage = nearestVillage?.Village;
                    }

                    if (targetVillage == null) return null;

                    // escolhe o hideout mais próximo da vila
                    var nearestHideout = Settlement.All
                        .Where(s => s.IsHideout && s.Hideout != null && !s.Hideout.IsInfested)
                        .OrderBy(s => s.Position2D.DistanceSquared(targetVillage.Settlement.Position2D))
                        .FirstOrDefault();

                    if (nearestHideout != null)
                    {
                        return new RuinsQuest(targetVillage, nearestHideout.Hideout);
                    }

                    return null;
                },
                10
            ));
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Intro
            starter.AddDialogLine("peregrin_intro", "start", "peregrin_options",
                "{=peregrin_intro}Greetings, traveler. How may I serve you, good sir? Tell me, are you a hunter of bounties?",
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "peregrin_storyteller",
                null);

            // “Tem alguma história?”
            starter.AddPlayerLine("peregrin_ask", "peregrin_options", "peregrin_offer",
                "{=peregrin_ask}Nay, I am not. Yet if I were, would thy head be worth a price in gold?",
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "peregrin_storyteller",
                null);

            // Nada disponível
            starter.AddDialogLine("peregrin_noquest", "peregrin_offer", "close_window",
                "{=peregrin_noquest}For now, I have nothing new to share. Return later.",
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "peregrin_storyteller"
                      && !CanOfferNextQuest(),
                null);

            // Oferta específica: Werewolf
            starter.AddDialogLine("peregrin_offer_werewolf", "peregrin_offer", "peregrin_offer_werewolf_player",
                "{=peregrin_offer_werewolf}Ha! My head bears no worth, yet the knowledge within may earn thee coin. The folk here whisper of dread beasts that stalk the night. The villagers live in fear.",
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "peregrin_storyteller"
                      && CanOfferNextQuest()
                      && _questQueue[_currentQuestIndex].QuestId == "rf_werewolf_quest",
                () =>
                {
                    var village = Settlement.All.FirstOrDefault(s => s.IsVillage);
                    if (village != null)
                        MBTextManager.SetTextVariable("VILLAGE", village.Name);
                });

            starter.AddPlayerLine("peregrin_accept_werewolf", "peregrin_offer_werewolf_player", "close_window",
                "{=peregrin_accept_werewolf}If such a fiend prowls, and its head be weighed in gold, I shall face it. The village shall know peace.",
                () => true,
                () => StartNextQuest());

            // Oferta específica: Ruins
            starter.AddDialogLine("peregrin_offer_ruins", "peregrin_offer", "peregrin_offer_ruins_player",
                "{=peregrin_offer_ruins}I’ve heard of forgotten ruins nearby, holding secrets and dangers alike.",
                () => CharacterObject.OneToOneConversationCharacter?.StringId == "peregrin_storyteller"
                      && CanOfferNextQuest()
                      && _questQueue[_currentQuestIndex].QuestId == "rf_ruins_quest",
                null);

            starter.AddPlayerLine("peregrin_accept_ruins", "peregrin_offer_ruins_player", "close_window",
                "{=peregrin_accept_ruins}Ruins, you say? I’ll see what mysteries lie within.",
                () => true,
                () => StartNextQuest());
        }

        private bool CanOfferNextQuest()
        {
            if (_currentQuestIndex >= _questQueue.Count) return false;
            if (CampaignTime.Now < _nextQuestAvailableTime) return false;

            var def = _questQueue[_currentQuestIndex];
            // Se já existe uma quest com o mesmo StringId ativa, não pode oferecer
            return !Campaign.Current.QuestManager.Quests.Any(q => q.StringId == def.QuestId);
        }

        private void StartNextQuest()
        {
            if (!CanOfferNextQuest()) return;

            var def = _questQueue[_currentQuestIndex];
            QuestBase quest = def.CreateQuest?.Invoke();

            if (quest != null)
            {
                // NÃO precisa AddQuest: o construtor da sua quest já chama StartQuest()
                _nextQuestAvailableTime = CampaignTime.DaysFromNow(def.CooldownDays);
                _currentQuestIndex++;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_nextQuestAvailableTime", ref _nextQuestAvailableTime);
            dataStore.SyncData("_currentQuestIndex", ref _currentQuestIndex);
        }

        private class QuestDefinition
        {
            public string QuestId { get; }
            public Func<QuestBase> CreateQuest { get; }
            public int CooldownDays { get; }

            public QuestDefinition(string questId, Func<QuestBase> createQuest, int cooldownDays)
            {
                QuestId = questId;
                CreateQuest = createQuest;
                CooldownDays = cooldownDays;
            }
        }
    }
}

