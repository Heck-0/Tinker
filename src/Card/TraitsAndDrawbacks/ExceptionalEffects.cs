using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Tinker.Card;

internal static class ExceptionalEffects
{

	[HarmonyPatch(typeof(Hook), "AfterDamageGiven")]
	internal static class LegacyPatch
	{
		private static void Postfix(ref Task __result, Creature target, DamageResult results, CardModel cardSource)
		{
			if (cardSource != null && !(CardStateStore.Get(cardSource)?.Property != "Legacy"))
			{
				__result = AfterDamage(__result, cardSource, target, results);
			}
		}
	}

	[HarmonyPatch(typeof(Hook), "AfterDamageGiven")]
	internal static class DamageTraitsPatch
	{
		private static void Postfix(ref Task __result, PlayerChoiceContext choiceContext, Creature target, DamageResult results, CardModel cardSource)
		{
			string text = ((cardSource == null) ? null : CardStateStore.Get(cardSource)?.Property);
			if (text == "Venomous")
			{
				__result = Venom(__result, choiceContext, cardSource, target, results);
			}
			else if (text == "Lifesteal")
			{
				StoreLifesteal(cardSource, target, results);
			}
		}
	}

	[HarmonyPatch(typeof(Hook), "AfterCardPlayed")]
	internal static class LifestealPatch
	{
		private static void Postfix(ref Task __result, CardPlay cardPlay)
		{
			CardModel cardModel = cardPlay?.Card;
			if (cardModel != null && !(CardStateStore.Get(cardModel)?.Property != "Lifesteal"))
			{
				__result = Lifesteal(__result, cardModel);
			}
		}
	}

	[HarmonyPatch(typeof(Hook), "ModifyCardPlayCount")]
	internal static class EchoPatch
	{
		private static void Postfix(ref int __result, CardModel card)
		{
			if (card != null && !(CardStateStore.Get(card)?.Property != "Echo") && PlaysThisCombat(card) == 0)
			{
				__result++;
			}
		}
	}

	[HarmonyPatch(typeof(Hook), "ModifyEnergyCostInCombat")]
	internal static class AwakeningPatch
	{
		private static void Postfix(ref decimal __result, CardModel card)
		{
			if (card != null && !card.EnergyCost.CostsX && !(CardStateStore.Get(card)?.Property != "Awakening") && PlaysThisCombat(card) >= ExceptionalCatalog.AwakeningPlays)
			{
				__result = default(decimal);
			}
		}
	}

	[HarmonyPatch(typeof(Hook), "ModifyKeywordsInCombat")]
	internal static class InexhaustiblePatch
	{
		private static void Postfix(CardModel card, ISet<CardKeyword> keywords)
		{
			if (card != null && keywords != null && !(CardStateStore.Get(card)?.Property != "Inexhaustible"))
			{
				keywords.Remove(CardKeyword.Exhaust);
			}
		}
	}

