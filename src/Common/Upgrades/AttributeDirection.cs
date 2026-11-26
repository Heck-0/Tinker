using System;

namespace Tinker.Common;

public static class AttributeDirection
{
	/// <summary>+1 if a higher value is better, -1 if lower is (see <see cref="UpgradeKey.Direction"/>).</summary>
	public static int SignOf(string attribute) => UpgradeKey.Parse(attribute).Direction;
}
