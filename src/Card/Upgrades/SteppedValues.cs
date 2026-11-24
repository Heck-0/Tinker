using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Tinker.Card;

/// <summary>
/// Card values that are fractions (damage multipliers). The Workshop counts them in whole units of
/// <see cref="Stepped.Step"/> measured from <see cref="Stepped.Neutral"/> (the value that means "no effect"), so
/// ×1.50 damage taken is 50 units of 0.01 above ×1.00, and prices compare a purchase with those 50 units.
/// </summary>
internal static class SteppedValues
{
	internal sealed record Stepped(decimal Step, decimal Neutral, int MinUnits, string Family);

	private static readonly Dictionary<(string Card, string Var), Stepped> Values = new Dictionary<(string, string), Stepped>
	{
		// Tank: you take ×1.5 damage (lower is better, down to ×1.0); allies take ×0.5 (lower is better, down to ×0.1).
		[("Tank", "DamageIncrease")] = new Stepped(0.01m, 1m, 0, "percentage"),
		[("Tank", "DamageDecrease")] = new Stepped(0.01m, 0m, 10, "percentage")
	};

	public static bool TryGet(CardModel card, string varName, out Stepped stepped)
	{
		stepped = null;
		return card != null && varName != null && Values.TryGetValue((card.GetType().Name, varName), out stepped);
	}

	public static int Units(DynamicVar value, Stepped s)
	{
		return (int)Math.Round((value.BaseValue - s.Neutral) / s.Step, MidpointRounding.AwayFromZero);
	}

	public static string Display(Stepped s, int units)
	{
		return "×" + (s.Neutral + units * s.Step).ToString("0.00");
	}

	// ------------------------------------------------------------------ Tank: the card's values reach its powers

	/// <summary>The damage multiplier the Tank's allies get, by the Tank player's creature, until GuardedPower is applied.</summary>
	private static readonly ConditionalWeakTable<Creature, StrongBox<decimal>> GuardFor = new ConditionalWeakTable<Creature, StrongBox<decimal>>();

	/// <summary>Tank applies TankPower from its own constants; copy the (upgraded) card's values onto it instead.</summary>
	[HarmonyPatch(typeof(TankPower), nameof(TankPower.AfterApplied))]
	internal static class TankPowerPatch
	{
		private static void Prefix(TankPower __instance, CardModel cardSource)
		{
			if (cardSource is Tank tank && __instance.Owner != null)
			{
				__instance.DynamicVars["DamageIncrease"].BaseValue = tank.DynamicVars["DamageIncrease"].BaseValue;
				GuardFor.AddOrUpdate(__instance.Owner, new StrongBox<decimal>(tank.DynamicVars["DamageDecrease"].BaseValue));
			}
		}
	}

	/// <summary>The allies' GuardedPower, applied by TankPower: the card's DamageDecrease.</summary>
	[HarmonyPatch(typeof(GuardedPower), nameof(GuardedPower.AfterApplied))]
	internal static class GuardedPowerPatch
	{
		private static void Postfix(GuardedPower __instance)
		{
			if (__instance.Applier != null && GuardFor.TryGetValue(__instance.Applier, out StrongBox<decimal> value))
			{
				__instance.DynamicVars["DamageDecrease"].BaseValue = value.Value;
			}
		}
	}
}
