using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;

namespace Tinker;

/// <summary>
/// The settings menu's labels. BaseLib looks them up in the "settings_ui" table under the menu's prefix
/// (TINKER-) and the slug of each setting's name; <see cref="Labels"/> lists them all in English, translated
/// for the game's language when installed.
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
            foreach (KeyValuePair<string, string> pair in Lang.T(Labels(), manager.Language))
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

    private static string Slug(string name) => StringHelper.Slugify(name);

    /// <summary>Every label in the settings menu (English; translated by language when installed).</summary>
    private static Dictionary<string, string> Labels()
    {
        return new Dictionary<string, string>
        {
            [Slug("SectionGeneral") + ".title"] = "General",
            [Slug("Preset") + ".title"] = "Preset",
            [Slug("Preset") + ".hover.title"] = "Preset",
            [Slug("Preset") + ".hover.desc"] = "Normal is the mod as designed. Easy and Hard change several values at once. Touch anything and it becomes Custom. In co-op, the host's settings are used.",
            [Slug("Preset") + ".Normal"] = "Normal",
            [Slug("Preset") + ".Easy"] = "Easy",
            [Slug("Preset") + ".Hard"] = "Hard",
            [Slug("Preset") + ".Custom"] = "Custom",
            [Slug("SectionPrices") + ".title"] = "Prices",
            [Slug("SectionWorkshop") + ".title"] = "Workshop",
            [Slug("WorkshopGoldRate") + ".title"] = "Gold per upgrade",
            [Slug("WorkshopGoldRate") + ".hover.title"] = "Gold per upgrade",
            [Slug("WorkshopGoldRate") + ".hover.desc"] = "What the smallest upgrade on a fresh card or relic costs. Others cost proportionally more.",
            [Slug("SectionEnchantments") + ".title"] = "Enchantments",
            [Slug("EnchantmentsEnabled") + ".title"] = "Enchantments in the Workshop",
            [Slug("EnchantmentsEnabled") + ".hover.title"] = "Enchantments in the Workshop",
            [Slug("EnchantmentsEnabled") + ".hover.desc"] = "The Workshop sells the game's enchantments for cards, each at its own price (balance.jsonc). Turn them off and it doesn't.",
            [Slug("Difficulty") + ".title"] = "Upgrade difficulty",
            [Slug("Difficulty") + ".hover.desc"] = "How much bigger upgrades, and upgrades on already-upgraded cards or relics, cost. Higher values raise Workshop prices.",
        };
    }

}
