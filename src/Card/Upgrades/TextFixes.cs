using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Card;

internal static class TextFixes
{
	private delegate string Fix(string text, int value);

	private sealed class Rule
	{
		public string Card;

		public string Var;

		public Fix Apply;
	}

	[HarmonyPatch]
	internal static class DescriptionPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.GetDeclaredMethods(typeof(CardModel)).First((MethodInfo m) => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);
		}

		[HarmonyPriority(800)]
		private static void Postfix(CardModel __instance, ref string __result)
		{
			try
			{
				__result = Rewrite(__instance, __result);
			}
			catch (Exception ex)
			{
				ModEntry.Error("Could not rewrite the text of " + (__instance?.Id)?.ToString() + ": " + ex.Message);
			}
		}
	}

	private static readonly List<Rule> Rules = new List<Rule>
	{
		R("Blur", "Blur", (string t, int n) => Sub(t, "at the start of your next turn", (Match _) => ($"at the start of your next {n} turns"))),
		R("Equilibrium", "Equilibrium", (string t, int n) => Sub(t, "this turn", (Match _) => ($"for {n} turns"))),
		R("Colossus", "Colossus", (string t, int n) => Sub(t, "this turn", (Match _) => ($"for {n} turns"))),
		R("ConsumingShadow", "ConsumingShadowPower", (string t, int n) => Sub(t, "your leftmost Orb", (Match _) => ($"your {n} leftmost Orbs"))),
		R("Seance", "Cards", (string t, int n) => Sub(t, "Transform a card", (Match _) => ($"Transform {n} cards"))),
		R("Voltaic", "CalculationBase", (string t, int n) => t + ($"\nChannel {n} additional Lightning Orbs.")),
		R("Voltaic", "CalculationExtra", (string t, int n) => t + ($"\nMultiply historical Lightning channels by {n}.")),
		R("BeatIntoShape", "CalculationBase", (string t, int n) => t + ($"\nInitial Forge: {n}.")),
		R("Sacrifice", "CalculationExtra", (string t, int n) => Sub(t, "equal to triple", (Match _) => ($"equal to {n} times"))),
		R("Sacrifice", "CalculationBase", (string t, int n) => t + ($"\nGain {n} additional Block.")),
		R("Adrenaline", "Cards", Draw),
		R("Backflip", "Cards", Draw),
		R("Finesse", "Cards", Draw),
		R("FlashOfSteel", "Cards", Draw),
		R("Slimed", "Cards", Draw),
		R("Ftl", "Cards", Draw),
		R("CompileDriver", "CalculationExtra", (string t, int n) => (Sub(t, "Draw \\d+ cards? for each", (Match _) => $"Draw {n} {Plural(n, "card", "cards")} for each"))),
		R("BodySlam", "ExtraDamage", EqualTo),
		R("GoldAxe", "ExtraDamage", EqualTo),
		R("MindBlast", "ExtraDamage", EqualTo),
		R("TimesUp", "ExtraDamage", EqualTo),
		R("Protector", "ExtraDamage", EqualTo),
		R("Unleash", "ExtraDamage", EqualTo),
		R("Mirage", "CalculationExtra", EqualTo),
		R("Mimic", "CalculationExtra", EqualTo),
		R("DemonicShield", "CalculationExtra", EqualTo),
		R("Stack", "CalculationExtra", EqualTo),
		R("Barrage", "CalculationExtra", HitsForEach),
		R("Finisher", "CalculationExtra", HitsForEach),
		R("FlakCannon", "CalculationExtra", HitsForEach),
		R("Flechettes", "CalculationExtra", HitsForEach),
		R("HelixDrill", "CalculationExtra", HitsForEach),
		R("LunarBlast", "CalculationExtra", HitsForEach),
		R("PullFromBelow", "CalculationExtra", HitsForEach),
		R("Radiate", "CalculationExtra", HitsForEach),
		R("Rattle", "CalculationExtra", (string t, int n) => (Sub(t, "Hits an additional time for each", (Match _) => (n != 1) ? $"Hits {n} additional times for each" : "Hits an additional time for each"))),
		R("TearAsunder", "CalculationExtra", (string t, int n) => (Sub(t, "Hits an additional time for each", (Match _) => (n != 1) ? $"Hits {n} additional times for each" : "Hits an additional time for each"))),
		R("EchoForm", "EchoForm", (string t, int n) => (n != 1) ? (Sub(t, "The first card you play each turn is played", (Match _) => $"The first {n} cards you play each turn are played")) : t),
		R("SignalBoost", "SignalBoostPower", (string t, int n) => (n != 1) ? (Sub(t, "The next Power you play is played", (Match _) => $"The next {n} Powers you play are played")) : t),
		R("CreativeAi", "CreativeAi", (string t, int n) => (n != 1) ? (Sub(t, "add a random Power", (Match _) => $"add {n} random Powers")) : t),
		R("FightMe", "HitCount", (string t, int n) => (Sub(t, "damage twice", (Match _) => (n != 1) ? $"damage {n} times" : "damage"))),
		R("HeirloomHammer", "Repeat", (string t, int n) => (n != 1) ? (Sub(t, "Add a copy of that card", (Match _) => $"Add {n} copies of that card")) : t),
		R("DualWield", "Cards", (string t, int n) => (n != 1) ? (Sub(t, "Add (?:a copy|\\d+ copies)", (Match _) => $"Add {n} copies")) : t),
		R("GraveWarden", "Cards", (string t, int n) => (n != 1) ? (Sub(t, "Add a \\[gold\\]Soul\\[/gold\\]", (Match _) => $"Add {n} [gold]Souls[/gold]")) : t),
		R("Reave", "Cards", (string t, int n) => (n != 1) ? (Sub(t, "Add a \\[gold\\]Soul(\\+?)\\[/gold\\]", (Match m) => $"Add {n} [gold]Souls{m.Groups[1].Value}[/gold]")) : t),
		R("Loop", "Loop", (string t, int n) => (Sub(t, "rightmost Orb(?:(?:\\s|\\[/?(?:green|blue)\\])*\\d+(?:\\s|\\[/?(?:green|blue)\\])*times(?:\\[/(?:green|blue)\\])?)?(?=\\.)", (Match _) => (n != 1) ? $"rightmost Orb {n} times" : "rightmost Orb"))),
		R("Spinner", "SpinnerPower", (string t, int n) => (Sub(t, "(At the start of your turn, \\[gold\\]Channel\\[/gold\\]) \\d+", (Match m) => $"{m.Groups[1].Value} {n}"))),
		R("Knockdown", "KnockdownPower", delegate(string t, int n)
		{
			string word = Times(n);
			return Sub(t, "takes (?:double|triple) damage", (Match _) => (word == null) ? $"takes {n} times the damage" : ("takes " + word + " damage"));
		}),
		R("MeteorShower", "VulnerablePower", (string t, int n) => (Sub(t, " and \\[gold\\]Vulnerable", (Match _) => $" and {n} [gold]Vulnerable")))
	};

	private static readonly Dictionary<string, List<Rule>> ByCard = (from r in Rules
		group r by r.Card).ToDictionary((IGrouping<string, Rule> g) => g.Key, (IGrouping<string, Rule> g) => g.ToList());

	private static Rule R(string card, string var, Fix apply)
	{
		return new Rule
		{
			Card = card,
			Var = var,
			Apply = apply
		};
	}

	private static string Sub(string text, string pattern, MatchEvaluator evaluator)
	{
		return Regex.Replace(text, pattern, evaluator, RegexOptions.None, TimeSpan.FromMilliseconds(50L, 0L));
	}

	private static string Plural(int n, string one, string many)
	{
		if (n != 1)
		{
			return many;
		}
		return one;
	}

	private static string Times(int n)
	{
		string[] array = new string[6] { null, null, "double", "triple", "quadruple", "quintuple" };
		if (n < 2 || n > 5)
		{
			return null;
		}
		return array[n];
	}

	private static string Draw(string text, int n)
	{
		return Sub(text, "\\b([Dd]raw) \\d+ cards?(?! for each)", (Match m) => $"{m.Groups[1].Value} {n} {Plural(n, "card", "cards")}");
	}

	private static string EqualTo(string text, int n)
	{
		if (n == 1)
		{
			return text;
		}
		string word = Times(n);
		return Sub(text, "\\bequal to ", (Match _) => (word == null) ? $"equal to {n} times " : ("equal to " + word + " "));
	}

	private static string HitsForEach(string text, int n)
	{
		if (n == 1)
		{
			return text;
		}
		return Sub(text, "( damage(?: to ALL enemies| to a random enemy)?) for each", (Match m) => $"{m.Groups[1].Value} {n} times for each");
	}

	internal static string Rewrite(CardModel card, string text)
	{
		if (card == null || string.IsNullOrEmpty(text))
		{
			return text;
		}
		text = FixedEffectPatches.Rewrite(card, text);
		text = ExpandedEffectPatches.Rewrite(card, text);
		text = AdvancedEnchantmentUpgrades.Rewrite(card, text);
		CardMachineState cardMachineState = CardStateStore.Get(card);
		if (cardMachineState != null && CardAttributes.HiddenCalculationCards.Contains(card.GetType().Name) && cardMachineState.RawDelta(AttributeKeys.Var("CalculationBase")) != 0)
		{
			text = text + "\n" + CardAttributes.CalculationLabel(card) + ": " + card.DynamicVars["CalculationBase"].IntValue + ".";
		}
		if (!ByCard.TryGetValue(card.GetType().Name, out var value))
		{
			return text;
		}
		if (cardMachineState == null)
		{
			return text;
		}
		foreach (Rule item in value)
		{
			if ((cardMachineState.RawDelta(AttributeKeys.Var(item.Var)) != 0 || cardMachineState.RawDelta(AttributeKeys.VarDown(item.Var)) != 0) && card.DynamicVars.TryGetValue(item.Var, out DynamicVar value2))
			{
				text = item.Apply(text, value2.IntValue);
			}
		}
		return text;
	}
}
