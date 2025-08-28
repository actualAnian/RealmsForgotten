using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
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
using RealmsForgotten.Quest.MissionBehaviors; // para RecordDamageMissionLogic (mesmo padrão da ThirdQuest)

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class EighthQuest : QuestBase
    {
        // ======== Campos base (fase 1) ========
        [SaveableField(0)] private JournalLog _meetPriestessLog;
        [SaveableField(1)] internal JournalLog _findSacredObjectLog;
        [SaveableField(2)] internal JournalLog _returnSacredObjectLog;
        [SaveableField(3)] private CampaignTime _leftHideoutTime = CampaignTime.Never;
        [SaveableField(4)] private bool _hasTriggeredPriestessReturnDialogue = false;
        [SaveableField(5)] private bool _owlSacredObjectDialogueStarted = false;
        [SaveableField(6)] private bool _ambushTriggered = false;
        [SaveableField(7)] private CampaignTime _ambushCheckStartTime = CampaignTime.Never;
        [SaveableField(8)] private bool _shouldTriggerPostAmbushOwlDialogue = false;

        // ======== Continuação (magos → site custom → boss → survivor → urkhai) ========
        [SaveableField(9)] private JournalLog _investigateMagesLog;
        [SaveableField(10)] private JournalLog _mageSiteLog;
        [SaveableField(11)] private JournalLog _orcTrailLog;
        [SaveableField(12)] private bool _mageInquiryTriggered = false;
        [SaveableField(13)] private bool _mageSiteMarked = false;

        [SaveableField(14)] private bool _mageBossDefeated = false;         // boss morreu dentro da missão
        [SaveableField(15)] private bool _survivorPendingAfterExit = false; // aguarda sair do site para conversar
        [SaveableField(16)] private bool _mageSurvivorDialogueDone = false;
        [SaveableField(17)] private bool _ninthQuestStarted = false;

        // ======== IDs (ajuste conforme seu mod) ========
        private const string RF_MAGE_SITE = "rf_mage_site";              // settlement custom (site dos magos)
        private const string RF_MAGE_BOSS_AGENT = "rf_mage_boss_agent";  // Character/Agent boss na missão do site
        private const string RF_MAGE_SURVIVOR_HERO = "rf_mage_survivor"; // herói/NPC sobrevivente
        private const string FIRST_TREE_TOWN = "town_FirstTree";
        private const string OWL_HERO_ID = "rf_the_owl";
        private const string PRIESTESS_CHAR_ID = "elvean_first_tree_druid_quest";
        private const string SACRED_OBJECT_ID = "sacred_object";

        private static Settlement FirstTreeSettlement => Settlement.Find(FIRST_TREE_TOWN);
        private static Settlement MageInvestigationSpot => Settlement.Find("castle_EN3"); // spot de “pista”
        private static Settlement MageSite => Settlement.Find(RF_MAGE_SITE);

        public EighthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold) { }

        // ======== Ciclo de vida ========
        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            ReinforceQuestLogs();
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            RegisterEvents();

            _findSacredObjectLog = AddDiscreteLog(
                new TextObject("The Priestess Awaits"),
                new TextObject("Strange parties with deformed raiders spread across the land. Investigate the rumours and search for clues."),
                0, 1
            );

            InitializeEighthQuestHideout();
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnLeaveSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted); // <<< como na ThirdQuest
        }

        protected override void OnTimedOut()
        {
            CompleteQuestWithFail();
        }

        protected override void OnFinalize()
        {
            CleanupEighthQuestHideout();
            if (MageSite != null) RemoveTrackedObject(MageSite);
        }

        public override TextObject Title => new TextObject("Eighth Quest: Call of the First Tree");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => false;

        // ======== Logs defensivos ========
        private void ReinforceQuestLogs()
        {
            if (_findSacredObjectLog == null)
            {
                _findSacredObjectLog = AddDiscreteLog(
                    new TextObject("The Priestess Awaits"),
                    new TextObject("Strange parties with deformed raiders spread across the land. Investigate the rumours and search for clues."),
                    0, 1
                );
                InformationManager.DisplayMessage(new InformationMessage("🛠 Reinstated missing 'Find Sacred Object' log."));
            }

            if (_returnSacredObjectLog == null && _findSacredObjectLog?.CurrentProgress == 1 && PlayerHasSacredObject())
            {
                _returnSacredObjectLog = AddDiscreteLog(
                    new TextObject("Return the Sacred Object"),
                    new TextObject("Return the sacred object to the First Tree Priestess."),
                    0, 1
                );
                InformationManager.DisplayMessage(new InformationMessage("🛠 Reinstated missing 'Return Sacred Object' log."));
            }
        }

        // ======== Eventos ========
        private void OnLeaveSettlement(MobileParty party, Settlement settlement)
        {
            if (!party.IsMainParty) return;

            // Saída do hideout inicial (teu fluxo Owl)
            if (settlement.StringId == "hideout_mountain_13")
            {
                if (!_owlSacredObjectDialogueStarted && _findSacredObjectLog?.CurrentProgress == 0)
                {
                    _owlSacredObjectDialogueStarted = true;
                    StartSacredObjectDialogue();
                }
            }

            // Saída do site custom dos magos → só agora conversa com o sobrevivente
            if (settlement.StringId == RF_MAGE_SITE && _mageBossDefeated && !_mageSurvivorDialogueDone)
            {
                if (_survivorPendingAfterExit)
                {
                    _survivorPendingAfterExit = false;
                    TriggerMageSurvivorConversation();
                }
            }
        }

        private void OnTick(float dt)
        {
            // Pós-emboscada: abre conversa com o Owl uma vez
            if (_shouldTriggerPostAmbushOwlDialogue)
            {
                _shouldTriggerPostAmbushOwlDialogue = false;

                Hero owl = Hero.FindFirst(h => h.StringId == OWL_HERO_ID);
                if (owl != null)
                {
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(owl.CharacterObject, PartyBase.MainParty)
                    );
                }
            }
        }

        protected override void HourlyTick()
        {
            // Owl após limpar hideout (fase 1)
            var hideout = Settlement.Find("hideout_mountain_13")?.Hideout;

            if (!_owlSacredObjectDialogueStarted
                && _findSacredObjectLog?.CurrentProgress == 0
                && hideout != null && !hideout.IsInfested)
            {
                _owlSacredObjectDialogueStarted = true;
                InformationManager.DisplayMessage(new InformationMessage("✅ Owl conversation triggered after hideout cleared."));
                StartSacredObjectDialogue();
            }

            // Investigação dos magos → marcar site
            OnHourlyTick_MageInvestigation();
        }

        private void OnDailyTick()
        {
            CheckPriestessProximity();

            if (!_ambushTriggered && _ambushCheckStartTime != CampaignTime.Never)
            {
                if ((CampaignTime.Now - _ambushCheckStartTime).ToDays >= 1f)
                {
                    _ambushTriggered = true;
                    _ambushCheckStartTime = CampaignTime.Never;
                    TriggerAmbush();
                }
            }
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (!mapEvent.IsPlayerMapEvent) return;

            // Detecta party da emboscada (fase 1)
            MobileParty ambushParty = null;

            var attackerParty = mapEvent.AttackerSide.LeaderParty?.MobileParty;
            var defenderParty = mapEvent.DefenderSide.LeaderParty?.MobileParty;

            if (attackerParty != null && attackerParty != MobileParty.MainParty &&
                attackerParty.StringId.StartsWith("rf_deformed_ambush_"))
            {
                ambushParty = attackerParty;
            }
            else if (defenderParty != null && defenderParty != MobileParty.MainParty &&
                     defenderParty.StringId.StartsWith("rf_deformed_ambush_"))
            {
                ambushParty = defenderParty;
            }

            if (ambushParty != null)
            {
                _ambushTriggered = true;
                _ambushCheckStartTime = CampaignTime.Never;

                ForceRemoveSacredObject();

                _returnSacredObjectLog?.UpdateCurrentProgress(1);

                _shouldTriggerPostAmbushOwlDialogue = true;

                AddDiscreteLog(
                    new TextObject("The Vessel Was Taken"),
                    new TextObject("You were ambushed and the vessel is gone. Speak with the Owl, then return to the Priestess."),
                    0, 1
                );
            }

            // A morte do boss dos magos é tratada dentro da missão (OnMissionStarted + RecordDamageMissionLogic)
        }

        // ======== Missão do site dos magos: marcar morte do boss sem behavior externo ========
        private void OnMissionStarted(IMission imission)
        {
            // Mesmo padrão da ThirdQuest: injeta RecordDamageMissionLogic com callback
            if (Settlement.CurrentSettlement != null &&
                Settlement.CurrentSettlement.StringId == RF_MAGE_SITE &&
                !_mageBossDefeated)
            {
                if (imission is Mission mission)
                {
                    mission.AddMissionBehavior(new RecordDamageMissionLogic((victim, attacker, damage) =>
                    {
                        if (victim?.Character == null) return;

                        // Quando o agente BOSS morrer…
                        if (victim.Character.StringId == RF_MAGE_BOSS_AGENT && damage >= victim.Health)
                        {
                            _mageBossDefeated = true;

                            // Se o player está dentro do site, só conversa ao sair
                            if (MobileParty.MainParty.CurrentSettlement != null &&
                                MobileParty.MainParty.CurrentSettlement.StringId == RF_MAGE_SITE)
                            {
                                _survivorPendingAfterExit = true;
                            }

                            // Marca o passo do site como concluído e remove tracking
                            _mageSiteLog?.UpdateCurrentProgress(1);
                            if (MageSite != null) RemoveTrackedObject(MageSite);
                        }
                    }));
                }
            }
        }

        // ======== Proximidade da sacerdotisa (fase 1) ========
        private void CheckPriestessProximity()
        {
            if (_hasTriggeredPriestessReturnDialogue || FirstTreeSettlement == null)
                return;

            if (_meetPriestessLog == null || _meetPriestessLog.CurrentProgress != 1)
                return;

            float distance = MobileParty.MainParty.Position2D.Distance(FirstTreeSettlement.GatePosition);
            if (distance <= 50f)
            {
                var priestess = CharacterObject.Find(PRIESTESS_CHAR_ID);
                if (priestess != null)
                {
                    _hasTriggeredPriestessReturnDialogue = true;
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(priestess)
                    );
                }
            }
        }

        // ======== Ambush (fase 1) ========
        private void TriggerAmbush()
        {
            try
            {
                Clan deformedClan = Clan.FindFirst(c => c.StringId == "deformed_villagers");
                if (deformedClan == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Deformed clan not found."));
                    return;
                }

                var troopPool = new Dictionary<string, int>
                {
                    { "deformed_villager_bandit", 30 },
                    { "deformed_villager_raider", 15 },
                    { "deformed_villager_chief", 5 },
                    { "deformed_villager_boss", 1 }
                };

                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                foreach (var kv in troopPool)
                {
                    var character = CharacterObject.Find(kv.Key);
                    if (character != null) troopRoster.AddToCounts(character, kv.Value);
                    else InformationManager.DisplayMessage(new InformationMessage($"⚠️ Troop not found: {kv.Key}"));
                }

                string uniqueId = $"rf_deformed_ambush_{MBRandom.RandomInt(10000)}";
                MobileParty ambushParty = BanditPartyComponent.CreateBanditParty(uniqueId, deformedClan, null, true);
                if (ambushParty == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Failed to create ambush party."));
                    return;
                }

                ambushParty.InitializeMobilePartyAroundPosition(
                    troopRoster,
                    TroopRoster.CreateDummyTroopRoster(),
                    MobileParty.MainParty.Position2D,
                    0f,
                    0f);

                ambushParty.Aggressiveness = 100f;
                ambushParty.Ai.SetMoveEngageParty(MobileParty.MainParty);
                ambushParty.SetCustomName(new TextObject("Deformed Ambushers"));

                InformationManager.DisplayMessage(new InformationMessage("☠️ A deformed ambush party has been spawned on your position!"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Ambush spawn error: {ex.Message}"));
            }
        }

        // ======== Continuação: investigação dos magos ========
        private void OnHourlyTick_MageInvestigation()
        {
            if (_investigateMagesLog == null || _mageInquiryTriggered || MageInvestigationSpot == null)
                return;

            float distance = MobileParty.MainParty.Position2D.Distance(MageInvestigationSpot.GatePosition);
            if (distance <= 50f)
            {
                _mageInquiryTriggered = true;

                InformationManager.ShowInquiry(new InquiryData(
                    "Strange Tracks",
                    "Your scouts notice drag marks and a faint magical residue leading into the mountains.",
                    true, false,
                    "Investigate", null,
                    () =>
                    {
                        _mageSiteLog ??= AddDiscreteLog(
                            new TextObject("Mage Site"),
                            new TextObject("Follow the tracks and investigate the suspicious site where the mages are hiding."),
                            0, 1
                        );

                        if (MageSite != null && !_mageSiteMarked)
                        {
                            AddTrackedObject(MageSite);
                            _mageSiteMarked = true;
                        }
                    },
                    null
                ));
            }
        }

        // ======== Conversa com o sobrevivente (após sair do site e boss morto) ========
        private void TriggerMageSurvivorConversation()
        {
            Hero survivor = Hero.FindFirst(h => h.StringId == RF_MAGE_SURVIVOR_HERO);
            if (survivor != null)
            {
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(survivor.CharacterObject, PartyBase.MainParty)
                );
            }
            else
            {
                // Fallback se o NPC não existir no XML
                InformationManager.ShowInquiry(new InquiryData(
                    "Sobrevivente",
                    "Eles nos forçaram a beber líquidos fétidos que entorpeciam e transformavam alguns... Muitos foram levados para as terras dos Urkhai.",
                    true, false,
                    "Entendido", null,
                    () =>
                    {
                        _orcTrailLog ??= AddDiscreteLog(
                            new TextObject("Trail to the Orcs"),
                            new TextObject("The survivor revealed that captives were taken into Urkhai territory. Travel there to continue your investigation."),
                            0, 1
                        );
                        _mageSurvivorDialogueDone = true;

                        // Inicia a Ninth quest (sem concluir a Eight)
                        StartNinthQuest();
                    },
                    null
                ));
            }
        }

        // ======== DialogFlows ========
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(SacredObjectDialogue, this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessReturnDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessFinalDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(MageSurvivorDialog, this);
        }

        private DialogFlow SacredObjectDialogue => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("Que diabos eram aquelas coisas? Pareciam humanas... mas torcidas. E encontrei isto entre os corpos."))
            .Condition(() =>
                Hero.OneToOneConversationHero?.StringId == OWL_HERO_ID &&
                _findSacredObjectLog?.CurrentProgress == 0)
            .NpcLine(new TextObject("Parece algum tipo de vaso."))
            .PlayerLine(new TextObject("A Sacerdotisa precisa ver isso. Talvez seja a pista que procurávamos."))
            .Consequence(() =>
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>(SACRED_OBJECT_ID);
                if (item != null)
                {
                    MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
                    InformationManager.DisplayMessage(new InformationMessage("✅ Sacred Object adicionado ao seu inventário."));

                    _findSacredObjectLog?.UpdateCurrentProgress(1);

                    _returnSacredObjectLog ??= AddDiscreteLog(
                        new TextObject("Return the Sacred Object"),
                        new TextObject("Return the sacred object to the First Tree Priestess."),
                        0, 1
                    );

                    _ambushCheckStartTime = CampaignTime.Now;
                    InformationManager.DisplayMessage(new InformationMessage("⚠️ Você sente que está sendo observado..."));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Sacred Object não encontrado.", Colors.Red));
                }
            })
            .CloseDialog();

        private DialogFlow PriestessReturnDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("O que foi aquilo? Saíram do nada!"))
            .Condition(() =>
                CharacterObject.OneToOneConversationCharacter?.HeroObject?.StringId == OWL_HERO_ID &&
                _returnSacredObjectLog?.CurrentProgress == 1 &&
                !PlayerHasSacredObject())
            .NpcLine(new TextObject("Nem nossos batedores viram. Pode ser feitiçaria."))
            .PlayerLine(new TextObject("O vaso... Eu o perdi na luta."))
            .NpcLine(new TextObject("Ou foi roubado. Se há magia, talvez nos seguiram. A emboscada pode ter sido só distração."))
            .PlayerLine(new TextObject("É possível. Mas não temos pistas."))
            .NpcLine(new TextObject("A não ser que os magos estejam envolvidos. Talvez possam rastrear — se estiverem dispostos."))
            .PlayerLine(new TextObject("Tem razão. Não temos outra pista."))
            .Consequence(() =>
            {
                ForceRemoveSacredObject();

                _meetPriestessLog ??= AddDiscreteLog(
                    new TextObject("Find the Priestess"),
                    new TextObject("Return to the Priestess at the First Tree and report what happened."),
                    0, 1
                );
                _meetPriestessLog.UpdateCurrentProgress(1);
            })
            .CloseDialog();

        private DialogFlow PriestessFinalDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("Sacerdotisa, fomos emboscados. O vaso foi levado."))
            .Condition(() =>
                CharacterObject.OneToOneConversationCharacter?.StringId == PRIESTESS_CHAR_ID &&
                _meetPriestessLog != null && _meetPriestessLog.CurrentProgress == 1 &&
                !PlayerHasSacredObject())
            .NpcLine(new TextObject("A magia antiga se move... Não foi acaso. Suspeito dos magos, mas sem provas."))
            .PlayerLine(new TextObject("Então vou investigá-los."))
            .Consequence(() =>
            {
                _investigateMagesLog ??= AddDiscreteLog(
                    new TextObject("Investigate the Mages"),
                    new TextObject("Travel near their lands and search for clues about their involvement."),
                    0, 1
                );
            })
            .CloseDialog();

        private DialogFlow MageSurvivorDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("Calma. Você está seguro. O que aconteceu aqui?"))
            .Condition(() =>
                CharacterObject.OneToOneConversationCharacter?.HeroObject?.StringId == RF_MAGE_SURVIVOR_HERO
                && _mageBossDefeated
                && !_mageSurvivorDialogueDone)
            .NpcLine(new TextObject("Eles nos forçaram a beber líquidos fétidos... Aos poucos, alguns enlouqueciam, outros mudavam."))
            .PlayerLine(new TextObject("Quem são 'eles'?"))
            .NpcLine(new TextObject("Magos. Ou algo pior, usando-os. Vi símbolos... E vi prisioneiros sendo levados para o leste, para as terras dos Urkhai."))
            .PlayerLine(new TextObject("Urkhai... Então é lá que devo procurar."))
            .NpcLine(new TextObject("Se for, vá rápido. Quem é levado para lá... raramente volta."))
            .Consequence(() =>
            {
                _orcTrailLog ??= AddDiscreteLog(
                    new TextObject("Trail to the Orcs"),
                    new TextObject("The survivor revealed that captives were taken into Urkhai territory. Travel there to continue your investigation."),
                    0, 1
                );
                _mageSurvivorDialogueDone = true;

                StartNinthQuest();
            })
            .CloseDialog();

        // ======== Utils ========
        private void InitializeEighthQuestHideout()
        {
            var hideout = Settlement.Find("hideout_mountain_13")?.Hideout;
            if (hideout != null)
            {
                QuestLibrary.InitializeHideoutIfNeeded(hideout);
                AddTrackedObject(hideout.Settlement);
            }
        }

        private void CleanupEighthQuestHideout()
        {
            var hideout = Settlement.Find("hideout_mountain_13")?.Hideout;
            if (hideout != null) RemoveTrackedObject(hideout.Settlement);
        }

        private void StartSacredObjectDialogue()
        {
            var owl = Hero.FindFirst(h => h.StringId == OWL_HERO_ID);
            if (owl != null)
            {
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(owl.CharacterObject, PartyBase.MainParty)
                );
            }
        }

        private bool PlayerHasSacredObject()
        {
            var item = MBObjectManager.Instance.GetObject<ItemObject>(SACRED_OBJECT_ID);
            return item != null && MobileParty.MainParty.ItemRoster.GetItemNumber(item) > 0;
        }

        private void ForceRemoveSacredObject()
        {
            var sacredObject = MBObjectManager.Instance.GetObject<ItemObject>(SACRED_OBJECT_ID);
            if (sacredObject == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Sacred Object definition missing."));
                return;
            }

            int count = MobileParty.MainParty.ItemRoster.GetItemNumber(sacredObject);
            if (count > 0)
            {
                MobileParty.MainParty.ItemRoster.AddToCounts(sacredObject, -count);
                InformationManager.DisplayMessage(new InformationMessage("⚠️ The Sacred Object was forcibly removed.", Colors.Red));
            }
        }

        // Tenta iniciar a NinthQuest direto; também emite um evento para quem preferir iniciar externamente.
        private void StartNinthQuest()
        {
            if (_ninthQuestStarted)
                return;

            NinthQuest newChapter = new NinthQuest("rf_nínth_quest_outro", QuestGiver, CampaignTime.DaysFromNow(999), 20000);
            newChapter.StartQuest();
        }
    }
}
