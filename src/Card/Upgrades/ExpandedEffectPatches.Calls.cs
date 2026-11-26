using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Tinker.Common;

namespace Tinker.Card;

// What the patched card code calls instead of the game's own: the native value adjusted by the card's upgrades.
internal static partial class ExpandedEffectPatches
{
	internal static int AdjustInt(int native, CardModel card, string attribute)
	{
		checked
		{
			switch (attribute)
			{
			case "extra:hangGrowth":
				return (int)Math.Min(999999999m, (decimal)native * (1m + (decimal)Delta(card, attribute)));
			case "extra:discount":
				return native * (1 + Delta(card, attribute));
			case "extra:killReplays":
				return native * (1 + Delta(card, attribute));
			default:
			{
				if (AttributeKeys.IsVar(attribute))
				{
					return card.DynamicVars[AttributeKeys.VarName(attribute)].IntValue;
				}
				int delta = Delta(card, attribute);
				if (attribute == "extra:hits" && card.GetType().Name == "Dismantle" && native == 1)
				{
					return native;
				}
				if (!(attribute == "extra:multiplier"))
				{
					return native + delta;
				}
				return native * (1 + delta);
			}
			}
		}
	}

	internal static decimal AdjustDecimal(decimal native, CardModel card, string attribute)
	{
		switch (attribute)
		{
		case "extra:splashPercent":
			return native * (100m + (decimal)Delta(card, attribute)) / 100m;
		case "extradown:regretPercent":
			return native * Math.Max(0m, 100m + (decimal)Delta(card, attribute)) / 100m;
		case "extra:discount":
			return native * (decimal)checked(1 + Delta(card, attribute));
		default:
		{
			if (AttributeKeys.IsVar(attribute))
			{
				return native * card.DynamicVars[AttributeKeys.VarName(attribute)].BaseValue;
			}
			int delta = Delta(card, attribute);
			if (!(attribute == "extra:multiplier"))
			{
				return native + (decimal)delta;
			}
			return native * (1m + (decimal)delta);
		}
		}
	}

	private static async Task Passive(PlayerChoiceContext context, OrbModel orb, Creature target, bool silent, CardModel card)
	{
		int count = 1 + (CardStateStore.Get(card)?.RawDelta("extra:passive") ?? 0);
		MethodInfo passive = AccessTools.GetDeclaredMethods(typeof(OrbCmd)).Single((MethodInfo m) => m.Name == "Passive" && m.GetParameters().Length >= 3);
		object[] arguments = ((passive.GetParameters().Length != 3) ? new object[4] { context, orb, target, silent } : new object[3] { context, orb, target });
		for (int i = 0; i < count; i++)
		{
			await (Task)passive.Invoke(null, arguments);
		}
	}

	internal static bool AdjustPlayability(bool native, CardModel card, string attribute)
	{
		int delta = Delta(card, attribute);
		if (native || delta <= 0)
		{
			return native;
		}
		return ((attribute == "extra:drawTolerance") ? PileType.Draw.GetPile(card.Owner).Cards.Count : PileType.Hand.GetPile(card.Owner).Cards.Count((CardModel c) => c.Type != CardType.Attack)) <= delta;
	}

	private static Task PassiveStable(PlayerChoiceContext context, OrbModel orb, Creature target, CardModel card)
	{
		return Passive(context, orb, target, silent: false, card);
	}

	private static int Delta(CardModel card, string key)
	{
		return BidirectionalEffects.RawDelta(card.GetType().Name, key, CardStateStore.Get(card));
	}

	private static void IncrementSelfDamage(PowerModel power, CardModel card)
	{
		if (power != null)
		{
			power.DynamicVars["SelfDamage"].BaseValue = Math.Max((card is Inferno) ? 1 : 0, power.DynamicVars["SelfDamage"].BaseValue + 1m + (decimal)Delta(card, "extradown:selfDamage"));
		}
	}

	internal static int RandomExhaustCount(CardModel source)
	{
		return Math.Max(0, 1 + Delta(source, (source is TrueGrit) ? "extra:exhaustMaximum" : "extradown:randomExhaust"));
	}

