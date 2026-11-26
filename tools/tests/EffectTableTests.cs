using System.Linq;
using Tinker.Common;
using Xunit;

namespace Tinker.Tests;

/// <summary>The special-effect tables (src/Common/Upgrades): what each card offers.</summary>
public class EffectTableTests
{
	[Theory]
	[InlineData("Zap", "effect:lightning")]
	[InlineData("BallLightning", "effect:lightning")]
	[InlineData("Coolheaded", "effect:frost")]
	[InlineData("ColdSnap", "effect:frost")]
	[InlineData("Glacier", "effect:frost")]
	[InlineData("Darkness", "effect:dark")]
	[InlineData("Null", "effect:dark")]
	[InlineData("ShadowShield", "effect:dark")]
	[InlineData("Fusion", "effect:plasma")]
	[InlineData("Ignition", "effect:plasma")]
	[InlineData("MeteorStrike", "effect:plasma")]
	[InlineData("Rainbow", "effect:lightning,effect:frost,effect:dark")]
	[InlineData("Chill", "effect:frostPerEnemy")]
	[InlineData("Tempest", "effect:extraLightning")]
	[InlineData("Glasswork", "effect:glass")]
	[InlineData("MultiCast", "effect:extraEvokes")]
	[InlineData("Dualcast", "effect:evokes")]
	[InlineData("Shatter", "effect:evokes")]
	[InlineData("Strike", "")]
	[InlineData("Defend", "")]
	[InlineData("Glasses", "")]
	[InlineData("Zip", "")]
	[InlineData("Shatters", "")]
	[InlineData(null, "")]
	public void FixedEffectsPerCard(string card, string effects)
	{
		Assert.Equal(effects, string.Join(",", FixedEffects.For(card)));
	}

	[Theory]
	[InlineData("Zap", "effect:lightning", 1)]
	[InlineData("Glacier", "effect:frost", 2)]
	[InlineData("MeteorStrike", "effect:plasma", 3)]
	[InlineData("Dualcast", "effect:evokes", 2)]
	[InlineData("MultiCast", "effect:extraEvokes", 0)]
	[InlineData("Tempest", "effect:extraLightning", 0)]
	[InlineData("Chill", "effect:frostPerEnemy", 1)]
	public void FixedEffectNativeValues(string card, string attribute, int native)
	{
		Assert.Equal(native, FixedEffects.Native(card, attribute));
	}

	[Fact]
	public void EffectKeysAreKnownAndSaveable()
	{
		CardMachineState state = new CardMachineState();
		foreach (string key in ExpandedEffects.Entries.Select(e => e.Attribute)
			.Concat(BidirectionalEffects.Entries.SelectMany(e => new[] { e.Up, e.Down }))
			.Concat(AdvancedEnchantmentEffects.Entries.Select(e => e.Attribute))
			.Concat(new[] { "effect:lightning", "effect:glass", "var:Damage", "vardown:Cards", "cost", "self:StrengthPower", "kw+:Retain", "enchant:Sharp" })
			.Distinct())
		{
			state.ApplySuccess(key, 1);
		}
		CardMachineState back = CardMachineState.FromJson(state.ToJson());
		Assert.Equal(state.ToJson(), back.ToJson());
	}
}
