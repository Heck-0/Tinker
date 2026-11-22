using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Card;

public static class Lines
{
	private static readonly Dictionary<string, string> PowerTitles = new Dictionary<string, string>();

	public static bool Spanish => Lang.Spanish;

	public static string Before => T("Antes", "Before");

	public static string After => T("Después", "After");

	public static string PropertyChanceTiny => T("< 1 % de ganar una:", "<1% chance of one:");

	public static string PropertyNone => T("Ninguna le sirve a esta carta", "None fit this card");

	public static string PropertyPickCard => T("Rara vez:", "Rarely:");

	public static string Failure => T("Fallo", "Failure");

	public static string Proceed => T("Continuar", "Proceed");

	public static string DisabledBySettings => T("Desactivado en los ajustes.", "Turned off in the settings.");

	public static string TraitsOff => T("Propiedades desactivadas en los ajustes", "Traits are turned off in the settings");

	public static bool IsSpanish(string language) => Lang.IsSpanish(language);

	private static string T(string es, string en) => Lang.T(es, en);

	public static string Pct(int percent)
	{
		if (Spanish)
		{
			return $"{percent} %";
		}
		return $"{percent}%";
	}

	public static string AttributeName(string attribute)
	{
		switch (attribute)
		{
		case "effect:lightning":
			return T("Orbes de Rayo", "Lightning orbs");
		case "effect:frost":
			return T("Orbes de Escarcha", "Frost orbs");
		case "effect:dark":
			return T("Orbes de Oscuridad", "Dark orbs");
		case "effect:plasma":
			return T("Orbes de Plasma", "Plasma orbs");
		case "effect:frostPerEnemy":
			return T("Escarcha por enemigo", "Frost per enemy");
		case "effect:evokes":
			return T("Descargas del orbe", "Orb evocations");
		case "effect:extraEvokes":
			return T("Descargas adicionales", "Extra evocations");
		default:
		{
			if (attribute == "cost")
			{
				return T("Coste", "Cost");
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
			return T("Daño", "Damage");
		case "Block":
			return T("Bloqueo", "Block");
		case "Cards":
			return T("Cartas", "Cards");
		case "Energy":
			return T("Energía", "Energy");
		case "HpLoss":
			return T("Pérdida de PV", "HP loss");
		case "Heal":
			return T("Curación", "Heal");
		case "Gold":
			return T("Oro", "Gold");
		case "MaxHp":
			return T("PV máximos", "Max HP");
		case "Stars":
			return T("Estrellas", "Stars");
		case "Summon":
			return T("Invocación", "Summon");
		case "Forge":
			return T("Forja", "Forge");
		case "Repeat":
			return T("Repeticiones", "Repeats");
		case "Sadness":
			return T("Tristeza añadida", "Sadness added");
		case "Joy":
			return T("Cartas de Alegría", "Joy cards");
		case "OnKillDamage":
			return T("Daño al matar", "Damage on kill");
		case "AimMultiplier":
			return T("Multiplicador de Puntería", "Aim multiplier");
		case "BaseAim":
			return T("Puntería base", "Base Aim");
		case "HitCount":
			return T("Golpes", "Hits");
		case "ExtraDamage":
			return T("Daño extra", "Extra damage");
		case "OstyDamage":
			return T("Daño de Osty", "Osty damage");
		case "CalculationBase":
			return T("Valor base", "Base value");
		case "CalculationExtra":
			return T("Valor por cada", "Value per");
		case "Accelerant":
			return T("Activaciones del Veneno", "Poison triggers");
		case "Attacks":
			return T("Ataques repetidos", "Attacks repeated");
		case "BlockForStars":
			return T("Bloqueo por estrella", "Block per star");
		case "BlockNextTurn":
			return T("Bloqueo del turno siguiente", "Next turn Block");
		case "BlockOnExhaust":
			return T("Bloqueo por carta Etérea", "Block per Ethereal card");
		case "BombDamage":
			return T("Daño de la bomba", "Bomb damage");
		case "ChokingDamage":
			return T("Daño de asfixia", "Choking damage");
		case "CorrosiveWave":
			return T("Veneno por robo", "Poison per draw");
		case "CreativeAi":
			return T("Poderes por turno", "Powers per turn");
		case "CuriousReduction":
			return T("Rebaja de coste", "Cost reduction");
		case "DamageBack":
			return T("Daño devuelto", "Damage back");
		case "EchoForm":
			return T("Cartas repetidas", "Cards repeated");
		case "EnemyStrength":
			return T("Fuerza del enemigo", "Enemy Strength");
		case "EnemyStrengthLoss":
			return T("Fuerza que pierde el enemigo", "Enemy Strength loss");
		case "StrengthLoss":
			return T("Fuerza que pierde el enemigo", "Enemy Strength loss");
		case "PlayerStrengthLoss":
			return T("Fuerza que pierdes", "Strength you lose");
		case "EnergizedEnergy":
			return T("Energía", "Energy");
		case "ExpertiseDexterity":
			return T("Destreza", "Dexterity");
		case "ExpertiseStrength":
			return T("Fuerza", "Strength");
		case "ExtraBlock":
			return T("Bloqueo extra", "Extra Block");
		case "ExtraCost":
			return T("Coste extra", "Extra cost");
		case "Frail":
			return T("Frágil", "Frail");
		case "SappingVulnerable":
			return T("Vulnerable", "Vulnerable");
		case "SappingWeak":
			return T("Débil", "Weak");
		case "Increase":
			return T("Aumento de daño", "Damage increase");
		case "Loop":
			return T("Activaciones del orbe", "Orb triggers");
		case "OrbSlots":
			return T("Huecos de orbe perdidos", "Orb slots lost");
		case "PanacheDamage":
			return T("Daño a todos", "Damage to all");
		case "PlayMax":
			return T("Límite de cartas", "Card limit");
		case "PoisonPerTurn":
			return T("Veneno por turno", "Poison per turn");
		case "Replay":
			return T("Repetición", "Replay");
		case "Shivs":
			return T("Navajas", "Shivs");
		case "Skills":
			return T("Habilidades repetidas", "Skills repeated");
		case "StarsPerTurn":
			return T("Estrellas por turno", "Stars per turn");
		case "Turns":
			return T("Turnos", "Turns");
		case "ViolenceHits":
			return T("Golpes", "Hits");
		case "WisdomCards":
			return T("Cartas", "Cards");
		case "Power":
			return T("Cantidad", "Amount");
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
	public static string PropertyChance(double percent)
	{
		return T($"{percent:0.#} % de ganar una:", $"{percent:0.#}% chance of one:");
	}

	public static string PropertyAlready(string name)
	{
		return T("Ya tiene " + name, "Has " + name);
	}

	public static string PropertyGained(string name)
	{
		return T("¡Nueva propiedad: " + name + "!", "New trait: " + name + "!");
	}

}
