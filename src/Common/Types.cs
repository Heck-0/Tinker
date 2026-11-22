namespace Tinker.Common;

// Types both roulettes used to define separately (identically). Names and values are unchanged, so
// settings files (enum names) and saved run rules (enum numbers) read the same as before.

/// <summary>Drop rarity for the support relics and potion.</summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare
}

/// <summary>Settings preset. Custom = values that match no preset.</summary>
public enum SettingsPreset
{
    Normal,
    Easy,
    Hard,
    Custom
}
