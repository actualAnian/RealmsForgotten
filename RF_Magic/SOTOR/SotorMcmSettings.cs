using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;

namespace SOTOR;

public sealed class SotorMcmSettings : AttributeGlobalSettings<SotorMcmSettings>
{
	private const string GroupFeatures = "{=sotor_mcm_grp_features}Features";

	private const string GroupTweaks = "{=sotor_mcm_grp_tweaks}Magic Tweaks";

	private const string GrpEff = "{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_eff}Spell Effectiveness";

	private const string GrpWindsKill = "{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_winds_kill}Mana on Magic Kill";

	private const string GrpArmor = "{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_armor}Armor Effect on Mana Recharge";

	private const string GroupShipMagic = "{=sotor_mcm_grp_ship}Ship Magic";

	private static readonly string[] SpellcraftAttrIds = new string[6] { "vigor", "control", "endurance", "cunning", "social", "intelligence" };

	public override string Id => "SOTOR_Settings_v1";

	// Nome visivel no MCM: o modulo vigente e o RF_Magic (Id/FolderName ficam como estao
	// de proposito — mudar aquilo apagaria as configuracoes ja salvas do jogador).
	public override string DisplayName => "RF Magic";

	public override string FolderName => "SOTOR";

	public override string FormatType => "json2";

	[SettingPropertyDropdown("{=sotor_mcm_hud_mode}Battle spell HUD", Order = 1, RequireRestart = false, HintText = "{=sotor_mcm_hud_mode_hint}When to show the bottom left spell and Mana panel in battle.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_features}Features", GroupOrder = 0)]
	public Dropdown<string> HudMode { get; set; } = new Dropdown<string>(new string[3] { "{=sotor_mcm_hud_always}Always", "{=sotor_mcm_hud_casting}Only while casting", "{=sotor_mcm_hud_hidden}Hidden" }, 0);

	[SettingPropertyBool("{=sotor_mcm_spell_dmg_log}Spell Damage Log", Order = 2, RequireRestart = false, HintText = "{=sotor_mcm_spell_dmg_log_hint}Shows a battle log line for your spell damage, healing, and friendly fire, colored by element.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_features}Features", GroupOrder = 0)]
	public bool EnableSpellDamageLog { get; set; } = true;

	[SettingPropertyBool("{=sotor_mcm_amber_thrown}Improved Amber Spear", Order = 3, RequireRestart = false, HintText = "{=sotor_mcm_amber_thrown_hint}Swaps Amber Spear for a magic javelin that scales off Throwing and Spellcraft.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_features}Features", GroupOrder = 0)]
	public bool UseThrownAmberSpear { get; set; } = true;

	[SettingPropertyBool("{=sotor_mcm_skeleton_armies}Skeleton Armies", Order = 4, RequireRestart = false, HintText = "{=sotor_mcm_skeleton_armies_hint}Enables the resurrection of fallen enemies as skeletons after victorious battles.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_features}Features", GroupOrder = 0)]
	public bool EnableSkeletonArmies { get; set; } = true;

	[SettingPropertyBool("{=sotor_mcm_mindcontrol_armies}Mind Controlled Armies", Order = 5, RequireRestart = false, HintText = "{=sotor_mcm_mindcontrol_armies_hint}Surviving units you mind controlled during a battle join your party afterward. Heroes never join. The in battle Mind Control spell works regardless of this setting.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_features}Features", GroupOrder = 0)]
	public bool EnableMindControlledArmies { get; set; } = true;

	[SettingPropertyBool("{=sotor_mcm_companion_casters}Companion Spellcasters", Order = 6, RequireRestart = false, HintText = "{=sotor_mcm_companion_casters_hint}Lets your clan companions and family use magic and appear in the spellbook's hero cycle. Turning it off later keeps their learned lores, spells and Mana intact for when you turn it back on. The main hero is always a caster.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_features}Features", GroupOrder = 0)]
	public bool EnableCompanionSpellcasters { get; set; }

