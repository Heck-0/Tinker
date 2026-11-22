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
            Copy(Tinker.Relic.ConfigText.Values(Tinker.Relic.Text.Spanish), "RELICUPGRADER-", labels);
            foreach (KeyValuePair<string, string> pair in Own(Lang.IsSpanish(manager.Language)))
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
    private static Dictionary<string, string> Own(bool es)
    {
        string T(string spanish, string english) => es ? spanish : english;
        return new Dictionary<string, string>
        {
            [Slug("SectionOdds") + ".title"] = T("Precios", "Prices"),
            [Slug("SectionWorkshop") + ".title"] = T("Taller", "Workshop"),
            [Slug("WorkshopGoldRate") + ".title"] = T("Oro por mejora", "Gold per upgrade"),
            [Slug("WorkshopGoldRate") + ".hover.title"] = T("Oro por mejora", "Gold per upgrade"),
            [Slug("WorkshopGoldRate") + ".hover.desc"] = T("Lo que cuesta la mejora más pequeña de una carta o reliquia sin mejorar. Las demás cuestan en proporción.", "What the smallest upgrade on a fresh card or relic costs. Others cost proportionally more."),
            [Slug("SectionTraits") + ".title"] = T("Cartas: propiedades", "Cards: traits"),
            [Slug("SectionBoons") + ".title"] = T("Reliquias: bufos", "Relics: boons"),
            [Slug("Difficulty") + ".title"] = T("Dificultad de mejora", "Upgrade difficulty"),
        };
    }

}
