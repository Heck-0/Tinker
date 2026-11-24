using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Tinker.Common.Rules;

namespace Tinker.Card;

/// <summary>
/// Which way each card value is upgraded: +1 higher is better, -1 lower is better, 0 not upgradable.
/// The rule engine decides from the card's own code (<see cref="GameRules"/>); <see cref="Overrides"/> holds the
/// values where the mod knows better: the rules can't tell (unknown, a cost vs. a limit), get it wrong (a cap
/// that looks like a debuff), or the value is upgraded through the mod's own effect code.
/// tools/divergence.py and the rulecheck tool compare the engine with these.
/// </summary>
internal static class VarDirections
{
	private static readonly Dictionary<(string Card, string Var), (int Sign, string Why)> Overrides = new Dictionary<(string, string), (int, string)>
	{
		[("Blur", "Blur")] = (1, "turnos de conservar Bloqueo; texto ampliado"),
		[("BrightenUp", "Sadness")] = (-1, "el Bufón: Tristeza añadida (estado injugable); solo se ofrece bajar"),
		[("BulkUp", "OrbSlots")] = (-1, "huecos de orbe que se pierden"),
		[("Colossus", "Colossus")] = (1, "duración, no porcentaje; texto ampliado"),
		[("Corruption", "Power")] = (1, "rules can't tell (unknown)"),
		[("CreativeAi", "CreativeAi")] = (1, "rules can't tell (unknown)"),
		[("DanseMacabre", "Energy")] = (-1, "umbral efectivo del poder; mínimo 1"),
		[("Debt", "Gold")] = (-1, "oro que se pierde"),
		[("Demesne", "Energy")] = (1, "energía adicional independiente, manteniendo el bonus conjunto antiguo"),
		[("Dominate", "StrengthPerVulnerable")] = (1, "multiplicador efectivo al aplicar Fuerza"),
		[("EchoForm", "EchoForm")] = (1, "rules can't tell (unknown)"),
		[("Encore", "EncorePower")] = (1, "el Bufón (v0.113)"),
		[("Equilibrium", "Equilibrium")] = (1, "turnos de retención; texto ampliado"),
		[("FightMe", "EnemyStrength")] = (-1, "rules can't tell (unknown)"),
		[("GeneticAlgorithm", "Block")] = (0, "reescribe su bloqueo al jugarse"),
		[("Glimmer", "PutBack")] = (-1, "devoluciones obligatorias independientes del robo"),
		[("HeavenlyDrill", "Energy")] = (-1, "umbral de energía para duplicar X"),
		[("HeavyShot", "AimMultiplier")] = (1, "el Bufón (v0.113)"),
		[("HiddenDaggers", "Cards")] = (-1, "descartes obligatorios; Shivs es un número independiente"),
		[("HiddenDaggers", "Shivs")] = (1, "rules can't tell (unknown)"),
		[("Hotfix", "FocusPower")] = (1, "rules can't tell (power (target unclear))"),
		[("Hyperbeam", "FocusPower")] = (1, "rules can't tell (power (target unclear))"),
		[("Jest", "Cards")] = (-1, "el Bufón: Tristeza añadida (estado injugable); solo se ofrece bajar"),
		[("MadScience", "ChokingDamage")] = (1, "rules can't tell (power (target unclear))"),
		[("MadScience", "CuriousReduction")] = (1, "rules can't tell (unknown)"),
		[("MadScience", "SappingVulnerable")] = (1, "rules can't tell (power (target unclear))"),
		[("MadScience", "SappingWeak")] = (1, "rules can't tell (power (target unclear))"),
		[("MonarchsGaze", "StrengthLoss")] = (1, "rules can't tell (unknown)"),
		[("Monologue", "Power")] = (1, "Fuerza por carta jugada"),
		[("NoEscape", "DoomThreshold")] = (-1, "divisor de Destino; mínimo 1"),
		[("PactsEnd", "Cards")] = (-1, "agotamientos requeridos para atacar"),
		[("PaleBlueDot", "CardPlay")] = (-1, "umbral efectivo del poder; mínimo 1"),
		[("PanicButton", "Turns")] = (-1, "turnos sin ganar Bloqueo; mínimo 1"),
		[("PhotonCut", "PutBack")] = (-1, "devoluciones obligatorias independientes del robo y daño"),
		[("RightHandHand", "Energy")] = (-1, "umbral de coste para recuperar la carta"),
		[("RollingBoulder", "IncrementAmount")] = (1, "configura DamageVar del poder instanciado"),
		[("SelfDeprecate", "Sadness")] = (-1, "el Bufón: Tristeza añadida (estado injugable); solo se ofrece bajar"),
		[("Shadowmeld", "Power")] = (0, "el texto dice «duplica»"),
		[("Shame", "Frail")] = (-1, "rules can't tell (unknown)"),
		[("SharedFate", "PlayerStrengthLoss")] = (-1, "rules can't tell (unknown)"),
		[("ShowOff", "BaseAim")] = (1, "el Bufón (v0.113)"),
		[("Sloth", "SlothPower")] = (1, "rules say -1 (power (debuff on self))"),
		[("SorrowfulStrike", "Cards")] = (-1, "el Bufón: Tristeza añadida (estado injugable); solo se ofrece bajar"),
		[("SovereignBlade", "Damage")] = (0, "reescribe su daño al jugarse"),
		[("Stampede", "Power")] = (1, "rules can't tell (unknown)"),
		[("TheBomb", "Turns")] = (-1, "turnos hasta que explota; mínimo 1"),
		[("TheScythe", "Damage")] = (0, "reescribe su daño al jugarse"),
		[("ToricToughness", "Turns")] = (1, "rules can't tell (unknown)"),
		[("Transfigure", "Energy")] = (-1, "sobrecoste efectivo en AddThisCombat"),
		[("WellLaidPlans", "RetainAmount")] = (1, "cantidad elegida para retener en la estable; la beta retiene toda la mano"),
		// Tank's values are only shown on the card; SteppedValues copies them onto TankPower / GuardedPower.
		[("Tank", "DamageIncrease")] = (-1, "damage you take (×1.5); fractional, see SteppedValues"),
		[("Tank", "DamageDecrease")] = (-1, "damage your allies take (×0.5); fractional, see SteppedValues")
	};

