using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Tinker.Common;

namespace Tinker.Card;

public static class CardAttributes
{
	internal static readonly HashSet<string> HiddenCalculationCards = new HashSet<string>(StringComparer.Ordinal)
	{
		"BodySlam", "GoldAxe", "MindBlast", "TimesUp", "Mimic", "Mirage", "DemonicShield", "Barrage", "Finisher", "FlakCannon",
		"Flechettes", "HelixDrill", "LunarBlast", "PullFromBelow", "Radiate", "Rattle", "TearAsunder", "CompileDriver", "Synchronize"
	};

	private static readonly HashSet<string> Buffs = new HashSet<string>(StringComparer.Ordinal)
	{
		"StrengthPower", "DexterityPower", "FocusPower", "VigorPower", "PlatingPower", "ThornsPower", "ArtifactPower", "BufferPower", "IntangiblePower", "MetallicizePower",
		"RegenPower", "RitualPower"
	};

	private static readonly HashSet<string> Debuffs = new HashSet<string>(StringComparer.Ordinal) { "VulnerablePower", "WeakPower", "PoisonPower", "DoomPower", "StranglePower", "SlowPower" };

	private static readonly HashSet<string> AlwaysGood = new HashSet<string>(StringComparer.Ordinal) { "RepeatVar", "HitCountVar", "HealVar", "StarsVar", "SummonVar", "ForgeVar" };

	internal static string CalculationFamily(CardModel card)
	{
		if (card.DynamicVars.ContainsKey("CalculatedHits"))
		{
			return "repeat";
		}
		if (card.DynamicVars.ContainsKey("CalculatedCards"))
		{
			return "draw";
		}
		if (card.DynamicVars.ContainsKey("CalculatedFocus"))
		{
			return "scaling";
		}
		if (card.DynamicVars.ContainsKey("CalculatedBlock"))
		{
			return "block";
		}
		return "damage";
	}

	internal static string CalculationLabel(CardModel card)
	{
		return CalculationFamily(card) switch
		{
			"repeat" => Lang.T("Additional hits"), 
			"draw" => Lang.T("Additional draw"), 
			"scaling" => Lang.T("Additional Focus"), 
			"block" => Lang.T("Additional flat Block"), 
			_ => Lang.T("Additional flat damage"), 
		};
	}

	private static bool HasRewrittenBaseValue(CardModel card)
	{
		if (!(card is GeneticAlgorithm) && !(card is TheScythe))
		{
			return card is SovereignBlade;
		}
		return true;
	}

	public static IEnumerable<string> SupportedFor(CardModel card)
	{
		foreach (string baseAttribute in BaseAttributes(card))
		{
			yield return baseAttribute;
		}
	}

	public static IEnumerable<string> BaseAttributes(CardModel card)
	{
		List<string> list = OneWayBaseAttributes(card).ToList();
		// A value that can go either way (e.g. a discard count) replaces its one-way entries with the
		// direction(s) it still has room for. Effects that need the expanded-effect patch need it active.
		foreach (BidirectionalEffect pair in BidirectionalEffects.For(card.GetType().Name))
		{
			bool applies;
			if (!AttributeKeys.IsVar(pair.Canonical))
			{
				applies = ExpandedEffectPatches.Enabled(card);
			}
			else if (!card.DynamicVars.ContainsKey(AttributeKeys.VarName(pair.Canonical)))
			{
				continue;
			}
			else
			{
				applies = ExpandedEffects.Find(pair.Card, pair.Canonical) == null || ExpandedEffectPatches.Enabled(card);
			}
			if (!applies)
			{
				continue;
			}
			list.Remove(pair.Canonical);
			list.Remove(pair.Reverse);
			int current = CurrentValue(card, pair.Canonical);
			if (current > Minimum(card, pair.Down))
			{
				list.Add(pair.Down);
			}
			if (current < Maximum(card, pair.Up))
			{
				list.Add(pair.Up);
			}
		}
		return list.Distinct();
	}

