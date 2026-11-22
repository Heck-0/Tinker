using System;

namespace Tinker.Common;

public static class AttributeDirection
{
	public static int SignOf(string attribute)
	{
		if (attribute == "cost")
		{
			return -1;
		}
		if (attribute.StartsWith("vardown:", StringComparison.Ordinal))
		{
			return -1;
		}
		if (attribute.StartsWith("extradown:", StringComparison.Ordinal))
		{
			return -1;
		}
		if (attribute.StartsWith("enchantdown:", StringComparison.Ordinal))
		{
			return -1;
		}
		if (attribute.StartsWith("self:", StringComparison.Ordinal))
		{
			return -1;
		}

		if (attribute.StartsWith("kw+:", StringComparison.Ordinal))
		{
			return -1;
		}
		return 1;
	}
}
