using System;

namespace Tinker.Common;

/// <summary>Shorthands for the most used upgrade keys; the format itself is <see cref="UpgradeKey"/>.</summary>
public static class AttributeKeys
{
	public const string Cost = UpgradeKey.CostText;

	/// <summary>"var:" + the card's value name.</summary>
	public static string Var(string dynamicVarName) => UpgradeKey.Of(UpgradeKey.Kinds.Var, dynamicVarName);

	/// <summary>"vardown:" + the card's value name (lower is better).</summary>
	public static string VarDown(string dynamicVarName) => UpgradeKey.Of(UpgradeKey.Kinds.VarDown, dynamicVarName);

	/// <summary>A key for one of the card's own values (var: or vardown:).</summary>
	public static bool IsVar(string attribute) => UpgradeKey.Parse(attribute).IsVar;

	/// <summary>The card value's name in a var: / vardown: key, else null.</summary>
	public static string VarName(string attribute)
	{
		UpgradeKey key = UpgradeKey.Parse(attribute);
		return key.IsVar ? key.Name : null;
	}
}