	private static async Task RandomExhaust(PlayerChoiceContext context, CardModel selected, bool causedByEthereal, bool skipVisuals, CardModel source)
	{
		int count = RandomExhaustCount(source);
		if (count != 0)
		{
			await GameCompat.Exhaust(context, selected, causedByEthereal, skipVisuals);
			await ExtraRandomExhausts(context, source, count - 1, causedByEthereal, skipVisuals);
		}
	}

	private static async Task<T> RandomExhaustResult<T>(PlayerChoiceContext context, CardModel selected, bool causedByEthereal, bool skipVisuals, CardModel source)
	{
		int count = RandomExhaustCount(source);
		if (count == 0)
		{
			return default(T);
		}
		T result = await (Task<T>)GameCompat.Exhaust(context, selected, causedByEthereal, skipVisuals);
		await ExtraRandomExhausts(context, source, count - 1, causedByEthereal, skipVisuals);
		return result;
	}

	private static async Task ExtraRandomExhausts(PlayerChoiceContext context, CardModel source, int count, bool causedByEthereal, bool skipVisuals)
	{
		for (int i = 0; i < count; i++)
		{
			if (source.Owner.Creature.IsDead)
			{
				break;
			}
			if (CombatManager.Instance.IsOverOrEnding)
			{
				break;
			}
			CardModel cardModel = source.Owner.RunState.Rng.CombatCardSelection.NextItem(PileType.Hand.GetPile(source.Owner).Cards);
			if (cardModel == null)
			{
				break;
			}
			await GameCompat.Exhaust(context, cardModel, causedByEthereal, skipVisuals);
		}
	}

	private static async Task AutoReplays(PlayerChoiceContext context, CardModel played, Creature target, AutoPlayType type, bool skipXCapture, bool skipCardPileVisuals, CardModel source)
	{
		await AutoPlayWithBonus(context, played, target, type, skipXCapture, skipCardPileVisuals, Delta(source, "extra:replays"));
	}

	internal static async Task AutoPlayWithBonus(PlayerChoiceContext context, CardModel played, Creature target, AutoPlayType type, bool skipXCapture, bool skipCardPileVisuals, int bonus)
	{
		AutoReplayCounter counter = AutoReplayCounters.GetOrCreateValue(played);
		int previous = counter.Extra;
		counter.Extra = checked(previous + bonus);
		try
		{
			await CardCmd.AutoPlay(context, played, target, type, skipXCapture, skipCardPileVisuals);
		}
		finally
		{
			counter.Extra = previous;
		}
	}

	private static async Task AutoDrawReplays(PlayerChoiceContext context, Player player, int count, CardPilePosition position, bool forceExhaust, CardModel source)
	{
		AutoReplayCounter counter = AutoReplayCounters.GetOrCreateValue(source);
		int previous = counter.Extra;
		counter.Extra = checked(previous + Delta(source, "extra:replays"));
		try
		{
			await CardPileCmd.AutoPlayFromDrawPile(context, player, count, position, forceExhaust);
		}
		finally
		{
			counter.Extra = previous;
		}
	}

	private static async Task AutoAttacks(PlayerChoiceContext context, CardModel played, Creature target, AutoPlayType type, bool skipXCapture, bool skipCardPileVisuals, CardModel source)
	{
		await CardCmd.AutoPlay(context, played, target, type, skipXCapture, skipCardPileVisuals);
		HashSet<CardModel> alreadyPlayed = new HashSet<CardModel> { played };
		for (int i = 0; i < Delta(source, "extra:power"); i++)
		{
			IEnumerable<CardModel> source2 = PileType.Draw.GetPile(source.Owner).Cards.Where((CardModel c) => c.Type == CardType.Attack && !alreadyPlayed.Contains(c));
			List<CardModel> list = source2.Where((CardModel c) => !c.Keywords.Contains(CardKeyword.Unplayable)).ToList();
			CardModel cardModel = ((list.Count > 0) ? list : source2.ToList()).StableShuffle(source.Owner.RunState.Rng.Shuffle).FirstOrDefault();
			if (cardModel != null)
			{
				alreadyPlayed.Add(cardModel);
				await CardCmd.AutoPlay(context, cardModel, null, type, skipXCapture, skipCardPileVisuals);
				continue;
			}
			break;
		}
	}

