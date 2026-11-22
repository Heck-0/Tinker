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
	private delegate string Fix(string text, int value, bool spanish);

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
				ModEntry.Error("No se pudo reescribir el texto de " + (__instance?.Id)?.ToString() + ": " + ex.Message);
			}
		}
	}

	private static readonly List<Rule> Rules = new List<Rule>
	{
		R("Blur", "Blur", (string t, int n, bool es) => Sub(t, es ? "al comienzo de tu siguiente turno" : "at the start of your next turn", (Match _) => es ? $"al comienzo de tus próximos {n} turnos" : $"at the start of your next {n} turns")),
		R("Equilibrium", "Equilibrium", (string t, int n, bool es) => Sub(t, es ? "este turno" : "this turn", (Match _) => es ? $"durante {n} turnos" : $"for {n} turns")),
		R("Colossus", "Colossus", (string t, int n, bool es) => Sub(t, es ? "durante este turno" : "this turn", (Match _) => es ? $"durante {n} turnos" : $"for {n} turns")),
		R("ConsumingShadow", "ConsumingShadowPower", (string t, int n, bool es) => Sub(t, es ? "el Orbe situado más a la izquierda" : "your leftmost Orb", (Match _) => es ? $"los {n} Orbes situados más a la izquierda" : $"your {n} leftmost Orbs")),
		R("Seance", "Cards", (string t, int n, bool es) => Sub(t, es ? "Transforma una carta" : "Transform a card", (Match _) => es ? $"Transforma {n} cartas" : $"Transform {n} cards")),
		R("Voltaic", "CalculationBase", (string t, int n, bool es) => t + (es ? $"\nInvoca {n} Relámpagos adicionales." : $"\nChannel {n} additional Lightning Orbs.")),
		R("Voltaic", "CalculationExtra", (string t, int n, bool es) => t + (es ? $"\nMultiplica los Relámpagos del historial por {n}." : $"\nMultiply historical Lightning channels by {n}.")),
		R("BeatIntoShape", "CalculationBase", (string t, int n, bool es) => t + (es ? $"\nForja inicial: {n}." : $"\nInitial Forge: {n}.")),
		R("Sacrifice", "CalculationExtra", (string t, int n, bool es) => Sub(t, es ? "igual al triple de" : "equal to triple", (Match _) => es ? $"igual a {n} veces" : $"equal to {n} times")),
		R("Sacrifice", "CalculationBase", (string t, int n, bool es) => t + (es ? $"\nGana {n} Bloqueo adicional." : $"\nGain {n} additional Block.")),
		R("Adrenaline", "Cards", Draw),
		R("Backflip", "Cards", Draw),
		R("Finesse", "Cards", Draw),
		R("FlashOfSteel", "Cards", Draw),
		R("Slimed", "Cards", Draw),
		R("Ftl", "Cards", Draw),
		R("CompileDriver", "CalculationExtra", (string t, int n, bool es) => (!es) ? Sub(t, "Draw \\d+ cards? for each", (Match _) => $"Draw {n} {Plural(n, "card", "cards")} for each") : Sub(t, "Roba \\d+ cartas? por cada", (Match _) => $"Roba {n} {Plural(n, "carta", "cartas")} por cada")),
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
		R("Rattle", "CalculationExtra", (string t, int n, bool es) => (!es) ? Sub(t, "Hits an additional time for each", (Match _) => (n != 1) ? $"Hits {n} additional times for each" : "Hits an additional time for each") : Sub(t, "Golpea una vez más por cada", (Match _) => (n != 1) ? $"Golpea {n} veces más por cada" : "Golpea una vez más por cada")),
		R("TearAsunder", "CalculationExtra", (string t, int n, bool es) => (!es) ? Sub(t, "Hits an additional time for each", (Match _) => (n != 1) ? $"Hits {n} additional times for each" : "Hits an additional time for each") : Sub(t, "Golpea una vez adicional por cada", (Match _) => (n != 1) ? $"Golpea {n} veces adicionales por cada" : "Golpea una vez adicional por cada")),
		R("EchoForm", "EchoForm", (string t, int n, bool es) => (n != 1) ? ((!es) ? Sub(t, "The first card you play each turn is played", (Match _) => $"The first {n} cards you play each turn are played") : Sub(t, "La primera carta que juegues cada turno se juega", (Match _) => $"Las primeras {n} cartas que juegues cada turno se juegan")) : t),
		R("SignalBoost", "SignalBoostPower", (string t, int n, bool es) => (n != 1) ? ((!es) ? Sub(t, "The next Power you play is played", (Match _) => $"The next {n} Powers you play are played") : Sub(t, "El siguiente Poder que juegues se jugará", (Match _) => $"Los siguientes {n} Poderes que juegues se jugarán")) : t),
		R("CreativeAi", "CreativeAi", (string t, int n, bool es) => (n != 1) ? ((!es) ? Sub(t, "add a random Power", (Match _) => $"add {n} random Powers") : Sub(t, "añade un Poder aleatorio", (Match _) => $"añade {n} Poderes aleatorios")) : t),
		R("FightMe", "HitCount", (string t, int n, bool es) => (!es) ? Sub(t, "damage twice", (Match _) => (n != 1) ? $"damage {n} times" : "damage") : Sub(t, "de daño dos veces", (Match _) => $"de daño {n} {Plural(n, "vez", "veces")}")),
		R("HeirloomHammer", "Repeat", (string t, int n, bool es) => (n != 1) ? ((!es) ? Sub(t, "Add a copy of that card", (Match _) => $"Add {n} copies of that card") : Sub(t, "Añade una copia de esa carta", (Match _) => $"Añade {n} copias de esa carta")) : t),
		R("DualWield", "Cards", (string t, int n, bool es) => (n != 1) ? ((!es) ? Sub(t, "Add (?:a copy|\\d+ copies)", (Match _) => $"Add {n} copies") : Sub(t, "Añade (?:una copia|\\d+ copias)", (Match _) => $"Añade {n} copias")) : t),
		R("GraveWarden", "Cards", (string t, int n, bool es) => (n != 1) ? ((!es) ? Sub(t, "Add a \\[gold\\]Soul\\[/gold\\]", (Match _) => $"Add {n} [gold]Souls[/gold]") : Sub(t, "Añade un \\[gold\\]Alma\\[/gold\\]", (Match _) => $"Añade {n} [gold]Almas[/gold]")) : t),
		R("Reave", "Cards", (string t, int n, bool es) => (n != 1) ? ((!es) ? Sub(t, "Add a \\[gold\\]Soul(\\+?)\\[/gold\\]", (Match m) => $"Add {n} [gold]Souls{m.Groups[1].Value}[/gold]") : Sub(t, "Añade un \\[gold\\]Alma(\\+?)\\[/gold\\]", (Match m) => $"Añade {n} [gold]Almas{m.Groups[1].Value}[/gold]")) : t),
		R("Loop", "Loop", (string t, int n, bool es) => (!es) ? Sub(t, "rightmost Orb(?:(?:\\s|\\[/?(?:green|blue)\\])*\\d+(?:\\s|\\[/?(?:green|blue)\\])*times(?:\\[/(?:green|blue)\\])?)?(?=\\.)", (Match _) => (n != 1) ? $"rightmost Orb {n} times" : "rightmost Orb") : Sub(t, "más a la derecha(?:(?:\\s|\\[/?(?:green|blue)\\])*\\d+(?:\\s|\\[/?(?:green|blue)\\])*veces(?:\\[/(?:green|blue)\\])?)?(?=\\.)", (Match _) => (n != 1) ? $"más a la derecha {n} veces" : "más a la derecha")),
		R("Spinner", "SpinnerPower", (string t, int n, bool es) => (!es) ? Sub(t, "(At the start of your turn, \\[gold\\]Channel\\[/gold\\]) \\d+", (Match m) => $"{m.Groups[1].Value} {n}") : Sub(t, "(Al comienzo de tu turno, \\[gold\\]Invoca\\[/gold\\]) \\d+", (Match m) => $"{m.Groups[1].Value} {n}")),
		R("Knockdown", "KnockdownPower", delegate(string t, int n, bool es)
		{
			string word = Times(n, es);
			return (!es) ? Sub(t, "takes (?:double|triple) damage", (Match _) => (word == null) ? $"takes {n} times the damage" : ("takes " + word + " damage")) : Sub(t, "recibe el (?:doble|triple) de daño", (Match _) => (word == null) ? $"recibe {n} veces el daño" : ("recibe el " + word + " de daño"));
		}),
		R("MeteorShower", "VulnerablePower", (string t, int n, bool es) => (!es) ? Sub(t, " and \\[gold\\]Vulnerable", (Match _) => $" and {n} [gold]Vulnerable") : Sub(t, " y \\[gold\\]Vulnerabilidad", (Match _) => $" y {n} de [gold]Vulnerabilidad"))
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

	private static string Times(int n, bool spanish)
	{
		string[] array = new string[6] { null, null, "double", "triple", "quadruple", "quintuple" };
		string[] array2 = new string[6] { null, null, "doble", "triple", "cuádruple", "quíntuple" };
		if (n < 2 || n > 5)
		{
			return null;
		}
		if (!spanish)
		{
			return array[n];
		}
		return array2[n];
	}

	private static string Draw(string text, int n, bool es)
	{
		if (!es)
		{
			return Sub(text, "\\b([Dd]raw) \\d+ cards?(?! for each)", (Match m) => $"{m.Groups[1].Value} {n} {Plural(n, "card", "cards")}");
		}
		return Sub(text, "\\b([Rr]oba) \\d+ cartas?(?! por cada)", (Match m) => $"{m.Groups[1].Value} {n} {Plural(n, "carta", "cartas")}");
	}

	private static string EqualTo(string text, int n, bool es)
	{
		if (n == 1)
		{
			return text;
		}
		string word = Times(n, es);
		if (!es)
		{
			return Sub(text, "\\bequal to ", (Match _) => (word == null) ? $"equal to {n} times " : ("equal to " + word + " "));
		}
		return Sub(text, "\\bigual (a tu|al|a la|a los) ", delegate(Match m)
		{
			string value = m.Groups[1].Value;
			string value2 = value switch
			{
				"a la" => "de la", 
				"al" => "del", 
				"a tu" => "de tu", 
				_ => "de los", 
			};
			string value3 = value switch
			{
				"a la" => "la", 
				"al" => "el", 
				"a tu" => "tu", 
				_ => "los", 
			};
			return (word != null) ? $"igual al {word} {value2} " : $"igual a {n} veces {value3} ";
		});
	}

	private static string HitsForEach(string text, int n, bool es)
	{
		if (n == 1)
		{
			return text;
		}
		if (!es)
		{
			return Sub(text, "( damage(?: to ALL enemies| to a random enemy)?) for each", (Match m) => $"{m.Groups[1].Value} {n} times for each");
		}
		return Sub(text, "(de daño(?: a TODOS los enemigos| a un enemigo aleatorio)?) por cada", (Match m) => $"{m.Groups[1].Value} {n} veces por cada");
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
		bool spanish = Lines.Spanish;
		foreach (Rule item in value)
		{
			if ((cardMachineState.RawDelta(AttributeKeys.Var(item.Var)) != 0 || cardMachineState.RawDelta(AttributeKeys.VarDown(item.Var)) != 0) && card.DynamicVars.TryGetValue(item.Var, out DynamicVar value2))
			{
				text = item.Apply(text, value2.IntValue, spanish);
			}
		}
		return text;
	}
}
