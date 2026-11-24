using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Tinker.Relic;

/// <summary>
/// Paper Phrog (Vulnerable +25%) and Paper Krane (Weak −15%) hard-code their bonus. Here each gets a value
/// (VulnerableBonus 0.25, WeakReduction 0.15) that the Workshop can upgrade, and their multiplier uses it.
/// </summary>
internal static class PaperRelics
{
	public const string VulnerableBonus = "VulnerableBonus";

	public const string WeakReduction = "WeakReduction";

	/// <summary>Neither relic declares values (RelicModel's empty default), so add one through the base getter.</summary>
	[HarmonyPatch(typeof(RelicModel), "CanonicalVars", MethodType.Getter)]
	internal static class ValuesPatch
	{
		private static void Postfix(RelicModel __instance, ref IEnumerable<DynamicVar> __result)
		{
			if (__instance is PaperPhrog)
			{
				__result = new DynamicVar[1] { new DynamicVar(VulnerableBonus, 0.25m) };
			}
			else if (__instance is PaperKrane)
			{
				__result = new DynamicVar[1] { new DynamicVar(WeakReduction, 0.15m) };
			}
		}
	}

	/// <summary>When the relic applies (it returned something other than the incoming multiplier), use its value.</summary>
	[HarmonyPatch(typeof(PaperPhrog), nameof(PaperPhrog.ModifyVulnerableMultiplier))]
	internal static class PhrogPatch
	{
		private static void Postfix(PaperPhrog __instance, decimal amount, ref decimal __result)
		{
			if (__result != amount && __instance.DynamicVars.TryGetValue(VulnerableBonus, out DynamicVar value))
			{
				__result = amount + value.BaseValue;
			}
		}
	}

	[HarmonyPatch(typeof(PaperKrane), nameof(PaperKrane.ModifyWeakMultiplier))]
	internal static class KranePatch
	{
		private static void Postfix(PaperKrane __instance, decimal amount, ref decimal __result)
		{
			if (__result != amount && __instance.DynamicVars.TryGetValue(WeakReduction, out DynamicVar value))
			{
				__result = Math.Max(0m, amount - value.BaseValue);
			}
		}
	}
}
