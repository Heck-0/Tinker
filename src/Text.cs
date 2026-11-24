using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;

namespace Tinker;

/// <summary>
/// Labels for the combined settings menu. BaseLib looks them up in the "settings_ui" table under the
/// menu's prefix (TINKER-). Most are the halves' existing labels (RULETA-..., RELICUPGRADER-...)
/// copied across, so every language they already had keeps working; section titles that changed
/// meaning in the merged layout get their own text.
/// </summary>
internal static class Text
{
    private const string Prefix = "TINKER-";

    public static void Install()
    {
        LocManager manager = LocManager.Instance;
        if (manager == null)
        {
            return;
        }
        try
        {
            Dictionary<string, string> labels = new Dictionary<string, string>();
            // Card labels first, so shared settings and the shared items use Card Upgrader's wording.
            if (Tinker.Card.Loc.TablesFor(manager.Language).TryGetValue("settings_ui", out Dictionary<string, string> card))
            {
                Copy(card, "RULETA-", labels);
            }
            Copy(Lang.T(Tinker.Relic.ConfigText.Values(), manager.Language), "RELICUPGRADER-", labels);
            foreach (KeyValuePair<string, string> pair in Lang.T(Own(), manager.Language))
            {
                labels[Prefix + pair.Key] = pair.Value;
            }
            manager.GetTable("settings_ui").MergeWith(labels);
        }
        catch (Exception ex)
        {
            Godot.GD.PushWarning("[Tinker] Could not install settings labels: " + ex.Message);
        }
    }

    private static void Copy(Dictionary<string, string> source, string fromPrefix, Dictionary<string, string> into)
    {
        foreach (KeyValuePair<string, string> pair in source)
        {
            if (pair.Key.StartsWith(fromPrefix, StringComparison.Ordinal))
            {
                string key = Prefix + pair.Key.Substring(fromPrefix.Length);
                into.TryAdd(key, pair.Value);
            }
        }
    }

    private static string Slug(string name) => StringHelper.Slugify(name);

    /// <summary>Titles for sections and settings that are new in the merged layout.</summary>
    private static Dictionary<string, string> Own()
    {
        return new Dictionary<string, string>
        {
            [Slug("SectionPrices") + ".title"] = "Prices",
            [Slug("SectionWorkshop") + ".title"] = "Workshop",
            [Slug("WorkshopGoldRate") + ".title"] = "Gold per upgrade",
            [Slug("WorkshopGoldRate") + ".hover.title"] = "Gold per upgrade",
            [Slug("WorkshopGoldRate") + ".hover.desc"] = "What the smallest upgrade on a fresh card or relic costs. Others cost proportionally more.",
            [Slug("SectionTraits") + ".title"] = "Cards: traits",
            [Slug("SectionBoons") + ".title"] = "Relics: boons",
            [Slug("Difficulty") + ".title"] = "Upgrade difficulty",
        };
    }

}
