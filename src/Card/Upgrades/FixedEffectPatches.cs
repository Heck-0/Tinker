using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Tinker.Common;

namespace Tinker.Card;

internal static class FixedEffectPatches
{
	private static readonly HashSet<Type> Active = new HashSet<Type>();

	private static readonly string[] Cards = new string[18]
	{
		"Zap", "BallLightning", "ColdSnap", "Coolheaded", "Glacier", "Darkness", "Fusion", "Rainbow", "Chill", "Dualcast",
		"MultiCast", "Tempest", "MeteorStrike", "Null", "ShadowShield", "Ignition", "Glasswork", "Shatter"
	};

	internal static bool Enabled(CardModel card)
	{
		if (card != null)
		{
			return Active.Contains(card.GetType());
		}
		return false;
	}

	/// <summary>The upgrade's name in the Workshop (in the game's language).</summary>
	internal static string AttributeName(CardModel card, string attribute)
	{
		return Lang.T(AttributeNameText(card, attribute));
	}

	private static string AttributeNameText(CardModel card, string attribute)
	{
		BidirectionalEffect bidirectionalEffect = BidirectionalEffects.Find(card?.GetType().Name, attribute);
		if (bidirectionalEffect != null)
		{
			return ((!(card is TrueGrit) || card.IsUpgraded) ? bidirectionalEffect.English : "Random Exhausts") + ((AttributeDirection.SignOf(attribute) <= 0) ? " (decrease)" : " (increase)");
		}
		if (EnchantmentEffects.Known(attribute))
		{
			return EnchantmentUpgrades.Label(card, attribute);
		}
		AdvancedEnchantmentEffect advancedEnchantmentEffect = AdvancedEnchantmentEffects.Find(attribute);
		if (advancedEnchantmentEffect != null)
		{
			return advancedEnchantmentEffect.English;
		}
		ExpandedEffect expandedEffect = ExpandedEffects.Find(card?.GetType().Name, attribute);
		if (card is Eidolon && expandedEffect != null)
		{
			if (!ExpandedEffectPatches.EidolonUsesAutoPlay)
			{
				return "Intangible";
			}
			return "Plays per automatic card";
		}
		if (card?.GetType().Name == "Tracking" && expandedEffect != null && ExpandedEffectPatches.TrackingUsesPercent)
		{
			return "Damage bonus against Weak (%)";
		}
		if (attribute == AttributeKeys.VarDown("Cards") && (card?.GetType().Name == "SorrowfulStrike" || card?.GetType().Name == "Jest"))
		{
			return "Sadness added";
		}
		if (attribute == AttributeKeys.Var("Increase") && card is GeneticAlgorithm)
		{
			return "Block growth per play";
		}
		if (AttributeKeys.VarName(attribute) == "CalculationBase" && card != null && CardAttributes.HiddenCalculationCards.Contains(card.GetType().Name))
		{
			return CardAttributes.CalculationLabel(card);
		}
		if (expandedEffect != null)
		{
			return expandedEffect.English;
		}
		if (attribute == "effect:extraLightning")
		{
			return "Additional Lightning to X";
		}
		if (attribute == "effect:glass")
		{
			return "Glass Orbs";
		}
		if (attribute == AttributeKeys.Var("Repeat"))
		{
			if (card is Chaos)
			{
				return "Random orbs";
			}
			if (card is Capacitor)
			{
				return "Orb slots";
			}
		}
		return Lines.AttributeName(attribute);
	}

	internal static void Install(Harmony harmony)
	{
		string[] cards = Cards;
		foreach (string text in cards)
		{
			try
			{
				Type type = typeof(CardModel).Assembly.GetType(typeof(Zap).Namespace + "." + text, throwOnError: true);
				Type type2 = AccessTools.DeclaredMethod(type, "OnPlay").GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
				if (type2 == null)
				{
					throw new InvalidOperationException("OnPlay is not async");
				}
				harmony.Patch(AccessTools.DeclaredMethod(type2, "MoveNext"), null, null, new HarmonyMethod(typeof(FixedEffectPatches), "Transpile"));
				Active.Add(type);
			}
			catch (Exception ex)
			{
				ModEntry.Warn("Expanded effects disabled for " + text + ": " + ex.GetBaseException().Message);
			}
		}
	}

	internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		List<CodeInstruction> list = instructions.ToList();
		FieldInfo fieldInfo = __originalMethod.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single((FieldInfo f) => typeof(CardModel).IsAssignableFrom(f.FieldType));
		string name = fieldInfo.FieldType.Name;
		int num = 0;
		List<CodeInstruction> list2 = new List<CodeInstruction>();
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			CodeInstruction codeInstruction = list[num2];
			MethodInfo methodInfo = codeInstruction.operand as MethodInfo;
			bool flag = name != "Glacier" && name != "MeteorStrike" && name != "Tempest" && methodInfo?.DeclaringType == typeof(OrbCmd) && methodInfo.Name == "Channel" && methodInfo.IsGenericMethod && methodInfo.GetParameters().Length == 2;
			bool flag2 = (name == "Dualcast" || name == "Shatter") && methodInfo?.DeclaringType == typeof(OrbCmd) && methodInfo.Name == "EvokeNext";
			bool flag3 = (name == "MultiCast" || name == "Tempest") && methodInfo?.Name == "ResolveEnergyXValue";
			bool flag4 = name == "Glacier" && codeInstruction.opcode == OpCodes.Ldc_I4_2 && num2 + 1 < list.Count && (list[num2 + 1].opcode == OpCodes.Blt || list[num2 + 1].opcode == OpCodes.Blt_S);
			bool flag5 = name == "MeteorStrike" && codeInstruction.opcode == OpCodes.Ldc_I4_3 && num2 + 1 < list.Count && (list[num2 + 1].opcode == OpCodes.Blt || list[num2 + 1].opcode == OpCodes.Blt_S);
			if (flag || flag2 || flag4 || flag5)
			{
				CodeInstruction codeInstruction2 = new CodeInstruction(OpCodes.Ldarg_0);
				codeInstruction2.labels.AddRange(codeInstruction.labels);
				codeInstruction2.blocks.AddRange(codeInstruction.blocks);
				list2.Add(codeInstruction2);
				list2.Add(new CodeInstruction(OpCodes.Ldfld, fieldInfo));
				MethodInfo methodInfo2 = AccessTools.Method(typeof(FixedEffectPatches), flag ? "Channel" : (flag2 ? "Evoke" : (flag5 ? "MeteorCount" : "GlacierCount")));
				if (flag)
				{
					Type type = methodInfo.GetGenericArguments()[0];
					if (!FixedEffects.Supports(name, ChannelEffect(name, type.Name)))
					{
						throw new InvalidOperationException("Unexpected orb type in " + name);
					}
					methodInfo2 = methodInfo2.MakeGenericMethod(type);
				}
				list2.Add(new CodeInstruction(OpCodes.Call, methodInfo2));
				num++;
			}
			else
			{
				list2.Add(codeInstruction);
				if (flag3)
				{
					list2.Add(new CodeInstruction(OpCodes.Ldarg_0));
					list2.Add(new CodeInstruction(OpCodes.Ldfld, fieldInfo));
					list2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixedEffectPatches), (name == "Tempest") ? "AddExtraLightning" : "AddExtraEvokes")));
					num++;
				}
			}
		}
		int num3;
		switch (name)
		{
		default:
			num3 = 1;
			break;
		case "Dualcast":
		case "Shatter":
			num3 = 2;
			break;
		case "Rainbow":
			num3 = 3;
			break;
		}
		int num4 = num3;
		if (num != num4)
		{
			throw new InvalidOperationException($"Unexpected IL signature in {name}: {num}/{num4}");
		}
		return list2;
	}

	private static int Value(CardModel card, string effect)
	{
		return FixedEffects.Value(card.GetType().Name, effect, CardStateStore.Get(card));
	}

	private static async Task Channel<T>(PlayerChoiceContext context, Player player, CardModel card) where T : OrbModel
	{
		string effect = ChannelEffect(card.GetType().Name, typeof(T).Name);
		int count = Value(card, effect);
		for (int i = 0; i < count; i++)
		{
			await OrbCmd.Channel<T>(context, player);
		}
	}

	private static string ChannelEffect(string card, string orb)
	{
		switch (orb)
		{
		case "LightningOrb":
			return "effect:lightning";
		case "DarkOrb":
			return "effect:dark";
		case "PlasmaOrb":
			return "effect:plasma";
		case "FrostOrb":
			if (!(card == "Chill"))
			{
				return "effect:frost";
			}
			return "effect:frostPerEnemy";
		case "GlassOrb":
			return "effect:glass";
		default:
			throw new InvalidOperationException("Unsupported orb type: " + orb);
		}
	}

	private static int GlacierCount(CardModel card)
	{
		return Value(card, "effect:frost");
	}

	private static int MeteorCount(CardModel card)
	{
		return Value(card, "effect:plasma");
	}

	private static async Task Evoke(PlayerChoiceContext context, Player player, bool dequeue, CardModel card)
	{
		int count = (dequeue ? 1 : (Value(card, "effect:evokes") - 1));
		for (int i = 0; i < count; i++)
		{
			await OrbCmd.EvokeNext(context, player, dequeue);
		}
	}

	private static int AddExtraEvokes(int energyX, CardModel card)
	{
		return checked(energyX + Value(card, "effect:extraEvokes"));
	}

	private static int AddExtraLightning(int energyX, CardModel card)
	{
		return checked(energyX + Value(card, "effect:extraLightning"));
	}

	internal static string Rewrite(CardModel card, string text)
	{
		CardMachineState cardMachineState = CardStateStore.Get(card);
		if (cardMachineState == null || !Enabled(card))
		{
			return text;
		}
		foreach (string item in FixedEffects.For(card.GetType().Name))
		{
			if (cardMachineState.RawDelta(item) != 0)
			{
				text = RewriteLine(text, item, Value(card, item), card.IsUpgraded);
			}
		}
		return text;
	}

	internal static string RewriteLine(string text, string effect, int value, bool upgraded)
	{
		string replacement;
		object obj;
		string text2;
		string pattern;
		switch (effect)
		{
		case "effect:evokes":
			pattern = "twice";
			replacement = value + " times";
			break;
		case "effect:extraEvokes":
		case "effect:extraLightning":
			pattern = "\\bX(?:\\+\\d+)?";
			replacement = "X+" + (value + (upgraded ? 1 : 0));
			break;
		default:
			obj = "Frost";
			goto IL_00e9;
		case "effect:glass":
			obj = "(?:Glass|Cristal)";
			goto IL_00e9;
		case "effect:plasma":
			obj = "Plasma";
			goto IL_00e9;
		case "effect:dark":
			obj = "Dark";
			goto IL_00e9;
		case "effect:lightning":
			{
				obj = "Lightning";
				goto IL_00e9;
			}
			IL_00e9:
			text2 = (string)obj;
			pattern = "((?:\\[gold\\])?Channel(?:\\[/gold\\])?\\s+)\\d+(\\s+(?:\\[gold\\])?" + text2 + "\\b)";
			return Regex.Replace(text, pattern, (Match m) => m.Groups[1].Value + value + m.Groups[2].Value, RegexOptions.None, TimeSpan.FromMilliseconds(50L, 0L));
		}
		return Regex.Replace(text, pattern, replacement, RegexOptions.None, TimeSpan.FromMilliseconds(50L, 0L));
	}
}
