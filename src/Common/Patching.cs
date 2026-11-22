using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace Tinker.Common;

/// <summary>
/// The one Harmony instance for the whole mod, and the tolerant "patch every [HarmonyPatch] class in
/// these namespaces" loop both halves used: a class that no longer fits the game is skipped and
/// logged instead of stopping the rest.
/// </summary>
public static class Patching
{
    public static readonly Harmony Harmony = new Harmony("tinker");

    /// <summary>
    /// Applies every [HarmonyPatch] class whose namespace is <paramref name="ns"/> or below it, except
    /// those under any of the <paramref name="skip"/> namespaces (patched by their own call).
    /// </summary>
    /// <returns>The number of classes that failed to patch.</returns>
    public static int PatchNamespace(string ns, string logPrefix, params string[] skip)
    {
        int failed = 0;
        foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(Patching).Assembly))
        {
            string typeNs = type.Namespace;
            bool inScope = Within(typeNs, ns) && !Array.Exists(skip, s => Within(typeNs, s));
            if (!inScope || type.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Length == 0)
            {
                continue;
            }
            try
            {
                Harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                failed++;
                Log.Warn(logPrefix + "Could not apply " + type.Name + ": " + ex.GetBaseException().Message);
            }
        }
        if (failed > 0)
        {
            Log.Warn($"{logPrefix}{failed} patch class(es) skipped on this game version.");
        }
        return failed;
    }

    private static bool Within(string typeNs, string ns) =>
        typeNs != null && (typeNs == ns || typeNs.StartsWith(ns + ".", StringComparison.Ordinal));
}
