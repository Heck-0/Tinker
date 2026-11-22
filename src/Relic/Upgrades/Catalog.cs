using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Relic;

public static class Catalog
{
	private static readonly Dictionary<string, RelicAttribute[]> Entries = Build();

	public static IReadOnlyDictionary<string, RelicAttribute[]> Supported => Entries;

	public static RelicAttribute[] For(RelicModel relic)
	{
		if (relic.IsMelted || relic.IsUsedUp || relic.HasBeenRemovedFromState)
		{
			return Array.Empty<RelicAttribute>();
		}
		return (Entries.GetValueOrDefault(relic.GetType().Name) ?? Array.Empty<RelicAttribute>()).Where((RelicAttribute a) => relic.DynamicVars.ContainsKey(a.Key) && a.Limit(relic) > 0).ToArray();
	}

	private static Dictionary<string, RelicAttribute[]> Build()
	{
		Dictionary<string, RelicAttribute[]> result = new Dictionary<string, RelicAttribute[]>(StringComparer.Ordinal);
		Group("BurningBlood BlackBlood BloodVial FakeBloodVial BookRepairKnife EternalFeather MealTicket MeatOnTheBone Pantograph Planisphere RegalPillow BookOfFiveRings", "Heal", "resource");
		Group("Anchor FakeAnchor HornCleat CaptainsWheel Orichalcum FakeOrichalcum CloakClasp BoneFlute DaughterOfTheWind DiamondDiadem GalacticDust IntimidatingHelmet OrnamentalFan ParryingShield Permafrost Regalite RippleBasin Sai ToughBandages TuningFork", "Block", "block");
		Group("CharonsAshes ForgottenSoul Kusarigama LetterOpener LostWisp MercuryHourglass Metronome MysticLighter ParryingShield ScreamingFlagon StoneCalendar Tingsha FestivePopper", "Damage", "damage");
		Group("RoyalPoison", "Damage", "drawback", -1);
		Group("MiniatureCannon InfusedCore FakeStrikeDummy StrikeDummy", "ExtraDamage", "damage");
		Group("SelfFormingClay", "BlockNextTurn", "block");
		Group("Vajra RedSkull SlingOfCourage Shuriken MiniRegent ReptileTrinket ToastyMittens RainbowRing SparklingRouge EmberTea", "StrengthPower", "scaling");
		Group("OddlySmoothStone Kunai BeltBuckle HelicalDart RainbowRing SparklingRouge", "DexterityPower", "scaling");
		Group("Akabeko", "VigorPower", "power");
		Group("BronzeScales", "ThornsPower", "power");
		Group("DataDisk", "FocusPower", "scaling");
		Group("Gorget", "PlatingPower", "power");
		Group("BagOfMarbles HandDrill", "VulnerablePower", "power");
		Group("RedMask", "WeakPower", "power");
		Group("TwistedFunnel SneckoSkull", "PoisonPower", "power");
		Group("BoundPhylactery", "Summon", "power");
		Group("PhylacteryUnbound", "StartOfCombat", "power");
		Group("PhylacteryUnbound", "StartOfTurn", "power");
		Group("Brimstone", "SelfStrength", "scaling");
		Group("Brimstone", "EnemyStrength", "drawback", -1);
		Group("ArtOfWar Candelabra Chandelier GremlinHorn HappyFlower FakeHappyFlower IvoryTile Lantern Nunchaku PaelsTears SealOfGold VenerableTeaSet FakeVenerableTeaSet VeryHotCocoa BoomingConch", "Energy", "energy");
		Group("IntimidatingHelmet", "Energy", "threshold", -1);
		Group("IvoryTile", "EnergyThreshold", "threshold", -1);
		Group("BlessedAntler BloodSoakedRose Ectoplasm PaelsFlesh PhilosophersStone PrismaticGem Sozu SpikedGauntlets VelvetChoker WhisperingEarring PumpkinCandle", "Energy", "persistentEnergy");
		Group("Bread", "GainEnergy", "persistentEnergy");
		Group("Bread", "LoseEnergy", "drawback", -1);
		Group("BagOfPreparation BoomingConch CentennialPuzzle GamePiece GremlinHorn Pocketwatch PollinousCore RingOfTheDrake RingOfTheSnake", "Cards", "draw");
		Group("BigHat OrangeDough RadiantPearl", "Cards", "generation");
		Group("ChoicesParadox PowerCell Toolbox", "Cards", "selection");
		Group("PaelsBlood SneckoEye Fiddle", "Cards", "persistentDraw");
		Group("BigMushroom", "Cards", "drawback", -1, 10000, "drawLoss");
		Group("BookOfFiveRings BrilliantScarf IronClub Kunai Kusarigama LetterOpener Nunchaku OrnamentalFan Shuriken TuningFork EternalFeather", "Cards", "threshold", -1);
		Group("HappyFlower FakeHappyFlower PollinousCore", "Turns", "threshold", -1);
		Group("RingOfTheDrake", "Turns", "duration");
		Group("StoneCalendar", "DamageTurn", "threshold", -1);
		Group("JossPaper", "ExhaustAmount", "threshold", -1);
		Group("Metronome", "OrbCount", "threshold", -1);
		Group("VelvetChoker", "Cards", "drawback");
		Group("MeatOnTheBone RedSkull", "HpThreshold", "percentage", 1, 100);
		Group("TheCourier", "Discount", "percentage", 1, 100);
		Group("MembershipCard", "Discount", "percentage", -1, 100);
		Group("TungstenRod", "HpLossReduction", "protection");
		Group("AmethystAubergine LuckyFysh MawBank", "Gold", "resource");
		Group("SealOfGold", "Gold", "drawback", -1);
		Group("DarkstonePeriapt ChosenCheese DragonFruit", "MaxHp", "permanentGrowth");
		Group("DivineDestiny DivineRight LunarPastry", "Stars", "resource");
		Group("GalacticDust", "Stars", "threshold", -1);
		Group("CrackedCore InfusedCore", "Lightning", "repeat");
		Group("RunicCapacitor", "Repeat", "repeat");
		Group("NinjaScroll", "Shivs", "repeat");
		Group("ChemicalX", "Increase", "repeat");
		// Combined build: the Fool's relics (docs/FOOL_CANDIDATES.md).
		Group("SongBow Storyteller WhittlingKnife", "AimPower", "power");
		Group("Storyteller", "EndOfTurnAim", "power");
		Group("ExerciseMat", "StretchPower", "power");
		Group("Fertilizer", "StrengthPower", "scaling");
		// Mighty Wings: a hit of at least DamageThreshold (30) damage gives StrengthPower (2) Strength.
		Group("MightyWings", "DamageThreshold", "threshold", -1);
		Group("MightyWings", "StrengthPower", "scaling");
		Group("Twine", "Block", "block");
		return result;
		void Group(string names, string key, string family, int direction = 1, int maximum = 10000, string? labelKey = null)
		{
			string[] array = names.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			foreach (string key2 in array)
			{
				result[key2] = (result.GetValueOrDefault(key2) ?? Array.Empty<RelicAttribute>()).Append(new RelicAttribute(key, family, direction, maximum, labelKey)).ToArray();
			}
		}
	}

	/// <summary>The most units one purchase can add: the relic value's limit (at most 999). Gold limits the rest.</summary>
	public static int MaxBet(RelicModel relic, RelicAttribute attribute)
	{
		return Math.Min(999, attribute.Limit(relic));
	}

	public static double Chance(RelicModel relic, RelicAttribute attribute, int units, bool forPrice = false)
	{
		RelicState relicState = Store.For(relic);

		UpgradeLedger valueOrDefault = relicState.Upgrades.GetValueOrDefault(attribute.Key);
		double otherStrain = 0.8 * relicState.Upgrades.Where<KeyValuePair<string, UpgradeLedger>>((KeyValuePair<string, UpgradeLedger> p) => p.Key != attribute.Key).Sum((KeyValuePair<string, UpgradeLedger> p) => Rules.Risk(p.Value.Family, p.Value.Reference, p.Value.Gain));
		return Rules.Chance(attribute.Family, valueOrDefault?.Reference ?? attribute.Value(relic), valueOrDefault?.Gain ?? 0, units, otherStrain, forPrice);
	}
}