	private static IEnumerable<string> OneWayBaseAttributes(CardModel card)
	{
		string text = EnchantmentUpgrades.Attribute(card);
		if (text != null)
		{
			yield return text;
		}
		foreach (string item in AdvancedEnchantmentUpgrades.For(card))
		{
			yield return item;
		}
		if (HasRewrittenBaseValue(card))
		{
			if (card is GeneticAlgorithm || card is TheScythe)
			{
				yield return AttributeKeys.Var("Increase");
				yield return AttributeKeys.Var((card is GeneticAlgorithm) ? "Block" : "Damage");
			}
			if (!card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0)
			{
				yield return "cost";
			}
			yield break;
		}
		if (FixedEffectPatches.Enabled(card))
		{
			foreach (string effect in FixedEffects.For(card.GetType().Name))
			{
				yield return effect;
			}
		}
		if (ExpandedEffectPatches.Enabled(card))
		{
			foreach (ExpandedEffect expandedEffect in from e in ExpandedEffects.For(card.GetType().Name)
				group e by e.Attribute into g
				select g.First())
			{
				if (!AttributeKeys.IsVar(expandedEffect.Attribute) && (!(card is Armaments) || !card.IsUpgraded) && CurrentValue(card, expandedEffect.Attribute) < Maximum(card, expandedEffect.Attribute) && (AttributeDirection.SignOf(expandedEffect.Attribute) > 0 || CurrentValue(card, expandedEffect.Attribute) > Minimum(card, expandedEffect.Attribute)))
				{
					yield return expandedEffect.Attribute;
				}
			}
		}
		if (card.GetType().Namespace == typeof(Acrobatics).Namespace)
		{
			foreach (string key in card.DynamicVars.Keys)
			{
				int direction = VarDirections.SignFor(card, key);
				if (direction > 0)
				{
					yield return AttributeKeys.Var(key);
				}
				else if (direction < 0 && CurrentValue(card, AttributeKeys.VarDown(key)) > Minimum(card, AttributeKeys.VarDown(key)))
				{
					yield return AttributeKeys.VarDown(key);
				}
			}
		}
		else
		{
			if (card.Type == CardType.Attack && card.DynamicVars.TryGetValue("Damage", out DynamicVar damage) && damage.GetType() == typeof(DamageVar))
			{
				yield return AttributeKeys.Var("Damage");
			}
			if (card.GainsBlock && card.DynamicVars.TryGetValue("Block", out DynamicVar block) && block.GetType() == typeof(BlockVar))
			{
				yield return AttributeKeys.Var("Block");
			}
			foreach (string varName in card.DynamicVars.Keys)
			{
				if (ModdedVarImproves(card, card.DynamicVars[varName]))
				{
					yield return AttributeKeys.Var(varName);
				}
			}
		}
		if (!card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0)
		{
			yield return "cost";
		}
	}

	public static int Minimum(CardModel card, string attribute)
	{
		string varName = AttributeKeys.VarName(attribute);
		if (SteppedValues.TryGet(card, varName, out SteppedValues.Stepped stepped))
		{
			return stepped.MinUnits;
		}
		if (!(varName == "Turns") && !(varName == "DoomThreshold"))
		{
			switch (attribute)
			{
			case "extradown:threshold":
			case "extradown:quest":
			case "extradown:poisonThreshold":
				break;
			default:
			{
				string cardName = card?.GetType().Name;
				if ((cardName == "MakeItSo" && varName == "Cards") || ((cardName == "HeavenlyDrill" || cardName == "RightHandHand") && varName == "Energy"))
				{
					return 1;
				}
				if ((cardName == "PaleBlueDot" && varName == "CardPlay") || (cardName == "DanseMacabre" && varName == "Energy"))
				{
					return 1;
				}
				return 0;
			}
			}
		}
		return 1;
	}

	public static int Maximum(CardModel card, string attribute)
	{
		if (ExpandedEffects.Find(card?.GetType().Name, attribute)?.Kind == "chooseGenerated")
		{
			return CurrentValue(card, "extra:options");
		}
		return ExpandedEffects.Maximum(attribute);
	}

	/// <summary>
	/// A power amount's trigger tier (see <see cref="Tinker.Common.Rules.RuleEngine.HookTiers"/>): how often the power
	/// pays out its Amount, for a card's PowerVar value or one of the mod's "extra:power" effects (Dark Embrace's
	/// draw per Exhaust). Only for upgrades that raise the value.
	/// </summary>
	private static string TriggerFamily(CardModel card, string attribute)
	{
		if (card == null || attribute == null)
		{
			return null;
		}
		if (UpgradeKey.Parse(attribute).Kind == UpgradeKey.Kinds.Var && card.DynamicVars.TryGetValue(AttributeKeys.VarName(attribute), out DynamicVar value)
			&& value.GetType().IsGenericType && value.GetType().Name.StartsWith("PowerVar", StringComparison.Ordinal))
		{
			return Tinker.Common.Rules.GameRules.PowerTriggerFamily(value.GetType().GetGenericArguments()[0].Name);
		}
		ExpandedEffect effect = ExpandedEffects.Find(card.GetType().Name, attribute);
		if (effect != null && effect.Kind == "power" && effect.Family != null && effect.Family.StartsWith("persistent", StringComparison.Ordinal))
		{
			return Tinker.Common.Rules.GameRules.PowerTriggerFamily(effect.Target);
		}
		return null;
	}

