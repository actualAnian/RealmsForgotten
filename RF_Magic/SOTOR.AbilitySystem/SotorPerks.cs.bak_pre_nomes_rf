using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace SOTOR.AbilitySystem;

public class SotorPerks
{
	private static SotorPerks _instance;

	private PerkObject _entrySpells;

	private PerkObject _adeptSpells;

	private PerkObject _masterSpells;

	private PerkObject _selfish;

	private PerkObject _wellControlled;

	private PerkObject _librarian;

	private PerkObject _storyTeller;

	private PerkObject _overCaster;

	private PerkObject _efficientSpellCaster;

	private PerkObject _improvision;

	private PerkObject _catalyst;

	private PerkObject _dampener;

	private PerkObject _arcaneLink;

	private PerkObject _trueTransmutation;

	public static SotorPerks Instance => _instance;

	public static PerkObject EntrySpells => _instance?._entrySpells;

	public static PerkObject AdeptSpells => _instance?._adeptSpells;

	public static PerkObject MasterSpells => _instance?._masterSpells;

	public static PerkObject Selfish => _instance?._selfish;

	public static PerkObject WellControlled => _instance?._wellControlled;

	public static PerkObject Librarian => _instance?._librarian;

	public static PerkObject StoryTeller => _instance?._storyTeller;

	public static PerkObject OverCaster => _instance?._overCaster;

	public static PerkObject EfficientSpellCaster => _instance?._efficientSpellCaster;

	public static PerkObject Improvision => _instance?._improvision;

	public static PerkObject Catalyst => _instance?._catalyst;

	public static PerkObject Dampener => _instance?._dampener;

	public static PerkObject ArcaneLink => _instance?._arcaneLink;

	public static PerkObject TrueTransmutation => _instance?._trueTransmutation;

	public static PerkObject Archmage => _instance?._trueTransmutation;

	public SotorPerks()
	{
		_instance = this;
		_entrySpells = Create("SotorEntrySpells");
		_adeptSpells = Create("SotorAdeptSpells");
		_masterSpells = Create("SotorMasterSpells");
		_selfish = Create("SotorSelfish");
		_wellControlled = Create("SotorWellControlled");
		_librarian = Create("SotorLibrarian");
		_storyTeller = Create("SotorStoryTeller");
		_overCaster = Create("SotorOverCaster");
		_efficientSpellCaster = Create("SotorEfficientSpellCaster");
		_improvision = Create("SotorImprovision");
		_catalyst = Create("SotorCatalyst");
		_dampener = Create("SotorDampener");
		_arcaneLink = Create("SotorArcaneLink");
		_trueTransmutation = Create("SotorTrueTransmutation");
		InitializeAll();
	}

	private static PerkObject Create(string id)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject(id));
	}

	private void InitializeAll()
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		_entrySpells.Initialize("Novice Spellcaster", spellcraft, 25, null, "Gain access to entry level spells.", PartyRole.Personal, 0f, EffectIncrementType.Invalid);
		_adeptSpells.Initialize("Adept Spellcaster", spellcraft, 125, null, "Gain access to adept level spells. Unlocks the ability to learn the Lore of Necromancy.", PartyRole.Personal, 0f, EffectIncrementType.Invalid);
		_masterSpells.Initialize("Master Spellcaster", spellcraft, 200, null, "Gain access to master level spells.", PartyRole.Personal, 0f, EffectIncrementType.Invalid);
		_selfish.Initialize("Selfish", spellcraft, 75, _wellControlled, "Your damaging spells do 90% reduced damage to yourself.", PartyRole.Personal, -0.9f, EffectIncrementType.AddFactor, "Your self targeted buff spells have 50% more duration.", PartyRole.Personal, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_wellControlled.Initialize("Well Controlled", spellcraft, 75, _selfish, "Your damaging spells do 30% less damage to troops in your party.", PartyRole.Personal, -0.3f, EffectIncrementType.AddFactor, "Gain 5% advantage in simulation battles.", PartyRole.PartyLeader, 0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_librarian.Initialize("Librarian", spellcraft, 275, _storyTeller, "You gain 25% more Spellcraft experience from casting spells.", PartyRole.Personal, 0.25f, EffectIncrementType.AddFactor, "Learning new spells and lores costs 50% less gold.", PartyRole.Personal, -0.5f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_storyTeller.Initialize("Storyteller", spellcraft, 275, _librarian, "Every companion in your party gains 1000 experience in a random skill per day.", PartyRole.PartyLeader, 1000f, EffectIncrementType.Add, "Your party gains a permanent +5 increase to party morale.", PartyRole.PartyLeader, 5f, EffectIncrementType.Add, TroopUsageFlags.None, TroopUsageFlags.None);
		_overCaster.Initialize("Overcaster", spellcraft, 175, _efficientSpellCaster, "Your instant damaging and healing spells are 20% more effective but cost 30% more winds of magic.", PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor, "", PartyRole.None, 0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_efficientSpellCaster.Initialize("Efficient Spellcaster", spellcraft, 175, _overCaster, "Your instant damaging and healing spells are 20% less effective, but cost 30% less winds of magic.", PartyRole.Personal, -0.2f, EffectIncrementType.AddFactor, "", PartyRole.None, -0.15f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_improvision.Initialize("Improvision", spellcraft, 225, _catalyst, "Your Winds of Magic is set to 25 if you have less than that at the beginning of the battle.", PartyRole.Personal, 25f, EffectIncrementType.Add, "+10% Persuasion chance during speech checks.", PartyRole.Personal, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_catalyst.Initialize("Catalyst", spellcraft, 225, _improvision, "For every legendary item in your equipment slots you gain +5 extra Winds of magic at the start of battle.", PartyRole.Personal, 5f, EffectIncrementType.Add, "You gain +20% Winds of Magic regeneration while waiting in a town.", PartyRole.Personal, 0.2f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_dampener.Initialize("Dampener", spellcraft, 250, _arcaneLink, "Damage dealt by your damaging spells is reduced by 15%, but troops in your formation take 30% less damage from spells.", PartyRole.Personal, -0.15f, EffectIncrementType.AddFactor, "You gain 5% ward save.", PartyRole.Personal, -0.05f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_arcaneLink.Initialize("Arcane Link", spellcraft, 250, _dampener, "Any buffs you cast on a friendly unit will now also apply to you even if you are not in range.", PartyRole.Personal, 1f, EffectIncrementType.Add, "As formation Captain, all troops in your formation deal additional 10% magic damage.", PartyRole.Captain, 0.1f, EffectIncrementType.AddFactor, TroopUsageFlags.None, TroopUsageFlags.None);
		_trueTransmutation.Initialize("Archmage", spellcraft, 300, null, "Become an Archmage. Unlocks the ability to learn the restricted High Magic and Dark Magic lores.", PartyRole.Personal, 0f, EffectIncrementType.Invalid);
		SotorLog.Info("SotorPerks: registered 14 Spellcraft perks.");
	}
}
