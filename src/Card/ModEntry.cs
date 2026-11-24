using System;
using System.Reflection;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Tinker.Card;

public static class ModEntry
{
	public const string ModId = "cardupgrader";

	public const string Version = "0.9.2";

	public const string LogPrefix = "[Tinker Card] ";

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
			RegisterNetwork();
			RegisterConfig();
			BalanceFile.Reload();
			Log.Info("[Tinker Card] active, version 0.9.2.");
		}
		catch (Exception ex)
		{
			Log.Error("[Tinker Card] failed to initialize: " + ex);
		}
	}


	private static void RegisterNetwork()
	{
		if (Environment.GetEnvironmentVariable("CARDUPGRADER_SELFTEST_NOREG") == "1")
		{
			Warn("Network registration skipped (control test).");
			return;
		}
		try
		{
			if (GameCompat.RegisterNetworkProperties(typeof(TinkerersDial)))
			{
				GameCompat.NetIdForPropertyName("CardUpgraderState");
				Info("Card state registered for co-op.");
			}
		}
		catch (Exception ex)
		{
			Error("Could not register card state for co-op: " + ex.GetBaseException().Message);
		}
	}

	private static void RegisterConfig()
	{
		try
		{
			// Combined build: the settings menu is Tinker.Config, registered by the combined entry.
			Settings.LocalChanged();
			Info($"Ajustes cargados (preajuste {Settings.Local.Preset}).");
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
