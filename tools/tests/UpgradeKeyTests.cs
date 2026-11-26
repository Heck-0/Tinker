using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tinker.Common;
using Xunit;

namespace Tinker.Tests;

/// <summary>Upgrade keys ("var:Damage", "extra:hits"...): the format, and every key written in the code.</summary>
public class UpgradeKeyTests
{
	[Theory]
	[InlineData("cost", UpgradeKey.Kinds.Cost, "", -1)]
	[InlineData("var:Damage", UpgradeKey.Kinds.Var, "Damage", 1)]
	[InlineData("vardown:Cards", UpgradeKey.Kinds.VarDown, "Cards", -1)]
	[InlineData("extra:hits", UpgradeKey.Kinds.Extra, "hits", 1)]
	[InlineData("extradown:threshold", UpgradeKey.Kinds.ExtraDown, "threshold", -1)]
	[InlineData("effect:frost", UpgradeKey.Kinds.Effect, "frost", 1)]
	[InlineData("enchant:Sharp", UpgradeKey.Kinds.Enchant, "Sharp", 1)]
	[InlineData("enchanteffect:goopyBlock", UpgradeKey.Kinds.EnchantEffect, "goopyBlock", 1)]
	[InlineData("enchantdown:slitherMaximum", UpgradeKey.Kinds.EnchantDown, "slitherMaximum", -1)]
	[InlineData("self:StrengthPower", UpgradeKey.Kinds.SelfPower, "StrengthPower", -1)]
	[InlineData("kw+:Retain", UpgradeKey.Kinds.AddedKeyword, "Retain", -1)]
	[InlineData("var:", UpgradeKey.Kinds.Unknown, null, 1)]
	[InlineData("nonsense", UpgradeKey.Kinds.Unknown, null, 1)]
	[InlineData(null, UpgradeKey.Kinds.Unknown, null, 1)]
	public void Parse(string text, UpgradeKey.Kinds kind, string name, int direction)
	{
		UpgradeKey key = UpgradeKey.Parse(text);
		Assert.Equal(kind, key.Kind);
		Assert.Equal(name, key.Name);
		Assert.Equal(direction, key.Direction);
		Assert.Equal(direction, AttributeDirection.SignOf(text));
		if (kind != UpgradeKey.Kinds.Unknown)
		{
			Assert.Equal(text, UpgradeKey.Of(kind, name));
		}
	}

	[Fact]
	public void VarHelpers()
	{
		Assert.Equal("var:Block", AttributeKeys.Var("Block"));
		Assert.Equal("vardown:Block", AttributeKeys.VarDown("Block"));
		Assert.Equal("Block", AttributeKeys.VarName("vardown:Block"));
		Assert.Null(AttributeKeys.VarName("extra:hits"));
		Assert.True(AttributeKeys.IsVar("var:Block"));
		Assert.False(AttributeKeys.IsVar("cost"));
	}

	/// <summary>
	/// Every upgrade key written in the code ("extra:hits" etc.) parses and is known to the table it belongs to,
	/// so a misspelled key fails here instead of silently never matching in the game.
	/// </summary>
	[Fact]
	public void EveryKeyInTheCodeIsKnown()
	{
		Regex literal = new Regex(@"""((?:vardown|var|extradown|extra|effect|enchanteffect|enchantdown|enchant|self|kw\+):[A-Za-z]+)""");
		HashSet<string> keys = new HashSet<string>();
		foreach (var (path, text) in Repo.SourceFiles())
		{
			foreach (Match m in literal.Matches(text))
			{
				keys.Add(m.Groups[1].Value);
			}
		}
		Assert.True(keys.Count > 50, "found only " + keys.Count + " keys");
		CardMachineState state = new CardMachineState();
		List<string> unknown = new List<string>();
		foreach (string key in keys.OrderBy(k => k))
		{
			try
			{
				state.ApplySuccess(key, 1);
			}
			catch (System.ArgumentException)
			{
				unknown.Add(key);
			}
		}
		Assert.True(unknown.Count == 0, "keys the effect tables don't know: " + string.Join(", ", unknown));
	}
}
