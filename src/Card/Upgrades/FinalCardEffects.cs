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
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Tinker.Card;

internal static class FinalCardEffects
{
	internal sealed class PlaySnapshot
	{
		internal HashSet<CardModel> Rares;
	}

	internal static readonly bool PlansUseSelectedRetain = new WellLaidPlans().DynamicVars.ContainsKey("RetainAmount");

	private const string BoostTurns = "shadow:boostTurns";

	private const string ShadowMultiplier = "extra:shadowMultiplier";

	internal static int Delta(CardModel card, string key)
	{
		return CardStateStore.Get(card)?.RawDelta(key) ?? 0;
	}

	internal static string DisplayValue(string key, int value)
	{
		return checked(key switch
		{
			"extra:shadowMultiplier" => "×" + value, 
			"extra:sharedForge" => Lines.Pct(value * 25), 
			"extra:blockGrowth" => Lines.Pct(value * 10), 
			_ => value.ToString(), 
		});
	}

	internal static decimal GrowthAmount(decimal block, int units)
	{
		if (!(block <= 0m) && units > 0)
		{
			return Math.Floor(block * (decimal)units / 10m);
		}
		return 0m;
	}

	internal static void BeforePlay(CardModel __instance, out PlaySnapshot __state)
	{
		__state = null;
		if (__instance is Anointed && Delta(__instance, "extra:rareDiscount") > 0)
		{
			__state = new PlaySnapshot
			{
				Rares = new HashSet<CardModel>(PileType.Draw.GetPile(__instance.Owner).Cards.Where((CardModel c) => c.Rarity == CardRarity.Rare))
			};
		}
	}

	internal static void AfterPlay(CardModel __instance, PlayerChoiceContext choiceContext, PlaySnapshot __state, ref Task __result)
	{
		string key = ((__instance is Apotheosis) ? "extra:permanentUpgrades" : ((__instance is CalculatedGamble) ? "extra:gambleDraw" : ((__instance is Anointed) ? "extra:rareDiscount" : "extra:initialSly")));
		int num = Delta(__instance, key);
		if (num > 0)
		{
			__result = FinishPlay(__result, __instance, choiceContext, __state, num);
		}
	}

