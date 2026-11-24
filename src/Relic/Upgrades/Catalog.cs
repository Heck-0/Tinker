using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using Tinker.Common.Rules;

namespace Tinker.Relic;

/// <summary>
/// Which relic values the Workshop upgrades, and how. The rule engine (<see cref="GameRules"/>) decides the
/// direction from the relic's own code, and <see cref="DefaultFamily"/> picks the cost category from the value's
/// name and type. <see cref="Overrides"/> holds the values where the mod knows better: a different cost category
/// (e.g. energy every turn is pricier than energy once), a direction the rules can't tell, a maximum or label,
/// or a fractional value (a multiplier) bought in steps.
/// </summary>
public static class Catalog
{
	private static readonly Dictionary<(string Relic, string Key), RelicAttribute> Overrides = BuildOverrides();

	private static readonly Dictionary<Type, RelicAttribute[]> Cache = new Dictionary<Type, RelicAttribute[]>();

	public static int OverrideCount => Overrides.Count;

	/// <summary>The relic's values the Workshop can upgrade right now.</summary>
	public static RelicAttribute[] For(RelicModel relic)
	{
		if (relic.IsMelted || relic.IsUsedUp || relic.HasBeenRemovedFromState)
		{
			return Array.Empty<RelicAttribute>();
		}
		return All(relic).Where((RelicAttribute a) => relic.DynamicVars.ContainsKey(a.Key) && a.Limit(relic) > 0).ToArray();
	}

	/// <summary>One upgradable value of this relic's class, or null.</summary>
	public static RelicAttribute? Find(RelicModel relic, string key)
	{
		return All(relic).FirstOrDefault((RelicAttribute a) => a.Key == key);
	}

	/// <summary>Every upgradable value of the relic's class (cached per class).</summary>
	public static RelicAttribute[] All(RelicModel relic)
	{
		Type type = relic.GetType();
		lock (Cache)
		{
			if (Cache.TryGetValue(type, out RelicAttribute[]? cached))
			{
				return cached;
			}
			List<RelicAttribute> list = new List<RelicAttribute>();
			foreach (VarInfo v in GameRules.Vars(relic))
			{
				if (Overrides.TryGetValue((type.Name, v.Name), out RelicAttribute? set))
				{
					list.Add(set);
					continue;
				}
				// A fraction (a multiplier) needs a step size, which only an override gives.
				if (relic.DynamicVars[v.Name].BaseValue % 1m != 0m)
				{
					continue;
				}
				Verdict verdict = GameRules.Decide(relic, v.Name);
				if (verdict.Sign != 0)
				{
					list.Add(new RelicAttribute(v.Name, DefaultFamily(v, verdict), verdict.Sign));
				}
			}
			RelicAttribute[] result = list.ToArray();
			Cache[type] = result;
			return result;
		}
	}

	/// <summary>The cost category for a value the rules decided (tools/relic_overrides.py uses the same mapping).</summary>
	public static string DefaultFamily(VarInfo v, Verdict verdict)
	{
		if (verdict.Sign < 0)
		{
			return (verdict.Why == "every N / threshold") ? "threshold" : "drawback";
		}
		switch (v.Name)
		{
		case "Damage":
		case "OstyDamage":
		case "ExtraDamage":
			return "damage";
		case "Block":
			return "block";
		case "Heal":
		case "Gold":
		case "MaxHp":
		case "Stars":
		case "Forge":
		case "Summon":
			return "resource";
		case "Cards":
			return "draw";
		case "Energy":
			return "energy";
		case "Repeat":
		case "HitCount":
			return "repeat";
		}
		if (v.Power == "StrengthPower" || v.Power == "DexterityPower" || v.Power == "FocusPower")
		{
			return "scaling";
		}
		if (v.Power != null || v.Name.EndsWith("Power", StringComparison.Ordinal))
		{
			return "power";
		}
		return v.Class switch
		{
			"DamageVar" or "OstyDamageVar" or "ExtraDamageVar" => "damage",
			"BlockVar" => "block",
			"CardsVar" => "draw",
			"EnergyVar" => "energy",
			"HealVar" or "GoldVar" or "MaxHpVar" or "StarsVar" or "ForgeVar" or "SummonVar" => "resource",
			"RepeatVar" or "HitCountVar" => "repeat",
			_ => "generic"
		};
	}

