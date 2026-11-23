using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace Tinker.Relic;

// When the relic settings are sent, frozen into a run and released.

[HarmonyPatch]
public static class SettingsLobbyPatch
{
	public static IEnumerable<MethodBase> TargetMethods()
	{
		return typeof(StartRunLobby).GetConstructors();
	}

	public static void Postfix(StartRunLobby __instance)
	{
		Settings.BindLobby(__instance.NetService);
	}
}

[HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
public static class SettingsStartPatch
{
	public static void Prefix(StartRunLobby __instance)
	{
		Settings.SendBeforeRun(__instance.NetService);
	}
}

[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class SettingsRunPatch
{
	[HarmonyPriority(0)]
	public static void Postfix(RunManager __instance, INetGameService netService)
	{
		Settings.Begin(__instance, netService);
	}
}

[HarmonyPatch(typeof(RunManager), "CleanUp")]
public static class SettingsEndPatch
{
	public static void Postfix()
	{
		Settings.End();
	}
}
