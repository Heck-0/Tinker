using System;
using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace Tinker;

/// <summary>
/// Single entry point for Tinker (formerly Upgrader Combined). Each half keeps its own initializer, save
/// keys and RNG ids ("cardupgrader" / "relicupgrader"), so runs made with the separate mods or with
/// Upgrader Combined carry over. A failure in one half does not stop the other from loading.
/// Since 1.2.0 the Harmony instance, settings menu and co-op settings messages are shared (see README).
/// </summary>
[ModInitializer("Initialize")]
public static class Entry
{
    public const string ModId = "tinker";

    public const string Version = "1.6.0";

    public static void Initialize()
    {
        Run("Card Upgrader", Tinker.Card.ModEntry.Initialize);
        Run("Relic Upgrader", Tinker.Relic.ModEntry.Initialize);
        Run("Shared patches", () => Patching.PatchNamespace("Tinker", "[Tinker] ", "Tinker.Card", "Tinker.Relic"));
        Run("Settings menu", delegate
        {
            BaseLib.Config.ModConfigRegistry.Register(ModId, new Config());
            Text.Install();
            Lang.OnLanguageSet(_ => Text.Install());
        });
        GD.Print("[Tinker] Loaded ", Version, " (Card Upgrader ", Tinker.Card.ModEntry.Version, " + Relic Upgrader ", Tinker.Relic.ModEntry.Version, ").");
    }

    private static void Run(string name, Action initialize)
    {
        try
        {
            initialize();
        }
        catch (Exception ex)
        {
            GD.PushError("[Tinker] " + name + " failed to initialize: " + ex);
        }
    }
}