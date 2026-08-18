using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Helpers;
using RF_AliveScenes.Config;
using RF_AliveScenes.Data;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_AliveScenes.Runtime;

/// <summary>
/// Cerebro da fala ambiente de uma missao: classifica o figurante, monta o contexto,
/// respeita cooldowns e devolve o texto ja formatado. Nao conhece UI — quem desenha e a
/// <see cref="UI.AliveScenesBubbleView"/>, avisada pelos eventos daqui.
/// </summary>
public sealed class SpeechDirector
{
    private const float MinDistanceBetweenSpeakers = 4f;
    private const float DialogueNeighbourRadius = 3f;

    /// <summary>agente, texto, e inimigo do jogador.</summary>
    public event Action<Agent, string, bool> AgentSpoke;

    /// <summary>o balao daquele agente expirou.</summary>
    public event Action<Agent> AgentStoppedSpeaking;

    private sealed class RunningDialogue
    {
        public DialogueDef Def;
        public int Index;
        public Agent First;
        public Agent Second;
        public Agent CurrentSpeaker;
        public float NextLineAt;
        public List<Agent> Listeners;
    }

    private readonly Mission _mission;
    private readonly BattleSetup _battle;
    private readonly SpeechSession _session = new();
    private readonly AliveScenesSettings _settings = AliveScenesSettings.Instance;

    private readonly Dictionary<Agent, float> _speakingUntil = new();
    private readonly Dictionary<Agent, float> _lastSpokeAt = new();
    private readonly List<RunningDialogue> _dialogues = new();

    private float _clock;
    private float _cooldownPeriod;
    private bool _staticContextReady;

    // contexto estatico da cena
    private bool _isRich;
    private bool _isPoor;
    private bool _inTavern;
    private bool _inKeep;
    private bool _inPort;
    private bool _isTown;
    private bool _isVillage;
    private bool _playerHasSword;
    private bool _playerHasBow;
    private bool _playerHasHorse;
    private bool _playerHasExpensiveArmor;
    private bool _playerHasExpensiveSword;

    public SpeechDirector(Mission mission, BattleSetup battle)
    {
        _mission = mission;
        _battle = battle;
        _cooldownPeriod = battle != null ? _settings.BattleCooldown : _settings.CasualCooldown;
    }

    public bool IsBattle => _battle != null;

    /// <summary>Ainda cabe mais uma conversa simultanea?</summary>
    public bool HasFreeSlot()
    {
        int limit = IsBattle ? _settings.BattleMaxConversations : _settings.CasualMaxConversations;
        return _speakingUntil.Count + _dialogues.Count < limit;
    }

    public void Tick(float dt)
    {
        _clock += dt;
        ExpireBubbles();
        AdvanceDialogues();
        ForgetOldSpeakers();
    }

    private void ExpireBubbles()
    {
        List<Agent> expired = null;
        foreach (KeyValuePair<Agent, float> pair in _speakingUntil)
        {
            if (pair.Value <= _clock)
            {
                (expired ??= new List<Agent>()).Add(pair.Key);
            }
        }

        if (expired == null)
        {
            return;
        }

        foreach (Agent agent in expired)
        {
            _speakingUntil.Remove(agent);
            AgentStoppedSpeaking?.Invoke(agent);
        }
    }

    private void AdvanceDialogues()
    {
        for (int i = _dialogues.Count - 1; i >= 0; i--)
        {
            RunningDialogue run = _dialogues[i];
            if (run.NextLineAt > _clock)
            {
                continue;
            }

            AgentStoppedSpeaking?.Invoke(run.CurrentSpeaker);

            if (run.Index >= run.Def.Lines.Length || !IsUsable(run.First) || !IsUsable(run.Second))
            {
                _dialogues.RemoveAt(i);
                continue;
            }

            DialogueLine line = run.Def.Lines[run.Index++];
            Agent speaker = line.ActorId == 2 ? run.Second : run.First;
            if (line.ActorId != 1 && line.ActorId != 2)
            {
                // linha sem ActorID valido: alterna em relacao a quem falou por ultimo
                speaker = run.CurrentSpeaker == run.First ? run.Second : run.First;
            }

            run.CurrentSpeaker = speaker;
            string text = FormatText(line.ToLocalizedRaw());
            run.NextLineAt = _clock + ReadingTime(text);
            _lastSpokeAt[speaker] = _clock;
            AgentSpoke?.Invoke(speaker, text, false);
        }
    }

