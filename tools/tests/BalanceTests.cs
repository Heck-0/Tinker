using System;
using System.Collections.Generic;
using System.Linq;
using Tinker.Common;
using Xunit;

namespace Tinker.Tests;

/// <summary>content/balance.jsonc (built in as the defaults) and the price formula.</summary>
public class BalanceTests
{
	[Fact]
	public void BuiltInBalanceLoadsWithoutWarnings()
	{
		// The constructor throws if the embedded file has a bad value; the mod would fail to load.
		BalanceConfig b = new BalanceConfig();
		Assert.True(b.Families.Count > 30);
		Assert.InRange(b.StrainFactor, 0.0, 5.0);
	}

	[Fact]
	public void EveryFamilyTheCodeUsesExists()
	{
		BalanceConfig b = new BalanceConfig();
		List<string> used = new List<string>();
		used.AddRange(ExpandedEffects.Entries.Select(e => e.Family));
		used.AddRange(BidirectionalEffects.Entries.Select(e => e.Family));
		used.AddRange(AdvancedEnchantmentEffects.Entries.Select(e => e.Family));
		foreach (string name in new[] { "Sown", "Spiral", "Glam", "Swift", "Sharp", "Vigorous", "Adroit", "Nimble", "Momentum", "Inky" })
		{
			used.Add(EnchantmentEffects.Family(EnchantmentEffects.Key(name)));
		}
		// Families picked in code by name (relic defaults, trigger tiers, card risk families).
		used.AddRange(new[] { "damage", "block", "draw", "energy", "repeat", "scaling", "power", "resource", "generic", "threshold", "drawback",
			"percentage", "protection", "duration", "generation", "selection", "persistentEnergy", "persistentDraw", "persistentGeneration",
			"persistentRepeat", "persistentDefense", "permanentGrowth", "deckRemoval", "orb", "cost",
			"triggerConstant", "triggerTurn", "triggerOccasional", "triggerCombat", "triggerRun" });
		string[] missing = used.Where(f => f != null).Distinct().Where(f => !b.Families.ContainsKey(f)).OrderBy(f => f).ToArray();
		Assert.True(missing.Length == 0, "families missing from balance.jsonc: " + string.Join(", ", missing));
	}

	[Fact]
	public void EnchantmentTableIsComplete()
	{
		BalanceConfig b = new BalanceConfig();
		Assert.Equal(22, b.Enchantments.Count);
		foreach (KeyValuePair<string, EnchantmentOffer> e in b.Enchantments)
		{
			Assert.True(e.Value.Price > 0, e.Key + " has no price");
			Assert.True(e.Value.Amount >= 1, e.Key + " has no amount");
		}
		Assert.Equal(400, b.Enchantments["Instinct"].Price);
		Assert.Null(b.Enchantments["Sharp"].Relic);
	}

	[Fact]
	public void OverrideFileChangesOnlyWhatItNames()
	{
		BalanceConfig b = new BalanceConfig();
		double damageWeight = b.Families["damage"].Weight;
		List<string> warnings = b.ApplyOverrides("{ \"families\": { \"block\": { \"weight\": 2 }, \"nope\": { \"weight\": 1 } }, \"enchantments\": { \"Sharp\": { \"price\": 90 }, \"Nope\": { \"price\": 1 } } }");
		Assert.Equal(2.0, b.Families["block"].Weight);
		Assert.Equal(damageWeight, b.Families["damage"].Weight);
		Assert.Equal(90, b.Enchantments["Sharp"].Price);
		Assert.Equal(2, b.Enchantments["Sharp"].Amount);
		Assert.Contains(warnings, w => w.Contains("\"nope\""));
		Assert.Contains(warnings, w => w.Contains("\"Nope\""));
		Assert.Equal(2, warnings.Count);
	}

	[Fact]
	public void BadOverrideFileIsIgnoredWithAWarning()
	{
		BalanceConfig b = new BalanceConfig();
		List<string> warnings = b.ApplyOverrides("{ not json");
		Assert.Single(warnings);
		Assert.Equal(0.8, b.StrainFactor);
	}

	[Theory]
	[InlineData(0.0, 25, 25)]
	[InlineData(1.0, 25, 68)]
	[InlineData(-50.0, 25, 1)]
	[InlineData(1000.0, 25, int.MaxValue)]
	public void GoldPrice(double exponent, int rate, int gold)
	{
		Assert.Equal(gold, WorkshopPrice.Gold(exponent, rate));
	}

	[Fact]
	public void SmallDamageUpgradeCostsAboutTheRate()
	{
		// +2 damage on a 6-damage card: 25 × e^(0.65 × 2/6) ≈ 31 gold.
		BalanceConfig b = new BalanceConfig();
		double e = UpgradeCost.Exponent(new UpgradeRequest("var:Damage", 6, 2, riskFamily: "damage"), b);
		Assert.Equal(31, WorkshopPrice.Gold(e, 25));
		// Each extra unit costs more; earlier upgrades raise the price.
		double bigger = UpgradeCost.Exponent(new UpgradeRequest("var:Damage", 6, 4, riskFamily: "damage"), b);
		double after = UpgradeCost.Exponent(new UpgradeRequest("var:Damage", 6, 2, existingGain: 2, riskFamily: "damage"), b);
		Assert.True(bigger > e && after > e);
	}

	[Fact]
	public void DifficultyScalesEveryFamily()
	{
		BalanceConfig normal = CardSettings.ForPreset(SettingsPreset.Normal).ApplyTo(new BalanceConfig());
		BalanceConfig hard = CardSettings.ForPreset(SettingsPreset.Hard).ApplyTo(new BalanceConfig());
		foreach (string f in normal.Families.Keys)
		{
			Assert.Equal(normal.Families[f].Weight * 1.3, hard.Families[f].Weight, 9);
		}
	}
}