	private static bool ShouldChannelGlass(bool upgraded, CardModel card)
	{
		if (!upgraded)
		{
			return Delta(card, "extra:channels") > 0;
		}
		return true;
	}

	private static async Task InitialGlass(PlayerChoiceContext context, Player player, CardModel card)
	{
		int count = (card.IsUpgraded ? 1 : 0) + Delta(card, "extra:channels");
		for (int i = 0; i < count; i++)
		{
			await OrbCmd.Channel<GlassOrb>(context, player);
		}
	}

	private static async Task<T> ApplyPower<T>(PlayerChoiceContext context, Creature target, decimal amount, Creature applier, CardModel cardSource, bool silent, CardModel card) where T : PowerModel
	{
		bool capture = !(card is BulletTime) || (Delta(card, "extra:allowedDraw") > 0 && FinalCardEffects.CanAttachDrawBudget(target));
		T val = await PowerCmd.Apply<T>(context, target, amount, applier, cardSource, silent);
		if (capture)
		{
			PowerParameters.Attach(val, card);
		}
		if (val != null && card is ShadowStep)
		{
			FinalCardEffects.AttachShadowDuration(val, card, amount);
		}
		return val;
	}

	private static Task<IEnumerable<CardModel>> DrawAndRedraw(PlayerChoiceContext context, decimal count, Player player, bool fromHandDraw, CardModel source)
	{
		return FinalCardEffects.DrawAndRedraw(context, count, player, fromHandDraw, source);
	}

	private static CardSelectorPrefs ExpandedPrefs(CardSelectorPrefs prefs, int count)
	{
		CardSelectorPrefs result = new CardSelectorPrefs(prefs.Prompt, count);
		result = result with { Cancelable = prefs.Cancelable };
		result = result with { RequireManualConfirmation = prefs.RequireManualConfirmation };
		result = result with { Comparison = prefs.Comparison };
		result = result with { UnpoweredPreviews = prefs.UnpoweredPreviews };
		result = result with { PretendCardsCanBePlayed = prefs.PretendCardsCanBePlayed };
		result.ShouldGlowGold = prefs.ShouldGlowGold;
		return result;
	}

	private static CardSelectorPrefs OptionalPrefs(CardSelectorPrefs prefs, int maximum)
	{
		CardSelectorPrefs result = new CardSelectorPrefs(prefs.Prompt, Math.Min(prefs.MinSelect, maximum), maximum);
		result = result with { Cancelable = prefs.Cancelable };
		result = result with { RequireManualConfirmation = true };
		result = result with { Comparison = prefs.Comparison };
		result = result with { UnpoweredPreviews = prefs.UnpoweredPreviews };
		result = result with { PretendCardsCanBePlayed = prefs.PretendCardsCanBePlayed };
		result.ShouldGlowGold = prefs.ShouldGlowGold;
		return result;
	}

	private static Task<IEnumerable<CardModel>> ChoosePile(PlayerChoiceContext context, CardPile pile, Player player, CardSelectorPrefs prefs, CardModel card)
	{
		return ChoosePileFiltered(context, pile, player, prefs, (CardModel _) => true, card);
	}

