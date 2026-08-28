using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SOTOR.AbilitySystem;

public class SotorSkills
{
	public const string SpellcraftId = "SotorSpellcraft";

	private static SotorSkills _instance;

	private SkillObject _spellcraft;

	// [RF-B] gancho: a magia progride pela skill 'arcane' do RF; SotorSpellcraft
	// fica como fallback quando o RealmsForgotten nao esta presente.
	public static SkillObject Spellcraft => SOTOR.RFIntegration.RFArcaneSkill.Resolve(_instance?._spellcraft);

	public SotorSkills()
	{
		_instance = this;
		// [RF-B] SkillObject e por jogo: solta o cache da resolucao entre partidas.
		SOTOR.RFIntegration.RFArcaneSkill.Reset();
		string resolvedId;
		CharacterAttribute characterAttribute = ResolveGoverningAttribute(out resolvedId);
		_spellcraft = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("SotorSpellcraft"));
		_spellcraft.Initialize(new TextObject("Spellcraft"), new TextObject("Your mastery of Mana. Higher Spellcraft increases spell damage and unlocks higher spell tiers."), new CharacterAttribute[1] { characterAttribute });
		SotorLog.Info("SotorSkills: registered 'SotorSpellcraft' (attr=" + resolvedId + ").");
	}

	private static CharacterAttribute ResolveGoverningAttribute(out string resolvedId)
	{
		string spellcraftAttributeId = SotorSettings.SpellcraftAttributeId;
		if (!string.IsNullOrWhiteSpace(spellcraftAttributeId))
		{
			CharacterAttribute characterAttribute = MBObjectManager.Instance?.GetObject<CharacterAttribute>(spellcraftAttributeId);
			if (characterAttribute != null)
			{
				resolvedId = spellcraftAttributeId;
				return characterAttribute;
			}
			SotorLog.Warn("SotorSkills: Spellcraft attribute '" + spellcraftAttributeId + "' not found; falling back to Intelligence.");
		}
		resolvedId = "intelligence";
		return DefaultCharacterAttributes.Intelligence;
	}
}
