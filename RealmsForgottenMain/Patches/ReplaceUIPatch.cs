using HarmonyLib;
using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(GauntletLayer), "LoadMovie", new Type[] { typeof(string), typeof(ViewModel) })]
    public static class ReplaceUIPatch
    {
        public static void Prefix(ref string movieName, ViewModel dataSource)
        {
            try
            {
                if (movieName == "CharacterDeveloper")
                    movieName = "RFCharacterDeveloper";

                // Overhaul visual do SOTOR (modulo RF_SotorUI, so arte).
                // Prefab NAO sobrepoe por nome duplicado (WidgetFactory guarda os
                // custom types num Dictionary.Add — primeiro vence), entao o
                // caminho correto e o mesmo desta classe: trocar o NOME do movie
                // no load. So redireciona se o prefab de destino existir — com o
                // RF_SotorUI desligado, o SOTOR abre a UI original dele.
                else if (movieName == "SotorSpellBook")
                    movieName = RemapIfPresent(movieName, "RFSotorSpellBook");
                else if (movieName == "AbilityHUD")
                    movieName = RemapIfPresent(movieName, "RFSotorAbilityHUD");
                else if (movieName == "AbilityRadialSelection")
                    movieName = RemapIfPresent(movieName, "RFSotorAbilityRadialSelection");
                else if (movieName == "ProjectileCrosshair")
                    movieName = RemapIfPresent(movieName, "RFSotorProjectileCrosshair");
            }
            catch
            {
            }
        }

        private static string RemapIfPresent(string original, string replacement)
        {
            try
            {
                var factory = TaleWorlds.Engine.GauntletUI.UIResourceManager.WidgetFactory;
                bool known = factory != null && factory.IsCustomType(replacement);
                global::RealmsForgotten.AiMade.RFLogger.Log(
                    $"[SotorUI] LoadMovie('{original}') -> factory={(factory != null ? "ok" : "NULL")} IsCustomType({replacement})={known}");
                if (known)
                {
                    return replacement;
                }
            }
            catch (Exception ex)
            {
                global::RealmsForgotten.AiMade.RFLogger.Log($"[SotorUI] RemapIfPresent('{original}') EXCEPTION: {ex}");
            }
            return original;
        }
    }
}
