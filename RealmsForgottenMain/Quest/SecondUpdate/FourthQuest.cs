using Helpers;
using RealmsForgotten.Quest.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static RealmsForgotten.Quest.QuestLibrary;

namespace RealmsForgotten.Quest.SecondUpdate
{
    internal class FourthQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog takeBossToLordLog;
        [SaveableField(1)]
        private float initialDistanceFromQuestGiver;
        [SaveableField(2)]
        private JournalLog goToMonasteryLog;

        public static Settlement QuestMonastery => Settlement.Find("retreat_monastery");

        [SaveableField(4)]
        private float initialDistanceToMonastery;
        [SaveableField(5)]
        private JournalLog captureHellboundLog;
        [SaveableField(6)]
        private bool successInPersuasion;
        [SaveableField(7)]
        private JournalLog nextUpdateLog;

        public static bool DisableSendTroops => Instance?.takeBossToLordLog?.CurrentProgress == 1;

        public static FourthQuest Instance;
        public FourthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            Instance = this;
        }

        public override TextObject Title => GameTexts.FindText("rf_quest_title_part_four");

        public override bool IsRemainingTimeHidden => true;
        public override string SpecialQuestType => "RfMainQuest";


        protected override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, OnCanHeroBecomePrisoner);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, BattleEnd);
            RegisterQuestEvents(this);
        }

        private float _owlEnsureCooldown;

        /// <summary>
        /// REGRA DO AUTOR: enquanto esta quest existe, o Owl esta VIVO e NA PARTY do
        /// jogador — sempre, em qualquer fase. Roda por tick (gancho de load de quest
        /// nao e confiavel): se ele morreu, o Resolve ja revive; se saiu do roster
        /// (morte na janela do bug, impostor que consumiu a vaga), volta em ate 2s.
        /// </summary>
        private void EnsureOwlInParty(float dt)
        {
            _owlEnsureCooldown -= dt;
            if (_owlEnsureCooldown > 0f)
                return;
            _owlEnsureCooldown = 2f;

            // Cirurgia completa (expulsa impostor, revive, renasce do template se o
            // heroi sumiu do save) — segura aqui porque o tick so roda com o jogo vivo.
            Hero owl = QuestHeroSuccessionBehavior.RepairImmortal(QuestHeroes.TheOwl);
            if (owl == null || owl.IsPrisoner || owl.PartyBelongedTo == MobileParty.MainParty)
                return;

            if (!owl.IsActive)
                owl.ChangeState(Hero.CharacterStates.Active);
            owl.HitPoints = Math.Max(owl.HitPoints, Math.Max(10, owl.MaxHitPoints / 2));
            AddHeroToPartyAction.Apply(owl, MobileParty.MainParty);
        }

        private void BattleEnd(MapEvent mapEvent)
        {
            MapEventSide? defeatedSide = mapEvent.DefenderSide.MissionSide == mapEvent.DefeatedSide
                ? mapEvent.DefenderSide
                : mapEvent.AttackerSide.MissionSide == mapEvent.DefeatedSide ? mapEvent.AttackerSide : null;

            if (captureHellboundLog?.CurrentProgress == 0 && defeatedSide?.LeaderParty?.Culture?.StringId == "hellbound_outlaw")
            {
                captureHellboundLog.UpdateCurrentProgress(1);
            }
        }

        // O fluxo historico da quest abre a conversa de mapa direto ao fim da batalha —
        // o vanilla (CampaignMapConversation) convive com encounter/map event em
        // teardown, e e assim que o dialogo "fura a fila" mesmo com a party inimiga
        // reengajando (exigir mapa limpo aqui deixava o dialogo mudo sob perseguicao).
        // As duas unicas guardas necessarias: nao abrir DENTRO de uma missao e nao
        // reabrir em cima de uma conversa ja em andamento. O crash de 2026-08-19 nunca
        // foi o teardown: era NRE de TheOwl nulo, corrigido na resolucao do papel.
        private static bool CanOpenMapConversation()
            => Mission.Current == null
               && Campaign.Current?.ConversationManager?.IsConversationInProgress != true;

        private void OnTick(float dt)
        {
            EnsureOwlInParty(dt);

            if (takeBossToLordLog?.CurrentProgress == 2)
            {
                if (Hero.MainHero.IsPrisoner)
                {
                    EndCaptivityAction.ApplyByReleasedAfterBattle(Hero.MainHero);
                    return;
                }

                if (CanOpenMapConversation() && TheOwl?.CharacterObject != null)
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
            }
            if (captureHellboundLog?.CurrentProgress == 1)
            {
                CharacterObject chief = CharacterObject.Find("hellbound_chief");
                if (CanOpenMapConversation() && chief != null)
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(chief));
            }
            if (captureHellboundLog?.CurrentProgress == 2)
            {
                // Guarda p/ instancia de replay (rf.quest.replay_ambush): nao criar uma
                // SEGUNDA 5ª quest se a original existe neste save.
                if (!Campaign.Current.QuestManager.Quests.Any(q => q is FifthQuest))
                {
                    new FifthQuest("rf_fifth_quest", QuestGiver, CampaignTime.Never, 50000).StartQuest();
                }
                CompleteQuestWithSuccess();
            }

        }

        private void OnCanHeroBecomePrisoner(Hero hero, ref bool canHeroBecome)
        {
            if (takeBossToLordLog?.CurrentProgress == 1 && (hero == Hero.MainHero || hero == TheOwl))
                canHeroBecome = false;
        }

        private void OnMissionStarted(IMission imission)
        {
            if (imission is Mission mission && takeBossToLordLog?.CurrentProgress == 1)
            {
                takeBossToLordLog.UpdateCurrentProgress(2);
            }
        }

        protected override void HourlyTick()
        {
            if (QuestGiver.IsActive && GetDistanceFromQuestGiver() <= initialDistanceFromQuestGiver * 0.7 && takeBossToLordLog?.CurrentProgress == 0)
            {
                takeBossToLordLog?.UpdateCurrentProgress(1);

                Clan hellboundClan = Clan.FindFirst(x => x.StringId == "cs_nelrog_raiders");
                CampaignVec2 spawnPos = MobileParty.MainParty.Position;
                PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("hellbound_outlaw_template");
                MobileParty hellboundParty = BanditPartyComponent.CreateBanditParty("quest_hellbound_party", hellboundClan, null, true, looterTemplate, spawnPos);

                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                string[] units = { "cs_nelrog_bandits_bandit", "cs_nelrog_bandits_raider", "cs_nelrog_bandits_chief" };

                troopRoster.AddToCounts(CharacterObject.Find("cs_nelrog_bandits_boss"), 1);
                for (int i = 0; i < 60; i++)
                    troopRoster.AddToCounts(CharacterObject.Find(units.GetRandomElement()), 1);

                hellboundParty.InitializeMobilePartyAtPosition(troopRoster, TroopRoster.CreateDummyTroopRoster(), spawnPos);

                hellboundParty.SetMoveEngageParty(MobileParty.MainParty, MobileParty.NavigationType.Default);
                hellboundParty.IgnoreForHours(0.2f);
                hellboundParty.Party.SetCustomName(new TextObject("{=rf_hellbound_party}Hellbound Raiders"));
                hellboundParty.Aggressiveness = 100f;

                // Force battle
                if (PlayerEncounter.Current == null)
                {
                    PlayerEncounter.RestartPlayerEncounter(hellboundParty.Party, MobileParty.MainParty.Party, true);
                    PlayerEncounter.StartBattle();
                }
            }           

            // 👇 this block must be OUTSIDE of the one above
            if (GetDistanceFromMonastery() <= initialDistanceToMonastery * 0.7 && takeBossToLordLog?.CurrentProgress == 3)
            {
                takeBossToLordLog.UpdateCurrentProgress(4);
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
            }
        }
        /// <summary>
        /// Rebobina a 4ª quest para o checkpoint DA EMBOSCADA dos Nelrog (pedido do autor
        /// para regravar narração, 2026-08-19: o save pré-batalha foi sobrescrito por
        /// quicksaves). Usar numa CÓPIA do save ("salvar como" antes!). O que faz:
        /// remove a party de emboscada remanescente, volta o estágio para 0 e rearma o
        /// gatilho de distância — a emboscada re-dispara no próximo tick de hora perto do
        /// lorde. Efeito colateral aceito: refazer o diálogo do Owl duplica a entrada
        /// "ir ao monastério" no diário (cosmético).
        /// </summary>
        /// <summary>
        /// TRAVA DE DISTRIBUIÇÃO: as ferramentas de filmagem/replay só existem na máquina
        /// do autor — exigem um arquivo-chave em Documentos (fora da pasta do mod, nunca
        /// distribuído). Em qualquer instalação de jogador isto é código morto.
        /// </summary>
        internal static readonly bool FilmingToolsEnabled = System.IO.File.Exists(
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord", "rf_dev_filming.flag"));

        [CommandLineFunctionality.CommandLineArgumentFunction("replay_ambush", "rf.quest")]
        public static string ReplayAmbush(List<string> args)
        {
            if (!FilmingToolsEnabled)
            {
                return "Ferramenta de dev desativada nesta máquina.";
            }
            if (Campaign.Current == null)
            {
                return "rf.quest.replay_ambush: campanha não está rodando.";
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Replay da emboscada dos Nelrog ===");

            FourthQuest quest = Instance;
            if (quest == null || quest.IsFinalized)
            {
                // Quest ja concluida (save avancado): recria uma INSTANCIA DE REPLAY so
                // para este save de filmagem — diario limpo, mesma emboscada, mesmo
                // dialogo do Owl. O lorde da quest vem do argumento ou de outra quest
                // principal ativa (a 5ª herda o mesmo giver).
                Hero giver = null;
                if (args != null && args.Count > 0)
                {
                    string name = string.Join(" ", args).Trim();
                    giver = Hero.AllAliveHeroes.FirstOrDefault(h => h.Name != null && h.Name.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (giver == null)
                    {
                        return $"Herói '{name}' não encontrado (vivo).";
                    }
                }

                giver ??= Campaign.Current.QuestManager.Quests
                    .FirstOrDefault(q => q is FifthQuest && !q.IsFinalized && q.QuestGiver != null && q.QuestGiver.IsAlive)?.QuestGiver;
                giver ??= Campaign.Current.QuestManager.Quests
                    .FirstOrDefault(q => !q.IsFinalized && q.QuestGiver != null && q.QuestGiver.IsAlive
                                         && q.GetType().Namespace?.StartsWith("RealmsForgotten") == true)?.QuestGiver;

                if (giver == null)
                {
                    return "A 4ª quest já foi concluída e não achei o lorde da quest neste save. Rode: rf.quest.replay_ambush <nome do lorde a quem você levou o boss>.";
                }

                quest = new FourthQuest("rf_fourth_quest_replay", giver, CampaignTime.Never, 0);
                quest.StartQuest();
                report.AppendLine($"Quest concluída neste save — instância de REPLAY criada (lorde: {giver.Name}).");
            }
            report.AppendLine($"Estado atual: takeBossToLord={quest.takeBossToLordLog?.CurrentProgress.ToString() ?? "null"}, " +
                              $"goToMonastery={quest.goToMonasteryLog?.CurrentProgress.ToString() ?? "null"}, " +
                              $"captureHellbound={quest.captureHellboundLog?.CurrentProgress.ToString() ?? "null"}");

            if (quest.captureHellboundLog != null)
            {
                report.AppendLine("AVISO: a fase de capturar o chefe Hellbound já começou — o replay volta só a emboscada; o resto da quest permanece adiantado.");
            }

            foreach (MobileParty party in MobileParty.All.ToList())
            {
                if (party.StringId != null && party.StringId.StartsWith("quest_hellbound_party") && party.IsActive)
                {
                    DestroyPartyAction.Apply(null, party);
                    report.AppendLine($"Party de emboscada remanescente removida: {party.StringId}");
                }
            }

            quest.takeBossToLordLog?.UpdateCurrentProgress(0);
            if (quest.takeBossToLordLog != null && quest.takeBossToLordLog.CurrentProgress != 0)
            {
                // UpdateCurrentProgress recusou descer — força pelo setter privado.
                var prop = typeof(JournalLog).GetProperty("CurrentProgress");
                prop?.GetSetMethod(true)?.Invoke(quest.takeBossToLordLog, new object[] { 0 });
            }
            report.AppendLine($"Estágio rebobinado para {quest.takeBossToLordLog?.CurrentProgress.ToString() ?? "null"}.");

            // Rearma o gatilho: com o "raio inicial" 1.5x a distância atual, a condição
            // (dist² <= inicial*0.7) já vale onde o jogador está — a emboscada vem no
            // próximo tick de hora. Posicione-se ANTES de rodar o comando.
            quest.initialDistanceFromQuestGiver = Math.Max(1f, quest.GetDistanceFromQuestGiver() * 1.5f);
            report.AppendLine("Gatilho rearmado: a emboscada dispara no próximo tick de hora (deixe o tempo correr).");
            report.AppendLine("LEMBRETE: rode isto numa CÓPIA do save (Save As antes de tudo).");
            return report.ToString();
        }

        private float GetDistanceFromQuestGiver() => MobileParty.MainParty.GetPosition2D.DistanceSquared(QuestGiver.PartyBelongedTo != null ? QuestGiver.PartyBelongedTo.GetPosition2D : QuestGiver.CurrentSettlement.GetPosition2D);
        private float GetDistanceFromMonastery() => MobileParty.MainParty.GetPosition2D.DistanceSquared(QuestMonastery.GetPosition2D);
        protected override void OnStartQuest()
        {
            SetDialogs();
            TextObject textObject = GameTexts.FindText("rf_fourth_quest_first_log");
            textObject.SetCharacterProperties("LORD", QuestGiver.CharacterObject);
            takeBossToLordLog = AddLog(textObject);

            initialDistanceFromQuestGiver = MobileParty.MainParty.GetPosition2D.DistanceSquared(QuestGiver.PartyBelongedTo != null ? QuestGiver.PartyBelongedTo.GetPosition2D : QuestGiver.CurrentSettlement.GetPosition2D);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            QuestLibrary.InitializeVariables();
            Instance = this;
        }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(FirstDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(UliahTableDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(MonkDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(HellboudPersuasionDialogFlow(), this);

        }


        private void FirstDialogConsequence()
        {
            takeBossToLordLog.UpdateCurrentProgress(3);
            AddHeroToPartyAction.Apply(TheOwl, MobileParty.MainParty);
            AddTrackedObject(QuestMonastery);
            goToMonasteryLog = AddLog(GameTexts.FindText("rf_fourth_quest_second_log"));
            QuestMonastery.IsVisible = true;
            QuestMonastery.IsInspected = true;
            initialDistanceToMonastery = MobileParty.MainParty.GetPosition2D.DistanceSquared(QuestMonastery.GetPosition2D);
        }

        private void ShowWaitScreen()
        {
            QuestUIManager.ShowNotification("After a while...", () => { }, false);
        }

        private void MonkDialogConsequence()
        {
            PartyBase.MainParty.AddMember(CharacterObject.Find("monk_knight"), 20);
            PartyBase.MainParty.ItemRoster.AddToCounts(new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("rfmisc_anorit_fire_stone_t3_rfthrowing50")), 10);

            captureHellboundLog = AddLog(GameTexts.FindText("rf_fourth_quest_third_log"));
            SpawnHellboundQuestPartiesNearSeaHideouts();
            goToMonasteryLog.UpdateCurrentProgress(1);
        }

        private void SpawnHellboundQuestPartiesNearSeaHideouts()
        {
            SpawnHellboundAtHideout("hideout_seaside_22");
            SpawnHellboundAtHideout("hideout_seaside_8");
            SpawnHellboundAtHideout("hideout_seaside_15");
        }

        private void SpawnHellboundAtHideout(string hideoutId)
        {
            Settlement hideout = Settlement.Find(hideoutId);
            if (hideout == null)
                return;

            Clan hellboundClan = Clan.FindFirst(x => x.StringId == "hellbound_outlaw");
            PartyTemplateObject template = Campaign.Current.ObjectManager
                .GetObject<PartyTemplateObject>("hellbound_outlaw_template");

            if (hellboundClan == null || template == null)
                return;

            CampaignVec2 spawnPos = hideout.GatePosition;

            // Create party with BanditPartyComponent to properly flag it as a bandit party
            MobileParty party = BanditPartyComponent.CreateBanditParty(
                "quest_hellbound_" + hideoutId,
                hellboundClan,
                hideout.Hideout, // Must be a Hideout, not Settlement
                false, // isBossParty parameter
                template, // PartyTemplateObject parameter
                spawnPos); // spawn position

            // Build additional troops if needed (the template already spawns basic troops)
            string[] units =
            {
        "hellbound_thief",
        "hellbound_bandit",
        "hellbound_chief"
    };

            CharacterObject boss = CharacterObject.Find("hellbound_boss");
            if (boss != null)
                party.MemberRoster.AddToCounts(boss, 1);

            for (int i = 0; i < 35; i++)
            {
                CharacterObject troop = CharacterObject.Find(units.GetRandomElement());
                if (troop != null)
                    party.MemberRoster.AddToCounts(troop, 1);
            }

            // Set party properties
            party.Party.SetCustomName(new TextObject("{=rf_hellbound_party}Hellbound Raiders"));
            party.Aggressiveness = 100f;
            // IsBandit is now automatically true because we used BanditPartyComponent.CreateBanditParty
            party.SetPartyUsedByQuest(true); // Mark as quest party to prevent despawn
            party.SetMoveEngageParty(MobileParty.MainParty, MobileParty.NavigationType.Default);
        }
        private TextObject LineWithPlayerLink()
        {
            TextObject text = GameTexts.FindText("rf_fourth_quest_monk_dialog_11");
            text.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
            return text;
        }

        private void OwlGivesTableConsequence()
        {
            PartyBase.MainParty.ItemRoster.Add(new ItemRosterElement(MBObjectManager.Instance.GetObject<ItemObject>("vortiak_stone_tablet")));
            takeBossToLordLog.UpdateCurrentProgress(5);
        }

        private DialogFlow HellboudPersuasionDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_fourth_quest_hellbound_dialog_1")).Condition(() => captureHellboundLog?.CurrentProgress == 1 && CharacterObject.OneToOneConversationCharacter?.StringId == "hellbound_chief")
                .GotoDialogState("quest_hellbound_dialog_start");

            dialogFlow.AddDialogLine("quest_hellbound_id_1", "quest_hellbound_dialog_start", "quest_hellbound_dialog_1",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_2").ToString(), null, StartPersuasion, this);

            dialogFlow.AddDialogLine("quest_hellbound_id_2", "quest_hellbound_dialog_1", "quest_hellbound_dialog_options_1",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_2").ToString(), () => !ConversationManager.GetPersuasionProgressSatisfied(), () => successInPersuasion = false, this);




            dialogFlow.AddPlayerLine("quest_hellbound_id_3", "quest_hellbound_dialog_options_1", "quest_hellbound_dialog_outcome_1",
                "{=!}{PERSUADE_ATTEMPT_1}", PersuasionOptionCondition_1, PersuasionOptionConsequence_1, this, 100, null, () => _persuasionTask.Options.ElementAt(0));

            dialogFlow.AddPlayerLine("quest_hellbound_id_4", "quest_hellbound_dialog_options_1", "quest_hellbound_dialog_outcome_1",
                "{=!}{PERSUADE_ATTEMPT_2}", PersuasionOptionCondition_2, PersuasionOptionConsequence_2, this, 100, null, () => _persuasionTask.Options.ElementAt(1));



            dialogFlow.AddDialogLine("quest_hellbound_id_5", "quest_hellbound_dialog_outcome_1", "quest_hellbound_dialog_options_2",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_1").ToString(), () => ConversationManager.GetPersuasionProgress() == 1, null, this);

            dialogFlow.AddDialogLine("quest_hellbound_id_6", "quest_hellbound_dialog_outcome_1", "quest_hellbound_dialog_options_2",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_1_wrong").ToString(), () => !ConversationManager.GetPersuasionProgressSatisfied(), null, this);



            dialogFlow.AddPlayerLine("quest_hellbound_id_7", "quest_hellbound_dialog_options_2", "quest_hellbound_dialog_outcome_2",
                "{=!}{PERSUADE_ATTEMPT_3}", PersuasionOptionCondition_3, PersuasionOptionConsequence_3, this, 100, null, () => _persuasionTask.Options.ElementAt(0));

            dialogFlow.AddPlayerLine("quest_hellbound_id_8", "quest_hellbound_dialog_options_2", "quest_hellbound_dialog_outcome_2",
                "{=!}{PERSUADE_ATTEMPT_4}", PersuasionOptionCondition_4, PersuasionOptionConsequence_4, this, 100, null, () => _persuasionTask.Options.ElementAt(1));


            dialogFlow.AddDialogLine("quest_hellbound_id_9", "quest_hellbound_dialog_outcome_2", "close_window",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_2").ToString(), ConversationManager.GetPersuasionProgressSatisfied, OnPersuasionComplete, this);

            dialogFlow.AddDialogLine("quest_hellbound_id_10", "quest_hellbound_dialog_outcome_2", "close_window",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_2_wrong").ToString(), () => !ConversationManager.GetPersuasionProgressSatisfied(), OnPersuasionComplete, this);


            return dialogFlow;

        }

        private void OnPersuasionComplete()
        {
            captureHellboundLog.UpdateCurrentProgress(2);
            successInPersuasion = true;
        }
        private bool PersuasionOptionCondition_1()
        {
            if (this._persuasionTask.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._persuasionTask.Options.ElementAt(0), false));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._persuasionTask.Options.ElementAt(0).Line);
                MBTextManager.SetTextVariable("PERSUADE_ATTEMPT_1", textObject, false);
                return true;
            }
            return false;
        }
        private bool PersuasionOptionCondition_2()
        {
            if (_persuasionTask.Options.Count > 1)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_persuasionTask.Options.ElementAt(1), false));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", _persuasionTask.Options.ElementAt(1).Line);
                MBTextManager.SetTextVariable("PERSUADE_ATTEMPT_2", textObject);
                return true;
            }
            return false;
        }
        private bool PersuasionOptionCondition_3()
        {
            if (this._persuasionTask.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._persuasionTask.Options.ElementAt(2), false));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._persuasionTask.Options.ElementAt(2).Line);
                MBTextManager.SetTextVariable("PERSUADE_ATTEMPT_3", textObject, false);
                return true;
            }
            return false;
        }
        private bool PersuasionOptionCondition_4()
        {
            if (_persuasionTask.Options.Count > 1)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_persuasionTask.Options.ElementAt(3), false));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", _persuasionTask.Options.ElementAt(3).Line);
                MBTextManager.SetTextVariable("PERSUADE_ATTEMPT_4", textObject);
                return true;
            }
            return false;
        }
        private void PersuasionOptionConsequence_1()
        {
            if (_persuasionTask.Options.Count > 0)
            {
                _persuasionTask.Options[0].BlockTheOption(true);
            }
        }
        private void PersuasionOptionConsequence_2()
        {
            if (_persuasionTask.Options.Count > 1)
            {
                _persuasionTask.Options[1].BlockTheOption(true);
            }
        }

        private void PersuasionOptionConsequence_3()
        {
            if (_persuasionTask.Options.Count > 2)
            {
                _persuasionTask.Options[2].BlockTheOption(true);
            }
        }
        private void PersuasionOptionConsequence_4()
        {
            if (_persuasionTask.Options.Count > 3)
            {
                _persuasionTask.Options[3].BlockTheOption(true);
            }
        }

        private PersuasionTask _persuasionTask;

        private void StartPersuasion()
        {
            PersuasionTask persuasionTask = new PersuasionTask(0);

            persuasionTask.FinalFailLine = GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_2_wrong");
            persuasionTask.TryLaterLine = null;
            persuasionTask.SpokenLine = GameTexts.FindText("rf_fourth_quest_hellbound_dialog_2");

            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyEasy, false,
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_player_1"), null, false, false, false);
            persuasionTask.AddOptionToTask(option);
            PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Negative, PersuasionArgumentStrength.ExtremelyHard, false,
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_player_1_wrong"), null, false, false, false);
            persuasionTask.AddOptionToTask(option2);

            PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyEasy, false,
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_player_2"), null, false, false, false);
            persuasionTask.AddOptionToTask(option3);
            PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Negative, PersuasionArgumentStrength.ExtremelyHard, false,
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_player_2_wrong"), null, false, false, false);
            persuasionTask.AddOptionToTask(option4);

            _persuasionTask = persuasionTask;

            ConversationManager.StartPersuasion(2f, 1f, 1f, 1f, 1f, 0f, PersuasionDifficulty.MediumHard);
        }
        private DialogFlow FirstDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_1"))
            .Condition(() => takeBossToLordLog?.CurrentProgress == 2 && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_5"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_6"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_7"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_8"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_9"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_10"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_11"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_13"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_15"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_17"))
            .Consequence(FirstDialogConsequence).CloseDialog();

        private DialogFlow UliahTableDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_1")).Condition(() => Hero.OneToOneConversationHero == TheOwl && takeBossToLordLog?.CurrentProgress == 4)
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_5")).Consequence(OwlGivesTableConsequence);

        private DialogFlow MonkDialogFlow => DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_1"))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "quest_monastery_priest" && goToMonasteryLog?.CurrentProgress == 0 && Settlement.CurrentSettlement == QuestMonastery)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_6"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_7"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_8"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_9")).Consequence(ShowWaitScreen)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_10"))
            .PlayerLine(LineWithPlayerLink())
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_13"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_15"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_17"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_18"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_19")).Consequence(MonkDialogConsequence).CloseDialog();
    }
}