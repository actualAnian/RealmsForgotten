using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SOTOR.RFIntegration;

/// <summary>
/// FASE 8 — carrega os feiticos do RealmsForgotten de arquivos <c>rf_*.xml</c> na
/// mesma pasta dos XMLs do TOR.
///
/// Por que arquivos separados e nao editar os do TOR: manter
/// <c>tor_abilitytemplates.xml</c> intocado significa que uma atualizacao do SOTOR
/// pode ser reaplicada sem perder o conteudo do RF, e o diff continua legivel.
///
/// Os arquivos sao lidos DEPOIS do arquivo base e na ordem alfabetica, e um
/// StringID repetido SOBRESCREVE de proposito — e assim que o RF ajusta um feitico
/// do TOR sem tocar no arquivo dele.
///
/// Tolerante a falha por arquivo: um XML quebrado registra aviso e os outros
/// continuam. Um feitico malformado nunca pode impedir o mod de carregar.
/// </summary>
public static class RFSpellOverlay
{
	/// <summary>Prefixo dos arquivos de conteudo RF.</summary>
	public const string FilePrefix = "rf_";

	/// <summary>
	/// Le todos os <c>rf_*.xml</c> da pasta, desserializando cada um como
	/// <c>List&lt;T&gt;</c> sob a raiz <paramref name="rootElement" />, e entrega a
	/// lista ao chamador (que decide como mesclar).
	/// </summary>
	/// <returns>Quantos itens foram entregues no total.</returns>
	public static int LoadRfXmls<T>(string baseFilePath, string rootElement, Action<List<T>> apply)
	{
		int total = 0;
		try
		{
			string folder = Path.GetDirectoryName(baseFilePath);
			if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
			{
				return 0;
			}

			// So os arquivos que ESPELHAM o nome do arquivo base: rf_abilitytemplates
			// para tor_abilitytemplates, e assim por diante. Antes cada loader tentava
			// os TRES rf_*.xml, e os dois que nao eram dele falhavam a desserializacao
			// e enchiam o log de avisos que pareciam defeito.
			string suffix = Path.GetFileName(baseFilePath);
			int underscore = suffix.IndexOf('_');
			if (underscore >= 0)
			{
				suffix = suffix.Substring(underscore + 1);
			}

			string[] files = Directory.GetFiles(folder, FilePrefix + suffix);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);

			XmlSerializer serializer = new XmlSerializer(typeof(List<T>), new XmlRootAttribute(rootElement));
			foreach (string file in files)
			{
				try
				{
					using (FileStream stream = File.OpenRead(file))
					{
						if (serializer.Deserialize(stream) is List<T> list && list.Count > 0)
						{
							apply(list);
							total += list.Count;
							SotorLog.Info($"RFSpellOverlay: {list.Count} entrada(s) de '{Path.GetFileName(file)}'.");
						}
					}
				}
				catch (Exception ex)
				{
					SotorLog.Warn("RFSpellOverlay: '" + Path.GetFileName(file) + "' ignorado (" + ex.GetType().Name + ": " + ex.Message + ").");
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("RFSpellOverlay: varredura falhou (" + ex.GetType().Name + ").");
		}
		return total;
	}
}
