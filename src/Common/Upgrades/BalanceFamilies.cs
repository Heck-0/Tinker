using System;

namespace Tinker.Common;

public static class BalanceFamilies
{

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
			UpgradeKey parsed = UpgradeKey.Parse(attribute);
			if (parsed.Kind == UpgradeKey.Kinds.Cost)
			{
				return "cost";
			}
			if (parsed.Kind == UpgradeKey.Kinds.VarDown)
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
