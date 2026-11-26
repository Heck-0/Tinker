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
/// tools/tests checks the engine's results (tools/tests/data/rules.tsv); tools/rulecheck shows them.
/// </summary>
internal static class VarDirections
{
	private static readonly Dictionary<(string Card, string Var), (int Sign, string Why)> Overrides = new Dictionary<(string, string), (int, string)>
	{
		[("Blur", "Blur")] = (1, "turns Block is kept; expanded text"),
		[("BrightenUp", "Sadness")] = (-1, "the Jester: Sadness added (unplayable status); only lowering is offered"),
		[("BulkUp", "OrbSlots")] = (-1, "orb slots lost"),
		[("Colossus", "Colossus")] = (1, "duration, not a percentage; expanded text"),
		[("Corruption", "Power")] = (1, "rules can't tell (unknown)"),
		[("CreativeAi", "CreativeAi")] = (1, "rules can't tell (unknown)"),
		[("DanseMacabre", "Energy")] = (-1, "the power's effective threshold; minimum 1"),
		[("Debt", "Gold")] = (-1, "gold lost"),
		[("Demesne", "Energy")] = (1, "separate extra Energy, keeping the old combined bonus"),
		[("Dominate", "StrengthPerVulnerable")] = (1, "effective multiplier when applying Strength"),
		[("EchoForm", "EchoForm")] = (1, "rules can't tell (unknown)"),
		[("Encore", "EncorePower")] = (1, "the Jester (v0.113)"),
		[("Equilibrium", "Equilibrium")] = (1, "turns retained; expanded text"),
		[("FightMe", "EnemyStrength")] = (-1, "rules can't tell (unknown)"),
		[("GeneticAlgorithm", "Block")] = (0, "rewrites its Block when played"),
		[("Glimmer", "PutBack")] = (-1, "required put-backs, separate from the draw"),
		[("HeavenlyDrill", "Energy")] = (-1, "Energy threshold for doubling X"),
		[("HeavyShot", "AimMultiplier")] = (1, "the Jester (v0.113)"),
		[("HiddenDaggers", "Cards")] = (-1, "required discards; Shivs is a separate number"),
		[("HiddenDaggers", "Shivs")] = (1, "rules can't tell (unknown)"),
		[("Hotfix", "FocusPower")] = (1, "rules can't tell (power (target unclear))"),
		[("Hyperbeam", "FocusPower")] = (1, "rules can't tell (power (target unclear))"),
		[("Jest", "Cards")] = (-1, "the Jester: Sadness added (unplayable status); only lowering is offered"),
		[("MadScience", "ChokingDamage")] = (1, "rules can't tell (power (target unclear))"),
		[("MadScience", "CuriousReduction")] = (1, "rules can't tell (unknown)"),
		[("MadScience", "SappingVulnerable")] = (1, "rules can't tell (power (target unclear))"),
		[("MadScience", "SappingWeak")] = (1, "rules can't tell (power (target unclear))"),
		[("MonarchsGaze", "StrengthLoss")] = (1, "rules can't tell (unknown)"),
		[("Monologue", "Power")] = (1, "Strength per card played"),
		[("NoEscape", "DoomThreshold")] = (-1, "Doom divisor; minimum 1"),
		[("PactsEnd", "Cards")] = (-1, "Exhausts required to attack"),
		[("PaleBlueDot", "CardPlay")] = (-1, "the power's effective threshold; minimum 1"),
		[("PanicButton", "Turns")] = (-1, "turns without gaining Block; minimum 1"),
		[("PhotonCut", "PutBack")] = (-1, "required put-backs, separate from the draw and damage"),
		[("RightHandHand", "Energy")] = (-1, "cost threshold for retrieving the card"),
		[("RollingBoulder", "IncrementAmount")] = (1, "sets the created power's DamageVar"),
		[("SelfDeprecate", "Sadness")] = (-1, "the Jester: Sadness added (unplayable status); only lowering is offered"),
		[("Shadowmeld", "Power")] = (0, "the text says \"double\""),
		[("Shame", "Frail")] = (-1, "rules can't tell (unknown)"),
		[("SharedFate", "PlayerStrengthLoss")] = (-1, "rules can't tell (unknown)"),
		[("ShowOff", "BaseAim")] = (1, "the Jester (v0.113)"),
		[("Sloth", "SlothPower")] = (1, "rules say -1 (power (debuff on self))"),
		[("SorrowfulStrike", "Cards")] = (-1, "the Jester: Sadness added (unplayable status); only lowering is offered"),
		[("SovereignBlade", "Damage")] = (0, "rewrites its damage when played"),
		[("Stampede", "Power")] = (1, "rules can't tell (unknown)"),
		[("TheBomb", "Turns")] = (-1, "turns until it explodes; minimum 1"),
		[("TheScythe", "Damage")] = (0, "rewrites its damage when played"),
		[("ToricToughness", "Turns")] = (1, "rules can't tell (unknown)"),
		[("Transfigure", "Energy")] = (-1, "effective surcharge in AddThisCombat"),
		[("WellLaidPlans", "RetainAmount")] = (1, "amount chosen to retain on stable; the beta retains the whole hand"),
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
