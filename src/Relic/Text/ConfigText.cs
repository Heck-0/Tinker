using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tinker.Relic;

public static class ConfigText
{
	public static Dictionary<string, string> Values()
	{
		Dictionary<string, string> d = new Dictionary<string, string>();
		Add("Preset", "Preset", "Normal preserves the original rules. Easy and Hard change several settings. Editing a setting switches to Custom.");
		Add("Difficulty", "Upgrade difficulty", "How much bigger upgrades, and upgrades on already-upgraded cards or relics, cost. Higher values raise Workshop prices.");
		Add("SectionGeneral", "General", "");
		Add("SectionPrices", "Prices", "");
		d["RELICUPGRADER-PRESET.Normal"] = "Normal";
		d["RELICUPGRADER-PRESET.Easy"] = "Easy";
		d["RELICUPGRADER-PRESET.Hard"] = "Hard";
		d["RELICUPGRADER-PRESET.Custom"] = "Custom";
		return d;
		void Add(string name, string en, string nh)
		{
			string text = "RELICUPGRADER-" + Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToUpperInvariant();
			d[text + ".title"] = en;
			d[text + ".hover.desc"] = nh;
		}
	}
}
