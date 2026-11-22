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
		Add("BoonsEnabled", "Activar bufos nuevos", "Enable new boons", "Permite que una compra en el taller dé un bufo a la reliquia. Los ya obtenidos permanecen activos.", "Lets a Workshop purchase give the relic a boon. Boons already gained stay active.");
		Add("BoonBase", "Factor de bufo", "Boon probability factor", "Probabilidad de bufo en cada compra, limitada por el máximo. Las mejoras más caras dan más.", "Chance of a boon on each purchase, limited by the cap. Pricier upgrades give a better chance.");
		Add("BoonMax", "Bufo máximo", "Maximum boon chance", "Probabilidad máxima de bufo en una sola compra.", "Highest boon chance on a single purchase.");
		Add("BoonStrength", "Furiosa", "Fierce", "Permite este bufo en nuevas compras. Los permitidos tienen el mismo peso.", "Allows this boon on new purchases. Allowed boons are equally likely.");
		Add("BoonDexterity", "Ágil", "Agile", "Permite este bufo en nuevas compras. Los permitidos tienen el mismo peso.", "Allows this boon on new purchases. Allowed boons are equally likely.");
		Add("BoonBlock", "Protectora", "Protective", "Permite este bufo en nuevas compras. Los permitidos tienen el mismo peso.", "Allows this boon on new purchases. Allowed boons are equally likely.");
		Add("BoonHeal", "Vital", "Vital", "Permite este bufo en nuevas compras. Los permitidos tienen el mismo peso.", "Allows this boon on new purchases. Allowed boons are equally likely.");
		Add("BoonEnergy", "Energizada", "Energized", "Permite este bufo en nuevas compras. Los permitidos tienen el mismo peso.", "Allows this boon on new purchases. Allowed boons are equally likely.");
		Add("BoonDraw", "Inspiradora", "Inspiring", "Permite este bufo en nuevas compras. Los permitidos tienen el mismo peso.", "Allows this boon on new purchases. Allowed boons are equally likely.");
		Add("SectionGeneral", "General", "General", "", "");
		Add("SectionOdds", "Precios", "Prices", "", "");
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
