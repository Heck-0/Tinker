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

internal static partial class ExpandedEffectPatches
{
	private sealed class StatusCounter
	{
		internal int Skip;
	}

	private sealed class AutoReplayCounter
	{
		internal int Extra;
	}

	[HarmonyPatch(typeof(Hook), "ModifyCardPlayCount")]
	internal static class AutoReplayHook
	{
		private static void Postfix(CardModel card, ref int __result)
		{
			checked
			{
				if (card != null && AutoReplayCounters.TryGetValue(card, out var value))
				{
					__result += value.Extra;
				}
			}
		}
	}

	private static readonly HashSet<Type> Active = new HashSet<Type>();

	private static readonly ConditionalWeakTable<CardModel, StatusCounter> StatusCounters = new ConditionalWeakTable<CardModel, StatusCounter>();

	private static readonly ConditionalWeakTable<CardModel, AutoReplayCounter> AutoReplayCounters = new ConditionalWeakTable<CardModel, AutoReplayCounter>();

	internal static readonly bool TrackingUsesPercent = PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(typeof(CardModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Powers.TrackingPower"), "ModifyDamageMultiplicative")).Any((CodeInstruction c) => c.operand is MethodInfo methodInfo && methodInfo.DeclaringType == typeof(decimal) && methodInfo.Name == "op_Division");

	internal static readonly bool EidolonUsesAutoPlay = PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(AccessTools.DeclaredMethod(typeof(Eidolon), "OnPlay").GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType, "MoveNext")).Any((CodeInstruction c) => c.operand is MethodInfo methodInfo && methodInfo.DeclaringType == typeof(CardCmd) && methodInfo.Name == "AutoPlay");

	internal static readonly bool RocketHasDiscount = PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(typeof(RocketPunch), "AfterCardGeneratedForCombat")).Any((CodeInstruction c) => c.operand is MethodInfo methodInfo && methodInfo.DeclaringType.Name == "CardEnergyCost" && methodInfo.Name == "AddUntilPlayed");

	internal static int Native(CardModel card, ExpandedEffect effect)
	{
		if (!(card is Prepared) || !(effect.Attribute == "extradown:discard"))
		{
			if (!(card.GetType().Name == "Tracking") || !TrackingUsesPercent)
			{
				if (!card.IsUpgraded)
				{
					return effect.Native;
				}
				return effect.Upgraded;
			}
			return 50;
		}
		return card.DynamicVars.Cards.IntValue;
	}

	internal static bool Enabled(CardModel card)
	{
		if (card != null)
		{
			return Active.Contains(card.GetType());
		}
		return false;
	}

	internal static void Install(Harmony harmony)
	{
		foreach (string item in ExpandedEffects.Entries.Select((ExpandedEffect e) => e.Card).Distinct())
		{
			if (item == "RocketPunch" && !RocketHasDiscount)
			{
				continue;
			}
			Type type = typeof(CardModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Cards." + item);
			if (type == null || (item == "Outbreak" && !PowerParameters.Enabled("OutbreakPower")) || (item == "WellLaidPlans" && FinalCardEffects.PlansUseSelectedRetain))
			{
				continue;
			}
			try
			{
				if (ExpandedEffects.For(item).Any((ExpandedEffect e) => e.Kind == "capture" && !PowerParameters.Enabled(e.Target)))
				{
					throw new InvalidOperationException("The power adapter is not available");
				}
				foreach (string item2 in (from e in ExpandedEffects.For(item)
					select e.Method).Distinct())
				{
					MethodInfo methodInfo = AccessTools.DeclaredMethod(type, item2);
					if (ExpandedEffects.For(item).Any((ExpandedEffect e) => e.Kind == "afterPlay"))
					{
						harmony.Patch(methodInfo, new HarmonyMethod(typeof(FinalCardEffects), "BeforePlay"), new HarmonyMethod(typeof(FinalCardEffects), "AfterPlay"));
						if (item == "Apotheosis")
						{
							PermanentUpgradeSelection.Install(harmony);
						}
					}
					else
					{
						Type type2 = methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
						harmony.Patch((type2 == null) ? methodInfo : AccessTools.DeclaredMethod(type2, "MoveNext"), null, null, new HarmonyMethod(typeof(ExpandedEffectPatches), "Transpile"));
					}
				}
				if (ExpandedEffects.For(item).Any((ExpandedEffect e) => e.Kind == "statuses"))
				{
					harmony.Patch(AccessTools.DeclaredMethod(type, "OnPlay"), new HarmonyMethod(typeof(ExpandedEffectPatches), "ResetStatuses"));
				}
				Active.Add(type);
			}
			catch (Exception ex)
			{
				ModEntry.Warn("Expansion disabled for " + item + ": " + ex.GetBaseException().Message);
			}
		}
	}
}
