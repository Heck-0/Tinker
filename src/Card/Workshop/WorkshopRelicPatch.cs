using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace Tinker.Card;

/// <summary>
/// Clicking Tinkerer's Dial in the relic bar opens the Workshop instead of the relic inspect screen.
/// In combat it opens the inspect screen as usual, which explains why.
/// </summary>
[HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
internal static class WorkshopRelicPatch
{
    private static bool Prefix(RelicModel model)
    {
        try
        {
            if (!(model is TinkerersDial))
            {
                return true;
            }
            Player player = model.Owner;
            string blocked = WorkshopFlow.BlockedReason(player);
            if (blocked == "")
            {
                // Already open, or another Tinker screen is: ignore the click.
                return false;
            }
            if (blocked != null)
            {
                return true;
            }
            TaskHelper.RunSafely(WorkshopFlow.OpenAsync(player));
            return false;
        }
        catch (Exception ex)
        {
            ModEntry.Error("No se pudo abrir el taller: " + ex);
            return true;
        }
    }
}
