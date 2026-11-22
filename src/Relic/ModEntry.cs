using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Tinker.Relic;

public static class ModEntry
{
	public const string Version = "0.5.1";

	public static void Initialize()
	{
		Store.Initialize();
		Text.Install();
		Lang.OnLanguageSet(manager => Text.Install(manager));
		Settings.Initialize();
		// Combined build: shared Harmony instance, patching only this half's namespace.
		Patching.PatchNamespace("Tinker.Relic", "[Tinker Relic] ");
		GD.Print("[Tinker Relic] Loaded ", "0.5.1", "; ", Catalog.Supported.Count, " relic definitions.");
	}
}