	private static async Task<IEnumerable<CardModel>> ChoosePileFiltered(PlayerChoiceContext context, CardPile pile, Player player, CardSelectorPrefs prefs, Func<CardModel, bool> filter, CardModel card)
	{
		string key = ((card is Cleanse) ? "extra:exhaustMaximum" : "extra:selection");
		int delta = Delta(card, key);
		if (delta == 0)
		{
			return await CardSelectCmd.FromCombatPile(context, pile, player, prefs, filter);
		}
		if (prefs.MaxSelect + delta <= 0)
		{
			return Array.Empty<CardModel>();
		}
		CardModel[] array = (await CardSelectCmd.FromCombatPile(context, pile, player, OptionalPrefs(prefs, prefs.MaxSelect + delta), filter)).ToArray();
		if (card is Cleanse)
		{
			CardModel[] array2 = array;
			foreach (CardModel cardModel in array2)
			{
				await GameCompat.Exhaust(context, cardModel);
			}
		}
		else
		{
			foreach (CardModel item in array.Reverse())
			{
				await CardPileCmd.Add(item, PileType.Draw, CardPilePosition.Top);
			}
		}
		return Array.Empty<CardModel>();
	}

	private static async Task<IEnumerable<CardModel>> ReduceHand(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool> filter, AbstractModel source, CardModel card)
	{
		int num = Math.Max(0, prefs.MaxSelect + Delta(card, "extradown:putBack"));
		if (num == 0)
		{
			return Array.Empty<CardModel>();
		}
		return await CardSelectCmd.FromHand(context, player, (num == prefs.MaxSelect) ? prefs : ExpandedPrefs(prefs, num), filter, source);
	}

	private static async Task<IEnumerable<CardModel>> ReduceDiscard(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool> filter, AbstractModel source, CardModel card)
	{
		int num = Math.Max(0, prefs.MaxSelect + Delta(card, "extradown:discard"));
		if (num == 0)
		{
			return Array.Empty<CardModel>();
		}
		return await CardSelectCmd.FromHandForDiscard(context, player, (num == prefs.MaxSelect) ? prefs : ExpandedPrefs(prefs, num), filter, source);
	}

	private static async Task<IEnumerable<CardModel>> ExtraHandActions(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool> filter, AbstractModel source, CardModel card)
	{
		bool transform = card is Begone;
		int delta = Delta(card, transform ? "extra:selection" : "extra:exhaustMaximum");
		if (delta == 0)
		{
			return await CardSelectCmd.FromHand(context, player, prefs, filter, source);
		}
		if (prefs.MaxSelect + delta <= 0)
		{
			return Array.Empty<CardModel>();
		}
		foreach (CardModel item in await CardSelectCmd.FromHand(context, player, OptionalPrefs(prefs, prefs.MaxSelect + delta), filter, source))
		{
			if (!transform)
			{
				await GameCompat.Exhaust(context, item);
				continue;
			}
			CardModel cardModel = card.CombatState.CreateCard<MinionStrike>(player);
			if (card.IsUpgraded)
			{
				CardCmd.Upgrade(cardModel);
			}
			await CardCmd.Transform(item, cardModel);
		}
		return Array.Empty<CardModel>();
	}

