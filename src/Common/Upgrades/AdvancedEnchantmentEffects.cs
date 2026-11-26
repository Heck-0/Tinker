using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

public static class AdvancedEnchantmentEffects
{
	public static readonly IReadOnlyList<AdvancedEnchantmentEffect> Entries = new AdvancedEnchantmentEffect[8]
	{
		E("Goopy", "enchanteffect:goopyBlock", 0, "block", "Accumulated Block"),
		E("Goopy", "enchanteffect:goopyGrowth", 1, "permanentGrowth", "Permanent growth per play"),
		E("Instinct", "enchanteffect:instinctDamage", 100, "percentage", "Additional damage (%)"),
		E("Corrupted", "enchanteffect:corruptedDamage", 50, "percentage", "Additional damage (%)"),
		E("SlumberingEssence", "enchanteffect:slumberingDiscount", 1, "persistentEnergy", "Cost reduction per turn"),
		E("Slither", "enchantdown:slitherMaximum", 3, "threshold", "Maximum random cost"),
		E("TezcatarasEmber", "enchanteffect:emberDamage", 3, "damage", "Additional damage"),
		E("Imbued", "enchanteffect:imbuedReplays", 1, "persistentRepeat", "Automatic plays at combat start")
	};

	private static AdvancedEnchantmentEffect E(string name, string key, int native, string family, string en)
	{
		return new AdvancedEnchantmentEffect
		{
			Enchantment = name,
			Attribute = key,
			Native = native,
			Family = family,
			English = en
		};
	}

	public static IEnumerable<AdvancedEnchantmentEffect> For(string name)
	{
		return Entries.Where((AdvancedEnchantmentEffect e) => e.Enchantment == name);
	}

	public static AdvancedEnchantmentEffect Find(string key)
	{
		return Entries.FirstOrDefault((AdvancedEnchantmentEffect e) => e.Attribute == key);
	}

	public static bool Known(string key)
	{
		return Find(key) != null;
	}
}
