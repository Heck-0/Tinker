using System;
using Tinker.Common;
using Xunit;

namespace Tinker.Tests;

/// <summary>What the mod saves: card upgrades (CardUpgraderState) and its settings.</summary>
public class StateTests
{
	[Fact]
	public void CardStateRoundTrips()
	{
		CardMachineState s = new CardMachineState();
		Assert.Equal(3, s.ApplySuccess("var:Damage", 3));
		Assert.Equal(-1, s.ApplySuccess("cost", 1));
		string json = s.ToJson();
		Assert.Equal("{\"v\":2,\"a\":{\"cost\":1,\"var:Damage\":3}}", json);
		CardMachineState back = CardMachineState.FromJson(json);
		Assert.Equal(3, back.RawDelta("var:Damage"));
		Assert.Equal(-1, back.RawDelta("cost"));
		Assert.False(back.IsEmpty);
	}

	[Fact]
	public void TraitFromBefore180IsIgnored()
	{
		CardMachineState s = CardMachineState.FromJson("{\"v\":2,\"a\":{\"var:Damage\":2},\"p\":{\"id\":\"Echo\",\"g\":0}}");
		Assert.Equal(2, s.RawDelta("var:Damage"));
		Assert.Equal("{\"v\":2,\"a\":{\"var:Damage\":2}}", s.ToJson());
	}

	[Theory]
	[InlineData("{\"v\":1,\"a\":{}}")]
	[InlineData("{\"v\":2,\"a\":{\"var:Damage\":-1}}")]
	[InlineData("{\"v\":2,\"a\":{\"nonsense\":1}}")]
	[InlineData("{\"v\":2}")]
	[InlineData("not json")]
	public void BadCardStateIsRejected(string json)
	{
		Assert.Throws<FormatException>(() => CardMachineState.FromJson(json));
	}

	[Fact]
	public void LowerIsBetterValuesStopAtTheBase()
	{
		CardMachineState s = new CardMachineState();
		s.ApplySuccess("cost", 3);
		s.ConstrainCostToBase(1);
		Assert.Equal(-1, s.RawDelta("cost"));
	}

	[Fact]
	public void SettingsRoundTripAndPresetsAreDetected()
	{
		CardSettings s = CardSettings.ForPreset(SettingsPreset.Hard);
		CardSettings back = CardSettings.FromJson(s.ToJson());
		Assert.Equal(130, back.UpgradeDifficulty);
		Assert.Equal(SettingsPreset.Hard, back.DetectPreset());
		back.WorkshopGoldRate = 40;
		Assert.Equal(SettingsPreset.Custom, back.DetectPreset());
	}

	[Fact]
	public void OldSettingsWithTraitsStillLoad()
	{
		CardSettings s = CardSettings.FromJson("{\"Preset\":0,\"WorkshopGoldRate\":30,\"TraitsEnabled\":true,\"TraitPrice\":150,\"DisabledTraits\":[\"Echo\"]}");
		Assert.NotNull(s);
		Assert.Equal(30, s.WorkshopGoldRate);
		Assert.True(s.EnchantmentsEnabled);
	}
}
