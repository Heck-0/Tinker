using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Card;

internal static class AdvancedEnchantmentUpgrades
{
	private static readonly HashSet<Type> Active = new HashSet<Type>();

	private static readonly FieldInfo Amount = AccessTools.Field(typeof(EnchantmentModel), "_amount");

	internal static IEnumerable<string> For(CardModel card)
	{
		if (card?.Enchantment == null || !Active.Contains(card.Enchantment.GetType()))
		{
			return Enumerable.Empty<string>();
		}
		return from e in AdvancedEnchantmentEffects.For(card.Enchantment.GetType().Name)
			where AttributeDirection.SignOf(e.Attribute) > 0 || Value(card, e.Attribute) > 0
			select e.Attribute;
	}

	internal static bool Matches(CardModel card, string key)
	{
		if (card?.Enchantment != null)
		{
			return AdvancedEnchantmentEffects.Find(key)?.Enchantment == card.Enchantment.GetType().Name;
		}
		return false;
	}

	internal static int Value(CardModel card, string key)
	{
		if (!Matches(card, key))
		{
			throw new InvalidOperationException("Encantamiento incompatible: " + key);
		}
		checked
		{
			return ((key == "enchanteffect:goopyBlock") ? unchecked((int)Amount.GetValue(card.Enchantment) - 1) : AdvancedEnchantmentEffects.Find(key).Native) + (CardStateStore.Get(card)?.RawDelta(key) ?? 0);
		}
	}

