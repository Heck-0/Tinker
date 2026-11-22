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

	public static double Chance(RelicModel relic, double rawChance)
	{
		if (Store.For(relic).Boon == null)
		{
			return Rules.BoonChance((double)Rules.Percent(rawChance) / 100.0);
		}
		return 0.0;
	}

	public static string? TryGrant(RelicModel relic, double rawChance, double chanceRoll, double pickRoll)
	{
		RelicState relicState = Store.For(relic);
		if (relicState.Boon != null || chanceRoll >= Chance(relic, rawChance))
		{
			return null;
		}
		string[] array = All.Where(Settings.Active.AllowsBoon).ToArray();
		if (array.Length == 0)
		{
			return null;
		}
		return relicState.Boon = array[Math.Min(array.Length - 1, (int)(Math.Clamp(pickRoll, 0.0, 0.999999999) * (double)array.Length))];
	}

	public static string Name(string id)
	{
		return id switch
		{
			"Strength" => Text.T("Furiosa", "Fierce"), 
			"Dexterity" => Text.T("Ágil", "Agile"), 
			"Block" => Text.T("Protectora", "Protective"), 
			"Heal" => Text.T("Vital", "Vital"), 
			"Energy" => Text.T("Energizada", "Energized"), 
			"Draw" => Text.T("Inspiradora", "Inspiring"), 
			_ => id, 
		};
	}

	public static string Description(string id)
	{
		return id switch
		{
			"Strength" => Text.T("Al iniciar cada combate: ganas 1 Fuerza.", "At the start of each combat: gain 1 Strength."), 
			"Dexterity" => Text.T("Al iniciar cada combate: ganas 1 Destreza.", "At the start of each combat: gain 1 Dexterity."), 
			"Block" => Text.T("Al iniciar cada combate: ganas 4 de Bloqueo.", "At the start of each combat: gain 4 Block."), 
			"Heal" => Text.T("Al iniciar cada combate: te curas 2 PV.", "At the start of each combat: heal 2 HP."), 
			"Energy" => Text.T("En tu primer turno: ganas 1 Energía.", "On your first turn: gain 1 Energy."), 
			"Draw" => Text.T("En tu primer turno: robas 1 carta más.", "On your first turn: draw 1 extra card."), 
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
