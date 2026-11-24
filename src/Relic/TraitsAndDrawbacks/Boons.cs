using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Tinker.Relic;

public static class Boons
{
	public static readonly string[] All = new string[6] { "Strength", "Dexterity", "Block", "Heal", "Energy", "Draw" };

	/// <summary>The boons the Workshop sells for this relic: the allowed ones, or none if it already has one.</summary>
	public static string[] ForSale(RelicModel relic)
	{
		return (Store.For(relic).Boon != null) ? Array.Empty<string>() : All.Where(Settings.Active.AllowsBoon).ToArray();
	}

	/// <summary>Gives the relic a boon bought in the Workshop. False if it can't have that one.</summary>
	public static bool Grant(RelicModel relic, string id)
	{
		if (!ForSale(relic).Contains(id))
		{
			return false;
		}
		Store.For(relic).Boon = id;
		return true;
	}

	public static string Name(string id)
	{
		return id switch
		{
			"Strength" => Text.T("Fierce"), 
			"Dexterity" => Text.T("Agile"), 
			"Block" => Text.T("Protective"), 
			"Heal" => Text.T("Vital"), 
			"Energy" => Text.T("Energized"), 
			"Draw" => Text.T("Inspiring"), 
			_ => id, 
		};
	}

	public static string Description(string id)
	{
		return id switch
		{
			"Strength" => Text.T("At the start of each combat: gain 1 Strength."), 
			"Dexterity" => Text.T("At the start of each combat: gain 1 Dexterity."), 
			"Block" => Text.T("At the start of each combat: gain 4 Block."), 
			"Heal" => Text.T("At the start of each combat: heal 2 HP."), 
			"Energy" => Text.T("On your first turn: gain 1 Energy."), 
			"Draw" => Text.T("On your first turn: draw 1 extra card."), 
			_ => id, 
		};
	}

	public static async Task ApplyStart(RelicModel relic, PlayerChoiceContext context)
	{
		string boon = Store.For(relic).Boon;
		Creature creature = relic.Owner.Creature;
		bool flag = creature.IsDead;
		if (!flag)
		{
			bool flag2;
			switch (boon)
			{
			case null:
			case "Energy":
			case "Draw":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (!flag)
		{
			relic.Flash();
			switch (boon)
			{
			case "Strength":
				await PowerCmd.Apply<StrengthPower>(context, creature, 1m, creature, null);
				break;
			case "Dexterity":
				await PowerCmd.Apply<DexterityPower>(context, creature, 1m, creature, null);
				break;
			case "Block":
				await CreatureCmd.GainBlock(creature, 4m, ValueProp.Unpowered, null);
				break;
			case "Heal":
				await CreatureCmd.Heal(creature, 2m);
				break;
			}
		}
	}

	public static IEnumerable<RelicModel> Active(Player player)
	{
		return player.Relics.Where((RelicModel r) => !r.IsMelted && !r.IsUsedUp && !r.HasBeenRemovedFromState && Store.For(r).Boon != null);
	}
}
