using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Tinker.Common;

namespace Tinker.Card;

public static class Lines
{
	private static readonly Dictionary<string, string> PowerTitles = new Dictionary<string, string>();

	public static string Before => T("Before");

	public static string After => T("After");

	public static string PropertyNone => T("None fit this card");

	public static string TraitsOff => T("Traits are turned off in the settings");

	private static string T(string en) => Lang.T(en);

	public static string Pct(int percent)
	{
		return $"{percent}%";
	}

	public static string AttributeName(string attribute)
	{
		switch (attribute)
		{
		case "effect:lightning":
			return T("Lightning orbs");
		case "effect:frost":
			return T("Frost orbs");
		case "effect:dark":
			return T("Dark orbs");
		case "effect:plasma":
			return T("Plasma orbs");
		case "effect:frostPerEnemy":
			return T("Frost per enemy");
		case "effect:evokes":
			return T("Orb evocations");
		case "effect:extraEvokes":
			return T("Extra evocations");
		default:
		{
			if (attribute == "cost")
			{
				return T("Cost");
			}
			string text = AttributeKeys.VarName(attribute);
			if (text == null)
			{
				return attribute;
			}
			return VariableName(text);
		}
		}
	}

	private static string VariableName(string key)
	{
		switch (key)
		{
		case "Damage":
			return T("Damage");
		case "Block":
			return T("Block");
		case "Cards":
			return T("Cards");
		case "Energy":
			return T("Energy");
		case "HpLoss":
			return T("HP loss");
		case "Heal":
			return T("Heal");
		case "Gold":
			return T("Gold");
		case "MaxHp":
			return T("Max HP");
		case "Stars":
			return T("Stars");
		case "Summon":
			return T("Summon");
		case "Forge":
			return T("Forge");
		case "Repeat":
			return T("Repeats");
		case "Sadness":
			return T("Sadness added");
		case "Joy":
			return T("Joy cards");
		case "OnKillDamage":
			return T("Damage on kill");
		case "AimMultiplier":
			return T("Aim multiplier");
		case "BaseAim":
			return T("Base Aim");
		case "HitCount":
			return T("Hits");
		case "ExtraDamage":
			return T("Extra damage");
		case "OstyDamage":
			return T("Osty damage");
		case "CalculationBase":
			return T("Base value");
		case "CalculationExtra":
			return T("Value per");
		case "Accelerant":
			return T("Poison triggers");
		case "Attacks":
			return T("Attacks repeated");
		case "BlockForStars":
			return T("Block per star");
		case "BlockNextTurn":
			return T("Next turn Block");
		case "BlockOnExhaust":
			return T("Block per Ethereal card");
		case "BombDamage":
			return T("Bomb damage");
		case "ChokingDamage":
			return T("Choking damage");
		case "CorrosiveWave":
			return T("Poison per draw");
		case "CreativeAi":
			return T("Powers per turn");
		case "CuriousReduction":
			return T("Cost reduction");
		case "DamageBack":
			return T("Damage back");
		case "EchoForm":
			return T("Cards repeated");
		case "EnemyStrength":
			return T("Enemy Strength");
		case "EnemyStrengthLoss":
			return T("Enemy Strength loss");
		case "StrengthLoss":
			return T("Enemy Strength loss");
		case "PlayerStrengthLoss":
			return T("Strength you lose");
		case "EnergizedEnergy":
			return T("Energy");
		case "ExpertiseDexterity":
			return T("Dexterity");
		case "ExpertiseStrength":
			return T("Strength");
		case "ExtraBlock":
			return T("Extra Block");
		case "ExtraCost":
			return T("Extra cost");
		case "Frail":
			return T("Frail");
		case "SappingVulnerable":
			return T("Vulnerable");
		case "SappingWeak":
			return T("Weak");
		case "Increase":
			return T("Damage increase");
		case "Loop":
			return T("Orb triggers");
		case "OrbSlots":
			return T("Orb slots lost");
		case "PanacheDamage":
			return T("Damage to all");
		case "PlayMax":
			return T("Card limit");
		case "PoisonPerTurn":
			return T("Poison per turn");
		case "Replay":
			return T("Replay");
		case "Shivs":
			return T("Shivs");
		case "Skills":
			return T("Skills repeated");
		case "StarsPerTurn":
			return T("Stars per turn");
		case "Turns":
			return T("Turns");
		case "ViolenceHits":
			return T("Hits");
		case "WisdomCards":
			return T("Cards");
		case "Power":
			return T("Amount");
		default:
			if (key.EndsWith("Power", StringComparison.Ordinal))
			{
				if (!PowerTitles.TryGetValue(key, out var value))
				{
					value = null;
					try
					{
						foreach (PowerModel allPower in ModelDb.AllPowers)
						{
							if (allPower.GetType().Name == key)
							{
								value = allPower.Title.GetFormattedText();
								break;
							}
						}
					}
					catch (Exception)
					{
						value = null;
					}
					PowerTitles[key] = value;
				}
				if (!string.IsNullOrEmpty(value))
				{
					return value;
				}
				key = key.Substring(0, key.Length - "Power".Length);
			}
			return Regex.Replace(key, "(?<=[a-z])(?=[A-Z])", " ");
		}
	}
	public static string PropertyAlready(string name)
	{
		return T("Has " + name);
	}

}
