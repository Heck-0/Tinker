using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BaseLib.Patches.Saves;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Random;

namespace Tinker.Relic;

public static class Store
{
	private sealed class PlayerState
	{
		public RouletteSave Save = new RouletteSave();
	}

	/// <summary>
	/// Per player, saved under relicupgrader_roulette_v2 : the relic rules frozen into the run, so every
	/// co-op player prices relic upgrades the same.
	/// </summary>
	public sealed class RouletteSave
	{
		public int Version { get; set; } = 2;

		public string? Rules { get; set; }
	}

	private static readonly ConditionalWeakTable<RelicModel, RelicState> Relics = new ConditionalWeakTable<RelicModel, RelicState>();

	private static readonly ConditionalWeakTable<Player, PlayerState> Players = new ConditionalWeakTable<Player, PlayerState>();

	public static RelicState For(RelicModel relic)
	{
		return Relics.GetValue(relic, (RelicModel _) => new RelicState());
	}

	public static string? SavedRules(Player p)
	{
		return Players.GetOrCreateValue(p).Save.Rules;
	}

	public static void SaveRules(Player p, string json)
	{
		Players.GetOrCreateValue(p).Save.Rules = json;
	}

	public static string SavePlayer(Player p)
	{
		return JsonSerializer.Serialize(Players.GetOrCreateValue(p).Save);
	}

	public static void RestorePlayer(Player p, string? json)
	{
		if (!string.IsNullOrEmpty(json))
		{
			RouletteSave save = JsonSerializer.Deserialize<RouletteSave>(json) ?? throw new FormatException("Empty relic save");
			if (save.Version != 2)
			{
				throw new FormatException("Invalid relic save");
			}
			if (!string.IsNullOrEmpty(save.Rules))
			{
				ModSettings.FromJson(save.Rules);
			}
			Players.GetOrCreateValue(p).Save = save;
		}
	}

	public static void Initialize()
	{
		if (!ExtendedSaveTypes.RegisterSavedValue("relicupgrader_state_v1", (RelicModel r) => For(r).ToJson(), delegate(RelicModel r, string? json)
		{
			Restore(r, json);
		}, delegate(string json, PacketWriter writer)
		{
			writer.WriteString(json);
		}, (PacketReader reader) => reader.ReadString()) || !ExtendedSaveTypes.RegisterSavedValue<Player, string>("relicupgrader_roulette_v2", SavePlayer, RestorePlayer, delegate(string json, PacketWriter writer)
		{
			writer.WriteString(json);
		}, (PacketReader reader) => reader.ReadString()))
		{
			throw new InvalidOperationException("BaseLib could not register Relic Upgrader saves");
		}
	}

	public static void Restore(RelicModel relic, string? json)
	{
		RelicState relicState = RelicState.FromJson(json);
		foreach (KeyValuePair<string, UpgradeLedger> upgrade in relicState.Upgrades)
		{
			upgrade.Deconstruct(out var key, out var value);
			string key2 = key;
			UpgradeLedger upgradeLedger = value;
			RelicAttribute relicAttribute = Catalog.Supported.GetValueOrDefault(relic.GetType().Name)?.FirstOrDefault((RelicAttribute a) => a.Key == key2);
			if (relicAttribute == null || relicAttribute.Direction != upgradeLedger.Direction || relicAttribute.Family != upgradeLedger.Family || !relic.DynamicVars.TryGetValue(key2, out DynamicVar value2))
			{
				throw new FormatException("Unknown upgraded relic attribute " + key2);
			}
			decimal num = upgradeLedger.Reference + (decimal)(upgradeLedger.Direction * upgradeLedger.Gain);
			if (num < 1m || num > (decimal)relicAttribute.Maximum)
			{
				throw new FormatException("Out-of-range relic attribute " + key2);
			}
			value2.BaseValue = num;
		}
		Relics.Remove(relic);
		Relics.Add(relic, relicState);
	}

	/// <summary>A Workshop purchase: raises (or lowers) the relic's value by <paramref name="units"/> and records it.</summary>
	public static string Upgrade(RelicModel relic, RelicAttribute attribute, int units)
	{
		if (!relic.IsMutable || !Catalog.For(relic).Contains(attribute) || units < 1 || units > attribute.Limit(relic))
		{
			throw new InvalidOperationException("Invalid relic upgrade");
		}
		RelicState relicState = For(relic);
		if (!relicState.Upgrades.TryGetValue(attribute.Key, out UpgradeLedger value))
		{
			value = new UpgradeLedger
			{
				Reference = attribute.Value(relic),
				Direction = attribute.Direction,
				Family = attribute.Family
			};
			relicState.Upgrades[attribute.Key] = value;
		}
		value.Gain += units;
		relic.DynamicVars[attribute.Key].BaseValue = value.Reference + (decimal)(value.Direction * value.Gain);
		return Text.Improved(attribute.Label, attribute.Value(relic));
	}
}
