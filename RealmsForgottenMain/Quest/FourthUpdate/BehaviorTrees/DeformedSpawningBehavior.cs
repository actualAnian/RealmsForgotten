using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using RealmsForgotten.Quest.FourthUpdate;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class DeformedSpawningBehavior : CampaignBehaviorBase
    {
        // Instância estática para fácil acesso de outras classes (como a SeventhQuest)
        public static DeformedSpawningBehavior Instance { get; private set; }

        private bool _spawningEnabled = false;
        private CampaignTime _nextSpawnTime = CampaignTime.Never;
        private int _spawnCount = 0;
        private bool _eighthQuestStarted = false;

        public DeformedSpawningBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // Este evento será registrado no início da campanha e sempre funcionará
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Este é o método CORRETO e ROBUSTO para salvar e carregar dados
            dataStore.SyncData("_spawningEnabled", ref _spawningEnabled);
            dataStore.SyncData("_nextSpawnTime", ref _nextSpawnTime);
            dataStore.SyncData("_spawnCount", ref _spawnCount);
            dataStore.SyncData("_eighthQuestStarted", ref _eighthQuestStarted);
        }

        // Método público para a SeventhQuest chamar quando for concluída
        public void BeginSpawning()
        {
            if (!_spawningEnabled)
            {
                InformationManager.DisplayMessage(new InformationMessage("A strange presence lingers in the land...", Colors.Red));
                _spawningEnabled = true;
                _nextSpawnTime = CampaignTime.DaysFromNow(3); // Inicia o contador
            }
        }

        private void OnDailyTick()
        {
            // Se o spawn não estiver ativo ou a 8ª quest já começou, não faz nada
            if (!_spawningEnabled || _eighthQuestStarted)
            {
                return;
            }

            // Se já spawnou 3 vezes, para o spawn e inicia a 8ª quest
            if (_spawnCount >= 3)
            {
                ShowEighthQuestPrompt();
                _eighthQuestStarted = true; // Impede que o prompt apareça novamente
                _spawningEnabled = false;   // Para permanentemente o spawn
                return;
            }

            // Se estiver na hora de spawnar
            if (CampaignTime.Now >= _nextSpawnTime)
            {
                SpawnDeformedParties();
                _nextSpawnTime = CampaignTime.DaysFromNow(2); // Agenda o próximo spawn
            }
        }

        // MÉTODO MOVIDO DA SEVENTHQUEST
        private void SpawnDeformedParties()
        {
            try
            {
                Clan deformedClan = Clan.FindFirst(c => c.StringId == "deformed_villagers");
                if (deformedClan == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Deformed clan not found for spawning."));
                    return;
                }

                var hideouts = Hideout.All
                    .Where(h => h.IsInfested && h.Settlement != null)
                    .OrderBy(_ => MBRandom.RandomInt())
                    .Take(10);

                foreach (var hideout in hideouts)
                {
                    SpawnDeformedParty(hideout, deformedClan);
                }

                _spawnCount++; // Incrementa o contador AQUI

                InformationManager.ShowInquiry(new InquiryData(
                    "Reports of attacks are growing!",
                    "Twisted hosts have been spotted attacking villagers and caravans.",
                    true, false, "Close", null, null, null
                ));

                InformationManager.DisplayMessage(new InformationMessage($"✅ Deformed Villager parties wave {_spawnCount}/3 spawned."));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Exception spawning Deformed parties: {ex.Message}", Colors.Red));
            }
        }

        // MÉTODO MOVIDO DA SEVENTHQUEST
        private void SpawnDeformedParty(Hideout hideout, Clan clan)
        {
            // (Este método é o mesmo que você já tinha na SeventhQuest, apenas copiado para cá)
            try
            {
                string partyId = $"deformed_party_{hideout.Settlement.StringId}_{MBRandom.RandomInt(10000, 99999)}";
                MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, clan, hideout, true);

                if (party == null) return;

                TroopRoster roster = TroopRoster.CreateDummyTroopRoster();
                roster.AddToCounts(CharacterObject.Find("deformed_villager_boss"), 1);
                roster.AddToCounts(CharacterObject.Find("deformed_villager_bandit"), 80);

                party.InitializeMobilePartyAroundPosition(roster, TroopRoster.CreateDummyTroopRoster(), hideout.Settlement.Position2D, 100f, 10f);
                party.SetCustomName(new TextObject("Deformed Villagers"));
                party.Aggressiveness = 100f;
                party.SetPartyObjective(MobileParty.PartyObjective.Aggressive);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Error spawning individual Deformed party: {ex.Message}"));
            }
        }

        // LÓGICA DE INÍCIO DA 8ª QUEST MOVIDA PARA CÁ
        public void ShowEighthQuestPrompt()
        {
            // O código aqui é o mesmo que você já tinha
            InformationManager.ShowInquiry(new InquiryData(
                "The Forest Whispers",
                "The Priestess of the First Tree sent you a messenger to inform you about recent attacks on innocent people. She asks your help to unveil the reason.",
                true, false,
                "Continue", null,
                () =>
                {
                    // A única mudança é que não precisamos mais de um log da 7ª quest
                    Hero owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");
                    if (owl?.CharacterObject != null)
                    {
                        // IMPORTANTE: O diálogo agora deve ser registrado pela 8ª quest, não aqui.
                        // Apenas iniciamos a quest, e ela cuidará de seus próprios diálogos.
                        StartEighthQuest();
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("❌ Could not find 'The Owl' to start the Eighth Quest!", Colors.Red));
                    }
                },
                null
            ));
        }

        private void StartEighthQuest()
        {
            // Apenas inicia a quest. O Hero.MainHero é o Giver padrão.
            new EighthQuest("rf_eighth_quest", Hero.MainHero, CampaignTime.Never, 0).StartQuest();
            InformationManager.DisplayMessage(
             new InformationMessage("🌳 The Eighth Quest has begun!")
         );
        }
    }
}