	public static string RiskFamily(CardModel card, string attribute)
	{
		string text = card?.GetType().Name;
		string varName = AttributeKeys.VarName(attribute);
		if (SteppedValues.TryGet(card, varName, out SteppedValues.Stepped stepped))
		{
			return stepped.Family;
		}
		if (attribute == AttributeKeys.Var("IntangiblePower"))
		{
			return "intangible";
		}
		string trigger = TriggerFamily(card, attribute);
		if (trigger != null)
		{
			return trigger;
		}
		if (attribute == AttributeKeys.Var("AfterimagePower") || attribute == AttributeKeys.Var("MetallicizePower") || attribute == AttributeKeys.Var("PlatingPower"))
		{
			return "persistentDefense";
		}
		BidirectionalEffect bidirectionalEffect = BidirectionalEffects.Find(text, attribute);
		if (bidirectionalEffect != null)
		{
			return bidirectionalEffect.Family;
		}
		ExpandedEffect expandedEffect = ExpandedEffects.Find(text, attribute);
		if (varName == "CalculationBase" && HiddenCalculationCards.Contains(text))
		{
			return CalculationFamily(card);
		}
		if (text == "Tracking" && expandedEffect != null && ExpandedEffectPatches.TrackingUsesPercent)
		{
			return "percentage";
		}
		if (expandedEffect != null)
		{
			return expandedEffect.Family;
		}
		if (varName == "Increase" && (text == "GeneticAlgorithm" || text == "TheScythe"))
		{
			return "permanentGrowth";
		}
		if (attribute == "vardown:DoomThreshold" || (attribute == "vardown:Energy" && (text == "HeavenlyDrill" || text == "RightHandHand")) || (attribute == "vardown:Cards" && text == "MakeItSo"))
		{
			return "threshold";
		}
		if (varName == "Cards" && (text == "BeatDown" || text == "Catastrophe"))
		{
			return "autoPlay";
		}
		// The Fool (game v0.113): values whose default category by name would be too cheap or too strong.
		if ((text == "Honing" && varName == "HoningPower") || (text == "SerratedArrows" && varName == "SerratedArrowsPower") || (text == "DislikeYou" && varName == "DislikeYouPower"))
		{
			return "percentage";
		}
		if (text == "HeavyShot" && varName == "AimMultiplier")
		{
			return "multiplier";
		}
		if (text == "KiVerse" && varName == "KiVersePower")
		{
			return "repeat";
		}
		if ((text == "Motif" && varName == "MotifPower") || (text == "LostLute" && varName == "LostLutePower") || (text == "Simpleton" && varName == "SimpletonPower"))
		{
			return "persistentRepeat";
		}
		if (varName == "Energy" && (text == "Compost" || text == "Yah"))
		{
			return "persistentEnergy";
		}
		if (text == "Timber" && varName == "OnKillDamage")
		{
			return "damage";
		}
		if (text == "KiChant" && varName == "Cards")
		{
			return "selection";
		}
		if (text == "Quietus" && varName == "QuietusPower")
		{
			return "multiplier";
		}
		if (text == "Ensemble" && varName == "Cards")
		{
			return "energy";
		}
		if (text == "Encore" && varName == "EncorePower")
		{
			return "repeat";
		}
		if ((varName == "Joy" && (text == "BrightenUp" || text == "SelfDeprecate")) || (text == "Ecstasy" && varName == "Cards"))
		{
			return "generation";
		}
		if (varName == "Cards")
		{
			switch (text)
			{
			case "Dredge":
			case "Seance":
			case "Charge":
				return "selection";
			}
		}
		if (varName == "Cards" && (text == "GlimpseBeyond" || text == "Plot"))
		{
			return "groupResource";
		}
		if (varName == "Cards" && text == "Demesne")
		{
			return "persistentEnergy";
		}
		if (text == "Voltaic" && varName == "CalculationBase")
		{
			return "orb";
		}
		if (text == "Voltaic" && varName == "CalculationExtra")
		{
			return "multiplier";
		}
		if (text == "WellLaidPlans" && varName == "RetainAmount")
		{
			return "selection";
		}
		if (text == "Feral" && varName == "FeralPower")
		{
			return "persistentRepeat";
		}
		if (text == "BeatIntoShape" && varName == "CalculationBase")
		{
			return "resource";
		}
		if ((text == "Blur" && varName == "Blur") || (text == "Equilibrium" && varName == "Equilibrium") || (text == "Colossus" && varName == "Colossus"))
		{
			return "duration";
		}
		if (text == "ConsumingShadow" && varName == "ConsumingShadowPower")
		{
			return "persistentRepeat";
		}
		return null;
	}