    private void ForgetOldSpeakers()
    {
        List<Agent> stale = null;
        foreach (KeyValuePair<Agent, float> pair in _lastSpokeAt)
        {
            if (pair.Value + _cooldownPeriod < _clock)
            {
                (stale ??= new List<Agent>()).Add(pair.Key);
            }
        }

        if (stale == null)
        {
            return;
        }

        foreach (Agent agent in stale)
        {
            _lastSpokeAt.Remove(agent);
        }
    }

    // ------------------------------------------------------------------ cena pacifica

    /// <summary>Tenta fazer o agente falar numa cena pacifica. Devolve vazio se nao rolar.</summary>
    public string TryCasualLine(Agent agent)
    {
        if (!_staticContextReady)
        {
            SetupStaticContext();
        }

        if (_inTavern && !_settings.TavernChatEnabled)
        {
            return string.Empty;
        }

        if (!IsUsable(agent) || IsBusy(agent) || HasSpeakerNearby(agent))
        {
            return string.Empty;
        }

        CharacterObject character = agent.Character as CharacterObject;
        ActionIndexCache action = agent.GetCurrentAction(0);
        string actionName = action.GetName() ?? string.Empty;

        if (actionName.Contains("act_talk_to"))
        {
            return string.Empty;
        }

        ActorType actorType = ClassifyTownAgent(agent, character, actionName);

        SpeechContext context = new SpeechContext
        {
            Season = CampaignTime.Now.GetSeasonOfYear,
            HasSword = _playerHasSword,
            HasBow = _playerHasBow,
            HasHorse = _playerHasHorse,
            HasExpensiveArmor = _playerHasExpensiveArmor,
            HasExpensiveSword = _playerHasExpensiveSword,
            IsStanding = actionName.Contains("act_stand"),
            IsFarming = actionName.Contains("pick_up_from"),
            IsRichSettlement = _isRich,
            IsPoorSettlement = _isPoor,
            InTavern = _inTavern,
            InKeep = _inKeep,
            InPort = _inPort
        };

        // Nobres e companheiros podem puxar um dialogo de dois, se houver com quem.
        if (actorType == ActorType.NOBLE || actorType == ActorType.COMPANION)
        {
            string opening = TryStartDialogue(agent, actorType);
            if (!string.IsNullOrEmpty(opening))
            {
                return opening;
            }
        }

        string raw = _session.PickOneLiner(actorType, context);
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        string text = FormatText(raw);
        MarkSpeaking(agent, text);
        return text;
    }

    private string TryStartDialogue(Agent agent, ActorType actorType)
    {
        MBList<Agent> neighbours = new MBList<Agent>();
        _mission.GetNearbyAgents(agent.Position.AsVec2, DialogueNeighbourRadius, neighbours);

        Agent partner = null;
        foreach (Agent candidate in neighbours)
        {
            if (candidate == agent || candidate == _mission.MainAgent || !IsUsable(candidate) || IsBusy(candidate))
            {
                continue;
            }
            if (candidate.Age <= 15f)
            {
                continue;
            }
            partner = candidate;
            break;
        }

        if (partner == null)
        {
            return string.Empty;
        }

        DialogueDef def = _session.PickDialogue(actorType);
        if (def == null || def.Lines.Length == 0)
        {
            return string.Empty;
        }

        List<Agent> listeners = new List<Agent>();
        foreach (Agent candidate in neighbours)
        {
            if (candidate != agent && candidate != partner && candidate != _mission.MainAgent)
            {
                listeners.Add(candidate);
            }
        }

        DialogueLine first = def.Lines[0];
        string text = FormatText(first.ToLocalizedRaw());

        _dialogues.Add(new RunningDialogue
        {
            Def = def,
            Index = 1,
            First = agent,
            Second = partner,
            CurrentSpeaker = agent,
            NextLineAt = _clock + ReadingTime(text),
            Listeners = listeners
        });

        _lastSpokeAt[agent] = _clock;
        _lastSpokeAt[partner] = _clock;
        return text;
    }

