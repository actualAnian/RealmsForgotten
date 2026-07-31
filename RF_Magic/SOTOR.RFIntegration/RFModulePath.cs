using System;
using System.IO;
using TaleWorlds.ModuleManager;

namespace SOTOR.RFIntegration;

/// <summary>
/// Onde o módulo mora em disco.
///
/// O fork nasceu dentro da pasta <c>SOTOR</c> e passou a viver em
/// <c>RF_Magic</c> (o módulo do RealmsForgotten que substitui o original). Como
/// o código localiza os próprios XMLs pelo caminho do módulo, um nome errado
/// aqui faria tudo carregar VAZIO — sem erro, sem crash, apenas magia nenhuma.
/// Por isso o nome fica num lugar só, e a resolução tem plano B: se a pasta
/// preferida não existir, cai na antiga. Assim a migração pode ser feita (ou
/// desfeita) sem tocar em código.
/// </summary>
public static class RFModulePath
{
	/// <summary>Módulo atual.</summary>
	public const string ModuleName = "RF_Magic";

	/// <summary>Nome anterior, mantido como plano B.</summary>
	public const string LegacyModuleName = "SOTOR";

	private static string _resolvedRoot;

	/// <summary>Raiz do módulo em disco (resolvida uma vez).</summary>
	public static string Root
	{
		get
		{
			if (!string.IsNullOrEmpty(_resolvedRoot))
			{
				return _resolvedRoot;
			}

			foreach (string name in new[] { ModuleName, LegacyModuleName })
			{
				try
				{
					string candidate = ModuleHelper.GetModuleFullPath(name);
					if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate))
					{
						_resolvedRoot = candidate;
						SotorLog.Info("Module root resolved to '" + name + "'.");
						return _resolvedRoot;
					}
				}
				catch (Exception)
				{
					// modulo nao instalado com esse nome — tenta o proximo
				}
			}

			SotorLog.Error("Could not resolve module folder ('" + ModuleName + "' nor '" + LegacyModuleName + "') — data files will not load.");
			return string.Empty;
		}
	}

	/// <summary>Caminho dentro do módulo, ex.: <c>Combine("ModuleData", "x.xml")</c>.</summary>
	public static string Combine(params string[] parts)
	{
		string path = Root;
		if (string.IsNullOrEmpty(path))
		{
			return string.Empty;
		}
		foreach (string part in parts)
		{
			path = Path.Combine(path, part);
		}
		return path;
	}
}
