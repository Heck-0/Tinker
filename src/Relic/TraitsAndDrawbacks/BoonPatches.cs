using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Tinker.Relic;

// Boons granted by successful relic upgrades.

[HarmonyPatch(typeof(Hook), "BeforeCombatStart")]
public static class CombatBoonPatch
{
	public static void Postfix(IRunState runState, ICombatState? combatState, ref Task __result)
	{
		if (combatState != null)
		{
			__result = ApplyAfter(__result, combatState);
		}
	}

	private static async Task ApplyAfter(Task original, ICombatState combat)
	{
		await original;
		BlockingPlayerChoiceContext context = new BlockingPlayerChoiceContext();
		foreach (Player player in combat.Players)
		{
			RelicModel[] array = Boons.Active(player).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				await Boons.ApplyStart(array[i], context);
			}
		}
	}
}

[HarmonyPatch(typeof(Hook), "ModifyHandDraw")]
public static class DrawBoonPatch
{
	public static void Postfix(Player player, ref decimal __result)
	{
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		if (playerCombatState != null && playerCombatState.TurnNumber <= 1)
		{
			__result += (decimal)Boons.Active(player).Count((RelicModel r) => Store.For(r).Boon == "Draw");
		}
	}
}

[HarmonyPatch(typeof(Hook), "AfterSideTurnStart")]
public static class EnergyBoonPatch
{
	public static void Postfix(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, ref Task __result)
	{
		__result = ApplyAfter(__result, participants, combatState);
	}

	private static async Task ApplyAfter(Task original, IReadOnlyList<Creature> participants, ICombatState combat)
	{
		await original;
		foreach (Player player in combat.Players.Where(delegate(Player p)
		{
			if (participants.Contains(p.Creature) && !p.Creature.IsDead)
			{
				PlayerCombatState playerCombatState = p.PlayerCombatState;
				if (playerCombatState != null)
				{
					return playerCombatState.TurnNumber <= 1;
				}
				return false;
			}
			return false;
		}))
		{
			RelicModel[] array = (from r in Boons.Active(player)
				where Store.For(r).Boon == "Energy"
				select r).ToArray();
			for (int num = 0; num < array.Length; num++)
			{
				array[num].Flash();
				await PlayerCmd.GainEnergy(1m, player);
			}
		}
	}
}
