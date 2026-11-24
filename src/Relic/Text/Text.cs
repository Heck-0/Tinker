using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using Tinker.Common;

namespace Tinker.Relic;

public static class Text
{
	public static string Language => Lang.Current;

	public static string T(string english) => Lang.T(english);

	public static void Install(LocManager? manager = null)
	{
		if (manager == null)
		{
			manager = LocManager.Instance;
		}
		if (manager == null)
		{
			return;
		}
		using (Lang.Installing(manager.Language))
		{
			manager.GetTable("relics").MergeWith(new Dictionary<string, string>
			{
				["RELICUPGRADER_REWARD"] = "{label}",
				["RELICUPGRADER_DESCRIPTION"] = "{original}\n\n{details}"
			});
			manager.GetTable("settings_ui").MergeWith(Lang.T(ConfigText.Values(), manager.Language));
		}
	}

	public static string Attribute(string key)
	{

		switch (key)
		{
		case "Heal":
			return T("Healing");
		case "Block":
		case "BlockNextTurn":
			return T("Block");
		case "Damage":
		case "ExtraDamage":
			return T("Damage");
		case "Cards":
			return T("Cards");
		case "Energy":
		case "GainEnergy":
			return T("Energy");
		case "DamageThreshold":
			return T("Damage needed in one hit");
		case "MaxHpLoss":
			return T("Max HP lost per turn");
		case "NimbleAmount":
			return T("Nimble on rewards");
		case "SwiftAmount":
			return T("Swift on rewards");
		case "Relics":
			return T("Extra relics");
		case "Elites":
			return T("Elites required");
		case "Dark":
			return T("Dark orbs");
		case "DamageMinimum":
			return T("Minimum damage");
		case "GoldIncrease":
			return T("Gold multiplier");
		case "DamageDecrease":
			return T("Damage taken (lethal Doom)");
		case "VulnerableBonus":
			return T("Extra damage vs Vulnerable");
		case "WeakReduction":
			return T("Extra Weak reduction");
		case "EnergyThreshold":
			return T("Energy threshold");
		case "LoseEnergy":
			return T("Energy lost");
		case "AimPower":
			return T("Aim");
		case "EndOfTurnAim":
			return T("End-of-turn Aim");
		case "StretchPower":
			return T("Stretch");
		case "StrengthPower":
		case "SelfStrength":
			return T("Strength");
		case "EnemyStrength":
			return T("Enemy strength");
		case "DexterityPower":
			return T("Dexterity");
		case "FocusPower":
			return T("Focus");
		case "VigorPower":
			return T("Vigor");
		case "ThornsPower":
			return T("Thorns");
		case "PlatingPower":
			return T("Plating");
		case "VulnerablePower":
			return T("Vulnerable");
		case "WeakPower":
			return T("Weak");
		case "PoisonPower":
			return T("Poison");
		case "Gold":
			return T("Gold");
		case "MaxHp":
			return T("Max HP");
		case "Stars":
			return T("Stars");
		case "Turns":
		case "DamageTurn":
			return T("Turns required");
		case "OrbCount":
			return T("Orbs required");
		case "ExhaustAmount":
			return T("Exhausts required");
		case "Discount":
			return T("Discount (%)");
		case "HpThreshold":
			return T("HP threshold (%)");
		case "HpLossReduction":
			return T("Damage reduction");
		case "Lightning":
			return T("Lightning");
		case "Shivs":
			return T("Shivs");
		case "Repeat":
			return T("Orb slots");
		case "Increase":
			return T("X bonus");
		case "Summon":
		case "StartOfTurn":
			return T("Summon");
		case "StartOfCombat":
			return T("Starting summon");
		default:
			return key;
		}
	}

}
