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
		List<CodeInstruction> code = instructions.ToList();
		FieldInfo cardField = __originalMethod.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single((FieldInfo f) => typeof(CardModel).IsAssignableFrom(f.FieldType));
		string cardName = cardField.FieldType.Name;
		int replaced = 0;
		List<CodeInstruction> output = new List<CodeInstruction>();
		for (int i = 0; i < code.Count; i++)
		{
			CodeInstruction instruction = code[i];
			MethodInfo called = instruction.operand as MethodInfo;
			bool isChannel = cardName != "Glacier" && cardName != "MeteorStrike" && cardName != "Tempest" && called?.DeclaringType == typeof(OrbCmd) && called.Name == "Channel" && called.IsGenericMethod && called.GetParameters().Length == 2;
			bool isEvoke = (cardName == "Dualcast" || cardName == "Shatter") && called?.DeclaringType == typeof(OrbCmd) && called.Name == "EvokeNext";
			bool isXValue = (cardName == "MultiCast" || cardName == "Tempest") && called?.Name == "ResolveEnergyXValue";
			bool isGlacierCount = cardName == "Glacier" && instruction.opcode == OpCodes.Ldc_I4_2 && i + 1 < code.Count && (code[i + 1].opcode == OpCodes.Blt || code[i + 1].opcode == OpCodes.Blt_S);
			bool isMeteorCount = cardName == "MeteorStrike" && instruction.opcode == OpCodes.Ldc_I4_3 && i + 1 < code.Count && (code[i + 1].opcode == OpCodes.Blt || code[i + 1].opcode == OpCodes.Blt_S);
			if (isChannel || isEvoke || isGlacierCount || isMeteorCount)
			{
				CodeInstruction self = new CodeInstruction(OpCodes.Ldarg_0);
				self.labels.AddRange(instruction.labels);
				self.blocks.AddRange(instruction.blocks);
				output.Add(self);
				output.Add(new CodeInstruction(OpCodes.Ldfld, cardField));
				MethodInfo method = AccessTools.Method(typeof(FixedEffectPatches), isChannel ? "Channel" : (isEvoke ? "Evoke" : (isMeteorCount ? "MeteorCount" : "GlacierCount")));
				if (isChannel)
				{
					Type orbType = called.GetGenericArguments()[0];
					if (!FixedEffects.Supports(cardName, ChannelEffect(cardName, orbType.Name)))
					{
						throw new InvalidOperationException("Unexpected orb type in " + cardName);
					}
					method = method.MakeGenericMethod(orbType);
				}
				output.Add(new CodeInstruction(OpCodes.Call, method));
				replaced++;
			}
			else
			{
				output.Add(instruction);
				if (isXValue)
				{
					output.Add(new CodeInstruction(OpCodes.Ldarg_0));
					output.Add(new CodeInstruction(OpCodes.Ldfld, cardField));
					output.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixedEffectPatches), (cardName == "Tempest") ? "AddExtraLightning" : "AddExtraEvokes")));
					replaced++;
				}
			}
		}
		int expectedSites;
		switch (cardName)
		{
		default:
			expectedSites = 1;
			break;
		case "Dualcast":
		case "Shatter":
			expectedSites = 2;
			break;
		case "Rainbow":
			expectedSites = 3;
			break;
		}
		int expected = expectedSites;
		if (replaced != expected)
		{
			throw new InvalidOperationException($"Unexpected IL signature in {cardName}: {replaced}/{expected}");
		}
		return output;
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

	/// <summary>Rewrites the number in the card's English text for one orb / evoke effect.</summary>
	internal static string RewriteLine(string text, string effect, int value, bool upgraded)
	{
		TimeSpan timeout = TimeSpan.FromMilliseconds(50L);
		switch (effect)
		{
		case "effect:evokes":
			return Regex.Replace(text, "twice", value + " times", RegexOptions.None, timeout);
		case "effect:extraEvokes":
		case "effect:extraLightning":
			return Regex.Replace(text, "\\bX(?:\\+\\d+)?", "X+" + (value + (upgraded ? 1 : 0)), RegexOptions.None, timeout);
		}
		string orb = effect switch
		{
			"effect:glass" => "Glass",
			"effect:plasma" => "Plasma",
			"effect:dark" => "Dark",
			"effect:lightning" => "Lightning",
			_ => "Frost",
		};
		// "Channel 2 Frost" -> "Channel <value> Frost", keeping any [gold] markup.
		string pattern = "((?:\\[gold\\])?Channel(?:\\[/gold\\])?\\s+)\\d+(\\s+(?:\\[gold\\])?" + orb + "\\b)";
		return Regex.Replace(text, pattern, (Match m) => m.Groups[1].Value + value + m.Groups[2].Value, RegexOptions.None, timeout);
	}
}