	private static readonly Dictionary<string, int> Hits = new Dictionary<string, int>
	{
		["AstralPulse"] = 2,
		["CelestialMight"] = -1,
		["Conflagration"] = -1,
		["DaggerSpray"] = 2,
		["Exterminate"] = -1,
		["FightMe"] = -1,
		["GunkUp"] = -1,
		["Maul"] = 2,
		["Peck"] = -1,
		["Refract"] = 2,
		["Ricochet"] = -1,
		["RipAndTear"] = 2,
		["SevenStars"] = -1,
		["SovereignBlade"] = -1,
		["SwordBoomerang"] = -1,
		["Thrash"] = 2,
		["TwinStrike"] = 2,
		["Uproar"] = 2,
		["RapidShot"] = -1,
		["WahKi"] = -1
	};

	internal static int SignFor(CardModel card, string varKey)
	{
		if (card == null || string.IsNullOrEmpty(varKey))
		{
			return 0;
		}
		string name = card.GetType().Name;
		if (ExpandedEffects.For(name).Any((ExpandedEffect e) => AttributeKeys.VarName(e.Attribute) == varKey) && !ExpandedEffectPatches.Enabled(card))
		{
			return 0;
		}
		if (Overrides.TryGetValue((name, varKey), out (int Sign, string Why) value))
		{
			return value.Sign;
		}
		return GameRules.Decide(card, varKey).Sign;
	}

	internal static int HitsFor(CardModel card)
	{
		if (card != null && ExpandedEffectPatches.Enabled(card) && ExpandedEffects.Find(card.GetType().Name, "extra:x")?.Family == "repeat")
		{
			return checked(3 + (CardStateStore.Get(card)?.RawDelta("extra:x") ?? 0));
		}
		if (card != null && ExpandedEffectPatches.Enabled(card) && ExpandedEffects.Find(card.GetType().Name, "extra:hits") != null)
		{
			return ExpandedEffects.Value(card.GetType().Name, "extra:hits", card.IsUpgraded, CardStateStore.Get(card));
		}
		if (card == null || !Hits.TryGetValue(card.GetType().Name, out var value))
		{
			return 1;
		}
		// The card's own hit count when it has one (it can be upgraded), else the table's fixed number.
		if (card.DynamicVars.TryGetValue("HitCount", out DynamicVar hits) || card.DynamicVars.TryGetValue("Repeat", out hits))
		{
			return Math.Max(1, hits.IntValue);
		}
		return Math.Max(1, value);
	}

}