	[SettingPropertyDropdown("{=sotor_mcm_spellcraft_attr}Spellcraft Attribute", Order = 0, RequireRestart = true, HintText = "{=sotor_mcm_spellcraft_attr_hint}Which attribute governs the Spellcraft skill's learning rate. Takes effect after a restart. Default Intelligence.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_features}Features", GroupOrder = 0)]
	public Dropdown<string> SpellcraftAttribute { get; set; } = new Dropdown<string>(new string[6] { "{=sotor_attr_vigor}Vigor", "{=sotor_attr_control}Control", "{=sotor_attr_endurance}Endurance", "{=sotor_attr_cunning}Cunning", "{=sotor_attr_social}Social", "{=sotor_attr_intelligence}Intelligence" }, 5);

	[SettingPropertyBool("{=sotor_mcm_disable_siege_magic}Disable Magic in Sieges", Order = 0, RequireRestart = false, HintText = "{=sotor_mcm_disable_siege_magic_hint}Disables spellcasting during siege battles.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_tweaks}Magic Tweaks", GroupOrder = 2)]
	public bool DisableMagicInSieges { get; set; }

	[SettingPropertyBool("{=sotor_mcm_spell_eff}Spell Effectiveness", Order = 0, RequireRestart = false, HintText = "{=sotor_mcm_spell_eff_hint}Adds a flat bonus to your Spell Effectiveness (scales spell damage and healing).")]
	[SettingPropertyGroup("{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_eff}Spell Effectiveness")]
	public bool EnableSpellEffectivenessTweak { get; set; }

	[SettingPropertyFloatingInteger("{=sotor_mcm_spell_eff_pct}Spell effectiveness bonus", -100f, 200f, "0\\%", Order = 1, RequireRestart = false, HintText = "{=sotor_mcm_spell_eff_pct_hint}Percent added to Spell Effectiveness. -100% means spells deal no damage, 0% is unchanged, 200% adds triple.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_eff}Spell Effectiveness")]
	public float SpellEffectivenessBonusPercent { get; set; }

	[SettingPropertyBool("{=sotor_mcm_winds_on_kill}Mana on Magic Kill", Order = 0, RequireRestart = false, HintText = "{=sotor_mcm_winds_on_kill_hint}Gives Mana on a spell kill. Player only.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_winds_kill}Mana on Magic Kill")]
	public bool EnableWindsOnMagicKill { get; set; }

	[SettingPropertyFloatingInteger("{=sotor_mcm_winds_on_kill_amt}Mana per magic kill", -15f, 30f, "0.0", Order = 1, RequireRestart = false, HintText = "{=sotor_mcm_winds_on_kill_amt_hint}How much Mana each spell kill grants.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_winds_kill}Mana on Magic Kill")]
	public float WindsOnMagicKillAmount { get; set; }

	[SettingPropertyBool("{=sotor_mcm_armor_recharge}Armor Effect on Mana Recharge", Order = 0, RequireRestart = false, HintText = "{=sotor_mcm_armor_recharge_hint}Changes how much armor weight slows Mana recharge.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_armor}Armor Effect on Mana Recharge")]
	public bool EnableArmorWomRechargeTweak { get; set; }

	[SettingPropertyFloatingInteger("{=sotor_mcm_armor_recharge_pct}Armor effect on Mana recharge", -100f, 200f, "0\\%", Order = 1, RequireRestart = false, HintText = "{=sotor_mcm_armor_recharge_pct_hint}-100% removes the armor penalty. 0% keeps default. 200% triples the armor penalty.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_tweaks}Magic Tweaks/{=sotor_mcm_grp_armor}Armor Effect on Mana Recharge")]
	public float ArmorWomRechargeEffectPercent { get; set; }

	[SettingPropertyBool("{=sotor_mcm_ship_dmg}Spells Damage Ships", Order = 0, RequireRestart = false, HintText = "{=sotor_mcm_ship_dmg_hint}Requires the War Sails expansion. When on, your spells damage enemy ship hulls, set them ablaze, and shred their sails in naval battles. When off, magic only harms the crew.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_ship}Ship Magic", GroupOrder = 1)]
	public bool EnableSpellShipDamage { get; set; } = true;

