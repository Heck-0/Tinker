using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Tinker.Card;

internal static class RewrittenCardValues
{
	private sealed class TemporaryGrowth
	{
		internal WeakReference<object> Combat;

		internal int Amount;
	}

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

	[HarmonyPatch(typeof(AbstractModel), "MutableClone")]
	internal static class CloneGrowth
	{
		private static void Postfix(AbstractModel __instance, AbstractModel __result)
		{
			if (__instance is CardModel key && __result is CardModel key2 && Growth.TryGetValue(key, out var value))
			{
				Growth.Remove(key2);
				Growth.Add(key2, new TemporaryGrowth
				{
					Combat = value.Combat,
					Amount = value.Amount
				});
			}
		}
	}

	private static readonly ConditionalWeakTable<CardModel, TemporaryGrowth> Growth = new ConditionalWeakTable<CardModel, TemporaryGrowth>();

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

	internal static bool AddTemporaryGrowth(CardModel card, string variable, int amount)
	{
		if (!Owns(card, AttributeKeys.Var(variable)))
		{
			return false;
		}
		TemporaryGrowth orCreateValue = Growth.GetOrCreateValue(card);
		object combatState = card.CombatState;
		if (orCreateValue.Combat == null || !orCreateValue.Combat.TryGetTarget(out var target) || target != combatState)
		{
			orCreateValue.Combat = new WeakReference<object>(combatState);
			orCreateValue.Amount = 0;
		}
		checked
		{
			orCreateValue.Amount += amount;
			return true;
		}
	}

	private static int Bonus(CardModel card, string variable)
	{
		CardMachineState cardMachineState = CardStateStore.Get(card);
		int num = cardMachineState?.RawDelta(AttributeKeys.Var(variable)) ?? 0;
		checked
		{
			if (Growth.TryGetValue(card, out var value) && value.Combat != null && value.Combat.TryGetTarget(out var target) && target == card.CombatState)
			{
				num += value.Amount;
			}
			return num;
		}
	}
}