	internal static class InexhaustibleLocationPatch
	{
		internal static void Install(Harmony harmony)
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(Hook), "ModifyCardPlayResultLocation");
			if (!(methodInfo == null))
			{
				MethodInfo method = AccessTools.Method(typeof(InexhaustibleLocationPatch), "Postfix").MakeGenericMethod(methodInfo.ReturnType);
				harmony.Patch(methodInfo, null, new HarmonyMethod(method));
			}
		}

		private static void Postfix<T>(ref T __result, CardModel card)
		{
			if (card != null && !card.IsDupe && !(CardStateStore.Get(card)?.Property != "Inexhaustible"))
			{
				object obj = __result;
				PileType pileType = (PileType)AccessTools.Field(typeof(T), "pileType").GetValue(obj);
				if (pileType == PileType.None || pileType == PileType.Exhaust)
				{
					__result = (T)Activator.CreateInstance(typeof(T), AccessTools.Field(typeof(T), "player").GetValue(obj), PileType.Discard, CardPilePosition.Bottom);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Hook), "ModifyCardPlayResultPileTypeAndPosition")]
	internal static class InexhaustibleLocationStablePatch
	{
		private static bool Prepare()
		{
			return AccessTools.Method(typeof(Hook), "ModifyCardPlayResultPileTypeAndPosition") != null;
		}

		private static void Postfix(ref (PileType, CardPilePosition) __result, CardModel card)
		{
			if (card != null && !card.IsDupe && !(CardStateStore.Get(card)?.Property != "Inexhaustible") && (__result.Item1 == PileType.None || __result.Item1 == PileType.Exhaust))
			{
				__result = (PileType.Discard, CardPilePosition.Bottom);
			}
		}
	}

	[HarmonyPatch]
	internal static class DescriptionPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.GetDeclaredMethods(typeof(CardModel)).First((MethodInfo m) => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);
		}

		private static void Postfix(CardModel __instance, ref string __result)
		{
			CardMachineState cardMachineState = CardStateStore.Get(__instance);
			ExceptionalDefinition exceptionalDefinition = ((cardMachineState != null && cardMachineState.Property != null) ? ExceptionalCatalog.Find(cardMachineState.Property) : null);
			if (exceptionalDefinition != null)
			{
				__result = __result + "\n" + "[color=#F3C14F]" + Name(exceptionalDefinition) + "[/color]";
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), "HoverTips", MethodType.Getter)]
	internal static class HoverTipPatch
	{
		private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			CardMachineState cardMachineState = CardStateStore.Get(__instance);
			ExceptionalDefinition exceptionalDefinition = ((cardMachineState != null && cardMachineState.Property != null) ? ExceptionalCatalog.Find(cardMachineState.Property) : null);
			if (exceptionalDefinition == null)
			{
				return;
			}
			try
			{
				List<IHoverTip> list = (__result ?? Enumerable.Empty<IHoverTip>()).ToList();
				list.Insert(0, LegendTips.Make(Name(exceptionalDefinition), LegendTips.Capitalize(Explain(exceptionalDefinition, cardMachineState.PropertyGrowth))));
				__result = list;
			}
			catch (Exception ex)
			{
				ModEntry.Error("No se pudo añadir el globo de la propiedad: " + ex);
			}
		}
	}

	private static readonly ConditionalWeakTable<CardModel, WeakReference<object>> LegacyPaid = new ConditionalWeakTable<CardModel, WeakReference<object>>();

	private static readonly ConditionalWeakTable<CardModel, StrongBox<int>> LifestealPending = new ConditionalWeakTable<CardModel, StrongBox<int>>();

	/// <summary>The trait's name (translated to Chinese when the game is in Chinese).</summary>
	public static string Name(ExceptionalDefinition d)
	{
		return Lang.Zh(NameText(d));
	}

	/// <summary>What the trait does (translated to Chinese when the game is in Chinese).</summary>
	public static string Explain(ExceptionalDefinition d, int growth)
	{
		return Lang.Zh(ExplainText(d, growth));
	}

	private static string NameText(ExceptionalDefinition d)
	{
		bool spanish = Lines.Spanish;
		switch (d.Id)
		{

		case "Inexhaustible":
			if (!spanish)
			{
				return "Inexhaustible";
			}
			return "Inagotable";
		case "Echo":
			if (!spanish)
			{
				return "Echo";
			}
			return "Eco";
		case "Awakening":
			if (!spanish)
			{
				return "Awakening";
			}
			return "Despertar";
		case "Legacy":
			if (!spanish)
			{
				return "Legacy";
			}
			return "Legado";
		case "Venomous":
			if (!spanish)
			{
				return "Venomous";
			}
			return "Ponzoña";
		case "Lifesteal":
			if (!spanish)
			{
				return "Lifesteal";
			}
			return "Vampirismo";
		default:
			return d.Id;
		}
	}

	private static string ExplainText(ExceptionalDefinition d, int growth)
	{
		bool spanish = Lines.Spanish;
		string text = ((growth <= 0) ? "" : (spanish ? $" Llevas +{growth}." : $" Total: +{growth}."));
		switch (d.Id)
		{

		case "Inexhaustible":
			if (!spanish)
			{
				return "No longer Exhausts. If it's a Power, it goes to your discard pile instead.";
			}
			return "Ya no se agota. Si es un poder, va al descarte en vez de desaparecer.";
		case "Echo":
			if (!spanish)
			{
				return "The first time you play it each combat, play it twice.";
			}
			return "La primera vez que la juegas en cada combate, se juega dos veces.";
		case "Awakening":
			if (spanish)
			{
				return "Después de jugarla una vez en un combate, cuesta 0 el resto del combate.";
			}
			return "After you play it once in a combat, it costs 0 for the rest of that combat.";
		case "Legacy":
			return (spanish ? $"Si mata a un élite o a un jefe, aumenta su daño en {5} de forma permanente. Una vez por combate." : $"If it kills an elite or boss, permanently increase its damage by {5}. Once per combat.") + text;
		case "Venomous":
			if (spanish)
			{
				return "Cada golpe aplica 1 de Veneno.";
			}
			return "Each hit applies 1 Poison.";
		case "Lifesteal":
			if (spanish)
			{
				return $"Al jugarla, te curas un {25}\u00a0% del daño sin bloquear que haga.";
			}
			return $"When played, heal for {25}% of the unblocked damage it deals.";
		default:
			return d.Id;
		}
	}

	public static HashSet<ExceptionalRequirement> Meets(CardModel card)
	{
		HashSet<ExceptionalRequirement> hashSet = new HashSet<ExceptionalRequirement>();
		List<string> list = CardAttributes.SupportedFor(card).ToList();
		if (list.Contains(AttributeKeys.Var("Damage")))
		{
			hashSet.Add(ExceptionalRequirement.Damage);
		}
		if (hashSet.Contains(ExceptionalRequirement.Damage) || (card.Type == CardType.Attack && card.DynamicVars.Keys.Any((string key) => key.IndexOf("Damage", StringComparison.Ordinal) >= 0)))
		{
			hashSet.Add(ExceptionalRequirement.DealtDamage);
		}

		// Combined build: Power cards no longer qualify for Inexhaustible (it made them return to the discard pile).
		if (card.Keywords.Contains(CardKeyword.Exhaust) && card.Type != CardType.Power)
		{
			hashSet.Add(ExceptionalRequirement.NativeExhaust);
		}
		bool num = !card.Keywords.Contains(CardKeyword.Unplayable);
		if (num)
		{
			hashSet.Add(ExceptionalRequirement.Playable);
		}
		if (num && card.Type != CardType.Power && !card.Keywords.Contains(CardKeyword.Exhaust) && !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0)
		{
			hashSet.Add(ExceptionalRequirement.PaidCost);
		}
		return hashSet;
	}

	public static List<ExceptionalDefinition> ApplicableTo(CardModel card)
	{
		HashSet<ExceptionalRequirement> hashSet = Meets(card);
		HashSet<ExceptionalRequirement> hashSet2 = MeetsAfterUpgrade(card);
		if (hashSet2 != null)
		{
			hashSet.IntersectWith(hashSet2);
		}
		return (from d in ExceptionalCatalog.Applicable(hashSet)
			where Settings.Active.IsTraitEnabled(d.Id)
			select d).ToList();
	}

	private static HashSet<ExceptionalRequirement> MeetsAfterUpgrade(CardModel card)
	{
		try
		{
			if (!card.IsUpgradable)
			{
				return null;
			}
			CardModel cardModel = ModelDb.GetById<CardModel>(card.Id).ToMutable();
			cardModel.Owner = card.Owner;
			for (int i = 0; i <= card.CurrentUpgradeLevel; i++)
			{
				cardModel.UpgradeInternal();
			}
			cardModel.FinalizeUpgradeInternal();
			CardMachineState cardMachineState = CardStateStore.Get(card);
			if (cardMachineState != null)
			{
				CardAttributes.ApplyWhole(cardModel, CardMachineState.FromJson(cardMachineState.ToJson()));
			}
			return Meets(cardModel);
		}
		catch (Exception ex)
		{
			ModEntry.Warn("No se pudo prever la forja de " + card.Id.Entry + ": " + ex.Message);
			return null;
		}
	}

	public static ExceptionalDefinition PropertyOf(CardModel card)
	{
		string text = CardStateStore.Get(card)?.Property;
		if (text == null)
		{
			return null;
		}
		return ExceptionalCatalog.Find(text);
	}

	public static ExceptionalDefinition TryGrant(CardModel card, CardMachineState state, double successChance, double chanceRoll, double pickRoll)
	{
		if (state.Property != null)
		{
			return null;
		}
		if (chanceRoll >= CardOdds.Balance.ExceptionalChanceFor(successChance))
		{
			return null;
		}
		ExceptionalDefinition exceptionalDefinition = ExceptionalCatalog.Choose(ApplicableTo(card), pickRoll);
		if (exceptionalDefinition == null)
		{
			return null;
		}
		state.GrantProperty(exceptionalDefinition.Id);
		return exceptionalDefinition;
	}

	public static void ApplyGrowth(CardModel card, CardMachineState state)
	{
		if (state != null && state.Property != null && state.PropertyGrowth > 0)
		{
			string text = VarFor(state.Property);
			if ((text == null || !RewrittenCardValues.Owns(card, AttributeKeys.Var(text))) && text != null && card.DynamicVars.TryGetValue(text, out DynamicVar value))
			{
				value.BaseValue += (decimal)state.PropertyGrowth;
			}
		}
	}

	private static string VarFor(string property)
	{
		switch (property)
		{
		default:
			return null;
		case "Legacy":
			return "Damage";
		}
	}


	internal static void TryLegacy(CardModel source, Creature target, DamageResult result)
	{
		if (source == null || target == null || result == null || !result.WasTargetKilled || CardStateStore.Get(source)?.Property != "Legacy" || !target.IsEnemy || !target.IsPrimaryEnemy)
		{
			return;
		}
		RoomType? roomType = source.Owner?.RunState?.CurrentRoom?.RoomType;
		if (roomType != RoomType.Elite && roomType != RoomType.Boss)
		{
			return;
		}
		CardModel deckVersion = source.DeckVersion;
		if (deckVersion == null)
		{
			return;
		}
		object combatState = source.CombatState;
		if (LegacyPaid.TryGetValue(deckVersion, out var value) && value.TryGetTarget(out var target2) && target2 == combatState)
		{
			return;
		}
		LegacyPaid.Remove(deckVersion);
		LegacyPaid.Add(deckVersion, new WeakReference<object>(combatState));
		int num = 5;
		if (!RewrittenCardValues.AddTemporaryGrowth(source, "Damage", num) && source.DynamicVars.TryGetValue("Damage", out DynamicVar value2))
		{
			value2.BaseValue += (decimal)num;
		}
		CardMachineState cardMachineState = CardStateStore.Get(deckVersion);
		if (!(cardMachineState?.Property != "Legacy"))
		{
			cardMachineState.AddPropertyGrowth(num);
			if (!RewrittenCardValues.Owns(deckVersion, AttributeKeys.Var("Damage")) && deckVersion.DynamicVars.TryGetValue("Damage", out DynamicVar value3))
			{
				value3.BaseValue += (decimal)num;
			}
			CardStateStore.Set(deckVersion, cardMachineState);
			ModEntry.Info($"Legacy: {deckVersion.Id.Entry} remata a {target} y crece para siempre (+{cardMachineState.PropertyGrowth}).");
		}
	}

	private static async Task AfterDamage(Task original, CardModel source, Creature target, DamageResult result)
	{
		await original;
		try
		{
			TryLegacy(source, target, result);
		}
		catch (Exception ex)
		{
			ModEntry.Error("No se pudo aplicar Legado: " + ex);
		}
	}

	private static int Dealt(DamageResult result)
	{
		if (result != null)
		{
			return Math.Max(0, result.UnblockedDamage - result.OverkillDamage);
		}
		return 0;
	}

	private static async Task Venom(Task original, PlayerChoiceContext choiceContext, CardModel source, Creature target, DamageResult result)
	{
		await original;
		try
		{
			// Every hit on a living enemy applies 1 Poison, blocked or not.
			if (result != null && target != null && target.IsEnemy && !target.IsDead)
			{
				await PowerCmd.Apply<PoisonPower>(choiceContext, target, 1, source.Owner?.Creature, source);
			}
		}
		catch (Exception ex)
		{
			ModEntry.Error("No se pudo aplicar Ponzoña: " + ex);
		}
	}

	private static void StoreLifesteal(CardModel source, Creature target, DamageResult result)
	{
		int num = Dealt(result);
		if (num > 0 && target != null && target.IsEnemy)
		{
			LifestealPending.GetValue(source, (CardModel _) => new StrongBox<int>()).Value += num * 25;
		}
	}

	private static async Task Lifesteal(Task original, CardModel card)
	{
		await original;
		try
		{
			if (LifestealPending.TryGetValue(card, out var value))
			{
				int num = value.Value / 100;
				value.Value %= 100;
				Creature creature = card.Owner?.Creature;
				if (num > 0 && creature != null && !creature.IsDead)
				{
					await CreatureCmd.Heal(creature, num);
				}
			}
		}
		catch (Exception ex)
		{
			ModEntry.Error("No se pudo aplicar Vampirismo: " + ex);
		}
	}

	private static int PlaysThisCombat(CardModel card)
	{
		CombatManager instance = CombatManager.Instance;
		if (instance?.History == null)
		{
			return 0;
		}
		return instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => e.CardPlay.Card == card);
	}
}
