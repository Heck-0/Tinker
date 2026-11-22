using System;
using System.Text.Json;

namespace Tinker.Relic;

public sealed class ModSettings
{
	public SettingsPreset Preset { get; set; }

	public int Difficulty { get; set; } = 100;

	public int MaxChance { get; set; } = 95;

	public int MinChance { get; set; } = 3;

	public bool BoonsEnabled { get; set; } = true;

	public int BoonBase { get; set; } = 20;

	public int BoonMax { get; set; } = 80;

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
		if (!double.IsFinite(MaxChance) || MaxChance < 10 || MaxChance > 100)
		{
			throw new FormatException("Invalid MaxChance");
		}
		if (!double.IsFinite(MinChance) || MinChance < 0 || MinChance > 50)
		{
			throw new FormatException("Invalid MinChance");
		}
		if (!double.IsFinite(BoonBase) || BoonBase < 0 || BoonBase > 100)
		{
			throw new FormatException("Invalid BoonBase");
		}
		if (!double.IsFinite(BoonMax) || BoonMax < 0 || BoonMax > 100)
		{
			throw new FormatException("Invalid BoonMax");
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
			modSettings.MinChance = 5;
			modSettings.BoonBase = 30;
			modSettings.BoonMax = 90;
		}
		if (preset == SettingsPreset.Hard)
		{
			modSettings.Difficulty = 130;
			modSettings.MaxChance = 90;
			modSettings.MinChance = 2;
			modSettings.BoonBase = 15;
			modSettings.BoonMax = 70;
		}
		return modSettings;
	}
}