	[SettingPropertyFloatingInteger("{=sotor_mcm_ship_dmg_pct}Ship damage", 0f, 300f, "0\\%", Order = 1, RequireRestart = false, HintText = "{=sotor_mcm_ship_dmg_pct_hint}Scales how much damage your spells deal to ships. 100% is the tuned default and 0% is the same as turning ship damage off.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_ship}Ship Magic", GroupOrder = 1)]
	public float SpellShipDamagePercent { get; set; } = 100f;

	[SettingPropertyBool("{=sotor_mcm_burning_deck}Burning Decks Hurt Crew", Order = 2, RequireRestart = false, HintText = "{=sotor_mcm_burning_deck_hint}Requires War Sails. When a ship is set ablaze, anyone standing on its deck takes fire damage over time until they get off. Vanilla leaves the crew unharmed on a burning ship.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_ship}Ship Magic", GroupOrder = 1)]
	public bool EnableBurningDeckDamage { get; set; } = true;

	[SettingPropertyFloatingInteger("{=sotor_mcm_burning_deck_dps}Burning deck damage", 0f, 20f, "0.0", Order = 3, RequireRestart = false, HintText = "{=sotor_mcm_burning_deck_dps_hint}Fire damage per second to anyone on an ablaze deck. Default 4.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_ship}Ship Magic", GroupOrder = 1)]
	public float BurningDeckDamagePerSecond { get; set; } = 4f;

	[SettingPropertyBool("{=sotor_mcm_abandon_ship}Crew Abandon Doomed Ships", Order = 4, RequireRestart = false, HintText = "{=sotor_mcm_abandon_ship_hint}Requires War Sails. Crew of a sinking or burning ship flee to the nearest safe ship by running across a bridge or swimming, preferring their own but boarding an enemy vessel if they must.")]
	[SettingPropertyGroup("{=sotor_mcm_grp_ship}Ship Magic", GroupOrder = 1)]
	public bool EnableAbandonShipAI { get; set; } = true;

	public void SyncToStore()
	{
		SotorSettings.UseThrownAmberSpear = UseThrownAmberSpear;
		SotorSettings.EnableSkeletonArmies = EnableSkeletonArmies;
		SotorSettings.EnableMindControlledArmies = EnableMindControlledArmies;
		SotorSettings.EnableCompanionSpellcasters = EnableCompanionSpellcasters;
		int num = ((SpellcraftAttribute != null) ? SpellcraftAttribute.SelectedIndex : 5);
		SotorSettings.SpellcraftAttributeId = ((num >= 0 && num < SpellcraftAttrIds.Length) ? SpellcraftAttrIds[num] : "intelligence");
		SotorSettings.HudMode = ((HudMode != null) ? HudMode.SelectedIndex : 0);
		SotorSettings.EnableSpellDamageLog = EnableSpellDamageLog;
		SotorSettings.EnableWindsOnMagicKill = EnableWindsOnMagicKill;
		SotorSettings.WindsOnMagicKillAmount = WindsOnMagicKillAmount;
		SotorSettings.EnableArmorWomRechargeTweak = EnableArmorWomRechargeTweak;
		SotorSettings.ArmorWomRechargeEffectPercent = ArmorWomRechargeEffectPercent;
		SotorSettings.EnableSpellEffectivenessTweak = EnableSpellEffectivenessTweak;
		SotorSettings.SpellEffectivenessBonusPercent = SpellEffectivenessBonusPercent;
		SotorSettings.DisableMagicInSieges = DisableMagicInSieges;
		SotorSettings.EnableSpellShipDamage = EnableSpellShipDamage;
		SotorSettings.SpellShipDamagePercent = SpellShipDamagePercent;
		SotorSettings.EnableBurningDeckDamage = EnableBurningDeckDamage;
		SotorSettings.BurningDeckDamagePerSecond = BurningDeckDamagePerSecond;
		SotorSettings.EnableAbandonShipAI = EnableAbandonShipAI;
	}
}
