using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace Tinker.Relic;

public static class Text
{
	public static string Language => Lang.Current;

	public static bool Spanish => Lang.IsSpanish(Language);

	public static string T(string spanish, string english, bool? isSpanish = null) => Lang.T(spanish, english, isSpanish);

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
			bool spanish = Spanish;
			manager.GetTable("settings_ui").MergeWith(Lang.Zh(ConfigText.Values(spanish), manager.Language));
		}
	}

	public static string Attribute(string key)
	{

		switch (key)
		{
		case "Heal":
			return T("Curación", "Healing");
		case "Block":
		case "BlockNextTurn":
			return T("Bloqueo", "Block");
		case "Damage":
		case "ExtraDamage":
			return T("Daño", "Damage");
		case "Cards":
			return T("Cartas", "Cards");
		case "Energy":
		case "GainEnergy":
			return T("Energía", "Energy");
		case "DamageThreshold":
			return T("Daño mínimo por golpe", "Damage needed in one hit");
		case "MaxHpLoss":
			return T("Máximo de PV perdidos por turno", "Max HP lost per turn");
		case "NimbleAmount":
			return T("Ágil en recompensas", "Nimble on rewards");
		case "SwiftAmount":
			return T("Veloz en recompensas", "Swift on rewards");
		case "Relics":
			return T("Reliquias extra", "Extra relics");
		case "Elites":
			return T("Élites necesarios", "Elites required");
		case "Dark":
			return T("Orbes de Oscuridad", "Dark orbs");
		case "DamageMinimum":
			return T("Daño mínimo", "Minimum damage");
		case "GoldIncrease":
			return T("Multiplicador de oro", "Gold multiplier");
		case "DamageDecrease":
			return T("Daño recibido (Condena letal)", "Damage taken (lethal Doom)");
		case "VulnerableBonus":
			return T("Daño extra contra Vulnerable", "Extra damage vs Vulnerable");
		case "WeakReduction":
			return T("Reducción extra de Débil", "Extra Weak reduction");
		case "EnergyThreshold":
			return T("Umbral de energía", "Energy threshold");
		case "LoseEnergy":
			return T("Energía perdida", "Energy lost");
		case "AimPower":
			return T("Puntería", "Aim");
		case "EndOfTurnAim":
			return T("Puntería al final del turno", "End-of-turn Aim");
		case "StretchPower":
			return T("Estiramiento", "Stretch");
		case "StrengthPower":
		case "SelfStrength":
			return T("Fuerza", "Strength");
		case "EnemyStrength":
			return T("Fuerza enemiga", "Enemy strength");
		case "DexterityPower":
			return T("Destreza", "Dexterity");
		case "FocusPower":
			return T("Concentración", "Focus");
		case "VigorPower":
			return T("Vigor", "Vigor");
		case "ThornsPower":
			return T("Espinas", "Thorns");
		case "PlatingPower":
			return T("Blindaje", "Plating");
		case "VulnerablePower":
			return T("Vulnerable", "Vulnerable");
		case "WeakPower":
			return T("Débil", "Weak");
		case "PoisonPower":
			return T("Veneno", "Poison");
		case "Gold":
			return T("Oro", "Gold");
		case "MaxHp":
			return T("PV máximos", "Max HP");
		case "Stars":
			return T("Estrellas", "Stars");
		case "Turns":
		case "DamageTurn":
			return T("Turnos necesarios", "Turns required");
		case "OrbCount":
			return T("Orbes necesarios", "Orbs required");
		case "ExhaustAmount":
			return T("Cartas agotadas necesarias", "Exhausts required");
		case "Discount":
			return T("Descuento (%)", "Discount (%)");
		case "HpThreshold":
			return T("Umbral de vida (%)", "HP threshold (%)");
		case "HpLossReduction":
			return T("Reducción de daño", "Damage reduction");
		case "Lightning":
			return T("Rayos", "Lightning");
		case "Shivs":
			return T("Dagas", "Shivs");
		case "Repeat":
			return T("Espacios de orbe", "Orb slots");
		case "Increase":
			return T("Aumento de X", "X bonus");
		case "Summon":
		case "StartOfTurn":
			return T("Invocación", "Summon");
		case "StartOfCombat":
			return T("Invocación inicial", "Starting summon");
		default:
			return key;
		}
	}

}
