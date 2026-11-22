using System;

namespace Tinker.Common;

public static class BalanceFamilies
{
	public const string Damage = "damage";

	public const string Block = "block";

	public const string Draw = "draw";

	public const string Energy = "energy";

	public const string Repeat = "repeat";

	public const string Orb = "orb";

	public const string Plasma = "plasma";

	public const string OrbRepeat = "orbRepeat";

	public const string Multiplier = "multiplier";

	public const string Scaling = "scaling";

	public const string Power = "power";

	public const string Resource = "resource";

	public const string Drawback = "drawback";

	public const string Cost = "cost";


	public const string Generic = "generic";

	public static string Of(string attribute)
	{
		if (EnchantmentEffects.Known(attribute))
		{
			return EnchantmentEffects.Family(attribute);
		}
		if (AdvancedEnchantmentEffects.Known(attribute))
		{
			return AdvancedEnchantmentEffects.Find(attribute).Family;
		}
		switch (attribute)
		{
		case "effect:plasma":
			return "plasma";
		case "effect:frostPerEnemy":
			return "multiplier";
		case "effect:evokes":
		case "effect:extraEvokes":
		case "effect:extraLightning":
			return "orbRepeat";
		default:
		{
			if (FixedEffects.Known(attribute))
			{
				return "orb";
			}
			if (attribute == "cost")
			{
				return "cost";
			}

			if (attribute.StartsWith("vardown:", StringComparison.Ordinal))
			{
				return "drawback";
			}
			string text = AttributeKeys.VarName(attribute);
			if (text == null)
			{
				return "generic";
			}
			switch (text)
			{
			case "Damage":
			case "OstyDamage":
				return "damage";
			case "ExtraDamage":
			case "CalculationExtra":
				return "multiplier";
			case "Block":
				return "block";
			case "Cards":
				return "draw";
			case "Energy":
				return "energy";
			case "Skills":
			case "Repeat":
			case "HitCount":
			case "Replay":
			case "BurstPower":
			case "DoubleTapPower":
			case "EchoForm":
			case "Attacks":
			case "AmplifyPower":
				return "repeat";
			case "Loop":
				return "scaling";
			case "FocusPower":
			case "DexterityPower":
			case "StrengthPower":
				return "scaling";
			case "Summon":
			case "MaxHp":
			case "Stars":
			case "Forge":
			case "Gold":
			case "Heal":
				return "resource";
			default:
				if (!text.EndsWith("Power", StringComparison.Ordinal))
				{
					return "generic";
				}
				return "power";
			}
		}
		}
	}
}
