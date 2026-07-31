using System;
using System.Collections.Generic;

namespace SOTOR.RFIntegration;

/// <summary>
/// Um foco arcano do RealmsForgotten: o cajado/varinha que serve de "bateria"
/// magica. Carregado de ModuleData/rf_arcane_foci.xml pelo
/// <see cref="ArcaneFocusRegistry" />.
///
/// A identidade do RF e que magia depende do instrumento: o foco define quanto
/// Winds voce acumula, com que velocidade recarrega, o quanto os feiticos rendem
/// e QUAIS escolas voce consegue canalizar. A skill entra como multiplicador por
/// cima disso (ver SotorSpellcraftHelper).
/// </summary>
public sealed class ArcaneFocusData
{
	/// <summary>Teto de tier de feitico para uma varinha (isWand="true").</summary>
	public const int WandMaxSpellTier = 2;

	public string ItemId { get; }

	/// <summary>Tier do proprio foco (1-5). Descritivo/para balanceamento futuro;
	/// nao restringe nada por si so.</summary>
	public int Tier { get; }

	/// <summary>Base do pool de Winds. E o valor que substitui a base fixa do
	/// SOTOR — sem foco a base e zero.</summary>
	public float MaxWinds { get; }

	public float RechargeMult { get; }

	public float EffectivenessMult { get; }

	/// <summary>Varinha: instrumento menor, so canaliza feiticos ate o tier
	/// <see cref="WandMaxSpellTier" />.</summary>
	public bool IsWand { get; }

	/// <summary>Escolas que este foco canaliza (BelongsToLoreID do
	/// tor_abilitytemplates). Lista VAZIA = sem restricao de escola — e o default
	/// deliberado, para que um foco novo mal-preenchido no XML nao trave tudo.</summary>
	public IReadOnlyList<string> Lores { get; }

	public ArcaneFocusData(string itemId, int tier, float maxWinds, float rechargeMult, float effectivenessMult, bool isWand, IReadOnlyList<string> lores)
	{
		ItemId = itemId;
		Tier = tier;
		MaxWinds = maxWinds;
		RechargeMult = rechargeMult;
		EffectivenessMult = effectivenessMult;
		IsWand = isWand;
		Lores = lores ?? new List<string>();
	}

	/// <summary>True se este foco canaliza a escola dada. Lista vazia = tudo
	/// liberado. Comparacao ordinal ignore-case porque os ids vem de XML escrito a mao.</summary>
	public bool AllowsLore(string loreId)
	{
		if (Lores == null || Lores.Count == 0)
		{
			return true;
		}

		if (string.IsNullOrEmpty(loreId))
		{
			return true;
		}

		for (int i = 0; i < Lores.Count; i++)
		{
			if (string.Equals(Lores[i], loreId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>Varinha corta em tier 2; cajado nao limita por tier.</summary>
	public bool AllowsSpellTier(int spellTier)
	{
		if (!IsWand)
		{
			return true;
		}

		return spellTier <= WandMaxSpellTier;
	}

	public override string ToString()
	{
		return $"{ItemId} (tier {Tier}, {(IsWand ? "wand" : "staff")}, maxWinds {MaxWinds}, lores {(Lores.Count == 0 ? "ALL" : string.Join("/", Lores))})";
	}
}
