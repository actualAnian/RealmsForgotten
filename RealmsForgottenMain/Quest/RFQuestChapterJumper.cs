using System;
using System.Linq;
using RealmsForgotten.Quest.SecondUpdate;
using RealmsForgotten.Quest.FourthUpdate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Quest
{
    /// <summary>
    /// FERRAMENTA DE FILMAGEM (2026-08-26, pedido do autor apos crashes comerem horas
    /// de progresso): salto direto para qualquer capitulo da quest principal, via MCM
    /// ou console (rf.quest.jump N). Gated pelo arquivo-flag local
    /// (FourthQuest.FilmingToolsEnabled) — em maquina de jogador e codigo morto.
    ///
    /// O salto CANCELA as quests principais RF ativas, limpa parties de quest
    /// conhecidas e inicia o capitulo alvo com o giver resolvido pelos papeis
    /// ([[QuestHeroes]]) — os fallbacks garantem giver vivo. Pre-condicoes finas de
    /// cada capitulo (itens/prisioneiros especificos) sao best-effort: o que faltar
    /// num capitulo especifico o autor reporta e a rotina de setup ganha o item.
    /// USAR NUMA COPIA DO SAVE.
    /// </summary>
    public static class RFQuestChapterJumper
    {
        public static readonly string[] ChapterNames =
        {
            "1 - Rescue Uliah",
            "2 - The Queen (Second Quest)",
            "2b - Anorit Relics",
            "3 - Athas (Third Quest)",
            "4 - Boss to the Lord (Nelrog ambush)",
            "5 - Fifth Quest (Owl treasure)",
            "5b - Fifth Quest, after the Elvean Polearm",
            "6 - Sixth Quest",
            "7 - Seventh Quest",
            "8 - Eighth Quest",
            "9 - Ninth Quest"
        };

        public static string JumpTo(int chapterIndex)
        {
            if (!FourthQuest.FilmingToolsEnabled)
            {
                return "Ferramenta de dev desativada nesta maquina.";
            }
            if (Campaign.Current == null)
            {
                return "Campanha nao esta rodando.";
            }
            if (chapterIndex < 0 || chapterIndex >= ChapterNames.Length)
            {
                return "Capitulo invalido.";
            }

            int cancelled = CancelActiveMainQuests();
            CleanupKnownQuestParties();

            QuestBase quest;
            try
            {
                quest = CreateChapter(chapterIndex);
            }
            catch (Exception ex)
            {
                return $"Falha ao criar o capitulo: {ex.Message}";
            }

            if (quest == null)
            {
                return "Nao consegui resolver o quest giver deste capitulo neste save.";
            }

            try
            {
                quest.StartQuest();
            }
            catch (Exception ex)
            {
                return $"Capitulo criado mas StartQuest falhou: {ex.Message}";
            }

            // Pos-setup por capitulo (estados intermediarios / pre-condicoes).
            try
            {
                if (chapterIndex == 6 && quest is FifthQuest fifthAfterPolearm)
                {
                    fifthAfterPolearm.SetupChapterAfterPolearm();
                }
                if (chapterIndex == 5 || chapterIndex == 6)
                {
                    // A 5a depende do Owl como companheiro (na cadeia real ele ja veio da 4a).
                    EnsureOwlWithPlayer();
                }
            }
            catch (Exception ex)
            {
                return $"Capitulo iniciado mas o setup do estado falhou: {ex.Message}";
            }

            string result = $"Capitulo iniciado: {ChapterNames[chapterIndex]} (quests canceladas: {cancelled}).";
            InformationManager.DisplayMessage(new InformationMessage(result, Colors.Yellow));
            return result;
        }

        private static int CancelActiveMainQuests()
        {
            int cancelled = 0;
            foreach (QuestBase quest in Campaign.Current.QuestManager.Quests.ToList())
            {
                if (quest == null || quest.IsFinalized)
                {
                    continue;
                }
                string ns = quest.GetType().Namespace ?? string.Empty;
                if (!ns.StartsWith("RealmsForgotten.Quest"))
                {
                    continue;
                }
                try
                {
                    quest.CompleteQuestWithCancel();
                    cancelled++;
                }
                catch (Exception ex)
                {
                    Debug.Print($"[RF_ChapterJump] cancelamento de {quest.GetType().Name} falhou: {ex.Message}");
                }
            }
            return cancelled;
        }

        private static void CleanupKnownQuestParties()
        {
            foreach (MobileParty party in MobileParty.All.ToList())
            {
                string id = party?.StringId;
                if (id != null && (id.StartsWith("quest_hellbound") || id == "owl_party") && party.IsActive)
                {
                    try
                    {
                        DestroyPartyAction.Apply(null, party);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Mesmo idioma do EnsureOwlInParty da FourthQuest: garante o Owl vivo, ativo e
        /// na party do jogador — os dialogos da 5a quest sao todos com ele.
        /// </summary>
        private static void EnsureOwlWithPlayer()
        {
            Hero owl = QuestHeroSuccessionBehavior.RepairImmortal(QuestHeroes.TheOwl);
            if (owl == null || owl.IsPrisoner || owl.PartyBelongedTo == MobileParty.MainParty)
            {
                return;
            }
            if (!owl.IsActive)
            {
                owl.ChangeState(Hero.CharacterStates.Active);
            }
            owl.HitPoints = Math.Max(owl.HitPoints, Math.Max(10, owl.MaxHitPoints / 2));
            AddHeroToPartyAction.Apply(owl, MobileParty.MainParty);
        }

        /// <summary>Giver da cadeia 3ª-7ª: senhor anorita → rainha → jogador.</summary>
        private static Hero ChainGiver()
        {
            Hero giver = QuestHeroes.Resolve(QuestHeroes.AnoritLord);
            if (giver == null || !giver.IsAlive)
            {
                giver = QuestHeroes.ResolveRulerConsort("empire");
            }
            return giver != null && giver.IsAlive ? giver : Hero.MainHero;
        }

        private static QuestBase CreateChapter(int index)
        {
            switch (index)
            {
                case 0:
                {
                    // Uliah = hero de id "questGiver" (privado no RescueUliahBehavior).
                    Hero uliah = Hero.AllAliveHeroes.FirstOrDefault(x => x.StringId == "questGiver") ?? Hero.MainHero;
                    return new RescueUliahBehavior.RescueUliahQuest("rescue_uliah_quest", uliah, CampaignTime.Never, 0);
                }
                case 1:
                {
                    Hero queen = QuestHeroes.ResolveRulerConsort("empire") ?? Hero.MainHero;
                    return new SecondQuest("rf_queen_quest", queen, CampaignTime.Never, 50000, false);
                }
                case 2:
                {
                    Hero anorit = QuestHeroes.Resolve(QuestHeroes.AnoritLord) ?? Hero.MainHero;
                    return new AnoritFindRelicsQuest("anorit_quest", anorit, CampaignTime.Never, 50000);
                }
                case 3:
                    return new ThirdQuest("athas_quest", ChainGiver(), CampaignTime.Never, 0);
                case 4:
                    return new FourthQuest("rf_fourth_quest", ChainGiver(), CampaignTime.Never, 100000);
                case 5:
                case 6: // 5b — mesmo capitulo; o estado pos-polearm e aplicado apos o StartQuest
                    return new FifthQuest("rf_fifth_quest", ChainGiver(), CampaignTime.Never, 50000);
                case 7:
                    return new SixthQuest("rf_sixth_quest", ChainGiver(), CampaignTime.Never, 50000);
                case 8:
                    return new SeventhQuest("rf_seventh_quest", ChainGiver(), CampaignTime.DaysFromNow(999), 20000);
                case 9:
                    return new EighthQuest("rf_eighth_quest", Hero.MainHero, CampaignTime.Never, 0);
                case 10:
                    return new NinthQuest("rf_ninth_quest", Hero.MainHero, CampaignTime.Never, 0);
                default:
                    return null;
            }
        }
    }
}