	private static bool ModdedVarImproves(CardModel card, DynamicVar variable)
	{
		if (variable == null)
		{
			return false;
		}
		Type type = variable.GetType();
		if (AlwaysGood.Contains(type.Name))
		{
			return true;
		}
		if (type == typeof(EnergyVar))
		{
			if (card.Type != CardType.Status && card.Type != CardType.Curse)
			{
				return !card.Keywords.Contains(CardKeyword.Unplayable);
			}
			return false;
		}
		if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(PowerVar<>))
		{
			return false;
		}
		string name = type.GetGenericArguments()[0].Name;
		if (Buffs.Contains(name))
		{
			return true;
		}
		if (card.TargetType == TargetType.AnyEnemy || card.TargetType == TargetType.AllEnemies || card.TargetType == TargetType.RandomEnemy)
		{
			return Debuffs.Contains(name);
		}
		return false;
	}

	public static int NativeReferenceValue(CardModel card, string attribute)
	{
		int currentValue = CurrentValue(card, attribute);
		return checked(currentValue - BidirectionalEffects.RawDelta(card.GetType().Name, attribute, CardStateStore.Get(card)));
	}

	public static int CurrentValue(CardModel card, string attribute)
	{
		if (EnchantmentEffects.Known(attribute))
		{
			return EnchantmentUpgrades.Value(card, attribute);
		}
		if (AdvancedEnchantmentEffects.Known(attribute))
		{
			return AdvancedEnchantmentUpgrades.Value(card, attribute);
		}
		if (FixedEffects.Known(attribute))
		{
			return FixedEffects.Value(card.GetType().Name, attribute, CardStateStore.Get(card));
		}
		if (ExpandedEffects.Known(attribute))
		{
			return checked(ExpandedEffectPatches.Native(card, ExpandedEffects.Find(card.GetType().Name, attribute)) + BidirectionalEffects.RawDelta(card.GetType().Name, attribute, CardStateStore.Get(card)));
		}
		if (attribute == "cost")
		{
			return card.EnergyCost.GetWithModifiers(CostModifiers.None);
		}

		if (AttributeKeys.IsVar(attribute) && card.DynamicVars.TryGetValue(AttributeKeys.VarName(attribute), out DynamicVar dynamicVar))
		{
			return SteppedValues.TryGet(card, AttributeKeys.VarName(attribute), out SteppedValues.Stepped stepped) ? SteppedValues.Units(dynamicVar, stepped) : dynamicVar.IntValue;
		}
		throw new NotSupportedException("Attribute not supported yet: " + attribute);
	}

	public static void ApplyRaw(CardModel card, string attribute, int raw)
	{
		if (raw == 0 || RewrittenCardValues.Owns(card, attribute))
		{
			return;
		}
		if (AdvancedEnchantmentEffects.Known(attribute))
		{
			if (!AdvancedEnchantmentUpgrades.Matches(card, attribute))
			{
				throw new InvalidOperationException("Encantamiento incompatible.");
			}
		}
		else if (EnchantmentEffects.Known(attribute))
		{
			if (EnchantmentUpgrades.Attribute(card) != attribute)
			{
				throw new InvalidOperationException("Encantamiento incompatible.");
			}
		}
		else if (ExpandedEffects.Known(attribute))
		{
			if (ExpandedEffects.Find(card.GetType().Name, attribute) == null)
			{
				throw new InvalidOperationException("Unsupported effect: " + attribute);
			}
		}
		else if (FixedEffects.Known(attribute))
		{
			if (!FixedEffects.Supports(card.GetType().Name, attribute))
			{
				throw new InvalidOperationException("Unsupported effect: " + attribute);
			}
		}
		else if (attribute == "cost")
		{
			int cost = checked(CurrentValue(card, attribute) + raw);
			if (card.EnergyCost.CostsX || cost < 0 || cost > 999)
			{
				throw new InvalidOperationException($"The change {raw:+#;-#} puts the cost out of range on {card.Id}.");
			}
			card.EnergyCost.SetCustomBaseCost(cost);
		}
		else
		{
			if (AttributeKeys.IsVar(attribute))
			{
				string varName = AttributeKeys.VarName(attribute);
				if (!card.DynamicVars.TryGetValue(varName, out DynamicVar value))
				{
					throw new InvalidOperationException($"{card.Id} has no variable {varName}.");
				}
				// A fractional value moves by its step per unit; its minimum is in units too.
				bool stepped = SteppedValues.TryGet(card, varName, out SteppedValues.Stepped step);
				decimal after = value.BaseValue + (stepped ? raw * step.Step : (decimal)raw);
				decimal units = stepped ? (after - step.Neutral) / step.Step : after;
				if (units < (decimal)Minimum(card, attribute) - 0.0001m || after > 999999999m)
				{
					throw new InvalidOperationException($"The change {raw:+#;-#} puts {varName} out of range on {card.Id}.");
				}
				value.BaseValue = after;
				card.DynamicVars.RecalculateForUpgradeOrEnchant();
				if (card is ShowOff showOff && varName == "BaseAim")
				{
					// Combined build: Show Off only recomputes its Aim (BaseAim + Aim gained from kills) on
					// upgrade, downgrade or a kill, so recompute it now or the bet wouldn't show up until then.
					HarmonyLib.AccessTools.Method(typeof(ShowOff), "UpdateAim")?.Invoke(showOff, null);
				}
			}
			else
			{
				ModEntry.Warn($"Attribute {attribute} has no applier; ignored on {card.Id}.");
			}
		}
	}

	public static void ApplyWhole(CardModel card, CardMachineState state)
	{
		string cardName = card.GetType().Name;
		foreach (BidirectionalEffect item in BidirectionalEffects.For(cardName))
		{
			if ((!AttributeKeys.IsVar(item.Canonical) || card.DynamicVars.ContainsKey(AttributeKeys.VarName(item.Canonical))) && (state.Ledgers.ContainsKey(item.Canonical) || state.Ledgers.ContainsKey(item.Reverse)))
			{
				BidirectionalEffects.ConstrainToBase(item, state, Math.Max(0, ProjectedBase(item)));
			}
		}
		foreach (string attribute in state.Ledgers.Keys.ToList())
		{
			if (AdvancedEnchantmentEffects.Known(attribute))
			{
				if (!AdvancedEnchantmentUpgrades.Matches(card, attribute))
				{
					state.RemoveAttribute(attribute);
				}
				else if (attribute == "enchantdown:slitherMaximum")
				{
					state.ConstrainToBase(attribute, AdvancedEnchantmentEffects.Find(attribute).Native);
				}
				else if (state.RawDelta(attribute) < 0)
				{
					throw new InvalidOperationException("Negative enchantment upgrade.");
				}
			}
			else if (EnchantmentEffects.Known(attribute))
			{
				if (EnchantmentUpgrades.Attribute(card) != attribute)
				{
					state.RemoveAttribute(attribute);
				}
				else if (state.RawDelta(attribute) < 0)
				{
					throw new InvalidOperationException("Negative enchantment upgrade.");
				}
			}
			else if (FixedEffects.Known(attribute))
			{
				if (!FixedEffects.Supports(card.GetType().Name, attribute))
				{
					state.RemoveAttribute(attribute);
				}
				else if (FixedEffects.Value(card.GetType().Name, attribute, state) < FixedEffects.Native(card.GetType().Name, attribute))
				{
					throw new InvalidOperationException("Effect amount out of range: " + attribute);
				}
			}
			else if (ExpandedEffects.Known(attribute))
			{
				if (ExpandedEffects.Find(card.GetType().Name, attribute) == null)
				{
					state.RemoveAttribute(attribute);
					continue;
				}
				BidirectionalEffect bidirectionalEffect = BidirectionalEffects.Find(cardName, attribute);
				if (((bidirectionalEffect != null) ? ProjectedBase(bidirectionalEffect) : ExpandedEffectPatches.Native(card, ExpandedEffects.Find(cardName, attribute))) + BidirectionalEffects.RawDelta(cardName, attribute, state) < Minimum(card, attribute))
				{
					throw new InvalidOperationException("Effect amount out of range: " + attribute);
				}
				BidirectionalEffect pair = BidirectionalEffects.Find(cardName, attribute);
				if (((pair != null) ? ProjectedBase(pair) : ExpandedEffectPatches.Native(card, ExpandedEffects.Find(cardName, attribute))) + BidirectionalEffects.RawDelta(cardName, attribute, state) > ((ExpandedEffects.Find(cardName, attribute)?.Kind == "chooseGenerated") ? ExpandedEffects.Maximum(attribute) : Maximum(card, attribute)))
				{
					throw new InvalidOperationException("Effect amount over the maximum: " + attribute);
				}
			}
			else
			{
				if (attribute == "cost")
				{
					if (card.EnergyCost.CostsX || card.EnergyCost.GetWithModifiers(CostModifiers.None) < 0)
					{
						state.RemoveAttribute(attribute);
					}
					else
					{
						state.ConstrainCostToBase(card.EnergyCost.GetWithModifiers(CostModifiers.None));
					}
					continue;
				}
				if (!AttributeKeys.IsVar(attribute) || (HasRewrittenBaseValue(card) && (!(AttributeKeys.VarName(attribute) == "Increase") || (!(card is GeneticAlgorithm) && !(card is TheScythe))) && !RewrittenCardValues.Owns(card, attribute)))
				{
					state.RemoveAttribute(attribute);
					ModEntry.Warn($"{card.Id}: dropping attribute {attribute} because this version can't keep it.");
					continue;
				}
				string varName = AttributeKeys.VarName(attribute);
				if (!card.DynamicVars.TryGetValue(varName, out DynamicVar value))
				{
					state.RemoveAttribute(attribute);
					ModEntry.Warn($"{card.Id}: dropping attribute {attribute}, which the card doesn't have.");
				}
				else
				{
					decimal native = (RewrittenCardValues.Owns(card, attribute) ? ((decimal)RewrittenCardValues.Native(card)) : value.BaseValue);
					if (BidirectionalEffects.Find(cardName, attribute) == null)
					{
						state.ConstrainToBase(attribute, Math.Max(0, (int)Math.Floor(native) - Minimum(card, attribute)));
					}
				}
			}
		}
		foreach (string attribute in state.Ledgers.Keys)
		{
			if (attribute == "cost")
			{
				int cost = checked(CurrentValue(card, attribute) + state.RawDelta(attribute));
				if (cost < 0 || cost > 999)
				{
					throw new InvalidOperationException($"{card.Id}: cost out of range when rebuilding the card.");
				}
			}
			else if (AttributeKeys.IsVar(attribute))
			{
				string varName = AttributeKeys.VarName(attribute);
				DynamicVar dynamicVar = card.DynamicVars[varName];
				decimal after = (RewrittenCardValues.Owns(card, attribute) ? ((decimal)RewrittenCardValues.Native(card)) : dynamicVar.BaseValue) + (decimal)BidirectionalEffects.RawDelta(cardName, attribute, state);
				if (after < (decimal)Minimum(card, attribute) || after > 999999999m)
				{
					throw new InvalidOperationException($"{card.Id}: {attribute} out of range when rebuilding the card.");
				}
			}
		}
		foreach (string attribute in state.Ledgers.Keys)
		{
			if (!AttributeKeys.IsVar(attribute) || BidirectionalEffects.Find(cardName, attribute) == null)
			{
				ApplyRaw(card, attribute, state.RawDelta(attribute));
			}
		}
		foreach (BidirectionalEffect pair in from p in BidirectionalEffects.For(cardName)
			where AttributeKeys.IsVar(p.Canonical)
			select p)
		{
			ApplyRaw(card, pair.Up, BidirectionalEffects.RawDelta(cardName, pair.Canonical, state));
		}
		int ProjectedBase(BidirectionalEffect pair)
		{
			if (AttributeKeys.IsVar(pair.Canonical))
			{
				return card.DynamicVars[AttributeKeys.VarName(pair.Canonical)].IntValue;
			}
			int native = ExpandedEffectPatches.Native(card, ExpandedEffects.Find(cardName, pair.Canonical));
			if (card is Prepared)
			{
				native += state.RawDelta(AttributeKeys.Var("Cards"));
			}
			return native;
		}
	}
}
