using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Tinker.Card;

/// <summary>Every run starts with Tinkerer's Dial (the Workshop).</summary>
internal static class DialGrant
{
    public static bool Has(Player player) => player.Relics.Any((RelicModel r) => r is TinkerersDial);

    [HarmonyPatch(typeof(Player), "PopulateStartingRelics")]
    internal static class StartingRelicPatch
    {
        private static void Postfix(Player __instance)
        {
            try
            {
                if (!Has(__instance))
                {
                    RelicModel dial = ModelDb.Relic<TinkerersDial>().ToMutable();
                    dial.FloorAddedToDeck = 1;
                    __instance.AddRelicInternal(dial, -1, silent: true);
                }
            }
            catch (Exception ex)
            {
                ModEntry.Error("Could not give the Tinkerer's Dial at the start: " + ex);
            }
        }
    }

    /// <summary>
    /// The Dial needs a pool: the game looks a relic's pool up for its tooltip and energy icon. The event pool is
    /// only used for that, never for random relics, so the Dial can't drop.
    /// </summary>
    [HarmonyPatch(typeof(EventRelicPool), "GenerateAllRelics")]
    internal static class DialPoolPatch
    {
        private static void Postfix(ref IEnumerable<RelicModel> __result)
        {
            __result = __result.Append(ModelDb.Relic<TinkerersDial>()).ToList();
        }
    }
}
