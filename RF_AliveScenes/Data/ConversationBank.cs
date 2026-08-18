using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RF_AliveScenes.Data;

/// <summary>Uma fala solta.</summary>
public sealed class OneLiner
{
    public string TranslationKey;
    public string Message;
    public ActorType[] Actors;
    public SpeechFrequency Frequency;
    public SpeechCondition[] Conditions;

    public string ToLocalizedRaw() => "{=" + TranslationKey + "}" + Message;
}

/// <summary>Uma linha de um dialogo de dois figurantes.</summary>
public sealed class DialogueLine
{
    public string TranslationKey;
    public string Message;
    public int ActorId;

    public string ToLocalizedRaw() => "{=" + TranslationKey + "}" + Message;
}

/// <summary>Dialogo completo (varias linhas alternando entre dois figurantes).</summary>
public sealed class DialogueDef
{
    public string Id;
    public SpeechFrequency Frequency;
    public ActorType[] Actors;
    public DialogueLine[] Lines;
}

/// <summary>
/// Contexto da cena/agente no momento em que a fala e escolhida. Struct para nao alocar
/// a cada agente testado.
/// </summary>
public struct SpeechContext
{
    public CampaignTime.Seasons Season;

    // Cena pacifica
    public bool HasSword;
    public bool HasBow;
    public bool HasHorse;
    public bool HasExpensiveArmor;
    public bool HasExpensiveSword;
    public bool IsStanding;
    public bool IsFarming;
    public bool IsRichSettlement;
    public bool IsPoorSettlement;
    public bool InTavern;
    public bool InKeep;
    public bool InPort;

    // Batalha
    public bool IsBattle;
    public bool Overpowered;
    public bool Underpowered;
    public bool SiegeDefender;
    public bool SiegeAttacker;
    public bool AgainstLooters;
    public bool AgainstVillagers;
    public bool DuringCombat;
    public bool AtSea;
    public bool Rainy;
    public bool Windy;
}

/// <summary>
/// Banco de falas, carregado uma vez por sessao de jogo e nunca mutado depois
/// (o controle de "ja falei isso" vive na <see cref="SpeechSession"/> de cada missao).
///
/// Diferencas para o mod original:
///   - indexado por ActorType no load, em vez de embaralhar as 2.200+ falas a cada pedido;
///   - valores desconhecidos no XML caem em ANY/GENERIC com aviso, em vez de virarem
///     silenciosamente o primeiro item do enum.
/// </summary>
public sealed class ConversationBank
{
    private static ConversationBank _instance;

    private readonly Dictionary<ActorType, List<OneLiner>> _oneLinersByActor = new();
    private readonly Dictionary<ActorType, List<DialogueDef>> _dialoguesByActor = new();

    public int OneLinerCount { get; private set; }
    public int DialogueCount { get; private set; }
    public bool IsLoaded { get; private set; }

