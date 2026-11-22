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
		if (relicState.Upgrades.Count == 0 && relicState.Boon == null)
		{
			return;
		}
		string variable = __result.GetFormattedText();
		if (__instance.GetType().Name == "MembershipCard" && relicState.Upgrades.ContainsKey("Discount"))
		{
			decimal value = 100m - __instance.DynamicVars["Discount"].BaseValue;
			variable = Text.T($"Los precios de la tienda son un {value} % más bajos.", $"Shop prices are {value}% lower.");
		}
		string variable2 = string.Join("\n", (from p in relicState.Upgrades.OrderBy<KeyValuePair<string, UpgradeLedger>, string>((KeyValuePair<string, UpgradeLedger> p) => p.Key, StringComparer.Ordinal)
			select "[green]" + (Catalog.Supported.GetValueOrDefault(__instance.GetType().Name)?.FirstOrDefault((RelicAttribute a) => a.Key == p.Key)?.Label ?? Text.Attribute(p.Key)) + " " + ((p.Value.Direction > 0) ? "+" : "−") + p.Value.Gain + "[/green]").Concat((relicState.Boon == null) ? ((IEnumerable<string>)Array.Empty<string>()) : ((IEnumerable<string>)new string[1] { "[gold]" + Boons.Name(relicState.Boon) + ": " + Boons.Description(relicState.Boon) + "[/gold]" })));
		LocString locString = new LocString("relics", "RELICUPGRADER_DESCRIPTION");
		locString.Add("original", variable);
		locString.Add("details", variable2);
		__result = locString;
	}
}
