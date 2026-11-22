using System;

namespace Tinker.Common;

public static class AttributeKeys
{
	public const string VarPrefix = "var:";

	public const string VarDownPrefix = "vardown:";

	public const string Cost = "cost";

	public const string SelfPowerPrefix = "self:";

	public const string AddedKeywordPrefix = "kw+:";


	public static string Var(string dynamicVarName)
	{
		return "var:" + dynamicVarName;
	}

	public static string VarDown(string dynamicVarName)
	{
		return "vardown:" + dynamicVarName;
	}

	public static bool IsVar(string attribute)
	{
		if (attribute != null)
		{
			if (!attribute.StartsWith("var:", StringComparison.Ordinal) || attribute.Length <= "var:".Length)
			{
				if (attribute.StartsWith("vardown:", StringComparison.Ordinal))
				{
					return attribute.Length > "vardown:".Length;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static string VarName(string attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		if (attribute.StartsWith("var:", StringComparison.Ordinal))
		{
			return attribute.Substring("var:".Length);
		}
		if (attribute.StartsWith("vardown:", StringComparison.Ordinal))
		{
			return attribute.Substring("vardown:".Length);
		}
		return null;
	}
}
