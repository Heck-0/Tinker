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
		foreach (string item2 in BaseAttributes(card))
		{
			yield return item2;
		}
	}

	public static IEnumerable<string> BaseAttributes(CardModel card)
	{
		List<string> list = OneWayBaseAttributes(card).ToList();
		foreach (BidirectionalEffect item in BidirectionalEffects.For(card.GetType().Name))
		{
			bool num;
			if (!AttributeKeys.IsVar(item.Canonical))
			{
				num = ExpandedEffectPatches.Enabled(card);
			}
			else
			{
				if (!card.DynamicVars.ContainsKey(AttributeKeys.VarName(item.Canonical)))
				{
					continue;
				}
				if (ExpandedEffects.Find(item.Card, item.Canonical) == null)
				{
					goto IL_007c;
				}
				num = ExpandedEffectPatches.Enabled(card);
			}
			if (!num)
			{
				continue;
			}
			goto IL_007c;
			IL_007c:
			list.Remove(item.Canonical);
			list.Remove(item.Reverse);
			int num2 = CurrentValue(card, item.Canonical);
			if (num2 > Minimum(card, item.Down))
			{
				list.Add(item.Down);
			}
			if (num2 < Maximum(card, item.Up))
			{
				list.Add(item.Up);
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
			foreach (string item2 in FixedEffects.For(card.GetType().Name))
			{
				yield return item2;
			}
		}
		if (ExpandedEffectPatches.Enabled(card))
		{
			foreach (ExpandedEffect item3 in from e in ExpandedEffects.For(card.GetType().Name)
				group e by e.Attribute into g
				select g.First())
			{
				if (!AttributeKeys.IsVar(item3.Attribute) && (!(card is Armaments) || !card.IsUpgraded) && CurrentValue(card, item3.Attribute) < Maximum(card, item3.Attribute) && (AttributeDirection.SignOf(item3.Attribute) > 0 || CurrentValue(card, item3.Attribute) > Minimum(card, item3.Attribute)))
				{
					yield return item3.Attribute;
				}
			}
		}
		if (card.GetType().Namespace == typeof(Acrobatics).Namespace)
		{
			foreach (string key in card.DynamicVars.Keys)
			{
				int num = VarDirections.SignFor(card, key);
				if (num > 0)
				{
					yield return AttributeKeys.Var(key);
				}
				else if (num < 0 && CurrentValue(card, AttributeKeys.VarDown(key)) > Minimum(card, AttributeKeys.VarDown(key)))
				{
					yield return AttributeKeys.VarDown(key);
				}
			}
		}
		else
		{
			if (card.Type == CardType.Attack && card.DynamicVars.TryGetValue("Damage", out DynamicVar value) && value.GetType() == typeof(DamageVar))
			{
				yield return AttributeKeys.Var("Damage");
			}
			if (card.GainsBlock && card.DynamicVars.TryGetValue("Block", out DynamicVar value2) && value2.GetType() == typeof(BlockVar))
			{
				yield return AttributeKeys.Var("Block");
			}
			foreach (string key2 in card.DynamicVars.Keys)
			{
				if (ModdedVarImproves(card, card.DynamicVars[key2]))
				{
					yield return AttributeKeys.Var(key2);
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
		string text = AttributeKeys.VarName(attribute);
		if (SteppedValues.TryGet(card, text, out SteppedValues.Stepped stepped))
		{
			return stepped.MinUnits;
		}
		if (!(text == "Turns") && !(text == "DoomThreshold"))
		{
			switch (attribute)
			{
			case "extradown:threshold":
			case "extradown:quest":
			case "extradown:poisonThreshold":
				break;
			default:
			{
				string text2 = card?.GetType().Name;
				if ((text2 == "MakeItSo" && text == "Cards") || ((text2 == "HeavenlyDrill" || text2 == "RightHandHand") && text == "Energy"))
				{
					return 1;
				}
				if ((text2 == "PaleBlueDot" && text == "CardPlay") || (text2 == "DanseMacabre" && text == "Energy"))
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
		if (attribute.StartsWith("var:", StringComparison.Ordinal) && card.DynamicVars.TryGetValue(AttributeKeys.VarName(attribute), out DynamicVar value)
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
		string text2 = AttributeKeys.VarName(attribute);
		if (SteppedValues.TryGet(card, text2, out SteppedValues.Stepped stepped))
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
		if (text2 == "CalculationBase" && HiddenCalculationCards.Contains(text))
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
		if (text2 == "Increase" && (text == "GeneticAlgorithm" || text == "TheScythe"))
		{
			return "permanentGrowth";
		}
		if (attribute == "vardown:DoomThreshold" || (attribute == "vardown:Energy" && (text == "HeavenlyDrill" || text == "RightHandHand")) || (attribute == "vardown:Cards" && text == "MakeItSo"))
		{
			return "threshold";
		}
		if (text2 == "Cards" && (text == "BeatDown" || text == "Catastrophe"))
		{
			return "autoPlay";
		}
		// The Fool (game v0.113): values whose default category by name would be too cheap or too strong.
		if ((text == "Honing" && text2 == "HoningPower") || (text == "SerratedArrows" && text2 == "SerratedArrowsPower") || (text == "DislikeYou" && text2 == "DislikeYouPower"))
		{
			return "percentage";
		}
		if (text == "HeavyShot" && text2 == "AimMultiplier")
		{
			return "multiplier";
		}
		if (text == "KiVerse" && text2 == "KiVersePower")
		{
			return "repeat";
		}
		if ((text == "Motif" && text2 == "MotifPower") || (text == "LostLute" && text2 == "LostLutePower") || (text == "Simpleton" && text2 == "SimpletonPower"))
		{
			return "persistentRepeat";
		}
		if (text2 == "Energy" && (text == "Compost" || text == "Yah"))
		{
			return "persistentEnergy";
		}
		if (text == "Timber" && text2 == "OnKillDamage")
		{
			return "damage";
		}
		if (text == "KiChant" && text2 == "Cards")
		{
			return "selection";
		}
		if (text == "Quietus" && text2 == "QuietusPower")
		{
			return "multiplier";
		}
		if (text == "Tune" && text2 == "Energy")
		{
			return "persistentEnergy";
		}
		if (text == "Ensemble" && text2 == "Cards")
		{
			return "energy";
		}
		if (text == "Encore" && text2 == "EncorePower")
		{
			return "repeat";
		}
		if ((text2 == "Joy" && (text == "BrightenUp" || text == "SelfDeprecate")) || (text == "Ecstasy" && text2 == "Cards"))
		{
			return "generation";
		}
		if (text2 == "Cards")
		{
			switch (text)
			{
			case "Dredge":
			case "Seance":
			case "Charge":
				return "selection";
			}
		}
		if (text2 == "Cards" && (text == "GlimpseBeyond" || text == "Plot"))
		{
			return "groupResource";
		}
		if (text2 == "Cards" && text == "Demesne")
		{
			return "persistentEnergy";
		}
		if (text == "Voltaic" && text2 == "CalculationBase")
		{
			return "orb";
		}
		if (text == "Voltaic" && text2 == "CalculationExtra")
		{
			return "multiplier";
		}
		if (text == "WellLaidPlans" && text2 == "RetainAmount")
		{
			return "selection";
		}
		if (text == "Feral" && text2 == "FeralPower")
		{
			return "persistentRepeat";
		}
		if (text == "BeatIntoShape" && text2 == "CalculationBase")
		{
			return "resource";
		}
		if ((text == "Blur" && text2 == "Blur") || (text == "Equilibrium" && text2 == "Equilibrium") || (text == "Colossus" && text2 == "Colossus"))
		{
			return "duration";
		}
		if (text == "ConsumingShadow" && text2 == "ConsumingShadowPower")
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
		int num = CurrentValue(card, attribute);
		return checked(num - BidirectionalEffects.RawDelta(card.GetType().Name, attribute, CardStateStore.Get(card)));
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

		if (AttributeKeys.IsVar(attribute) && card.DynamicVars.TryGetValue(AttributeKeys.VarName(attribute), out DynamicVar value2))
		{
			return SteppedValues.TryGet(card, AttributeKeys.VarName(attribute), out SteppedValues.Stepped stepped) ? SteppedValues.Units(value2, stepped) : value2.IntValue;
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
			int num = checked(CurrentValue(card, attribute) + raw);
			if (card.EnergyCost.CostsX || num < 0 || num > 999)
			{
				throw new InvalidOperationException($"The change {raw:+#;-#} puts the cost out of range on {card.Id}.");
			}
			card.EnergyCost.SetCustomBaseCost(num);
		}
		else
		{
			if (AttributeKeys.IsVar(attribute))
			{
				string text = AttributeKeys.VarName(attribute);
				if (!card.DynamicVars.TryGetValue(text, out DynamicVar value))
				{
					throw new InvalidOperationException($"{card.Id} has no variable {text}.");
				}
				// A fractional value moves by its step per unit; its minimum is in units too.
				bool stepped = SteppedValues.TryGet(card, text, out SteppedValues.Stepped step);
				decimal num2 = value.BaseValue + (stepped ? raw * step.Step : (decimal)raw);
				decimal units = stepped ? (num2 - step.Neutral) / step.Step : num2;
				if (units < (decimal)Minimum(card, attribute) - 0.0001m || num2 > 999999999m)
				{
					throw new InvalidOperationException($"The change {raw:+#;-#} puts {text} out of range on {card.Id}.");
				}
				value.BaseValue = num2;
				card.DynamicVars.RecalculateForUpgradeOrEnchant();
				if (card is ShowOff showOff && text == "BaseAim")
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
		foreach (string item2 in state.Ledgers.Keys.ToList())
		{
			if (AdvancedEnchantmentEffects.Known(item2))
			{
				if (!AdvancedEnchantmentUpgrades.Matches(card, item2))
				{
					state.RemoveAttribute(item2);
				}
				else if (item2 == "enchantdown:slitherMaximum")
				{
					state.ConstrainToBase(item2, AdvancedEnchantmentEffects.Find(item2).Native);
				}
				else if (state.RawDelta(item2) < 0)
				{
					throw new InvalidOperationException("Negative enchantment upgrade.");
				}
			}
			else if (EnchantmentEffects.Known(item2))
			{
				if (EnchantmentUpgrades.Attribute(card) != item2)
				{
					state.RemoveAttribute(item2);
				}
				else if (state.RawDelta(item2) < 0)
				{
					throw new InvalidOperationException("Negative enchantment upgrade.");
				}
			}
			else if (FixedEffects.Known(item2))
			{
				if (!FixedEffects.Supports(card.GetType().Name, item2))
				{
					state.RemoveAttribute(item2);
				}
				else if (FixedEffects.Value(card.GetType().Name, item2, state) < FixedEffects.Native(card.GetType().Name, item2))
				{
					throw new InvalidOperationException("Effect amount out of range: " + item2);
				}
			}
			else if (ExpandedEffects.Known(item2))
			{
				if (ExpandedEffects.Find(card.GetType().Name, item2) == null)
				{
					state.RemoveAttribute(item2);
					continue;
				}
				BidirectionalEffect bidirectionalEffect = BidirectionalEffects.Find(cardName, item2);
				if (((bidirectionalEffect != null) ? ProjectedBase(bidirectionalEffect) : ExpandedEffectPatches.Native(card, ExpandedEffects.Find(cardName, item2))) + BidirectionalEffects.RawDelta(cardName, item2, state) < Minimum(card, item2))
				{
					throw new InvalidOperationException("Effect amount out of range: " + item2);
				}
				BidirectionalEffect bidirectionalEffect2 = BidirectionalEffects.Find(cardName, item2);
				if (((bidirectionalEffect2 != null) ? ProjectedBase(bidirectionalEffect2) : ExpandedEffectPatches.Native(card, ExpandedEffects.Find(cardName, item2))) + BidirectionalEffects.RawDelta(cardName, item2, state) > ((ExpandedEffects.Find(cardName, item2)?.Kind == "chooseGenerated") ? ExpandedEffects.Maximum(item2) : Maximum(card, item2)))
				{
					throw new InvalidOperationException("Effect amount over the maximum: " + item2);
				}
			}
			else
			{
				if (item2 == "cost")
				{
					if (card.EnergyCost.CostsX || card.EnergyCost.GetWithModifiers(CostModifiers.None) < 0)
					{
						state.RemoveAttribute(item2);
					}
					else
					{
						state.ConstrainCostToBase(card.EnergyCost.GetWithModifiers(CostModifiers.None));
					}
					continue;
				}
				if (!AttributeKeys.IsVar(item2) || (HasRewrittenBaseValue(card) && (!(AttributeKeys.VarName(item2) == "Increase") || (!(card is GeneticAlgorithm) && !(card is TheScythe))) && !RewrittenCardValues.Owns(card, item2)))
				{
					state.RemoveAttribute(item2);
					ModEntry.Warn($"{card.Id}: dropping attribute {item2} because this version can't keep it.");
					continue;
				}
				string key = AttributeKeys.VarName(item2);
				if (!card.DynamicVars.TryGetValue(key, out DynamicVar value))
				{
					state.RemoveAttribute(item2);
					ModEntry.Warn($"{card.Id}: dropping attribute {item2}, which the card doesn't have.");
				}
				else
				{
					decimal d = (RewrittenCardValues.Owns(card, item2) ? ((decimal)RewrittenCardValues.Native(card)) : value.BaseValue);
					if (BidirectionalEffects.Find(cardName, item2) == null)
					{
						state.ConstrainToBase(item2, Math.Max(0, (int)Math.Floor(d) - Minimum(card, item2)));
					}
				}
			}
		}
		foreach (string key3 in state.Ledgers.Keys)
		{
			if (key3 == "cost")
			{
				int num = checked(CurrentValue(card, key3) + state.RawDelta(key3));
				if (num < 0 || num > 999)
				{
					throw new InvalidOperationException($"{card.Id}: cost out of range when rebuilding the card.");
				}
			}
			else if (AttributeKeys.IsVar(key3))
			{
				string key2 = AttributeKeys.VarName(key3);
				DynamicVar dynamicVar = card.DynamicVars[key2];
				decimal num2 = (RewrittenCardValues.Owns(card, key3) ? ((decimal)RewrittenCardValues.Native(card)) : dynamicVar.BaseValue) + (decimal)BidirectionalEffects.RawDelta(cardName, key3, state);
				if (num2 < (decimal)Minimum(card, key3) || num2 > 999999999m)
				{
					throw new InvalidOperationException($"{card.Id}: {key3} out of range when rebuilding the card.");
				}
			}
		}
		foreach (string key4 in state.Ledgers.Keys)
		{
			if (!AttributeKeys.IsVar(key4) || BidirectionalEffects.Find(cardName, key4) == null)
			{
				ApplyRaw(card, key4, state.RawDelta(key4));
			}
		}
		foreach (BidirectionalEffect item3 in from p in BidirectionalEffects.For(cardName)
			where AttributeKeys.IsVar(p.Canonical)
			select p)
		{
			ApplyRaw(card, item3.Up, BidirectionalEffects.RawDelta(cardName, item3.Canonical, state));
		}
		int ProjectedBase(BidirectionalEffect pair)
		{
			if (AttributeKeys.IsVar(pair.Canonical))
			{
				return card.DynamicVars[AttributeKeys.VarName(pair.Canonical)].IntValue;
			}
			int num3 = ExpandedEffectPatches.Native(card, ExpandedEffects.Find(cardName, pair.Canonical));
			if (card is Prepared)
			{
				num3 += state.RawDelta(AttributeKeys.Var("Cards"));
			}
			return num3;
		}
	}
}
