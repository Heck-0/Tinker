using System.Reflection;
using MegaCrit.Sts2.Core.HoverTips;

namespace Tinker;

/// <summary>
/// Shims for Slay the Spire 2 v0.113.0, where HoverTip.Id and HoverTip.IsDebuff lost their setters
/// (the game now derives Id from the LocString key in the constructor).
///
/// Both mods build many tooltips from one shared LocString key and tell them apart with a custom Id,
/// so we still need to set it: write the auto-property backing fields directly. If a later game
/// version renames them, the tooltip simply keeps the game's defaults.
/// </summary>
internal static class GameCompat113
{
    private static readonly FieldInfo IdField = Backing("Id");

    private static readonly FieldInfo IsDebuffField = Backing("IsDebuff");

    private static FieldInfo Backing(string property)
    {
        return typeof(HoverTip).GetField("<" + property + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public static HoverTip WithIdentity(HoverTip tip, string id, bool? isDebuff = null)
    {
        object boxed = tip;
        IdField?.SetValue(boxed, id);
        if (isDebuff.HasValue)
        {
            IsDebuffField?.SetValue(boxed, isDebuff.Value);
        }
        return (HoverTip)boxed;
    }
}
