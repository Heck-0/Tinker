using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Tinker.Card;

internal static class RewrittenCardValues
{
	[HarmonyPatch(typeof(CardModel), "get_DynamicVars")]
	internal static class Values
	{
		private static void Postfix(CardModel __instance, DynamicVarSet __result)
		{
			if (__instance is GeneticAlgorithm geneticAlgorithm)
			{
				__result.Block.BaseValue = checked(geneticAlgorithm.CurrentBlock + Bonus(geneticAlgorithm, "Block"));
			}
			else if (__instance is TheScythe theScythe)
			{
				__result.Damage.BaseValue = checked(theScythe.CurrentDamage + Bonus(theScythe, "Damage"));
			}
			else if (__instance is Guilty guilty)
			{
				__result["Combats"].BaseValue = Math.Max(0, 5 - guilty.CombatsSeen + (CardStateStore.Get(guilty)?.RawDelta("extradown:quest") ?? 0));
			}
			else if (__instance.GetType().Name == "Dowsing")
			{
				__result["Rooms"].BaseValue = Math.Max(0, 5 - (int)AccessTools.PropertyGetter(__instance.GetType(), "RoomsEntered").Invoke(__instance, null) + (CardStateStore.Get(__instance)?.RawDelta("extradown:quest") ?? 0));
			}
			else if (__instance is Normality)
			{
				__result["CalculationBase"].BaseValue = 3 + (CardStateStore.Get(__instance)?.RawDelta("extra:normalityLimit") ?? 0);
			}
		}
	}

	[HarmonyPatch(typeof(CalculatedVar), "Calculate")]
	internal static class NormalityRemaining
	{
		private static void Postfix(CalculatedVar __instance, ref decimal __result)
		{
			if (AccessTools.Field(typeof(DynamicVar), "_owner").GetValue(__instance) is Normality normality)
			{
				int num = 3 + (CardStateStore.Get(normality)?.RawDelta("extra:normalityLimit") ?? 0);
				int num2 = ((normality.CombatState != null) ? ((int)AccessTools.PropertyGetter(typeof(Normality), "CardsPlayedThisTurn").Invoke(normality, null)) : 0);
				__result = Math.Max(0, num - num2);
			}
		}
	}

	internal static bool Owns(CardModel card, string attribute)
	{
		if (!(card is GeneticAlgorithm) || !(attribute == AttributeKeys.Var("Block")))
		{
			if (card is TheScythe)
			{
				return attribute == AttributeKeys.Var("Damage");
			}
			return false;
		}
		return true;
	}

	internal static int Native(CardModel card)
	{
		if (!(card is GeneticAlgorithm geneticAlgorithm))
		{
			return ((TheScythe)card).CurrentDamage;
		}
		return geneticAlgorithm.CurrentBlock;
	}

	/// <summary>Units bought for the value the card rewrites itself.</summary>
	private static int Bonus(CardModel card, string variable)
	{
		return CardStateStore.Get(card)?.RawDelta(AttributeKeys.Var(variable)) ?? 0;
	}
}