	private static Dictionary<(string, string), RelicAttribute> BuildOverrides()
	{
		Dictionary<(string, string), RelicAttribute> result = new Dictionary<(string, string), RelicAttribute>();
		// Kept from the old hand-written catalog where the engine + DefaultFamily would differ (tools/relic_overrides.py).
		Override("BigHat", "Cards", "generation");
		Override("BigMushroom", "Cards", "drawback", -1, 10000, "drawLoss");
		Override("BlessedAntler", "Energy", "persistentEnergy");
		Override("BloodSoakedRose", "Energy", "persistentEnergy");
		Override("BoundPhylactery", "Summon", "power");
		Override("Bread", "GainEnergy", "persistentEnergy");
		Override("BrilliantScarf", "Cards", "threshold", -1);
		Override("Brimstone", "EnemyStrength", "drawback", -1);
		Override("Brimstone", "SelfStrength", "scaling");
		Override("ChemicalX", "Increase", "repeat");
		Override("ChoicesParadox", "Cards", "selection");
		Override("ChosenCheese", "MaxHp", "permanentGrowth");
		Override("CrackedCore", "Lightning", "repeat");
		Override("DarkstonePeriapt", "MaxHp", "permanentGrowth");
		Override("DragonFruit", "MaxHp", "permanentGrowth");
		Override("Ectoplasm", "Energy", "persistentEnergy");
		Override("FakeStrikeDummy", "ExtraDamage", "damage");
		Override("Fiddle", "Cards", "persistentDraw");
		Override("HandDrill", "VulnerablePower", "power");
		Override("HelicalDart", "DexterityPower", "scaling");
		Override("InfusedCore", "ExtraDamage", "damage");
		Override("InfusedCore", "Lightning", "repeat");
		Override("IntimidatingHelmet", "Energy", "threshold", -1);
		Override("IvoryTile", "EnergyThreshold", "threshold", -1);
		Override("MeatOnTheBone", "HpThreshold", "percentage", 1, 100);
		Override("MembershipCard", "Discount", "percentage", -1, 100);
		Override("Metronome", "OrbCount", "threshold", -1);
		Override("MightyWings", "DamageThreshold", "threshold", -1);
		Override("MiniatureCannon", "ExtraDamage", "damage");
		Override("NinjaScroll", "Shivs", "repeat");
		Override("OrangeDough", "Cards", "generation");
		Override("PaelsBlood", "Cards", "persistentDraw");
		Override("PaelsFlesh", "Energy", "persistentEnergy");
		Override("ParryingShield", "Block", "block");
		Override("PhilosophersStone", "Energy", "persistentEnergy");
		Override("PhylacteryUnbound", "StartOfCombat", "power");
		Override("PhylacteryUnbound", "StartOfTurn", "power");
		Override("PollinousCore", "Turns", "threshold", -1);
		Override("PowerCell", "Cards", "selection");
		Override("PrismaticGem", "Energy", "persistentEnergy");
		Override("PumpkinCandle", "Energy", "persistentEnergy");
		Override("RadiantPearl", "Cards", "generation");
		Override("RedSkull", "HpThreshold", "percentage", 1, 100);
		Override("RedSkull", "StrengthPower", "scaling");
		Override("ReptileTrinket", "StrengthPower", "scaling");
		Override("RingOfTheDrake", "Turns", "duration");
		Override("SelfFormingClay", "BlockNextTurn", "block");
		Override("SneckoEye", "Cards", "persistentDraw");
		Override("SneckoSkull", "PoisonPower", "power");
		Override("Sozu", "Energy", "persistentEnergy");
		Override("SpikedGauntlets", "Energy", "persistentEnergy");
		Override("StoneCalendar", "DamageTurn", "threshold", -1);
		Override("StrikeDummy", "ExtraDamage", "damage");
		Override("TheCourier", "Discount", "percentage", 1, 100);
		Override("Toolbox", "Cards", "selection");
		Override("TungstenRod", "HpLossReduction", "protection");
		Override("VelvetChoker", "Cards", "drawback");
		Override("VelvetChoker", "Energy", "persistentEnergy");
		Override("WhisperingEarring", "Energy", "persistentEnergy");
		// Reviewed in docs/COVERAGE_GAPS.md: the rules' guess is wrong or too generic.
		Override("BlessedAntler", "Cards", "drawback", -1);
		Override("FuneraryMask", "Cards", "generation");
		Override("LizardTail", "Heal", "percentage", 1, 100);
		Override("StoneCracker", "Cards", "generic");
		Override("WarHammer", "Cards", "generic");
		// Fractions, bought in steps of 0.01 (prices count steps from the "no effect" value).
		Stepped("BowlerHat", "GoldIncrease", 1, neutral: 1m, minimum: 1m, maximum: 3m);
		Stepped("UndyingSigil", "DamageDecrease", -1, neutral: 0m, minimum: 0.1m, maximum: 1m);
		// Paper Phrog / Paper Krane: their hard-coded bonus becomes a value (PaperRelics).
		Stepped("PaperPhrog", "VulnerableBonus", 1, neutral: 0m, minimum: 0m, maximum: 1m);
		Stepped("PaperKrane", "WeakReduction", 1, neutral: 0m, minimum: 0m, maximum: 0.75m);
		return result;
		void Override(string relic, string key, string family, int direction = 1, decimal maximum = 10000m, string? labelKey = null)
		{
			result[(relic, key)] = new RelicAttribute(key, family, direction, maximum, labelKey);
		}
		void Stepped(string relic, string key, int direction, decimal neutral, decimal minimum, decimal maximum)
		{
			result[(relic, key)] = new RelicAttribute(key, "percentage", direction, maximum)
			{
				Step = 0.01m,
				Neutral = neutral,
				Minimum = minimum
			};
		}
	}

	/// <summary>The most units one purchase can add: the relic value's limit (at most 999). Gold limits the rest.</summary>
	public static int MaxUnits(RelicModel relic, RelicAttribute attribute)
	{
		return Math.Min(999, attribute.Limit(relic));
	}

	/// <summary>The upgrade's cost exponent: its size next to the base value, plus the relic's other upgrades.</summary>
	public static double Exponent(RelicModel relic, RelicAttribute attribute, int units)
	{
		RelicState relicState = Store.For(relic);
		UpgradeLedger? own = relicState.Upgrades.GetValueOrDefault(attribute.Key);
		double otherStrain = 0.8 * relicState.Upgrades.Where((KeyValuePair<string, UpgradeLedger> p) => p.Key != attribute.Key)
			.Sum((KeyValuePair<string, UpgradeLedger> p) => Rules.Risk(p.Value.Family, Find(relic, p.Key)?.PriceReference(p.Value.Reference) ?? p.Value.Reference, p.Value.Gain));
		return Rules.Exponent(attribute.Family, attribute.PriceReference(own?.Reference ?? attribute.Value(relic)), own?.Gain ?? 0, units, otherStrain);
	}
}