    // ---------------------------------------------------------------------- batalha

    /// <summary>Tenta fazer o soldado falar. Devolve vazio se nao rolar.</summary>
    public string TryBattleLine(Agent agent)
    {
        if (_battle == null || !IsUsable(agent))
        {
            return string.Empty;
        }

        if (agent.IsHero || agent.IsRunningAway || agent.IsCheering || agent == _mission.MainAgent)
        {
            return string.Empty;
        }

        if (IsBusy(agent) || HasSpeakerNearby(agent))
        {
            return string.Empty;
        }

        bool inCombat = IsInCombat(agent);
        if (inCombat && !_settings.BattleAllowDuringCombat)
        {
            return string.Empty;
        }

        bool playerSide = IsPlayerRelated(agent);
        GetWeather(out bool rainy, out bool windy);

        SpeechContext context = new SpeechContext
        {
            Season = CampaignTime.Now.GetSeasonOfYear,
            IsBattle = true,
            Overpowered = playerSide ? _battle.PlayerSideOverpowered : !_battle.PlayerSideOverpowered,
            Underpowered = playerSide ? _battle.PlayerSideUnderpowered : !_battle.PlayerSideUnderpowered,
            SiegeAttacker = _battle.IsSiege && agent.Team != null && agent.Team.IsAttacker,
            SiegeDefender = _battle.IsSiege && agent.Team != null && agent.Team.IsDefender,
            AgainstLooters = _battle.AgainstLooters,
            AgainstVillagers = _battle.AgainstVillagers,
            DuringCombat = inCombat,
            AtSea = _battle.AtSea,
            Rainy = rainy,
            Windy = windy
        };

        ActorType actorType = _battle.AtSea ? ClassifySailor(agent) : ClassifySoldier(agent);

        string raw = _session.PickOneLiner(actorType, context);
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        string enemyFaction = agent.Team != null && agent.Team.IsAttacker
            ? _battle.DefenderFactionName
            : _battle.AttackerFactionName;

        string text = FormatText(raw, enemyFaction);
        MarkSpeaking(agent, text);
        return text;
    }

    /// <summary>Falas de inimigo podem ser desligadas pela config (o mod original ignorava a opcao).</summary>
    public bool MayAgentSpeak(Agent agent)
    {
        if (!IsBattle || _settings.BattleEnemyChatEnabled)
        {
            return true;
        }
        return _mission.MainAgent == null || !agent.IsEnemyOf(_mission.MainAgent);
    }

    // ------------------------------------------------------------------ classificacao

