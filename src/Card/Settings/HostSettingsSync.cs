using System;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace Tinker.Card;

internal static class HostSettingsSync
{
	// The network messages are Tinker.HostSettingsMessage (shared with the relic side) and
	// Tinker.RequestHostSettingsMessage. They call back into these two methods.

	/// <summary>Client side: the host's card settings arrived.</summary>
	internal static void Receive(string json)
	{
		if (!IsHost)
		{
			CardSettings modSettings = CardSettings.FromJson(json);
			if (modSettings == null)
			{
				ModEntry.Error("Unreadable host settings; keeping the previous ones.");
				return;
			}
			Settings.HostOverride = modSettings;
			ModEntry.Info($"Using the host's settings (preset {modSettings.Preset}).");
		}
	}

	/// <summary>Host side: a client asked for the card settings.</summary>
	internal static void OnRequest()
	{
		if (IsHost)
		{
			SendHostSettings();
		}
	}

	[HarmonyPatch(typeof(RunManager), "InitializeShared")]
	internal static class StartPatch
	{
		[HarmonyPriority(200)]
		private static void Postfix(RunManager __instance)
		{
			switch (__instance.NetService?.Type ?? NetGameType.None)
			{
			case NetGameType.Host:
				Settings.HostOverride = null;
				SendHostSettings();
				break;
			case NetGameType.Client:
				try
				{
					CustomMessageWrapper.Send(new Tinker.RequestHostSettingsMessage());
					break;
				}
				catch (Exception ex)
				{
					ModEntry.Error("Could not request the host's settings: " + ex.Message);
					break;
				}
			default:
				Settings.HostOverride = null;
				break;
			}
		}
	}

	[HarmonyPatch(typeof(RunManager), "CleanUp")]
	internal static class EndPatch
	{
		private static void Postfix()
		{
			if (Settings.HostOverride != null)
			{
				ModEntry.Info("Co-op run over: back to your own settings.");
			}
			Settings.HostOverride = null;
		}
	}

	private static bool IsHost
	{
		get
		{
			RunManager instance = RunManager.Instance;
			if (instance == null)
			{
				return false;
			}
			return instance.NetService?.Type == NetGameType.Host;
		}
	}

	private static void SendHostSettings()
	{
		try
		{
			CustomMessageWrapper.Send(new Tinker.HostSettingsMessage
			{
				CardJson = Settings.Local.ToJson()
			});
			ModEntry.Info("Host settings sent.");
		}
		catch (Exception ex)
		{
			ModEntry.Error("Could not send the host settings: " + ex.Message);
		}
	}
}