    public static ConversationBank Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ConversationBank();
                _instance.Load();
            }
            return _instance;
        }
    }

    public IReadOnlyList<OneLiner> OneLinersFor(ActorType actor)
        => _oneLinersByActor.TryGetValue(actor, out List<OneLiner> list) ? list : (IReadOnlyList<OneLiner>)Array.Empty<OneLiner>();

    public IReadOnlyList<DialogueDef> DialoguesFor(ActorType actor)
        => _dialoguesByActor.TryGetValue(actor, out List<DialogueDef> list) ? list : (IReadOnlyList<DialogueDef>)Array.Empty<DialogueDef>();

    private void Load()
    {
        string path = Path.Combine(
            ModuleHelper.GetModuleFullPath("RealmsForgotten"),
            "ModuleData", "RFAliveScenes", "ConversationData.xml");

        if (!File.Exists(path))
        {
            Debug.Print("[RF_AliveScenes] ConversationData.xml nao encontrado em " + path);
            return;
        }

        XmlDocument doc = new XmlDocument();
        try
        {
            doc.Load(path);
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Falha ao ler ConversationData.xml: " + e.Message);
            return;
        }

        foreach (XmlNode node in doc.GetElementsByTagName("OneLiner"))
        {
            OneLiner line = new OneLiner
            {
                TranslationKey = Attr(node, "TranslationKey"),
                Message = Attr(node, "Message"),
                Actors = ParseActors(Attr(node, "ActorType")),
                Frequency = ParseFrequency(Attr(node, "Frequency")),
                Conditions = ParseConditions(Attr(node, "Condition"))
            };

            if (string.IsNullOrEmpty(line.Message))
            {
                continue;
            }

            OneLinerCount++;
            Index(_oneLinersByActor, line.Actors, line);
        }

        int autoId = 0;
        foreach (XmlNode node in doc.GetElementsByTagName("Dialogue"))
        {
            string id = Attr(node, "id");
            if (string.IsNullOrEmpty(id))
            {
                id = "rf_dialogue_" + autoId++;
            }

            List<DialogueLine> lines = new List<DialogueLine>();
            XmlNodeList lineNodes = node.SelectNodes("Line");
            if (lineNodes != null)
            {
                foreach (XmlNode lineNode in lineNodes)
                {
                    lines.Add(new DialogueLine
                    {
                        TranslationKey = Attr(lineNode, "TranslationKey"),
                        Message = Attr(lineNode, "Message"),
                        ActorId = int.TryParse(Attr(lineNode, "ActorID"), out int actorId) ? actorId : 0
                    });
                }
            }

            if (lines.Count == 0)
            {
                continue;
            }

            DialogueDef dialogue = new DialogueDef
            {
                Id = id,
                Frequency = ParseFrequency(Attr(node, "Frequency")),
                Actors = ParseActors(Attr(node, "ActorType")),
                Lines = lines.ToArray()
            };

            DialogueCount++;
            Index(_dialoguesByActor, dialogue.Actors, dialogue);
        }

        IsLoaded = OneLinerCount > 0 || DialogueCount > 0;
        Debug.Print($"[RF_AliveScenes] Banco carregado: {OneLinerCount} falas, {DialogueCount} dialogos.");
    }

    /// <summary>
    /// Poe o item em todos os baldes de ator dele. ActorType.GENERIC entra em TODOS os
    /// baldes conhecidos, que e como o mod original tratava GENERIC no filtro.
    /// </summary>
    private static void Index<T>(Dictionary<ActorType, List<T>> map, ActorType[] actors, T item)
    {
        if (actors == null || actors.Length == 0)
        {
            AddTo(map, ActorType.GENERIC, item);
            return;
        }

        foreach (ActorType actor in actors)
        {
            if (actor == ActorType.GENERIC)
            {
                foreach (ActorType every in (ActorType[])Enum.GetValues(typeof(ActorType)))
                {
                    AddTo(map, every, item);
                }
                return;
            }
        }

        foreach (ActorType actor in actors)
        {
            AddTo(map, actor, item);
        }
    }

    private static void AddTo<T>(Dictionary<ActorType, List<T>> map, ActorType key, T item)
    {
        if (!map.TryGetValue(key, out List<T> list))
        {
            list = new List<T>();
            map[key] = list;
        }
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }

    private static string Attr(XmlNode node, string name) => node?.Attributes?[name]?.Value ?? string.Empty;

    private static SpeechFrequency ParseFrequency(string value)
        => Enum.TryParse(value, true, out SpeechFrequency parsed) ? parsed : SpeechFrequency.GENERIC;

    private static ActorType[] ParseActors(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<ActorType>();
        }

        string[] parts = value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        List<ActorType> result = new List<ActorType>(parts.Length);
        foreach (string part in parts)
        {
            if (Enum.TryParse(part.Trim(), true, out ActorType parsed))
            {
                result.Add(parsed);
            }
            else
            {
                Debug.Print("[RF_AliveScenes] ActorType desconhecido no XML: " + part);
            }
        }
        return result.ToArray();
    }

    private static SpeechCondition[] ParseConditions(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<SpeechCondition>();
        }

        string[] parts = value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        List<SpeechCondition> result = new List<SpeechCondition>(parts.Length);
        foreach (string part in parts)
        {
            if (Enum.TryParse(part.Trim(), true, out SpeechCondition parsed))
            {
                result.Add(parsed);
            }
            else
            {
                Debug.Print("[RF_AliveScenes] Condition desconhecida no XML: " + part);
            }
        }
        return result.ToArray();
    }
}
