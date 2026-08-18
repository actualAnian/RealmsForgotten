using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace RF_AliveScenes.Config;

/// <summary>
/// Opcoes do RF_AliveScenes no MCM. Mesmo padrao do RF_IsoCam e do RF_PartyVisuals
/// (AttributeGlobalSettings). Quem le esses valores em runtime e a
/// <see cref="AliveScenesSettings"/>, que aguenta o MCM estar ausente.
///
/// Os textos ficam em ingles, como o resto da UI do mod.
/// </summary>
public sealed class Settings : AttributeGlobalSettings<Settings>
{
    public override string Id => "RF_AliveScenes";
    public override string DisplayName => "RF Alive Scenes";
    public override string FolderName => "RF_AliveScenes";
    public override string FormatType => "json";

    private const string GroupGeneral = "{=rf_as_g_general}General";
    private const string GroupCasual = "{=rf_as_g_casual}Towns, Villages and Taverns";
    private const string GroupBattle = "{=rf_as_g_battle}Battle and Sea";
    private const string GroupCrowds = "{=rf_as_g_crowds}Crowds";

    // ------------------------------------------------------------------------ general

    private bool _enabled = true;

    [SettingPropertyBool("{=rf_as_enable}Enable ambient chatter", RequireRestart = false,
        HintText = "{=rf_as_enable_desc}Master switch. When off, nobody speaks and no extra crowd is spawned. Applies from the next scene.")]
    [SettingPropertyGroup(GroupGeneral)]
    public bool Enabled
    {
        get => _enabled;
        set { if (value != _enabled) { _enabled = value; OnPropertyChanged(); } }
    }

    private float _waitTimeMultiplier = 0.13f;

    [SettingPropertyFloatingInteger("{=rf_as_read}Reading time per character", 0.05f, 0.40f, "0.00", RequireRestart = false,
        HintText = "{=rf_as_read_desc}Seconds the speech bubble stays on screen per character of text, with a 2s minimum. Higher = lines linger longer. Default 0.13.")]
    [SettingPropertyGroup(GroupGeneral)]
    public float WaitTimeMultiplier
    {
        get => _waitTimeMultiplier;
        set { if (value != _waitTimeMultiplier) { _waitTimeMultiplier = value; OnPropertyChanged(); } }
    }

    private float _visibleDistance = 10f;

    [SettingPropertyFloatingInteger("{=rf_as_dist}Hearing distance", 3f, 30f, "0", RequireRestart = false,
        HintText = "{=rf_as_dist_desc}How far from you (in metres) a speech bubble is still shown. Default 10.")]
    [SettingPropertyGroup(GroupGeneral)]
    public float VisibleDistance
    {
        get => _visibleDistance;
        set { if (value != _visibleDistance) { _visibleDistance = value; OnPropertyChanged(); } }
    }

    private bool _profanityFilter;

    [SettingPropertyBool("{=rf_as_prof}Profanity filter", RequireRestart = false,
        HintText = "{=rf_as_prof_desc}When on, swearing is masked (F***). The original mod had this option inverted; here it does what it says.")]
    [SettingPropertyGroup(GroupGeneral)]
    public bool ProfanityFilterEnabled
    {
        get => _profanityFilter;
        set { if (value != _profanityFilter) { _profanityFilter = value; OnPropertyChanged(); } }
    }

    // ------------------------------------------------- towns, villages and taverns

    private bool _casualChat = true;

    [SettingPropertyBool("{=rf_as_casual}Chatter in towns and villages", RequireRestart = false,
        HintText = "{=rf_as_casual_desc}Townsfolk speaking up in towns, villages, taverns, the lord's hall and the port.")]
    [SettingPropertyGroup(GroupCasual)]
    public bool CasualChatEnabled
    {
        get => _casualChat;
        set { if (value != _casualChat) { _casualChat = value; OnPropertyChanged(); } }
    }

    private int _casualCooldown = 40;

