using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.BannerWorks
{
    /// <summary>
    /// Persistence and live application for the RF banner editor (pattern
    /// studied from Omen Editor, reimplemented for RF):
    ///
    ///   • Applying is trivial — <c>clan.Banner</c>/<c>kingdom.Banner</c> have
    ///     public setters; afterwards every party and settlement visual is
    ///     marked dirty so the map repaints immediately.
    ///   • Persistence is one XML in Documents\...\Configs\RF_BannerWorks\
    ///     (clanId→code, realmId→code), written atomically and re-applied on
    ///     every session launch. GLOBAL by design, like an authoring tool:
    ///     banners edited here apply to all campaigns, and nothing is stored
    ///     in saves.
    ///   • Editing a ruling clan also recolors its kingdom (and vice versa) —
    ///     that is how the game itself pairs them.
    /// </summary>
    internal static class RFBannerWorksStore
    {
        private static readonly object SyncRoot = new object();

        private static string XmlPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "RF_BannerWorks", "banner_overrides.xml");

        internal static bool ApplyAndPersistClan(Clan clan, string bannerCode, out string error)
        {
            error = string.Empty;
            if (clan == null || string.IsNullOrWhiteSpace(bannerCode))
            {
                error = "No clan or banner to apply.";
                return false;
            }

            lock (SyncRoot)
            {
                try
                {
                    // Round-trip through Banner both validates the code and
                    // normalizes it before it is stored anywhere.
                    string normalized = new Banner(bannerCode).BannerCode;
                    (Dictionary<string, string> clans, Dictionary<string, string> realms) = Read();
                    clans[clan.StringId] = normalized;

                    Kingdom kingdom = clan.Kingdom;
                    bool rules = kingdom != null && kingdom.RulingClan == clan;
                    if (rules)
                    {
                        realms[kingdom.StringId] = normalized;
                    }

                    Write(clans, realms);
                    clan.Banner = new Banner(normalized);
                    if (rules)
                    {
                        kingdom.Banner = new Banner(normalized);
                    }
                    MarkAllBannerVisualsDirty();
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }
        }

        internal static bool ApplyAndPersistKingdom(Kingdom kingdom, string bannerCode, out string error)
        {
            error = string.Empty;
            if (kingdom == null || string.IsNullOrWhiteSpace(bannerCode))
            {
                error = "No realm or banner to apply.";
                return false;
            }

            lock (SyncRoot)
            {
                try
                {
                    string normalized = new Banner(bannerCode).BannerCode;
                    (Dictionary<string, string> clans, Dictionary<string, string> realms) = Read();
                    realms[kingdom.StringId] = normalized;
                    Clan rulingClan = kingdom.RulingClan;
                    if (rulingClan != null)
                    {
                        clans[rulingClan.StringId] = normalized;
                    }

                    Write(clans, realms);
                    kingdom.Banner = new Banner(normalized);
                    if (rulingClan != null)
                    {
                        rulingClan.Banner = new Banner(normalized);
                    }
                    MarkAllBannerVisualsDirty();
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }
        }

        /// <summary>Re-applies every stored override — called on session launch
        /// so edited banners survive save/load and new campaigns alike.</summary>
        internal static void ApplyAll()
        {
            if (Campaign.Current == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                (Dictionary<string, string> clans, Dictionary<string, string> realms) = Read();
                if (clans.Count == 0 && realms.Count == 0)
                {
                    return;
                }

                bool any = false;
                foreach (KeyValuePair<string, string> pair in clans)
                {
                    Clan clan = Clan.All.FirstOrDefault(c => c != null && string.Equals(c.StringId, pair.Key, StringComparison.OrdinalIgnoreCase));
                    if (clan != null && NeedsReplacing(clan.Banner, pair.Value))
                    {
                        try
                        {
                            clan.Banner = new Banner(pair.Value);
                            any = true;
                        }
                        catch
                        {
                            // a bad stored code must never break session launch
                        }
                    }
                }

                foreach (KeyValuePair<string, string> pair in realms)
                {
                    Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k != null && string.Equals(k.StringId, pair.Key, StringComparison.OrdinalIgnoreCase));
                    if (kingdom != null && NeedsReplacing(kingdom.Banner, pair.Value))
                    {
                        try
                        {
                            kingdom.Banner = new Banner(pair.Value);
                            any = true;
                        }
                        catch
                        {
                        }
                    }
                }

                if (any)
                {
                    MarkAllBannerVisualsDirty();
                }
            }
        }

        /// <summary>
        /// Vale a pena trocar o objeto Banner? Só quando o código armazenado difere
        /// do atual.
        ///
        /// Antes o ApplyAll substituía o Banner de clãs e reinos VIVOS a cada carga
        /// de sessão, mesmo quando o código era idêntico — troca de objeto do mundo
        /// sem nenhum ganho, e cada troca invalida visuais dependentes. Comparar
        /// primeiro elimina esse churn: numa sessão sem mudança de bandeira, nada é
        /// substituído.
        /// </summary>
        private static bool NeedsReplacing(Banner current, string storedCode)
        {
            if (string.IsNullOrWhiteSpace(storedCode))
            {
                return false;
            }
            if (current == null)
            {
                return true;
            }
            try
            {
                return !string.Equals(current.BannerCode, storedCode, StringComparison.Ordinal);
            }
            catch
            {
                return true;
            }
        }

        private static void MarkAllBannerVisualsDirty()
        {
            // GUARDA: visuais de party/settlement e nameplates sao do MAPA. Com uma
            // missao ativa nao ha o que atualizar, e mexer em visual durante a cena de
            // batalha e a categoria de risco que queremos evitar.
            if (Campaign.Current == null || Mission.Current != null)
            {
                return;
            }
            foreach (MobileParty party in Campaign.Current.MobileParties)
            {
                party?.Party?.SetVisualAsDirty();
            }
            foreach (Settlement settlement in Campaign.Current.Settlements)
            {
                settlement?.Party?.SetVisualAsDirty();
            }

            // The 3D party icons rebuild from the dirty flag above, but map
            // NAMEPLATES cache their banner image and vanilla never marks it
            // dirty when a clan's banner object is replaced — without this the
            // new banner only showed up after a save/reload.
            RFBannerWorksRefresh.RefreshMapNameplates();
        }

        private static (Dictionary<string, string> clans, Dictionary<string, string> realms) Read()
        {
            var clans = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var realms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(XmlPath))
                {
                    var document = new XmlDocument { XmlResolver = null };
                    document.Load(XmlPath);
                    foreach (XmlNode node in document.SelectNodes("/BannerOverrides/Clan"))
                    {
                        string id = node.Attributes?["id"]?.Value;
                        string code = node.Attributes?["code"]?.Value;
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(code))
                        {
                            clans[id] = code;
                        }
                    }
                    foreach (XmlNode node in document.SelectNodes("/BannerOverrides/Realm"))
                    {
                        string id = node.Attributes?["id"]?.Value;
                        string code = node.Attributes?["code"]?.Value;
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(code))
                        {
                            realms[id] = code;
                        }
                    }
                }
            }
            catch
            {
                // unreadable file = start fresh; the next save rewrites it
            }
            return (clans, realms);
        }

        private static void Write(Dictionary<string, string> clans, Dictionary<string, string> realms)
        {
            string directory = Path.GetDirectoryName(XmlPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var document = new XmlDocument();
            document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root = document.CreateElement("BannerOverrides");
            document.AppendChild(root);
            foreach (KeyValuePair<string, string> pair in clans)
            {
                XmlElement element = document.CreateElement("Clan");
                element.SetAttribute("id", pair.Key);
                element.SetAttribute("code", pair.Value);
                root.AppendChild(element);
            }
            foreach (KeyValuePair<string, string> pair in realms)
            {
                XmlElement element = document.CreateElement("Realm");
                element.SetAttribute("id", pair.Key);
                element.SetAttribute("code", pair.Value);
                root.AppendChild(element);
            }

            string temp = XmlPath + ".tmp";
            document.Save(temp);
            if (File.Exists(XmlPath))
            {
                File.Replace(temp, XmlPath, null);
            }
            else
            {
                File.Move(temp, XmlPath);
            }
        }
    }
}
