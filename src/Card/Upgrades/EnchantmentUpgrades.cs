using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Tinker.Card;

public static class EnchantmentUpgrades
{
	[HarmonyPatch(typeof(EnchantmentModel), "get_Amount")]
	private static class AmountOverlay
	{
		private static void Postfix(EnchantmentModel __instance, ref int __result)
		{
			checked
			{
				__result += Delta(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(EnchantmentModel), "get_DynamicVars")]
	private static class VariablesOverlay
	{
		private static void Postfix(EnchantmentModel __instance, DynamicVarSet __result)
		{
			if (__instance.HasCard)
			{
				int num = Delta(__instance);
				if (__instance is Spiral || __instance is Glam)
				{
					__result["Times"].BaseValue = 1 + num;
				}
				else if (__instance is Inky)
				{
					__result.Weak.BaseValue = 1 + num;
				}
				else if (__instance is Adroit)
				{
					__result.Block.BaseValue = __instance.Amount;
				}
			}
		}
	}

	[HarmonyPatch(typeof(EnchantmentModel), "ToSerializable")]
	private static class NativeSerialization
	{
		private static void Postfix(EnchantmentModel __instance, SerializableEnchantment __result)
		{
			if (EnchantmentEffects.Supported(__instance.GetType().Name))
			{
				__result.Amount = (int)AmountField.GetValue(__instance);
			}
		}
	}

	private static readonly FieldInfo AmountField = AccessTools.Field(typeof(EnchantmentModel), "_amount");

	public static string Attribute(CardModel card)
	{
		if (card.Enchantment == null || !EnchantmentEffects.Supported(card.Enchantment.GetType().Name))
		{
			return null;
		}
		return EnchantmentEffects.Key(card.Enchantment.GetType().Name);
	}

	public static int Delta(EnchantmentModel enchantment)
	{
		if (enchantment == null || !enchantment.HasCard || !(Attribute(enchantment.Card) == EnchantmentEffects.Key(enchantment.GetType().Name)))
		{
			return 0;
		}
		return CardStateStore.Get(enchantment.Card)?.RawDelta(EnchantmentEffects.Key(enchantment.GetType().Name)) ?? 0;
	}

	public static int Native(EnchantmentModel enchantment)
	{
		if (!(enchantment is Spiral) && !(enchantment is Glam) && !(enchantment is Inky))
		{
			return (int)AmountField.GetValue(enchantment);
		}
		return 1;
	}

	public static int Value(CardModel card, string attribute)
	{
		if (Attribute(card) != attribute)
		{
			throw new InvalidOperationException("El encantamiento ya no corresponde a " + attribute);
		}
		return checked(Native(card.Enchantment) + (CardStateStore.Get(card)?.RawDelta(attribute) ?? 0));
	}

	public static string Label(CardModel card, string attribute)
	{
		string formattedText = card.Enchantment.Title.GetFormattedText();
		string text;
		switch (card.Enchantment.GetType().Name)
		{
		case "Spiral":
		case "Glam":
			text = (Lines.Spanish ? "Repeticiones" : "Replays");
			break;
		case "Sown":
			text = (Lines.Spanish ? "Energía" : "Energy");
			break;
		case "Swift":
			text = (Lines.Spanish ? "Robo" : "Draw");
			break;
		case "Momentum":
			text = (Lines.Spanish ? "Daño por uso" : "Damage per play");
			break;
		case "Adroit":
		case "Nimble":
			text = (Lines.Spanish ? "Bloqueo" : "Block");
			break;
		case "Inky":
			text = (Lines.Spanish ? "Débil" : "Weak");
			break;
		default:
			text = (Lines.Spanish ? "Daño" : "Damage");
			break;
		}
		return formattedText + ": " + text;
	}
}