	private static async Task<CardModel> ChooseGenerated(PlayerChoiceContext context, IReadOnlyList<CardModel> cards, Player player, bool canSkip, CardModel source)
	{
		int count = Math.Min(cards.Count, 1 + Delta(source, "extra:selection"));
		if (cards.Count <= 3 && count == 1)
		{
			return await CardSelectCmd.FromChooseACardScreen(context, cards, player, canSkip);
		}
		CardSelectorPrefs prefs = GeneratedSelectionPrefs(count, canSkip);
		CardModel[] array = (await CardSelectCmd.FromSimpleGrid(context, cards, player, prefs)).ToArray();
		if (count == 1)
		{
			return array.FirstOrDefault();
		}
		CardModel[] array2 = array;
		foreach (CardModel cardModel in array2)
		{
			if (source.GetType().Name != "Quasar")
			{
				cardModel.SetToFreeThisTurn();
			}
			await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, player);
		}
		return null;
	}

	private static CardSelectorPrefs GeneratedSelectionPrefs(int count, bool canSkip)
	{
		CardSelectorPrefs result = new CardSelectorPrefs(new LocString("card_selection", canSkip ? "UPGRADER_GENERATED_OPTIONAL" : "UPGRADER_GENERATED_CARDS"), (!canSkip) ? count : 0, count);
		result = result with { Cancelable = canSkip };
		result = result with { RequireManualConfirmation = true };
		return result;
	}

	private static void HuntRewards(CombatRoom room, Player player, Reward reward, CardModel source)
	{
		room.AddExtraReward(player, reward);
		for (int i = 0; i < Delta(source, "extra:generation"); i++)
		{
			room.AddExtraReward(player, new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3 + Delta(source, "extra:options"), player, (PlayerChoiceSynchronizer)null));
		}
	}

	private static async Task<PotionProcureResult> ProcurePotions(PotionModel potion, Player player, int slotIndex, CardModel source)
	{
		PotionProcureResult first = await PotionCmd.TryToProcure(potion, player, slotIndex);
		if (!first.success)
		{
			return first;
		}
		for (int i = 0; i < Delta(source, "extra:generation"); i++)
		{
			if (!(await PotionCmd.TryToProcure(PotionFactory.CreateRandomPotionInCombat(player, player.RunState.Rng.CombatPotionGeneration).ToMutable(), player)).success)
			{
				break;
			}
		}
		return first;
	}

	private static void ResetStatuses(CardModel __instance)
	{
		StatusCounters.GetOrCreateValue(__instance).Skip = -(CardStateStore.Get(__instance)?.RawDelta("extradown:statuses") ?? 0);
	}

	private static Task<CardPileAddResult> AddStatus(CardModel generated, PileType pile, Player creator, CardPilePosition position, CardModel source)
	{
		StatusCounter orCreateValue = StatusCounters.GetOrCreateValue(source);
		if (generated.Type == CardType.Status && orCreateValue.Skip > 0)
		{
			orCreateValue.Skip--;
			return Task.FromResult(new CardPileAddResult
			{
				success = false,
				cardAdded = generated
			});
		}
		return CardPileCmd.AddGeneratedCardToCombat(generated, pile, creator, position);
	}

	/// <summary>
	/// Wah Ki (game v0.114; Recall before): lets you pick extra cards; the card itself puts the first one on top
	/// of the draw pile, and this puts the extras there too.
	/// </summary>
	private static async Task<IEnumerable<CardModel>> RetrieveToTop(PlayerChoiceContext context, CardPile pile, Player player, CardSelectorPrefs prefs, CardModel card)
	{
		int delta = Delta(card, "extra:selection");
		if (delta <= 0)
		{
			return await CardSelectCmd.FromCombatPile(context, pile, player, prefs);
		}
		CardModel[] picked = (await CardSelectCmd.FromCombatPile(context, pile, player, ExpandedPrefs(prefs, checked(prefs.MaxSelect + delta)))).ToArray();
		for (int i = 1; i < picked.Length; i++)
		{
			await CardPileCmd.Add(picked[i], PileType.Draw, CardPilePosition.Top);
		}
		return picked.Take(1).ToArray();
	}

	private static Task<IEnumerable<CardModel>> Retrieve(PlayerChoiceContext context, CardPile pile, Player player, CardSelectorPrefs prefs, CardModel card)
	{
		return RetrieveFiltered(context, pile, player, prefs, (CardModel _) => true, card);
	}

	private static async Task<IEnumerable<CardModel>> RetrieveFiltered(PlayerChoiceContext context, CardPile pile, Player player, CardSelectorPrefs prefs, Func<CardModel, bool> filter, CardModel card)
	{
		int delta = Delta(card, "extra:selection");
		if (delta <= 0)
		{
			return await CardSelectCmd.FromCombatPile(context, pile, player, prefs, filter);
		}
		CardModel[] array = (await CardSelectCmd.FromCombatPile(context, pile, player, ExpandedPrefs(prefs, checked(prefs.MaxSelect + delta)), filter)).ToArray();
		CardModel[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			await CardPileCmd.Add(array2[i], PileType.Hand);
		}
		return Array.Empty<CardModel>();
	}

	private static async Task<CardModel> UpgradeHand(PlayerChoiceContext context, Player player, CardModel source, CardModel card)
	{
		int delta = Delta(card, "extra:selection");
		if (delta <= 0 || card.IsUpgraded)
		{
			return await CardSelectCmd.FromHandForUpgrade(context, player, source);
		}
		for (int i = 0; i <= delta; i++)
		{
			CardModel cardModel = await CardSelectCmd.FromHandForUpgrade(context, player, source);
			if (cardModel == null)
			{
				break;
			}
			CardCmd.Upgrade(cardModel);
		}
		return null;
	}

	private static async Task<IEnumerable<CardModel>> SelectHand(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool> filter, AbstractModel source, CardModel card)
	{
		return await CardSelectCmd.FromHand(context, player, ExpandedPrefs(prefs, prefs.MaxSelect + Delta(card, "extra:selection")), filter, source);
	}

	private static async Task<IEnumerable<CardModel>> KeywordHand(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool> filter, AbstractModel source, CardModel card)
	{
		if (Delta(card, "extra:selection") <= 0)
		{
			return await CardSelectCmd.FromHand(context, player, prefs, filter, source);
		}
		IEnumerable<CardModel> enumerable = ((!(card is SculptingStrike)) ? (await SelectHand(context, player, prefs, filter, source, card)) : (await CardSelectCmd.FromHand(context, player, OptionalPrefs(prefs, prefs.MaxSelect + Delta(card, "extra:selection")), filter, source)));
		foreach (CardModel item in enumerable)
		{
			if (card is HandTrick)
			{
				CardCmd.ApplySingleTurnSly(item);
				continue;
			}
			CardCmd.ApplyKeyword(item, (card is SculptingStrike) ? CardKeyword.Ethereal : CardKeyword.Retain);
		}
		return Array.Empty<CardModel>();
	}

	private static async Task<CardPileAddResult> AddGenerated(CardModel generated, PileType pile, Player creator, CardPilePosition position, CardModel source)
	{
		Player beneficiary = ((source is Largesse) ? generated.Owner : creator);
		CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(generated, pile, creator, position);
		int delta = Delta(source, "extra:generation");
		if (delta <= 0)
		{
			return result;
		}
		bool colorless = source is ManifestAuthority || source is Largesse;
		IEnumerable<CardModel> source2 = (colorless ? ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(beneficiary.UnlockState, creator.RunState.CardMultiplayerConstraint) : creator.Character.CardPool.GetUnlockedCards(creator.UnlockState, creator.RunState.CardMultiplayerConstraint));
		if (source is BlotOutTheSky)
		{
			// Like the card itself (game v0.114): a Shoot card from the Fool's pool, whoever plays it.
			source2 = ModelDb.CardPool<FoolCardPool>().GetUnlockedCards(creator.UnlockState, creator.RunState.CardMultiplayerConstraint);
		}
		if (source is BlotOutTheSky)
		{
			source2 = source2.Where((CardModel c) => c.Tags.Contains(CardTag.Shoot));
		}
		else if (!colorless)
		{
			source2 = source2.Where((CardModel c) => c.Type == generated.Type);
		}
		CardModel[] array = CardFactory.GetDistinctForCombat(beneficiary, source2.Where((CardModel c) => c.Id != generated.Id), delta, creator.RunState.Rng.CombatCardGeneration).ToArray();
		foreach (CardModel cardModel in array)
		{
			if (colorless)
			{
				if (source.IsUpgraded)
				{
					CardCmd.Upgrade(cardModel);
				}
			}
			else
			{
				cardModel.SetToFreeThisTurn();
			}
			await CardPileCmd.AddGeneratedCardToCombat(cardModel, pile, creator, position);
		}
		return result;
	}

	private static async Task<CardPileAddResult> AddSouls(CardModel generated, PileType pile, Player creator, CardPilePosition position, CardModel source)
	{
		CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(generated, pile, creator, position);
		foreach (Soul item in Soul.Create(creator, Delta(source, "extra:generation"), source.CombatState))
		{
			await CardPileCmd.AddGeneratedCardToCombat(item, pile, creator, position);
		}
		return result;
	}
}