	internal static void Install(Harmony harmony)
	{
		foreach (string item in AdvancedEnchantmentEffects.Entries.Select((AdvancedEnchantmentEffect e) => e.Enchantment).Distinct())
		{
			Type type = typeof(EnchantmentModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Enchantments." + item);
			if (type == null)
			{
				continue;
			}
			try
			{
				switch (item)
				{
				case "Instinct":
				case "Corrupted":
					harmony.Patch(AccessTools.DeclaredMethod(type, "EnchantDamageMultiplicative"), null, new HarmonyMethod(typeof(AdvancedEnchantmentUpgrades), "Damage"));
					break;
				case "Goopy":
					harmony.Patch(AccessTools.DeclaredMethod(type, "EnchantBlockAdditive"), null, new HarmonyMethod(typeof(AdvancedEnchantmentUpgrades), "Block"));
					harmony.Patch(AccessTools.DeclaredMethod(type, "AfterCardPlayed"), new HarmonyMethod(typeof(AdvancedEnchantmentUpgrades), "Grow"));
					break;
				case "TezcatarasEmber":
					harmony.Patch(AccessTools.DeclaredMethod(type, "EnchantDamageAdditive"), null, new HarmonyMethod(typeof(AdvancedEnchantmentUpgrades), "EmberDamage"));
					break;
				case "Imbued":
				{
					MethodInfo element = AccessTools.DeclaredMethod(type, "AfterAutoPrePlayPhaseEntered");
					harmony.Patch(AccessTools.DeclaredMethod(element.GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType, "MoveNext"), null, null, new HarmonyMethod(typeof(AdvancedEnchantmentUpgrades), "Transpile"));
					break;
				}
				default:
					harmony.Patch(AccessTools.DeclaredMethod(type, (item == "Slither") ? "NextEnergyCost" : "BeforeFlush"), null, null, new HarmonyMethod(typeof(AdvancedEnchantmentUpgrades), "Transpile"));
					break;
				}
				Active.Add(type);
			}
			catch (Exception ex)
			{
				ModEntry.Warn("Encantamiento ampliado desactivado " + item + ": " + ex.GetBaseException().Message);
			}
		}
	}

	private static int Delta(EnchantmentModel e, string key)
	{
		if (!e.HasCard || !Matches(e.Card, key))
		{
			return 0;
		}
		return CardStateStore.Get(e.Card)?.RawDelta(key) ?? 0;
	}

	private static void Damage(EnchantmentModel __instance, ref decimal __result)
	{
		if (__result > 1m)
		{
			__result += (decimal)Delta(__instance, (__instance.GetType().Name == "Instinct") ? "enchanteffect:instinctDamage" : "enchanteffect:corruptedDamage") / 100m;
		}
	}

	private static void Block(EnchantmentModel __instance, ref decimal __result)
	{
		__result += (decimal)Delta(__instance, "enchanteffect:goopyBlock");
	}

	private static void EmberDamage(EnchantmentModel __instance, ref decimal __result)
	{
		if (__result > 0m)
		{
			__result += (decimal)Delta(__instance, "enchanteffect:emberDamage");
		}
	}

	private static Task ImbuedPlay(PlayerChoiceContext context, CardModel card, Creature target, AutoPlayType type, bool skipXCapture, bool skipCardPileVisuals, EnchantmentModel enchantment)
	{
		return ExpandedEffectPatches.AutoPlayWithBonus(context, card, target, type, skipXCapture, skipCardPileVisuals, Delta(enchantment, "enchanteffect:imbuedReplays"));
	}

	private static bool Grow(EnchantmentModel __instance, CardPlay cardPlay, ref Task __result)
	{
		int num = Delta(__instance, "enchanteffect:goopyGrowth");
		if (num == 0 || cardPlay.Card != __instance.Card)
		{
			return true;
		}
		checked
		{
			__instance.Amount = (int)Amount.GetValue(__instance) + 1 + num;
			EnchantmentModel enchantmentModel = __instance.Card.DeckVersion?.Enchantment;
			if (enchantmentModel != null)
			{
				enchantmentModel.Amount = (int)Amount.GetValue(enchantmentModel) + 1 + num;
			}
			__result = Task.CompletedTask;
			return false;
		}
	}

	internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		List<CodeInstruction> list = instructions.ToList();
		List<CodeInstruction> list2 = new List<CodeInstruction>();
		FieldInfo fieldInfo = __originalMethod.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((FieldInfo f) => f.Name == "<>4__this" && typeof(EnchantmentModel).IsAssignableFrom(f.FieldType));
		if (fieldInfo?.FieldType.Name == "Imbued")
		{
			int num = 0;
			foreach (CodeInstruction item in list)
			{
				if (item.operand is MethodInfo methodInfo && methodInfo.DeclaringType == typeof(CardCmd) && methodInfo.Name == "AutoPlay")
				{
					CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldarg_0);
					codeInstruction.labels.AddRange(item.labels);
					codeInstruction.blocks.AddRange(item.blocks);
					list2.Add(codeInstruction);
					list2.Add(new CodeInstruction(OpCodes.Ldfld, fieldInfo));
					list2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEnchantmentUpgrades), "ImbuedPlay")));
					num++;
				}
				else
				{
					list2.Add(item);
				}
			}
			if (num != 1)
			{
				throw new InvalidOperationException("Firma automática de Imbued inesperada: " + num);
			}
			return list2;
		}
		bool flag = __originalMethod.DeclaringType.Name == "Slither";
		int num2 = 0;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			CodeInstruction codeInstruction2 = list[num3];
			bool num4;
			if (!flag)
			{
				if (codeInstruction2.LoadsConstant(-1L) && num3 + 2 < list.Count && list[num3 + 1].LoadsConstant(0L) && list[num3 + 2].operand is MethodInfo methodInfo2)
				{
					num4 = methodInfo2.Name == "AddUntilPlayed";
					goto IL_0244;
				}
			}
			else if (codeInstruction2.LoadsConstant(4L) && num3 + 1 < list.Count && list[num3 + 1].operand is MethodInfo methodInfo3)
			{
				num4 = methodInfo3.Name == "NextInt";
				goto IL_0244;
			}
			goto IL_0246;
			IL_0244:
			if (num4)
			{
				CodeInstruction codeInstruction3 = new CodeInstruction(OpCodes.Ldarg_0);
				codeInstruction3.labels.AddRange(codeInstruction2.labels);
				codeInstruction3.blocks.AddRange(codeInstruction2.blocks);
				list2.Add(codeInstruction3);
				list2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEnchantmentUpgrades), flag ? "Bound" : "Discount")));
				num2++;
				continue;
			}
			goto IL_0246;
			IL_0246:
			list2.Add(codeInstruction2);
		}
		if (num2 != 1)
		{
			throw new InvalidOperationException("Firma de coste del encantamiento inesperada: " + num2);
		}
		return list2;
	}

	private static int Bound(EnchantmentModel e)
	{
		return Math.Max(1, 4 + Delta(e, "enchantdown:slitherMaximum"));
	}

	private static int Discount(EnchantmentModel e)
	{
		return checked(-1 - Delta(e, "enchanteffect:slumberingDiscount"));
	}

	internal static string Rewrite(CardModel card, string text)
	{
		if (card?.Enchantment == null || !Active.Contains(card.Enchantment.GetType()))
		{
			return text;
		}
		foreach (AdvancedEnchantmentEffect item in AdvancedEnchantmentEffects.For(card.Enchantment.GetType().Name))
		{
			CardMachineState cardMachineState = CardStateStore.Get(card);
			if (cardMachineState == null || cardMachineState.RawDelta(item.Attribute) == 0)
			{
				continue;
			}
			if (item.Attribute == "enchanteffect:goopyGrowth")
			{
				string text2 = ExpandedEffectText.RewriteLine(text, "Goopy", item.Attribute, "enchantmentText", Value(card, item.Attribute), Lines.Spanish);
				if (text2 != null)
				{
					text = text2;
					continue;
				}
			}
			string text3 = (Lines.Spanish ? item.Spanish : item.English);
			text = text + "\n" + text3 + ": " + Value(card, item.Attribute) + ((item.Family == "percentage") ? "%" : "") + ".";
		}
		return text;
	}
}
