using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Quest
{
    /// <summary>
    /// Papel de missao ocupado por um heroi fixo do XML (o "rei dos Dugrast", o "senhor
    /// anorita"...). Guarda de quem era o papel e por onde procurar quem herda.
    /// </summary>
    public sealed class QuestHeroRole
    {
        public string RoleId;

        /// <summary>StringId do heroi original definido no XML.</summary>
        public string OriginalHeroId;

        /// <summary>
        /// Reino cujo trono carrega o papel (opcional). Quando o papel e de um rei, o
        /// herdeiro certo e quem senta no trono agora — a sucessao vanilla ja cuida disso.
        /// </summary>
        public string KingdomId;

        /// <summary>Cultura de ultimo recurso, quando nem cla nem reino sobraram (opcional).</summary>
        public string CultureId;

        /// <summary>
        /// Personagens unicos de historia (o Owl) nao devem ser "herdados" por um nobre
        /// qualquer da mesma cultura — para eles isso fica falso e o papel morre com o dono.
        /// </summary>
        public bool AllowCultureFallback = true;

        /// <summary>Nome usado no aviso ao jogador quando alguem herda o papel.</summary>
        public TextObject Title;
    }

    /// <summary>
    /// Resolve "quem responde por este papel agora".
    ///
    /// Motivo (feedback de jogador, 2026-08-18): a missao do rei dos Dugrast checava
    /// <c>Hero.OneToOneConversationHero?.StringId == "lord_dugrast_faction_1"</c>. Quando
    /// Borgul Tharn morre na campanha — e ele morre, e um lorde normal — o dialogo nunca
    /// mais aparece e a missao trava sem aviso nenhum.
    ///
    /// A ordem de sucessao e, do mais para o menos legitimo:
    ///   1. substituto ja escolhido antes (e ainda vivo) — o papel nao fica pulando de dono;
    ///   2. o heroi original, se estiver vivo;
    ///   3. quem ocupa o trono do reino do papel (sucessao vanilla);
    ///   4. lider do cla do original, ou o nobre mais graduado que sobrou nesse cla;
    ///   5. nobre vivo da mesma cultura, preferindo reis e depois lideres de cla.
    ///
    /// A escolha e persistida na campanha, entao o jogador nao ve o papel trocar de dono a
    /// cada carregamento.
    /// </summary>
    public static class QuestHeroes
    {
        public const string DwarfKing = "dwarf_king";
        public const string AnoritLord = "anorit_lord";
        public const string DreadKing = "dread_king";
        public const string AserailMercenaryLord = "mercenary_lord_3_1";
        public const string TheOwl = "the_owl";

        private static readonly Dictionary<string, QuestHeroRole> Roles = new List<QuestHeroRole>
        {
            new QuestHeroRole
            {
                RoleId = DwarfKing,
                OriginalHeroId = "lord_dugrast_faction_1",
                KingdomId = "dwarf_kingdom",
                CultureId = "dwarf",
                Title = new TextObject("{=rf_role_dwarf_king}King of the Dugrast")
            },
            new QuestHeroRole
            {
                RoleId = AnoritLord,
                OriginalHeroId = "lord_WE9_l",
                CultureId = "west_realm",
                Title = new TextObject("{=rf_role_anorit_lord}Lord of Anorit")
            },
            new QuestHeroRole
            {
                RoleId = DreadKing,
                OriginalHeroId = "lord_2_1",
                Title = new TextObject("{=rf_role_dread_king}The Dreadking")
            },
            new QuestHeroRole
            {
                RoleId = AserailMercenaryLord,
                OriginalHeroId = "lord_3_1",
                Title = new TextObject("{=rf_role_merc_lord}The mercenary lord")
            },
            new QuestHeroRole
            {
                // Personagem unico: sem herdeiro. Se o Owl morre, quem chama trata o null.
                RoleId = TheOwl,
                OriginalHeroId = "rf_the_owl",
                AllowCultureFallback = false,
                Title = new TextObject("{=rf_role_the_owl}The Owl")
            }
        }.ToDictionary(r => r.RoleId, r => r);

        /// <summary>Heroi que responde pelo papel hoje. Pode ser null se nao sobrou ninguem.</summary>
        public static Hero Resolve(string roleId)
        {
            if (!Roles.TryGetValue(roleId, out QuestHeroRole role) || Campaign.Current == null)
            {
                return null;
            }

            try
            {
                Hero stored = QuestHeroSuccessionBehavior.GetSuccessor(roleId);
                if (IsUsable(stored))
                {
                    return stored;
                }

                Hero original = MBObjectManager.Instance.GetObject<Hero>(role.OriginalHeroId);
                if (IsUsable(original))
                {
                    return original;
                }

                Hero heir = FindHeir(role, original);
                if (heir != null)
                {
                    QuestHeroSuccessionBehavior.SetSuccessor(roleId, heir, role, original);
                }
                return heir;
            }
            catch (Exception e)
            {
                Debug.Print("[RF_QuestHeroes] Falha ao resolver o papel " + roleId + ": " + e.Message);
                return null;
            }
        }

        /// <summary>Este heroi e quem responde pelo papel? Use nas condicoes de dialogo.</summary>
        public static bool Is(Hero hero, string roleId)
        {
            if (hero == null)
            {
                return false;
            }

            // Atalho barato: se ainda e o dono original, nem precisa resolver a sucessao.
            if (Roles.TryGetValue(roleId, out QuestHeroRole role) &&
                hero.StringId == role.OriginalHeroId && hero.IsAlive)
            {
                return true;
            }

            return hero == Resolve(roleId);
        }

        /// <summary>Quem esta na conversa responde pelo papel?</summary>
        public static bool IsInConversation(string roleId) => Is(Hero.OneToOneConversationHero, roleId);

        /// <summary>
        /// Consorte do trono de um reino (a "rainha" das missoes). Mesma ideia dos papeis
        /// acima, mas aqui nem precisa de cache: o trono ja e dinamico. Se a consorte morreu,
        /// quem responde e quem senta no trono; se o reino sumiu, devolve null em vez de
        /// estourar (o codigo antigo fazia Kingdom.All.First(...).Leader.Spouse e quebrava).
        /// </summary>
        public static Hero ResolveRulerConsort(string kingdomId)
        {
            try
            {
                if (Campaign.Current == null)
                {
                    return null;
                }

                Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == kingdomId && !k.IsEliminated)
                                  ?? Kingdom.All.FirstOrDefault(k => k.StringId == kingdomId);
                if (kingdom == null)
                {
                    return null;
                }

                Hero leader = kingdom.Leader;
                if (IsUsable(leader?.Spouse))
                {
                    return leader.Spouse;
                }
                return IsUsable(leader) ? leader : null;
            }
            catch (Exception e)
            {
                Debug.Print("[RF_QuestHeroes] Falha ao resolver a consorte de " + kingdomId + ": " + e.Message);
                return null;
            }
        }

        internal static QuestHeroRole GetRole(string roleId)
            => Roles.TryGetValue(roleId, out QuestHeroRole role) ? role : null;

        internal static IEnumerable<QuestHeroRole> AllRoles => Roles.Values;

        private static bool IsUsable(Hero hero) => hero != null && hero.IsAlive && !hero.IsDisabled;

        private static Hero FindHeir(QuestHeroRole role, Hero original)
        {
            // 1. O trono. Para papel de rei este e o herdeiro certo: a sucessao vanilla ja
            //    escolheu alguem quando o rei morreu.
            Kingdom kingdom = FindKingdom(role, original);
            if (kingdom != null && !kingdom.IsEliminated && IsUsable(kingdom.Leader) && kingdom.Leader != original)
            {
                return kingdom.Leader;
            }

            // 2. O cla do original.
            Clan clan = original?.Clan;
            if (clan != null && !clan.IsEliminated)
            {
                if (IsUsable(clan.Leader) && clan.Leader != original)
                {
                    return clan.Leader;
                }

                Hero fromClan = clan.Heroes
                    .Where(h => IsUsable(h) && h != original && h.IsLord)
                    .OrderByDescending(h => h.Age)
                    .FirstOrDefault();
                if (fromClan != null)
                {
                    return fromClan;
                }
            }

            // 3. Mesma cultura: primeiro reis, depois lideres de cla, depois o cla mais forte.
            if (!role.AllowCultureFallback)
            {
                return null;
            }

            CultureObject culture = FindCulture(role, original);
            if (culture == null)
            {
                return null;
            }

            List<Hero> candidates = Hero.AllAliveHeroes
                .Where(h => IsUsable(h) && h.IsLord && h.Culture == culture && h != original && h != Hero.MainHero)
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates
                .OrderByDescending(h => h.Clan != null && h.Clan.Kingdom != null && h.Clan.Kingdom.Leader == h ? 1 : 0)
                .ThenByDescending(h => h.Clan != null && h.Clan.Leader == h ? 1 : 0)
                .ThenByDescending(h => h.Clan?.Tier ?? 0)
                .ThenByDescending(h => h.Clan?.Renown ?? 0f)
                .First();
        }

        private static Kingdom FindKingdom(QuestHeroRole role, Hero original)
        {
            if (!string.IsNullOrEmpty(role.KingdomId))
            {
                Kingdom byId = Kingdom.All.FirstOrDefault(k => k.StringId == role.KingdomId);
                if (byId != null)
                {
                    return byId;
                }
            }
            return original?.Clan?.Kingdom;
        }

        private static CultureObject FindCulture(QuestHeroRole role, Hero original)
        {
            if (original?.Culture != null)
            {
                return original.Culture;
            }
            if (!string.IsNullOrEmpty(role.CultureId))
            {
                return MBObjectManager.Instance.GetObject<CultureObject>(role.CultureId);
            }
            return null;
        }
    }

    /// <summary>
    /// Guarda na campanha quem herdou cada papel de missao e avisa o jogador na troca.
    /// Sem isso o papel seria recalculado a cada save/load e poderia cair em outro nobre.
    /// </summary>
    public class QuestHeroSuccessionBehavior : CampaignBehaviorBase
    {
        private static QuestHeroSuccessionBehavior _instance;

        private Dictionary<string, Hero> _successors = new Dictionary<string, Hero>();

        public QuestHeroSuccessionBehavior()
        {
            _instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_rfQuestHeroSuccessors", ref _successors);
            if (_successors == null)
            {
                _successors = new Dictionary<string, Hero>();
            }
            _instance = this;
        }

        internal static Hero GetSuccessor(string roleId)
        {
            if (_instance?._successors == null)
            {
                return null;
            }
            return _instance._successors.TryGetValue(roleId, out Hero hero) ? hero : null;
        }

        internal static void SetSuccessor(string roleId, Hero heir, QuestHeroRole role, Hero original)
        {
            if (_instance == null || heir == null)
            {
                return;
            }

            _instance._successors[roleId] = heir;
            Announce(role, heir, original);
        }

        /// <summary>
        /// Quando o dono atual do papel morre, so limpamos o cache: quem chamar o Resolve
        /// depois recebe o proximo da linha (e o jogador e avisado nessa hora).
        /// </summary>
        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (victim == null || _successors.Count == 0)
            {
                return;
            }

            List<string> affected = _successors
                .Where(pair => pair.Value == victim)
                .Select(pair => pair.Key)
                .ToList();

            foreach (string roleId in affected)
            {
                _successors.Remove(roleId);
            }
        }

        private static void Announce(QuestHeroRole role, Hero heir, Hero original)
        {
            try
            {
                TextObject message = new TextObject("{=rf_role_inherited}{OLD_NAME} is dead. {NEW_NAME} now answers as {ROLE}.");
                message.SetTextVariable("OLD_NAME", original?.Name ?? role.Title);
                message.SetTextVariable("NEW_NAME", heir.Name);
                message.SetTextVariable("ROLE", role.Title);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Yellow));
                Debug.Print("[RF_QuestHeroes] Papel " + role.RoleId + " herdado por " + heir.StringId);
            }
            catch
            {
                // aviso e cosmetico; nunca deve derrubar a resolucao do papel
            }
        }
    }
}
