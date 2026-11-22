using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Tinker.Relic;

public sealed class RelicState
{
	public int Version { get; set; } = 1;

	public Dictionary<string, UpgradeLedger> Upgrades { get; set; } = new Dictionary<string, UpgradeLedger>(StringComparer.Ordinal);

	public string? Boon { get; set; }

	public string ToJson()
	{
		return JsonSerializer.Serialize(this);
	}

	public static RelicState FromJson(string? json)
	{
		RelicState relicState = (string.IsNullOrEmpty(json) ? new RelicState() : (JsonSerializer.Deserialize<RelicState>(json) ?? throw new FormatException("Empty relic state")));
		if (relicState.Version != 1 || relicState.Upgrades.Count > 32 || (relicState.Boon != null && !Boons.All.Contains<string>(relicState.Boon, StringComparer.Ordinal)) || relicState.Upgrades.Any<KeyValuePair<string, UpgradeLedger>>((KeyValuePair<string, UpgradeLedger> p) => p.Value == null || p.Value.Gain < 0 || p.Value.Gain > 10000 || (p.Value.Direction != 1 && p.Value.Direction != -1) || !Rules.Families.ContainsKey(p.Value.Family)))
		{
			throw new FormatException("Invalid relic state");
		}
		return relicState;
	}

	public RelicState Copy()
	{
		return FromJson(ToJson());
	}
}
