using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Tinker.Relic;

[HarmonyPatch(typeof(Toolbox), "BeforeHandDraw")]
public static class ToolboxSelectionPatch
{
	public static bool Prefix(Toolbox __instance, Player player, PlayerChoiceContext choiceContext, ref Task __result)
	{
		if (__instance.DynamicVars.Cards.IntValue <= 3)
		{
			return true;
		}
		__result = Choose(__instance, player, choiceContext);
		return false;
	}

	private static async Task Choose(Toolbox relic, Player player, PlayerChoiceContext context)
	{
		if (player != relic.Owner)
		{
			return;
		}
		PlayerCombatState? playerCombatState = relic.Owner.PlayerCombatState;
		if (playerCombatState == null || playerCombatState.TurnNumber != 1)
		{
			return;
		}
		relic.Flash();
		List<CardModel> list = CardFactory.GetDistinctForCombat(relic.Owner, ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint), relic.DynamicVars.Cards.IntValue, relic.Owner.RunState.Rng.CombatCardGeneration).ToList();
		if (list.Count == 0)
		{
			return;
		}
		LocString locString = new LocString("relics", "RELICUPGRADER_REWARD");
		locString.Add("label", Text.T("Choose a card to add to your hand."));
		foreach (CardModel item in await CardSelectCmd.FromSimpleGrid(context, list, relic.Owner, new CardSelectorPrefs(locString, 1)))
		{
			await CardPileCmd.AddGeneratedCardToCombat(item, PileType.Hand, player);
		}
	}
}
