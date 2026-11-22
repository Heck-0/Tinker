using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

public static class AdvancedEnchantmentEffects
{
	public const string GoopyBlock = "enchanteffect:goopyBlock";

	public const string GoopyGrowth = "enchanteffect:goopyGrowth";

	public const string Instinct = "enchanteffect:instinctDamage";

	public const string Corrupted = "enchanteffect:corruptedDamage";

	public const string Slumbering = "enchanteffect:slumberingDiscount";

	public const string Slither = "enchantdown:slitherMaximum";

	public const string Ember = "enchanteffect:emberDamage";

	public const string Imbued = "enchanteffect:imbuedReplays";

	public static readonly IReadOnlyList<AdvancedEnchantmentEffect> Entries = new AdvancedEnchantmentEffect[8]
	{
		E("Goopy", "enchanteffect:goopyBlock", 0, "block", "Bloqueo acumulado", "Accumulated Block"),
		E("Goopy", "enchanteffect:goopyGrowth", 1, "permanentGrowth", "Crecimiento permanente por uso", "Permanent growth per play"),
		E("Instinct", "enchanteffect:instinctDamage", 100, "percentage", "Daño adicional (%)", "Additional damage (%)"),
		E("Corrupted", "enchanteffect:corruptedDamage", 50, "percentage", "Daño adicional (%)", "Additional damage (%)"),
		E("SlumberingEssence", "enchanteffect:slumberingDiscount", 1, "persistentEnergy", "Rebaja de coste por turno", "Cost reduction per turn"),
		E("Slither", "enchantdown:slitherMaximum", 3, "threshold", "Coste aleatorio máximo", "Maximum random cost"),
		E("TezcatarasEmber", "enchanteffect:emberDamage", 3, "damage", "Daño adicional", "Additional damage"),
		E("Imbued", "enchanteffect:imbuedReplays", 1, "persistentRepeat", "Jugadas automáticas al inicio", "Automatic plays at combat start")
	};

	private static AdvancedEnchantmentEffect E(string name, string key, int native, string family, string es, string en)
	{
		return new AdvancedEnchantmentEffect
		{
			Enchantment = name,
			Attribute = key,
			Native = native,
			Family = family,
			Spanish = es,
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
