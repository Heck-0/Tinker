using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Relic;

[HarmonyPatch(typeof(RelicModel), "get_DynamicDescription")]
public static class RelicDescriptionPatch
{
	public static void Postfix(RelicModel __instance, ref LocString __result)
	{
		if (!__instance.IsMutable)
		{
			return;
		}
		RelicState relicState = Store.For(__instance);
		if (relicState.Upgrades.Count == 0)
		{
			return;
		}
		string variable = __result.GetFormattedText();
		if (__instance.GetType().Name == "MembershipCard" && relicState.Upgrades.ContainsKey("Discount"))
		{
			decimal value = 100m - __instance.DynamicVars["Discount"].BaseValue;
			variable = Text.T($"Shop prices are {value}% lower.");
		}
		string variable2 = string.Join("\n", (from p in relicState.Upgrades.OrderBy<KeyValuePair<string, UpgradeLedger>, string>((KeyValuePair<string, UpgradeLedger> p) => p.Key, StringComparer.Ordinal)
			select "[green]" + (Catalog.Find(__instance, p.Key)?.Label ?? Text.Attribute(p.Key)) + " " + ((p.Value.Direction > 0) ? "+" : "−") + ((p.Value.Gain * (Catalog.Find(__instance, p.Key)?.Step ?? 1m)).ToString("0.##")) + "[/green]"));
		LocString locString = new LocString("relics", "RELICUPGRADER_DESCRIPTION");
		locString.Add("original", variable);
		locString.Add("details", variable2);
		__result = locString;
	}
}
