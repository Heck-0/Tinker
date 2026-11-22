using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace Tinker.Relic;

public static class Settings
{
	private static ModSettings? _run;

	private static ModSettings? _pending;

	private static ModSettings? _preview;

	private static INetGameService? _lobbyNet;

	public static ModSettings Local { get; set; } = new ModSettings();

	public static ModSettings Active => _preview ?? _run ?? Local;

	public static void Initialize()
	{
		// Combined build: the settings menu is Tinker.Config, registered by the combined entry.
	}

	public static void BindLobby(INetGameService net)
	{
		UnbindLobby();
		_pending = null;
		_lobbyNet = net;
		net.RegisterMessageHandler<CustomMessageWrapper>(HandleLobbyMessage);
	}

	private static void HandleLobbyMessage(CustomMessageWrapper wrapper, ulong sender)
	{
		if (wrapper.Message is Tinker.HostSettingsMessage hostSettingsMessage && hostSettingsMessage.RelicJson != null)
		{
			hostSettingsMessage.HandleMessage(sender);
		}
	}

	private static void UnbindLobby()
	{
		_lobbyNet?.UnregisterMessageHandler<CustomMessageWrapper>(HandleLobbyMessage);
		_lobbyNet = null;
	}

	public static void SendBeforeRun(INetGameService net)
	{
		if (net.Type == NetGameType.Host)
		{
			_pending = Local.Copy();
			CustomMessageWrapper.Send(new Tinker.HostSettingsMessage
			{
				RelicJson = _pending.ToJson()
			}, net);
		}
	}

	public static void Receive(string json, ulong sender)
	{
		if ((_lobbyNet ?? RunManager.Instance.NetService) is INetClientGameService netClientGameService)
		{
			NetClient? netClient = netClientGameService.NetClient;
			if (netClient != null && netClient.HostNetId == sender)
			{
				_pending = ModSettings.FromJson(json);
				GD.Print("[Tinker Relic] Received host settings before run initialization.");
			}
		}
	}

	public static void Begin(RunManager manager, INetGameService net)
	{
		IReadOnlyList<Player> players = manager.DebugOnlyGetState().Players;
		string[] array = (from s in players.Select(Store.SavedRules)
			where !string.IsNullOrEmpty(s)
			select s).Distinct().ToArray();
		if (array.Length > 1)
		{
			throw new InvalidOperationException("Mismatched saved relic settings");
		}
		object obj;
		if (array.Length != 1)
		{
			obj = _pending?.Copy();
			if (obj == null)
			{
				if (net.Type == NetGameType.Client)
				{
					throw new InvalidOperationException("Relic host settings were not received");
				}
				obj = Local.Copy();
			}
		}
		else
		{
			obj = ModSettings.FromJson(array[0]);
		}
		_run = (ModSettings?)obj;
		string json = _run.ToJson();
		foreach (Player item in players)
		{
			Store.SaveRules(item, json);
		}
		_pending = null;
		UnbindLobby();
		Text.Install();
	}

	public static void End()
	{
		_run = null;
		_pending = null;
		UnbindLobby();
		Text.Install();
	}
}
