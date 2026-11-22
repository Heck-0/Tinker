using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace Tinker.Common;

/// <summary>
/// The game's two language events, hooked once for the whole mod. Each half registers its translation
/// tables with <see cref="Lang.OnLanguageSet"/> / <see cref="Lang.OnTablesLoaded"/> instead of patching
/// the game itself (they used to have one patch each, and the settings menu a third).
/// </summary>
internal static class LanguageHooks
{
    [HarmonyPatch(typeof(LocManager), "SetLanguageInternal")]
    internal static class LanguageSetPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(LocManager __instance)
        {
            Lang.LanguageSet(__instance);
        }
    }

    [HarmonyPatch(typeof(LocManager), "LoadTablesFromPath")]
    internal static class TablesLoadedPatch
    {
        private static void Postfix(object __result, string language)
        {
            Lang.TablesLoaded(__result?.GetType().GetField("Item1")?.GetValue(__result) as Dictionary<string, LocTable>, language);
        }
    }
}