    [SettingPropertyInteger("{=rf_as_casual_cd}Cooldown per person (s)", 5, 180, "0", RequireRestart = false,
        HintText = "{=rf_as_casual_cd_desc}Seconds before the SAME person may speak again. Default 40.")]
    [SettingPropertyGroup(GroupCasual)]
    public int CasualCooldown
    {
        get => _casualCooldown;
        set { if (value != _casualCooldown) { _casualCooldown = value; OnPropertyChanged(); } }
    }

    private int _casualMax = 5;

    [SettingPropertyInteger("{=rf_as_casual_max}Simultaneous lines", 1, 12, "0", RequireRestart = false,
        HintText = "{=rf_as_casual_max_desc}How many speech bubbles may exist at once in a peaceful scene. Default 5.")]
    [SettingPropertyGroup(GroupCasual)]
    public int CasualMaxConversations
    {
        get => _casualMax;
        set { if (value != _casualMax) { _casualMax = value; OnPropertyChanged(); } }
    }

    private int _casualChance = 55;

    [SettingPropertyInteger("{=rf_as_casual_chance}Chance per person (%)", 0, 100, "0", RequireRestart = false,
        HintText = "{=rf_as_casual_chance_desc}Chance that the person picked actually opens their mouth. Default 55.")]
    [SettingPropertyGroup(GroupCasual)]
    public int CasualChancePerPerson
    {
        get => _casualChance;
        set { if (value != _casualChance) { _casualChance = value; OnPropertyChanged(); } }
    }

    private bool _tavernChat = true;

    [SettingPropertyBool("{=rf_as_tavern}Chatter in taverns", RequireRestart = false,
        HintText = "{=rf_as_tavern_desc}Turn off for a silent tavern; the rest of the town keeps talking.")]
    [SettingPropertyGroup(GroupCasual)]
    public bool TavernChatEnabled
    {
        get => _tavernChat;
        set { if (value != _tavernChat) { _tavernChat = value; OnPropertyChanged(); } }
    }

    private int _tavernCooldown = 15;

    [SettingPropertyInteger("{=rf_as_tavern_cd}Tavern cooldown (s)", 5, 120, "0", RequireRestart = false,
        HintText = "{=rf_as_tavern_cd_desc}A tavern is chattier than the street, so it gets a shorter cooldown per person. Default 15.")]
    [SettingPropertyGroup(GroupCasual)]
    public int TavernCooldown
    {
        get => _tavernCooldown;
        set { if (value != _tavernCooldown) { _tavernCooldown = value; OnPropertyChanged(); } }
    }

    // ------------------------------------------------------------- battle and sea

    private bool _battleChat = true;

    [SettingPropertyBool("{=rf_as_battle}Chatter in battle", RequireRestart = false,
        HintText = "{=rf_as_battle_desc}Soldiers speaking up in field battles, sieges and naval battles.")]
    [SettingPropertyGroup(GroupBattle)]
    public bool BattleChatEnabled
    {
        get => _battleChat;
        set { if (value != _battleChat) { _battleChat = value; OnPropertyChanged(); } }
    }

    private int _battleCooldown = 7;

    [SettingPropertyInteger("{=rf_as_battle_cd}Cooldown per soldier (s)", 3, 120, "0", RequireRestart = false,
        HintText = "{=rf_as_battle_cd_desc}Seconds before the SAME soldier may speak again. Default 7.")]
    [SettingPropertyGroup(GroupBattle)]
    public int BattleCooldown
    {
        get => _battleCooldown;
        set { if (value != _battleCooldown) { _battleCooldown = value; OnPropertyChanged(); } }
    }

    private int _battleMax = 3;

    [SettingPropertyInteger("{=rf_as_battle_max}Simultaneous lines", 1, 10, "0", RequireRestart = false,
        HintText = "{=rf_as_battle_max_desc}How many speech bubbles may exist at once in battle. Default 3.")]
    [SettingPropertyGroup(GroupBattle)]
    public int BattleMaxConversations
    {
        get => _battleMax;
        set { if (value != _battleMax) { _battleMax = value; OnPropertyChanged(); } }
    }