    private ActorType ClassifyTownAgent(Agent agent, CharacterObject character, string actionName)
    {
        if (IsPlayerCompanion(agent))
        {
            return ActorType.COMPANION;
        }

        Occupation occupation = character != null ? character.Occupation : Occupation.NotAssigned;

        if (occupation == Occupation.Lord)
        {
            return ActorType.NOBLE;
        }
        if (actionName.Contains("beggar"))
        {
            return ActorType.BEGGAR;
        }

        switch (occupation)
        {
            case Occupation.GangLeader:
                return ActorType.GANG_LEADER;
            case Occupation.PrisonGuard:
            case Occupation.Guard:
            case Occupation.Soldier:
            case Occupation.CaravanGuard:
                return ActorType.GUARD;
            case Occupation.Gangster:
            case Occupation.Bandit:
                return ActorType.THUG;
            case Occupation.Blacksmith:
                return ActorType.BLACKSMITH;
            case Occupation.Armorer:
                return ActorType.ARMORER;
            case Occupation.Weaponsmith:
                return ActorType.WEAPONSMITH;
            case Occupation.GoodsTrader:
            case Occupation.Merchant:
            case Occupation.HorseTrader:
                return ActorType.TRADER;
            case Occupation.Tavernkeeper:
                return ActorType.TAVERN_OWNER;
            case Occupation.Musician:
                return ActorType.MUSICIAN;
            case Occupation.ShipWright:
                return ActorType.SHIPWRIGHT;
            case Occupation.Special:
                return ActorType.BARBER;
        }

        if (character != null && Settlement.CurrentSettlement?.Culture != null &&
            character == Settlement.CurrentSettlement.Culture.GangleaderBodyguard)
        {
            return ActorType.THUG;
        }

        bool isKid = character != null && character.Age < 10f;
        bool isTeen = character != null && character.Age < 18f;

        if (_isVillage)
        {
            return isKid ? ActorType.KID_VILLAGE : isTeen ? ActorType.TEEN_VILLAGE : ActorType.ADULT_VILLAGE;
        }
        if (_isTown)
        {
            return isKid ? ActorType.KID_TOWN : isTeen ? ActorType.TEEN_TOWN : ActorType.ADULT_TOWN;
        }
        return isKid ? ActorType.KID_TOWN : isTeen ? ActorType.TEEN_TOWN : ActorType.GENERIC;
    }

    private static ActorType ClassifySoldier(Agent agent)
    {
        if (agent.HasMount)
        {
            return ActorType.BATTLE_CAVALRY;
        }
        return HasRangedWeapon(agent) ? ActorType.BATTLE_RANGED : ActorType.BATTLE_INFANTRY;
    }

    /// <summary>
    /// No navio: quem esta preso a um objeto usavel (remo, balista, leme) e o remador —
    /// SEA_ROW. O resto e tripulacao de pe. O mod original tinha os dois trocados.
    /// </summary>
    private static ActorType ClassifySailor(Agent agent)
        => agent.IsUsingGameObject ? ActorType.SEA_ROW : ActorType.SEA_STANDING;

