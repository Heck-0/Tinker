using System;
using System.Reflection;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Tinker.Card;

public static class ModEntry
{
	public const string Version = "0.9.2";

	public static void Initialize()
	{
		try
		{
			Loc.Install();
			Lang.OnTablesLoaded(Loc.MergeIntoTables);
			Patching.PatchNamespace("Tinker.Card", "[Tinker Card] ");
			FixedEffectPatches.Install(Patching.Harmony);
			PowerParameters.Install(Patching.Harmony);
			ExpandedEffectPatches.Install(Patching.Harmony);
			AdvancedEnchantmentUpgrades.Install(Patching.Harmony);
			RegisterConfig();
			BalanceFile.Reload();
			Log.Info("[Tinker Card] active, version 0.9.2.");
		}
		catch (Exception ex)
		{
			Log.Error("[Tinker Card] failed to initialize: " + ex);
		}
	}


	private static void RegisterConfig()
	{
		try
		{
			// Combined build: the settings menu is Tinker.Config, registered by the combined entry.
			Settings.LocalChanged();
			Info($"Settings loaded (preset {Settings.Local.Preset}).");
		}
		catch (Exception ex)
		{
			Error("Could not create the settings menu: " + ex.GetBaseException().Message);
		}
	}

	public static void Info(string text)
	{
		Log.Info("[Tinker Card] " + text);
	}

	public static void Warn(string text)
	{
		Log.Warn("[Tinker Card] " + text);
	}

	public static void Error(string text)
	{
		Log.Error("[Tinker Card] " + text);
	}
}
