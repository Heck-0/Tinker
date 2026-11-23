using System;
using System.Text.Json;

namespace Tinker.Relic;

public sealed class ModSettings
{
	public SettingsPreset Preset { get; set; }

	public int Difficulty { get; set; } = 100;

	public bool BoonsEnabled { get; set; } = true;

	/// <summary>Workshop: what a boon costs (bought directly, one per relic).</summary>
	public int BoonPrice { get; set; } = 100;

	public bool BoonStrength { get; set; } = true;

	public bool BoonDexterity { get; set; } = true;

	public bool BoonBlock { get; set; } = true;

	public bool BoonHeal { get; set; } = true;

	public bool BoonEnergy { get; set; } = true;

	public bool BoonDraw { get; set; } = true;

	public ModSettings Copy()
	{
		return FromJson(ToJson());
	}

	public string ToJson()
	{
		return JsonSerializer.Serialize(this);
	}

	public static ModSettings FromJson(string json)
	{
		ModSettings? obj = JsonSerializer.Deserialize<ModSettings>(json) ?? throw new FormatException("Empty settings");
		obj.Validate();
		return obj;
	}

	public void Validate()
	{
		if (!Enum.IsDefined(Preset))
		{
			throw new FormatException("Invalid Preset");
		}
		if (!double.IsFinite(Difficulty) || Difficulty < 25 || Difficulty > 300)
		{
			throw new FormatException("Invalid Difficulty");
		}
		if (BoonPrice < 0 || BoonPrice > 9999)
		{
			throw new FormatException("Invalid BoonPrice");
		}
	}

	public bool AllowsBoon(string id)
	{
		bool flag = BoonsEnabled;
		if (flag)
		{
			flag = id switch
			{
				"Strength" => BoonStrength, 
				"Dexterity" => BoonDexterity, 
				"Block" => BoonBlock, 
				"Heal" => BoonHeal, 
				"Energy" => BoonEnergy, 
				"Draw" => BoonDraw, 
				_ => false, 
			};
		}
		return flag;
	}

	public static ModSettings ForPreset(SettingsPreset preset)
	{
		ModSettings modSettings = new ModSettings
		{
			Preset = preset
		};
		if (preset == SettingsPreset.Easy)
		{
			modSettings.Difficulty = 70;
			modSettings.BoonPrice = 75;
		}
		if (preset == SettingsPreset.Hard)
		{
			modSettings.Difficulty = 130;
			modSettings.BoonPrice = 150;
		}
		return modSettings;
	}
}
