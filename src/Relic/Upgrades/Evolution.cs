using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Relic;

public static class Evolution
{
	private static readonly Dictionary<string, (string Target, string FromKey, string ToKey)> Pairs = new Dictionary<string, (string, string, string)>
	{
		["BurningBlood"] = ("BlackBlood", "Heal", "Heal"),
		["RingOfTheSnake"] = ("RingOfTheDrake", "Cards", "Cards"),
		["DivineRight"] = ("DivineDestiny", "Stars", "Stars"),
		["BoundPhylactery"] = ("PhylacteryUnbound", "Summon", "StartOfCombat"),
		["CrackedCore"] = ("InfusedCore", "Lightning", "Lightning")
	};

	public static bool Transfer(RelicModel old, RelicModel next)
	{
		if (!Pairs.TryGetValue(old.GetType().Name, out (string, string, string) value) || next.GetType().Name != value.Item1)
		{
			return false;
		}
		RelicState relicState = Store.For(old);
		RelicState relicState2 = Store.For(next).Copy();
		string key;
		foreach (KeyValuePair<string, UpgradeLedger> upgrade in relicState.Upgrades)
		{
			upgrade.Deconstruct(out key, out var value2);
			string text = key;
			UpgradeLedger upgradeLedger = value2;
			string target = ((text == value.Item2) ? value.Item3 : text);
			RelicAttribute relicAttribute = Catalog.Find(next, target);
			if (relicAttribute == null || !next.DynamicVars.ContainsKey(target))
			{
				throw new InvalidOperationException("Unmapped relic evolution attribute " + text);
			}
			relicState2.Upgrades[target] = new UpgradeLedger
			{
				Reference = next.DynamicVars[target].BaseValue,
				Gain = upgradeLedger.Gain,
				Direction = relicAttribute.Direction,
				Family = relicAttribute.Family
			};
		}
		Store.Restore(next, relicState2.ToJson());
		return true;
	}
}
