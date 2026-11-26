using System;

namespace Tinker.Common;

/// <summary>
/// An upgrade's key: what the Workshop upgrades on a card, as saved in CardUpgraderState and used everywhere else.
/// Written "kind:name" ("var:Damage", "extra:hits", "effect:frost", "enchant:Sharp"), or just "cost". This is the
/// one place that knows the format; tools/tests checks every key written in the code against it.
/// </summary>
public readonly struct UpgradeKey : IEquatable<UpgradeKey>
{
	public enum Kinds
	{
		Unknown,
		/// <summary>The card's energy cost ("cost"); lower is better.</summary>
		Cost,
		/// <summary>One of the card's own values ("var:Damage").</summary>
		Var,
		/// <summary>One of the card's own values where lower is better ("vardown:Cards").</summary>
		VarDown,
		/// <summary>An expanded effect, from ExpandedEffects / BidirectionalEffects ("extra:hits").</summary>
		Extra,
		/// <summary>An expanded effect where lower is better ("extradown:threshold").</summary>
		ExtraDown,
		/// <summary>An orb / evoke effect, from FixedEffects ("effect:frost").</summary>
		Effect,
		/// <summary>The amount of the card's enchantment, from EnchantmentEffects ("enchant:Sharp").</summary>
		Enchant,
		/// <summary>A value inside the card's enchantment, from AdvancedEnchantmentEffects ("enchanteffect:goopyBlock").</summary>
		EnchantEffect,
		/// <summary>An enchantment value where lower is better ("enchantdown:slitherMaximum").</summary>
		EnchantDown,
		/// <summary>A power the card applies to you, reduced ("self:StrengthPower").</summary>
		SelfPower,
		/// <summary>A keyword the card gained, removed ("kw+:Retain").</summary>
		AddedKeyword,
	}

	public const string CostText = "cost";

	private static readonly (string Prefix, Kinds Kind)[] Prefixes =
	{
		("var:", Kinds.Var),
		("vardown:", Kinds.VarDown),
		("extra:", Kinds.Extra),
		("extradown:", Kinds.ExtraDown),
		("effect:", Kinds.Effect),
		("enchant:", Kinds.Enchant),
		("enchanteffect:", Kinds.EnchantEffect),
		("enchantdown:", Kinds.EnchantDown),
		("self:", Kinds.SelfPower),
		("kw+:", Kinds.AddedKeyword),
	};

	/// <summary>The key as written ("var:Damage").</summary>
	public string Text { get; }

	public Kinds Kind { get; }

	/// <summary>The part after the prefix ("Damage"); empty for cost.</summary>
	public string Name { get; }

	private UpgradeKey(string text, Kinds kind, string name)
	{
		Text = text;
		Kind = kind;
		Name = name;
	}

	public static UpgradeKey Parse(string text)
	{
		if (text == CostText)
		{
			return new UpgradeKey(text, Kinds.Cost, "");
		}
		if (text != null)
		{
			foreach ((string prefix, Kinds kind) in Prefixes)
			{
				if (text.Length > prefix.Length && text.StartsWith(prefix, StringComparison.Ordinal))
				{
					return new UpgradeKey(text, kind, text.Substring(prefix.Length));
				}
			}
		}
		return new UpgradeKey(text, Kinds.Unknown, null);
	}

	/// <summary>The key of a kind and name ("var" + "Damage" -> "var:Damage").</summary>
	public static string Of(Kinds kind, string name)
	{
		if (kind == Kinds.Cost)
		{
			return CostText;
		}
		foreach ((string prefix, Kinds k) in Prefixes)
		{
			if (k == kind)
			{
				return prefix + name;
			}
		}
		throw new ArgumentOutOfRangeException(nameof(kind));
	}

	/// <summary>+1 when a higher value is better, -1 when lower is (cost, the "down" kinds, self powers, added keywords).</summary>
	public int Direction => Kind is Kinds.Cost or Kinds.VarDown or Kinds.ExtraDown or Kinds.EnchantDown or Kinds.SelfPower or Kinds.AddedKeyword ? -1 : 1;

	/// <summary>One of the card's own values (var: or vardown:).</summary>
	public bool IsVar => Kind is Kinds.Var or Kinds.VarDown;

	public bool Equals(UpgradeKey other) => Text == other.Text;

	public override bool Equals(object obj) => obj is UpgradeKey other && Equals(other);

	public override int GetHashCode() => Text?.GetHashCode() ?? 0;

	public override string ToString() => Text;
}