    private static bool HasRangedWeapon(Agent agent)
    {
        MissionEquipment equipment = agent.Equipment;
        for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
        {
            MissionWeapon weapon = equipment[i];
            if (weapon.IsEmpty || weapon.Item == null)
            {
                continue;
            }
            if (weapon.Item.ItemType == ItemObject.ItemTypeEnum.Bow ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsPlayerCompanion(Agent agent)
    {
        try
        {
            if (!agent.IsHero || Clan.PlayerClan == null)
            {
                return false;
            }
            CharacterObject character = agent.Character as CharacterObject;
            if (character == null)
            {
                return false;
            }
            foreach (Hero companion in Clan.PlayerClan.Companions)
            {
                if (companion.CharacterObject == character)
                {
                    return true;
                }
            }
        }
        catch
        {
            // heroi sem clan / campanha ausente: simplesmente nao e companheiro
        }
        return false;
    }

    // ------------------------------------------------------------------------ estado

    private bool IsUsable(Agent agent)
        => agent != null && agent.IsHuman && agent.IsActive() && agent.Health > 0f;

    private bool IsBusy(Agent agent)
    {
        if (_speakingUntil.ContainsKey(agent) || _lastSpokeAt.ContainsKey(agent))
        {
            return true;
        }
        foreach (RunningDialogue run in _dialogues)
        {
            if (run.First == agent || run.Second == agent || (run.Listeners != null && run.Listeners.Contains(agent)))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasSpeakerNearby(Agent agent)
    {
        Vec3 position = agent.Position;
        foreach (Agent speaker in _speakingUntil.Keys)
        {
            if (speaker.Position.Distance(position) < MinDistanceBetweenSpeakers)
            {
                return true;
            }
        }
        return false;
    }

    private void MarkSpeaking(Agent agent, string text)
    {
        _speakingUntil[agent] = _clock + ReadingTime(text);
        _lastSpokeAt[agent] = _clock;
    }

    private float ReadingTime(string text)
    {
        float seconds = text.Length * (float)_settings.WaitTimeMultiplier;
        return seconds < 2f ? 2f : seconds;
    }

    private bool IsInCombat(Agent agent)
    {
        try
        {
            return _mission.GetNearbyEnemyAgentCount(agent.Team, agent.Position.AsVec2, 15f) > 0;
        }
        catch
        {
            return false;
        }
    }

    private bool IsPlayerRelated(Agent agent)
        => agent.Team != null && (agent.Team == _mission.PlayerTeam || agent.Team == _mission.PlayerAllyTeam);

    private void SetupStaticContext()
    {
        _staticContextReady = true;

        Settlement settlement = Settlement.CurrentSettlement ?? Hero.MainHero?.CurrentSettlement;
        if (settlement != null)
        {
            _isTown = settlement.IsTown;
            _isVillage = settlement.IsVillage;
            _isRich = IsRich(settlement);
            _isPoor = !_isRich;
            _inTavern = IsAtLocation(settlement, "tavern");
            _inKeep = IsAtLocation(settlement, "lords_hall");
            _inPort = IsAtLocation(settlement, "port");
        }

        if (_inTavern)
        {
            _cooldownPeriod = _settings.TavernCooldown;
        }

        UpdatePlayerGear();
    }

    private static bool IsRich(Settlement settlement)
    {
        if (settlement.SettlementComponent != null)
        {
            return (int)settlement.SettlementComponent.GetProsperityLevel() > 0;
        }
        return settlement.Town != null && settlement.Town.Prosperity > 5000f;
    }

    private static bool IsAtLocation(Settlement settlement, string locationId)
    {
        try
        {
            if (settlement == null || !settlement.IsTown || CampaignMission.Current == null || LocationComplex.Current == null)
            {
                return false;
            }
            return CampaignMission.Current.Location == LocationComplex.Current.GetLocationWithId(locationId);
        }
        catch
        {
            return false;
        }
    }

    private void UpdatePlayerGear()
    {
        Agent main = _mission.MainAgent;
        if (main == null)
        {
            _staticContextReady = false; // tenta de novo quando o jogador existir
            return;
        }

        MissionEquipment equipment = main.Equipment;
        for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
        {
            MissionWeapon weapon = equipment[i];
            if (weapon.IsEmpty || weapon.Item == null)
            {
                continue;
            }

            ItemObject.ItemTypeEnum type = weapon.Item.ItemType;
            if (type == ItemObject.ItemTypeEnum.Bow || type == ItemObject.ItemTypeEnum.Crossbow ||
                type == ItemObject.ItemTypeEnum.Arrows || type == ItemObject.ItemTypeEnum.Bolts)
            {
                _playerHasBow = true;
            }
            else if (type == ItemObject.ItemTypeEnum.OneHandedWeapon || type == ItemObject.ItemTypeEnum.TwoHandedWeapon)
            {
                _playerHasSword = true;
                if (SafeAveragePrice(weapon.Item) > 3000)
                {
                    _playerHasExpensiveSword = true;
                }
            }
        }

        _playerHasHorse = main.MountAgent != null;

        EquipmentElement body = main.SpawnEquipment[EquipmentIndex.Body];
        if (!body.IsEmpty && body.Item != null && SafeAveragePrice(body.Item) > 1000)
        {
            _playerHasExpensiveArmor = true;
        }
    }

    private static int SafeAveragePrice(ItemObject item)
    {
        try
        {
            return QuestHelper.GetAveragePriceOfItemInTheWorld(item);
        }
        catch
        {
            return 0;
        }
    }

    private void GetWeather(out bool rainy, out bool windy)
    {
        rainy = false;
        windy = false;

        try
        {
            MapWeatherModel.WeatherEvent weather =
                Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(Campaign.Current.MainParty.GetPosition2D);
            rainy = weather == MapWeatherModel.WeatherEvent.LightRain ||
                    weather == MapWeatherModel.WeatherEvent.HeavyRain ||
                    weather == MapWeatherModel.WeatherEvent.Storm;
            windy = weather == MapWeatherModel.WeatherEvent.Storm ||
                    weather == MapWeatherModel.WeatherEvent.Blizzard;
        }
        catch
        {
            // sem campanha (custom battle) — segue sem clima
        }

        try
        {
            if (!windy && _mission.Scene != null && _mission.Scene.GetGlobalWindVelocity().Length > 0.5f)
            {
                windy = true;
            }
        }
        catch
        {
        }
    }

    // ------------------------------------------------------------------------- texto

    private string FormatText(string raw, string enemyFaction = null)
    {
        raw = ApplyProfanityFilter(raw);

        TextObject text = new TextObject(raw, null);
        Settlement settlement = Settlement.CurrentSettlement ?? Hero.MainHero?.CurrentSettlement;

        if (Hero.MainHero != null)
        {
            text.SetTextVariable("PLAYER_NAME", Hero.MainHero.Name);
        }
        if (enemyFaction != null)
        {
            text.SetTextVariable("ENEMY_FACTION", enemyFaction);
        }

        try
        {
            if (settlement != null)
            {
                string name = settlement.Name?.ToString() ?? string.Empty;
                text.SetTextVariable("SETTLEMENT", name);
                text.SetTextVariable("SETTLEMENT_NAME", name);
                text.SetTextVariable("SIEGE_SETTLEMENT_NAME", name);
                text.SetTextVariable("SETTLEMENT_OWNER", settlement.Owner?.Name?.ToString() ?? name);
                text.SetTextVariable("CRIMINAL_NAME", GetCriminalName(settlement));
                text.SetTextVariable("CULTURAL_NAME", RandomCultureName(settlement.Culture));
                text.SetTextVariable("RANDOM_FACTION", RandomOtherFactionName(settlement.MapFaction));
            }
            else
            {
                text.SetTextVariable("RANDOM_FACTION", RandomOtherFactionName(Hero.MainHero?.MapFaction));
                text.SetTextVariable("CULTURAL_NAME", RandomCultureName(Hero.MainHero?.Culture));
            }
        }
        catch
        {
            // variavel que nao pode ser resolvida simplesmente fica como esta
        }

        return text.ToString();
    }

    private static string GetCriminalName(Settlement settlement)
    {
        if (settlement.Town != null)
        {
            foreach (Hero notable in settlement.Notables)
            {
                if (notable.IsGangLeader)
                {
                    return notable.Name.ToString();
                }
            }
        }
        return RandomCultureName(settlement.Culture);
    }

    private static string RandomCultureName(CultureObject culture)
    {
        if (culture == null || culture.MaleNameList == null || culture.MaleNameList.Count == 0)
        {
            return string.Empty;
        }
        return culture.MaleNameList.GetRandomElement().ToString();
    }

    private static string RandomOtherFactionName(IFaction avoid)
    {
        try
        {
            List<IFaction> factions = new List<IFaction>();
            foreach (IFaction faction in Campaign.Current.Factions)
            {
                if (avoid == null || faction.MapFaction != avoid.MapFaction)
                {
                    factions.Add(faction);
                }
            }
            if (factions.Count > 0)
            {
                return factions[MBRandom.RandomInt(factions.Count)].Name.ToString();
            }
        }
        catch
        {
        }
        return string.Empty;
    }

    /// <summary>
    /// Mascara o que estiver entre &lt;profanity&gt;. No mod original a condicao estava
    /// invertida: ligar o filtro mostrava o palavrao inteiro.
    /// </summary>
    private string ApplyProfanityFilter(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        try
        {
            bool mask = _settings.ProfanityFilterEnabled;
            string replaced = Regex.Replace(input, "<profanity>(.*?)</profanity>", match =>
            {
                string word = match.Groups[1].Value;
                if (!mask || word.Length == 0)
                {
                    return word;
                }
                return word[0] + new string('*', word.Length - 1);
            });
            return Regex.Replace(replaced, "</?profanity>", string.Empty);
        }
        catch
        {
            return input;
        }
    }
}
