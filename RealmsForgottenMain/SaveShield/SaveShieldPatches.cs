using System;
using HarmonyLib;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.SaveShield
{
    /// <summary>
    /// Observadores nos pontos de entrada de save e load. Todo finalizer devolve a exceção
    /// intocada — isto NÃO engole nada (ver <see cref="SaveFailureReporter"/>).
    ///
    /// Levam a categoria RFSaveShield e são aplicados UMA vez, cedo, pelo
    /// <see cref="SaveShieldBootstrap"/> em OnSubModuleLoad — o primeiro Load da sessão
    /// acontece antes de OnGameInitializationFinished, então o sweep tardio não os cobriria.
    /// A categoria também os esconde do sweep de não-categorizados (lição do LOTRAOM:
    /// Harmony não deduplica; um patch aplicado por dois mecanismos roda duas vezes).
    ///
    /// Alvos verificados por reflexão nas DLLs 1.4.8 instaladas em 2026-08-18:
    ///   static CheckSaveableTypes() / static Save(Object, MetaData, String, ISaveDriver) /
    ///   static Load(String, ISaveDriver, Boolean) — a sobrecarga de 2 args delega para esta.
    /// </summary>
    // CheckSaveableTypes: onde "dois mods com o mesmo save ID" estoura, como um opaco
    // "same key has already been added".
    [HarmonyPatchCategory(SaveShieldBootstrap.Category)]
    [HarmonyPatch(typeof(SaveManager), "CheckSaveableTypes")]
    internal static class SaveManagerCheckSaveableTypesPatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                SaveFailureReporter.Instance.Report(__exception, "O registro de tipos de save");
            }
            return __exception;
        }
    }

    [HarmonyPatchCategory(SaveShieldBootstrap.Category)]
    [HarmonyPatch(typeof(SaveManager), "Save")]
    internal static class SaveManagerSavePatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                SaveFailureReporter.Instance.Report(__exception, "Salvar o jogo");
            }
            return __exception;
        }
    }

    [HarmonyPatchCategory(SaveShieldBootstrap.Category)]
    [HarmonyPatch(typeof(SaveManager), "Load", new[] { typeof(string), typeof(ISaveDriver), typeof(bool) })]
    internal static class SaveManagerLoadPatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                SaveFailureReporter.Instance.Report(__exception, "Carregar o save");
            }
            return __exception;
        }
    }

    // NÃO patchados, de propósito: SaveHandler.QuickSaveCurrentGame, SaveAs e SignalAutoSave.
    // Os três só registram a INTENÇÃO de salvar (SetSaveArgs) e retornam; o save de verdade
    // roda num frame posterior. Um finalizer neles nunca observaria a falha — seria custo puro
    // sem cobertura. Os finalizers no SaveManager embrulham o trabalho real.

    /// <summary>Aplica os patches da categoria uma única vez. Chamado do OnSubModuleLoad —
    /// SaveManager é serialização gerenciada (TaleWorlds.SaveSystem), não classe de engine
    /// nativa, então o alerta "não patchar cedo" não se aplica (mesmo precedente do
    /// PatchAthasScholarLoadRepair).</summary>
    public static class SaveShieldBootstrap
    {
        public const string Category = "RFSaveShield";

        private static bool _applied;

        public static void Apply(Harmony harmony)
        {
            if (_applied)
            {
                return;
            }
            _applied = true;

            try
            {
                harmony.PatchCategory(typeof(SaveShieldBootstrap).Assembly, Category);
            }
            catch (Exception e)
            {
                TaleWorlds.Library.Debug.Print("[RF_SaveShield] Falha ao aplicar patches: " + e.Message);
            }
        }
    }
}