	private static async Task FinishPlay(Task native, CardModel source, PlayerChoiceContext context, PlaySnapshot snapshot, int count)
	{
		await native;
		if (CombatManager.Instance.IsOverOrEnding || source.Owner.Creature.IsDead)
		{
			return;
		}
		if (source is CalculatedGamble)
		{
			await CardPileCmd.Draw(context, count, source.Owner);
			return;
		}
		if (source is Anointed)
		{
			CardModel[] array = PileType.Hand.GetPile(source.Owner).Cards.Where((CardModel c) => snapshot.Rares.Contains(c)).ToArray();
			foreach (CardModel cardModel in array)
			{
				if (!cardModel.EnergyCost.CostsX)
				{
					cardModel.EnergyCost.AddThisTurn(-count, reduceOnly: true);
				}
			}
			return;
		}
		if (source is MasterPlanner)
		{
			if (!PileType.Hand.GetPile(source.Owner).Cards.Any(Eligible))
			{
				return;
			}
			{
				foreach (CardModel item in await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(new LocString("card_selection", "UPGRADER_INITIAL_SLY"), 0, count), context: context, player: source.Owner, filter: Eligible, source: source))
				{
					if (Eligible(item))
					{
						CardCmd.ApplyKeyword(item, CardKeyword.Sly);
					}
				}
				return;
			}
		}
		int num2 = source.Owner.Deck.Cards.Count((CardModel c) => c.IsUpgradable);
		if (num2 == 0)
		{
			return;
		}
		CardSelectorPrefs upgradePrefs = new CardSelectorPrefs(new LocString("card_selection", "UPGRADER_PERMANENT_UPGRADE"), 0, Math.Min(count, num2))
		{
			Cancelable = true,
			RequireManualConfirmation = true
		};
		MethodInfo method = typeof(PlayerChoiceContext).GetMethod("SignalPlayerChoiceBegun");
		await (Task)method.Invoke(context, (method.GetParameters().Length != 2) ? new object[1] { PlayerChoiceOptions.None } : new object[2]
		{
			source.Owner,
			PlayerChoiceOptions.None
		});
		try
		{
			CardModel[] array = (from c in (await CardSelectCmd.FromDeckForUpgrade(source.Owner, upgradePrefs)).Distinct()
				where source.Owner.Deck.Cards.Contains(c) && c.IsUpgradable
				select c).Take(count).ToArray();
			for (int num = 0; num < array.Length; num++)
			{
				CardCmd.Upgrade(array[num]);
			}
		}
		finally
		{
			await context.SignalPlayerChoiceEnded();
		}
		static bool Eligible(CardModel c)
		{
			if (c.Type == CardType.Skill)
			{
				return !c.Keywords.Contains(CardKeyword.Sly);
			}
			return false;
		}
	}

	internal static CardModel BuffGenerated(CardModel generated, CardModel source)
	{
		string key = ((source is Guards) ? "extra:generatedBlock" : "extra:generatedDamage");
		int num = Delta(source, key);
		if (num <= 0)
		{
			return generated;
		}
		string dynamicVarName = ((source is Guards) ? "Block" : "Damage");
		CardMachineState cardMachineState = CardStateStore.Get(generated)?.Clone() ?? new CardMachineState();
		int raw = cardMachineState.ApplySuccess(AttributeKeys.Var(dynamicVarName), num);
		CardAttributes.ApplyRaw(generated, AttributeKeys.Var(dynamicVarName), raw);
		CardStateStore.Set(generated, cardMachineState);
		return generated;
	}

	internal static async Task<IEnumerable<CardModel>> DrawAndRedraw(PlayerChoiceContext context, decimal count, Player player, bool fromHandDraw, CardModel source)
	{
		CardModel[] drawn = (await CardPileCmd.Draw(context, count, player, fromHandDraw)).ToArray();
		int maximum = Delta(source, "extra:redraw");
		if (maximum <= 0 || CombatManager.Instance.IsOverOrEnding || player.Creature.IsDead)
		{
			return drawn;
		}
		HashSet<CardModel> candidates = new HashSet<CardModel>(drawn);
		int num = PileType.Hand.GetPile(player).Cards.Count(Eligible);
		if (num == 0)
		{
			return drawn;
		}
		CardSelectorPrefs prefs = new CardSelectorPrefs(new LocString("card_selection", "UPGRADER_REDRAW"), 0, Math.Min(maximum, num));
		CardModel[] array = (await CardSelectCmd.FromHand(context, player, prefs, Eligible, source)).Distinct().Take(maximum).ToArray();
		if (array.Length != 0)
		{
			await CardCmd.DiscardAndDraw(context, array, array.Length);
		}
		return drawn;
		bool Eligible(CardModel c)
		{
			return candidates.Contains(c);
		}
	}

	internal static bool InstallPower(Harmony harmony, string name, Type type)
	{
		switch (name)
		{
		case "HammerTimePower":
		case "OutbreakPower":
		{
			string name2 = ((name == "HammerTimePower") ? "AfterForge" : "AfterPowerAmountChanged");
			Type stateMachineType = AccessTools.DeclaredMethod(type, name2).GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType;
			harmony.Patch(AccessTools.DeclaredMethod(stateMachineType, "MoveNext"), null, null, new HarmonyMethod(typeof(FinalCardEffects), "PowerTranspile"));
			return true;
		}
		case "BarricadePower":
			harmony.Patch(AccessTools.DeclaredMethod(typeof(AbstractModel), "AfterSideTurnEndLate"), null, new HarmonyMethod(typeof(FinalCardEffects), "AfterTurnEnd"));
			return true;
		case "ShadowStepPower":
		{
			harmony.Patch(AccessTools.DeclaredMethod(type, "AfterSideTurnStart"), null, new HarmonyMethod(typeof(FinalCardEffects), "AfterShadowStart"));
			Type type2 = type.Assembly.GetType("MegaCrit.Sts2.Core.Models.Powers.DoubleDamagePower", throwOnError: true);
			harmony.Patch(AccessTools.DeclaredMethod(type2, "ModifyDamageMultiplicative"), null, new HarmonyMethod(typeof(FinalCardEffects), "ShadowDamage"));
			harmony.Patch(AccessTools.DeclaredMethod(type2, "AfterSideTurnEnd"), null, new HarmonyMethod(typeof(FinalCardEffects), "AfterDoubleEnd"));
			return true;
		}
		case "WellLaidPlansPower":
			if (!PlansUseSelectedRetain)
			{
				harmony.Patch(AccessTools.DeclaredMethod(typeof(AbstractModel), "ModifyHandDraw"), null, new HarmonyMethod(typeof(FinalCardEffects), "PlanningDraw"));
			}
			return true;
		default:
		{
			if (name != "NoDrawPower")
			{
				return false;
			}
			MethodInfo original = AccessTools.Method(typeof(CardPileCmd), "Draw", new Type[4]
			{
				typeof(PlayerChoiceContext),
				typeof(decimal),
				typeof(Player),
				typeof(bool)
			});
			harmony.CreateReversePatcher(original, new HarmonyMethod(typeof(FinalCardEffects), "NativeDraw")).Patch();
			harmony.Patch(original, new HarmonyMethod(typeof(FinalCardEffects), "BoundedDraw"));
			harmony.Patch(AccessTools.DeclaredMethod(type, "ShouldDraw"), null, new HarmonyMethod(typeof(FinalCardEffects), "AllowBudgetDraw"));
			harmony.Patch(AccessTools.Method(typeof(Hook), "AfterCardDrawn"), new HarmonyMethod(typeof(FinalCardEffects), "SpendDrawBudget"));
			harmony.Patch(AccessTools.Method(typeof(Hook), "AfterPowerAmountChanged"), new HarmonyMethod(typeof(FinalCardEffects), "FreezeForeignDraw"));
			return true;
		}
		}
	}

	private static IEnumerable<CodeInstruction> PowerTranspile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		List<CodeInstruction> code = instructions.Select((CodeInstruction c) => new CodeInstruction(c)).ToList();
		FieldInfo fieldInfo = __originalMethod.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single((FieldInfo f) => f.Name == "<>4__this");
		bool flag = fieldInfo.FieldType.Name == "HammerTimePower";
		int num = 0;
		Dictionary<int, List<CodeInstruction>> insertions = new Dictionary<int, List<CodeInstruction>>();
		for (int num2 = 0; num2 < code.Count; num2++)
		{
			if (flag && code[num2].operand is MethodInfo methodInfo && methodInfo.DeclaringType.Name == "ForgeCmd" && methodInfo.Name == "Forge")
			{
				int key = ExpandedEffectPatches.ArgumentEnd(code, num2, methodInfo, 0) + 1;
				insertions.Add(key, new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, fieldInfo),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FinalCardEffects), "SharedForge"))
				});
				num++;
			}
			if (!flag && code[num2].LoadsConstant(3L))
			{
				CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldarg_0);
				codeInstruction.labels.AddRange(code[num2].labels);
				codeInstruction.blocks.AddRange(code[num2].blocks);
				code[num2] = codeInstruction;
				insertions.Add(num2 + 1, new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldfld, fieldInfo),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FinalCardEffects), "PoisonThreshold"))
				});
				num++;
			}
		}
		if (num != (flag ? 1 : 2))
		{
			throw new InvalidOperationException("Firma inesperada de " + fieldInfo.FieldType.Name + ": " + num);
		}
		for (int i = 0; i <= code.Count; i++)
		{
			if (insertions.TryGetValue(i, out var value))
			{
				foreach (CodeInstruction item in value)
				{
					yield return item;
				}
			}
			if (i < code.Count)
			{
				yield return code[i];
			}
		}
	}

	internal static decimal SharedForge(decimal amount, PowerModel power)
	{
		return amount * (decimal)PowerParameters.Value(power, "extra:sharedForge", 4) / 4m;
	}

	internal static int PoisonThreshold(PowerModel power)
	{
		return Math.Max(1, PowerParameters.Value(power, "extradown:poisonThreshold", 3));
	}

	private static void PlanningDraw(AbstractModel __instance, Player player, ref decimal __result)
	{
		if (__instance is PowerModel powerModel && powerModel.GetType().Name == "WellLaidPlansPower" && powerModel.Owner.Player == player)
		{
			__result += (decimal)PowerParameters.Value(powerModel, "extra:planningDraw", 0);
		}
	}

	private static void AfterTurnEnd(AbstractModel __instance, IEnumerable<Creature> participants, ref Task __result)
	{
		if (__instance is PowerModel powerModel && powerModel.GetType().Name == "BarricadePower" && participants.Contains(powerModel.Owner))
		{
			__result = GrowBlock(__result, new Creature[1] { powerModel.Owner });
		}
	}

	private static async Task GrowBlock(Task native, Creature[] participants)
	{
		await native;
		foreach (Creature creature in participants)
		{
			if (!creature.IsDead && !CombatManager.Instance.IsOverOrEnding)
			{
				PowerModel power = creature.Powers.FirstOrDefault((PowerModel p) => p.GetType().Name == "BarricadePower");
				decimal num = GrowthAmount(creature.Block, PowerParameters.Value(power, "extra:blockGrowth", 0));
				if (num > 0m)
				{
					await CreatureCmd.GainBlock(creature, num, ValueProp.Unpowered, null);
				}
			}
		}
	}

	internal static void AttachShadowDuration(PowerModel power, CardModel card, decimal duration)
	{
		if (Delta(card, "extra:shadowMultiplier") > 0)
		{
			PowerParameters.Set(power, "shadow:boostTurns", checked(PowerParameters.Value(power, "shadow:boostTurns", 0) + (int)duration));
		}
	}

	private static void AfterShadowStart(PowerModel __instance, IReadOnlyList<Creature> participants, ref Task __result)
	{
		if (participants.Contains(__instance.Owner))
		{
			__result = TransferShadow(__result, __instance);
		}
	}

	private static async Task TransferShadow(Task native, PowerModel source)
	{
		await native;
		PowerModel powerModel = source.Owner.Powers.FirstOrDefault((PowerModel p) => p.GetType().Name == "DoubleDamagePower");
		int num = PowerParameters.Value(source, "shadow:boostTurns", 0);
		if (powerModel != null && num != 0)
		{
			PowerParameters.Set(powerModel, "extra:shadowMultiplier", Math.Max(PowerParameters.Value(powerModel, "extra:shadowMultiplier", 2), PowerParameters.Value(source, "extra:shadowMultiplier", 2)));
			PowerParameters.Set(powerModel, "shadow:boostTurns", checked(PowerParameters.Value(powerModel, "shadow:boostTurns", 0) + num));
		}
	}

	private static void ShadowDamage(PowerModel __instance, ref decimal __result)
	{
		if (__result == 2m && PowerParameters.Value(__instance, "shadow:boostTurns", 0) > 0)
		{
			__result = PowerParameters.Value(__instance, "extra:shadowMultiplier", 2);
		}
	}

	private static void AfterDoubleEnd(PowerModel __instance, IEnumerable<Creature> participants, ref Task __result)
	{
		if (participants.Contains(__instance.Owner))
		{
			__result = EndBoost(__result, __instance);
		}
	}

	private static async Task EndBoost(Task native, PowerModel power)
	{
		await native;
		if (PowerParameters.Has(power, "shadow:boostTurns"))
		{
			PowerParameters.Set(power, "shadow:boostTurns", Math.Max(0, PowerParameters.Value(power, "shadow:boostTurns", 0) - 1));
		}
	}

	private static PowerModel DrawBlocker(Player player)
	{
		return player.Creature.Powers.FirstOrDefault((PowerModel p) => p.GetType().Name == "NoDrawPower");
	}

	internal static bool CanAttachDrawBudget(Creature creature)
	{
		PowerModel powerModel = creature.Powers.FirstOrDefault((PowerModel p) => p.GetType().Name == "NoDrawPower");
		if (powerModel != null)
		{
			if (PowerParameters.Has(powerModel, "extra:allowedDraw"))
			{
				return PowerParameters.Value(powerModel, "draw:foreignBlock", 0) == 0;
			}
			return false;
		}
		return true;
	}

	private static void FreezeForeignDraw(PowerModel power, decimal amount, CardModel cardSource)
	{
		if (!(power.GetType().Name != "NoDrawPower") && !(amount <= 0m) && (!(cardSource is BulletTime) || Delta(cardSource, "extra:allowedDraw") <= 0))
		{
			PowerParameters.Set(power, "draw:foreignBlock", 1);
			if (PowerParameters.Has(power, "extra:allowedDraw"))
			{
				PowerParameters.Set(power, "extra:allowedDraw", 0);
			}
		}
	}

	private static void AllowBudgetDraw(PowerModel __instance, Player player, bool fromHandDraw, ref bool __result)
	{
		if (!__result && !fromHandDraw && player == __instance.Owner.Player && PowerParameters.Value(__instance, "extra:allowedDraw", 0) > 0)
		{
			__result = true;
		}
	}

	private static void SpendDrawBudget(CardModel card, bool fromHandDraw)
	{
		if (!fromHandDraw)
		{
			PowerModel power = DrawBlocker(card.Owner);
			int num = PowerParameters.Value(power, "extra:allowedDraw", 0);
			if (num > 0)
			{
				PowerParameters.Set(power, "extra:allowedDraw", num - 1);
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Task<IEnumerable<CardModel>> NativeDraw(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw)
	{
		throw new InvalidOperationException("El adaptador nativo de robo no está instalado.");
	}

	private static bool BoundedDraw(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw, ref Task<IEnumerable<CardModel>> __result)
	{
		if (fromHandDraw || PowerParameters.Value(DrawBlocker(player), "extra:allowedDraw", 0) <= 0)
		{
			return true;
		}
		__result = DrawWithinBudget(choiceContext, count, player);
		return false;
	}

	private static async Task<IEnumerable<CardModel>> DrawWithinBudget(PlayerChoiceContext context, decimal count, Player player)
	{
		List<CardModel> result = new List<CardModel>();
		decimal requested = Math.Ceiling(Math.Max(0m, count));
		for (decimal i = default(decimal); i < requested; ++i)
		{
			if (PowerParameters.Value(DrawBlocker(player), "extra:allowedDraw", 0) <= 0)
			{
				break;
			}
			CardModel[] array = (await NativeDraw(context, 1m, player, fromHandDraw: false)).ToArray();
			if (array.Length == 0)
			{
				break;
			}
			result.AddRange(array);
		}
		return result;
	}

	internal static void AddTexts(Dictionary<string, Dictionary<string, string>> tables, bool spanish)
	{
		tables["card_selection"]["UPGRADER_PERMANENT_UPGRADE"] = (spanish ? "Mejora permanentemente hasta {Amount} cartas." : "Permanently Upgrade up to {Amount} cards.");
		tables["card_selection"]["UPGRADER_INITIAL_SLY"] = (spanish ? "Da Astucia a hasta {Amount} Habilidades." : "Give Sly to up to {Amount} Skills.");
		tables["card_selection"]["UPGRADER_REDRAW"] = (spanish ? "Descarta hasta {Amount} cartas para robar otras tantas." : "Discard up to {Amount} cards to draw that many cards.");
	}
}