    private int _battleChance = 55;

    [SettingPropertyInteger("{=rf_as_battle_chance}Chance per soldier (%)", 0, 100, "0", RequireRestart = false,
        HintText = "{=rf_as_battle_chance_desc}Chance that the soldier picked actually speaks. Default 55.")]
    [SettingPropertyGroup(GroupBattle)]
    public int BattleChancePerPerson
    {
        get => _battleChance;
        set { if (value != _battleChance) { _battleChance = value; OnPropertyChanged(); } }
    }

    private bool _allowDuringCombat = true;

    [SettingPropertyBool("{=rf_as_combat}Speak while fighting", RequireRestart = false,
        HintText = "{=rf_as_combat_desc}Allows lines with an enemy within 15m. When off, soldiers only talk before the clash and during lulls.")]
    [SettingPropertyGroup(GroupBattle)]
    public bool BattleAllowDuringCombat
    {
        get => _allowDuringCombat;
        set { if (value != _allowDuringCombat) { _allowDuringCombat = value; OnPropertyChanged(); } }
    }

    private bool _enemyChat = true;

    [SettingPropertyBool("{=rf_as_enemy}Enemy lines", RequireRestart = false,
        HintText = "{=rf_as_enemy_desc}Also show what the enemy side shouts, in red.")]
    [SettingPropertyGroup(GroupBattle)]
    public bool BattleEnemyChatEnabled
    {
        get => _enemyChat;
        set { if (value != _enemyChat) { _enemyChat = value; OnPropertyChanged(); } }
    }

    // ----------------------------------------------------------------- crowds

    private bool _crowds = true;

    [SettingPropertyBool("{=rf_as_crowd}Extra townsfolk", RequireRestart = false,
        HintText = "{=rf_as_crowd_desc}Clones residents to fill out town scenes. Applies the next time you enter a town.")]
    [SettingPropertyGroup(GroupCrowds)]
    public bool CrowdsEnabled
    {
        get => _crowds;
        set { if (value != _crowds) { _crowds = value; OnPropertyChanged(); } }
    }

    private int _crowdMin = 1;

    [SettingPropertyInteger("{=rf_as_crowd_min}Clones per spot (minimum)", 0, 5, "0", RequireRestart = false,
        HintText = "{=rf_as_crowd_min_desc}Minimum copies made of each resident found. Default 1.")]
    [SettingPropertyGroup(GroupCrowds)]
    public int CrowdMultiplicationMin
    {
        get => _crowdMin;
        set { if (value != _crowdMin) { _crowdMin = value; OnPropertyChanged(); } }
    }

    private int _crowdMax = 2;

    [SettingPropertyInteger("{=rf_as_crowd_max}Clones per spot (maximum)", 0, 5, "0", RequireRestart = false,
        HintText = "{=rf_as_crowd_max_desc}Maximum copies made of each resident. The original mod used 3-5 and dragged the framerate down in large scenes. Default 2.")]
    [SettingPropertyGroup(GroupCrowds)]
    public int CrowdMultiplicationMax
    {
        get => _crowdMax;
        set { if (value != _crowdMax) { _crowdMax = value; OnPropertyChanged(); } }
    }

    private int _crowdCap = 40;

    [SettingPropertyInteger("{=rf_as_crowd_cap}Hard cap per scene", 0, 200, "0", RequireRestart = false,
        HintText = "{=rf_as_crowd_cap_desc}Hard limit of extra agents in a single scene, whatever the per-spot numbers say. RF scenes are big, so raise this carefully. Default 40.")]
    [SettingPropertyGroup(GroupCrowds)]
    public int CrowdMaxExtraAgents
    {
        get => _crowdCap;
        set { if (value != _crowdCap) { _crowdCap = value; OnPropertyChanged(); } }
    }
}
