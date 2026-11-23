using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tinker.Relic;

public static class ConfigText
{
	public static Dictionary<string, string> Values(bool spanish)
	{
		Dictionary<string, string> d = new Dictionary<string, string>();
		Add("Preset", "Preajuste", "Preset", "Normal conserva las reglas originales. Fácil y Difícil modifican varios ajustes. Al cambiar uno, pasa a Personalizado.", "Normal preserves the original rules. Easy and Hard change several settings. Editing a setting switches to Custom.");
		Add("Difficulty", "Dificultad de las mejoras", "Upgrade difficulty", "Cuánto encarecen las mejoras grandes y las de cartas o reliquias ya mejoradas. Un valor mayor sube los precios del taller.", "How much bigger upgrades, and upgrades on already-upgraded cards or relics, cost. Higher values raise Workshop prices.");
		Add("BoonsEnabled", "Bufos en el taller", "Boons in the Workshop", "Si los quitas, el taller no vende bufos. Los ya comprados siguen activos.", "Turn them off and the Workshop doesn't sell boons. Boons already bought stay active.");
		Add("BoonPrice", "Precio de un bufo", "Boon price", "Oro que cuesta comprar un bufo para una reliquia. Uno por reliquia.", "Gold to buy a boon for a relic. One per relic.");
		Add("BoonStrength", "Furiosa", "Fierce", "El taller vende este bufo.", "The Workshop sells this boon.");
		Add("BoonDexterity", "Ágil", "Agile", "El taller vende este bufo.", "The Workshop sells this boon.");
		Add("BoonBlock", "Protectora", "Protective", "El taller vende este bufo.", "The Workshop sells this boon.");
		Add("BoonHeal", "Vital", "Vital", "El taller vende este bufo.", "The Workshop sells this boon.");
		Add("BoonEnergy", "Energizada", "Energized", "El taller vende este bufo.", "The Workshop sells this boon.");
		Add("BoonDraw", "Inspiradora", "Inspiring", "El taller vende este bufo.", "The Workshop sells this boon.");
		Add("SectionGeneral", "General", "General", "", "");
		Add("SectionPrices", "Precios", "Prices", "", "");
		Add("SectionBoons", "Bufos", "Boons", "", "");
		d["RELICUPGRADER-PRESET.Normal"] = Text.T("Normal", "Normal", spanish);
		d["RELICUPGRADER-PRESET.Easy"] = Text.T("Fácil", "Easy", spanish);
		d["RELICUPGRADER-PRESET.Hard"] = Text.T("Difícil", "Hard", spanish);
		d["RELICUPGRADER-PRESET.Custom"] = Text.T("Personalizado", "Custom", spanish);
		return d;
		void Add(string name, string es, string en, string eh, string nh)
		{
			string text = "RELICUPGRADER-" + Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToUpperInvariant();
			d[text + ".title"] = Text.T(es, en, spanish);
			d[text + ".hover.desc"] = Text.T(eh, nh, spanish);
		}
	}
}
