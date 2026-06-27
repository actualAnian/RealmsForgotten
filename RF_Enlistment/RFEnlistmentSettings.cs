using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace RF_Enlistment;

public sealed class RFEnlistmentSettings : AttributeGlobalSettings<RFEnlistmentSettings>
{
    public override string Id => "RF_Enlistment";

    public override string DisplayName => "RF Enlistment";

    public override string FolderName => "RealmsForgotten";

    public override string FormatType => "json2";

    [SettingPropertyBool("Enable enlistment system", RequireRestart = false, HintText = "Turn the RF Enlistment gameplay system on or off.")]
    [SettingPropertyGroup("RF Enlistment/Development")]
    public bool EnableRecruitmentStub { get; set; } = true;

    [SettingPropertyBool("Messages", RequireRestart = false, HintText = "Enable RF Enlistment messages.")]
    [SettingPropertyGroup("RF Enlistment/Gameplay")]
    public bool MessagesEnabled { get; set; } = true;

    [SettingPropertyBool("Custom dialogue text", RequireRestart = false, HintText = "Load dialogue text from the module strings file.")]
    [SettingPropertyGroup("RF Enlistment/Gameplay")]
    public bool CustomText { get; set; } = true;

    [SettingPropertyBool("Pause when entering settlement", RequireRestart = false, HintText = "Pause time when entering a settlement while enlisted systems are active.")]
    [SettingPropertyGroup("RF Enlistment/Gameplay")]
    public bool PauseEnterSettlement { get; set; } = true;

    [SettingPropertyFloatingInteger("Duty event probability", 0f, 5f, "#0.00", RequireRestart = false, HintText = "Controls how often extra service duty events can appear while you are enlisted and traveling.")]
    [SettingPropertyGroup("RF Enlistment/Events")]
    public float BanditSpawnProbability { get; set; } = 0.04f;

    [SettingPropertyBool("Enable armorer", RequireRestart = false, HintText = "Allow service equipment to be issued by your commander.")]
    [SettingPropertyGroup("RF Enlistment/Features")]
    public bool EnableArmorer { get; set; } = true;

    [SettingPropertyBool("Allow enlisting as a lord", RequireRestart = false, HintText = "Allow the player to enlist even after becoming a lord.")]
    [SettingPropertyGroup("RF Enlistment/Test")]
    public bool AllowEnlistingAsALord { get; set; } = true;

    [SettingPropertyBool("Allow enlisting in minor factions", RequireRestart = false, HintText = "Allow service under minor faction lords.")]
    [SettingPropertyGroup("RF Enlistment/Test")]
    public bool AllowEnlistingInMinorFactions { get; set; } = true;

    [SettingPropertyFloatingInteger("Wage multiplier", 0f, 5f, "x0.00", RequireRestart = false, HintText = "Multiplier applied to daily service wages.")]
    [SettingPropertyGroup("RF Enlistment/Tweaks")]
    public float WageMultiplier { get; set; } = 1f;

    [SettingPropertyFloatingInteger("Level up XP multiplier", 0f, 5f, "x0.00", RequireRestart = false, HintText = "Multiplier for assignment-related skill gain.")]
    [SettingPropertyGroup("RF Enlistment/Tweaks")]
    public float LevelUpXPMultiplier { get; set; } = 1f;

    [SettingPropertyFloatingInteger("Daily XP multiplier", 0f, 5f, "x0.00", RequireRestart = false, HintText = "Multiplier for daily enlistment service XP.")]
    [SettingPropertyGroup("RF Enlistment/Tweaks")]
    public float DailyXPMultiplier { get; set; } = 1f;

    [SettingPropertyInteger("XP per kill", 0, 1000, RequireRestart = false, HintText = "Service XP gained per enemy you personally bring down in battle.")]
    [SettingPropertyGroup("RF Enlistment/Tweaks")]
    public int XPGainedPerKill { get; set; } = 25;

    [SettingPropertyInteger("Gold per kill", 0, 1000, RequireRestart = false, HintText = "Optional bonus gold gained per enemy you personally bring down in battle.")]
    [SettingPropertyGroup("RF Enlistment/Tweaks")]
    public int GoldLootedPerKill { get; set; } = 0;

    [SettingPropertyFloatingInteger("Daily leadership XP", 0f, 500f, "#0.0", RequireRestart = false, HintText = "Leadership XP earned each day while serving.")]
    [SettingPropertyGroup("RF Enlistment/Tweaks")]
    public float DailyLeadershipXp { get; set; } = 25f;

    [SettingPropertyFloatingInteger("Hourly training XP", 0f, 100f, "#0.0", RequireRestart = false, HintText = "Base skill XP used when drilling with the troops.")]
    [SettingPropertyGroup("RF Enlistment/Tweaks")]
    public float HourlyTrainingXp { get; set; } = 5f;
}
