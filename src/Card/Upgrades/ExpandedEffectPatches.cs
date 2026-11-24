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

internal static class ExpandedEffectPatches
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

	internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		List<CodeInstruction> list = instructions.Select((CodeInstruction c) => new CodeInstruction(c)).ToList();
		FieldInfo fieldInfo = __originalMethod.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault((FieldInfo f) => f.Name == "<>4__this" && typeof(CardModel).IsAssignableFrom(f.FieldType));
		Type obj = fieldInfo?.FieldType ?? __originalMethod.DeclaringType;
		string methodName = ((fieldInfo == null) ? __originalMethod.Name : __originalMethod.DeclaringType.Name.Split('>')[0].TrimStart('<'));
		Dictionary<int, List<CodeInstruction>> dictionary = new Dictionary<int, List<CodeInstruction>>();
		HashSet<int> hashSet = new HashSet<int>();
		foreach (ExpandedEffect item in from e in ExpandedEffects.For(obj.Name)
			where e.Method == methodName
			select e)
		{
			int num = 0;
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				MethodInfo methodInfo = list[num2].operand as MethodInfo;
				int num3 = -1;
				bool flag = false;
				string text = null;
				if (item.Kind == "redraw" && methodInfo?.DeclaringType == typeof(CardPileCmd) && methodInfo.Name == "Draw")
				{
					text = "DrawAndRedraw";
				}
				if (item.Kind == "generatedStats" && methodInfo?.DeclaringType == typeof(CardCmd) && methodInfo.Name == "Transform" && methodInfo.GetParameters()[1].ParameterType == typeof(CardModel))
				{
					int key = ArgumentEnd(list, num2, methodInfo, 1) + 1;
					List<CodeInstruction> list2 = Load(fieldInfo);
					list2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FinalCardEffects), "BuffGenerated")));
					dictionary.Add(key, list2);
					num++;
					continue;
				}
				if (item.Kind == "targetCost" && methodInfo?.DeclaringType.Name == "CardEnergyCost" && (methodInfo.Name == "SetThisCombat" || methodInfo.Name == "SetThisTurnOrUntilPlayed"))
				{
					num3 = 0;
				}
				if (item.Kind == "tolerance" && list[num2].opcode == OpCodes.Ret)
				{
					List<CodeInstruction> list3 = Load(fieldInfo);
					list3[0].labels.AddRange(list[num2].labels);
					list[num2].labels.Clear();
					list3.Add(new CodeInstruction(OpCodes.Ldstr, item.Attribute));
					list3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), "AdjustPlayability")));
					dictionary.Add(num2, list3);
					num++;
					continue;
				}
				if (item.Kind == "capture" && MatchesGeneric(methodInfo, "PowerCmd", "Apply", item.Target))
				{
					text = "ApplyPower";
				}
				if ((item.Kind == "autoReplays" || item.Kind == "attackAutoPlay" || item.Kind == "eidolon") && methodInfo?.DeclaringType == typeof(CardCmd) && methodInfo.Name == "AutoPlay")
				{
					text = ((item.Kind == "attackAutoPlay") ? "AutoAttacks" : "AutoReplays");
				}
				if (item.Kind == "autoDrawReplays" && methodInfo?.DeclaringType == typeof(CardPileCmd) && methodInfo.Name == "AutoPlayFromDrawPile")
				{
					text = "AutoDrawReplays";
				}
				if (item.Kind == "initialOrb" && MatchesGeneric(methodInfo, "OrbCmd", "Channel", item.Target))
				{
					text = "InitialGlass";
				}
				if (item.Kind == "shotDamage" && methodInfo?.DeclaringType == typeof(TheShot) && methodInfo.Name == "BuffFromFlorescence")
				{
					text = "BuffShotFromFlorescence";
				}
				if (item.Kind == "selfIncrement" && methodInfo?.Name == "IncrementSelfDamage")
				{
					text = "IncrementSelfDamage";
				}
				if ((item.Kind == "randomExhaust" || item.Kind == "mixedExhaust") && methodInfo?.DeclaringType == typeof(CardCmd) && methodInfo.Name == "Exhaust")
				{
					text = ((methodInfo.ReturnType == typeof(Task)) ? "RandomExhaust" : "RandomExhaustResult");
				}
				if (item.Kind == "retrieve" && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromCombatPile")
				{
					text = ((methodInfo.GetParameters().Length == 4) ? "Retrieve" : "RetrieveFiltered");
				}
				if (item.Kind == "retrieveTop" && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromCombatPile" && methodInfo.GetParameters().Length == 4)
				{
					text = "RetrieveToTop";
				}
				if ((item.Kind == "putBack" || item.Kind == "exhaustPile") && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromCombatPile")
				{
					text = ((methodInfo.GetParameters().Length == 4) ? "ChoosePile" : "ChoosePileFiltered");
				}
				if ((item.Kind == "reduceHand" || item.Kind == "variableSelection") && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromHand")
				{
					text = "ReduceHand";
				}
				if ((item.Kind == "reduceDiscard" || item.Kind == "variableSelection") && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromHandForDiscard")
				{
					text = "ReduceDiscard";
				}
				if ((item.Kind == "transformHand" || item.Kind == "exhaustHand" || item.Kind == "mixedExhaust") && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromHand")
				{
					text = "ExtraHandActions";
				}
				if (item.Kind == "upgradeHand" && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromHandForUpgrade")
				{
					text = "UpgradeHand";
				}
				if (item.Kind == "keywordHand" && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromHand")
				{
					text = "KeywordHand";
				}
				if (item.Kind == "selectHand" && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromHand")
				{
					text = "SelectHand";
				}
				if ((item.Kind == "generate" || item.Kind == "souls") && methodInfo?.DeclaringType == typeof(CardPileCmd) && methodInfo.Name == "AddGeneratedCardToCombat")
				{
					text = ((item.Kind == "souls") ? "AddSouls" : "AddGenerated");
				}
				if (item.Kind == "potions" && methodInfo?.DeclaringType == typeof(PotionCmd) && methodInfo.Name == "TryToProcure" && !methodInfo.IsGenericMethod)
				{
					text = "ProcurePotions";
				}
				if (item.Kind == "statuses" && methodInfo?.DeclaringType == typeof(CardPileCmd) && methodInfo.Name == "AddGeneratedCardToCombat")
				{
					text = "AddStatus";
				}
				if (item.Kind == "options" && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromChooseACardScreen")
				{
					text = "ChooseGenerated";
				}
				if (item.Kind == "chooseGenerated" && methodInfo?.DeclaringType == typeof(CardSelectCmd) && methodInfo.Name == "FromChooseACardScreen")
				{
					num++;
					continue;
				}
				if (item.Kind == "huntRewards" && methodInfo?.DeclaringType == typeof(CombatRoom) && methodInfo.Name == "AddExtraReward")
				{
					text = "HuntRewards";
				}
				if (text != null)
				{
					List<CodeInstruction> list4 = Load(fieldInfo);
					list4[0].labels.AddRange(list[num2].labels);
					list[num2].labels.Clear();
					MethodInfo methodInfo2 = AccessTools.Method(typeof(ExpandedEffectPatches), text);
					if (text == "ApplyPower")
					{
						methodInfo2 = methodInfo2.MakeGenericMethod(methodInfo.GetGenericArguments());
					}
					if (text == "RandomExhaustResult")
					{
						methodInfo2 = methodInfo2.MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments());
					}
					list4.Add(new CodeInstruction(OpCodes.Call, methodInfo2));
					dictionary.Add(num2, list4);
					hashSet.Add(num2);
					num++;
					continue;
				}
				if ((item.Kind == "hits" || item.Kind == "conditionalHits") && methodInfo?.Name == "WithHitCount")
				{
					num3 = 0;
				}
				if (item.Kind == "power" && MatchesGeneric(methodInfo, "PowerCmd", "Apply", item.Target))
				{
					num3 = 2;
				}
				if (item.Kind == "eidolon" && MatchesGeneric(methodInfo, "PowerCmd", "Apply", "IntangiblePower"))
				{
					num3 = 2;
				}
				if (item.Kind == "spreadDebuffs" && methodInfo?.DeclaringType == typeof(PowerCmd) && (methodInfo.Name == "ModifyAmount" || (methodInfo.Name == "Apply" && !methodInfo.IsGenericMethod)))
				{
					num3 = ((methodInfo.Name == "ModifyAmount") ? 2 : 3);
				}
				if (item.Kind == "splashPercent" && methodInfo?.DeclaringType == typeof(CreatureCmd) && methodInfo.Name == "Damage" && methodInfo.GetParameters().Length >= 6 && typeof(IEnumerable<Creature>).IsAssignableFrom(methodInfo.GetParameters()[1].ParameterType))
				{
					num3 = 2;
				}
				if (item.Kind == "selfPercent" && methodInfo?.DeclaringType == typeof(CreatureCmd) && methodInfo.Name == "Damage")
				{
					num3 = 2;
				}
				if (item.Kind == "initialOrb" && methodInfo?.DeclaringType == typeof(CardModel) && methodInfo.Name == "get_IsUpgraded")
				{
					List<CodeInstruction> list5 = Load(fieldInfo);
					list5.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), "ShouldChannelGlass")));
					dictionary.Add(num2 + 1, list5);
					num++;
					continue;
				}
				if (item.Kind == "varMultiplier" && MatchesGeneric(methodInfo, "PowerCmd", "Apply", item.Target))
				{
					num3 = 2;
				}
				if (item.Kind == "varSurcharge" && methodInfo?.Name == "AddThisCombat" && methodInfo.DeclaringType.Name == "CardEnergyCost")
				{
					num3 = 0;
				}
				if ((item.Kind == "discount" || item.Kind == "surcharge") && methodInfo?.DeclaringType.Name == "CardEnergyCost" && (methodInfo.Name == "AddThisCombat" || methodInfo.Name == "AddThisTurn" || methodInfo.Name == "AddUntilPlayed"))
				{
					num3 = 0;
				}
				if (item.Kind == "grantReplays" && methodInfo?.Name == "set_BaseReplayCount")
				{
					num3 = 0;
				}
				if (item.Kind == "vulnerableMultiplier" && MatchesGeneric(methodInfo, "PowerCmd", "Apply", item.Target))
				{
					num3 = 2;
				}
				if (item.Kind == "draw" && methodInfo?.DeclaringType == typeof(CardPileCmd) && methodInfo.Name == "Draw")
				{
					num3 = 1;
				}
				if (item.Kind == "hangGrowth" && num == 0 && methodInfo?.DeclaringType == typeof(Math) && methodInfo.Name == "Max" && methodInfo.ReturnType == typeof(int))
				{
					num3 = 1;
				}
				if (item.Kind == "killReplays" && methodInfo?.DeclaringType == typeof(Enumerable) && methodInfo.Name == "Count" && methodInfo.IsGenericMethod && methodInfo.GetGenericArguments()[0].Name == "DamageResult")
				{
					flag = true;
				}
				if (item.Kind == "autoPlay" && methodInfo?.Name == "AutoPlayFromDrawPile")
				{
					num3 = 2;
				}
				if (item.Kind == "options" && methodInfo?.DeclaringType.Name == "CardFactory" && methodInfo.Name == "GetDistinctForCombat")
				{
					num3 = 2;
				}
				if (item.Kind == "rewardChoices" && list[num2].opcode == OpCodes.Newobj && list[num2].operand is ConstructorInfo constructorInfo && constructorInfo.DeclaringType == typeof(CardReward))
				{
					num3 = 1;
				}
				if (item.Kind == "shivMultiplier" && methodInfo?.DeclaringType == typeof(Shiv) && methodInfo.Name == "CreateInHand")
				{
					num3 = 1;
				}
				if (item.Kind == "generationMultiplier" && methodInfo?.DeclaringType.Name == "CardFactory" && methodInfo.Name == "GetForCombat")
				{
					num3 = 2;
				}
				if (item.Kind == "enchant" && MatchesGeneric(methodInfo, "CardCmd", "Enchant", item.Target))
				{
					num3 = 1;
				}
				if (item.Kind == "energyMultiplier" && methodInfo?.DeclaringType == typeof(PlayerCmd) && methodInfo.Name == "GainEnergy")
				{
					num3 = 0;
				}
				if (item.Kind == "blockMultiplier" && methodInfo?.DeclaringType == typeof(CreatureCmd) && methodInfo.Name == "GainBlock")
				{
					num3 = 1;
				}
				if (item.Kind == "x" && (methodInfo?.Name == "ResolveEnergyXValue" || methodInfo?.Name == "ResolveStarXValue"))
				{
					flag = true;
				}
				if (item.Kind == "literal" && list[num2].LoadsConstant(item.Native))
				{
					flag = true;
				}
				if (item.Kind == "darkPassive" && list[num2].opcode == OpCodes.Stfld && list[num2].operand is FieldInfo fieldInfo2 && fieldInfo2.Name.StartsWith("<triggerCount>", StringComparison.Ordinal))
				{
					flag = true;
				}
				if (item.Kind == "teslaPassive" && methodInfo?.DeclaringType == typeof(OrbCmd) && methodInfo.Name == "Passive")
				{
					if (num == 0)
					{
						List<CodeInstruction> list6 = Load(fieldInfo);
						list6[0].labels.AddRange(list[num2].labels);
						list[num2].labels.Clear();
						list6.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), (methodInfo.GetParameters().Length == 3) ? "PassiveStable" : "Passive")));
						dictionary.Add(num2, list6);
						hashSet.Add(num2);
					}
					num++;
				}
				else if (num3 >= 0 || flag)
				{
					int key2;
					Type type;
					if (num3 >= 0)
					{
						MethodBase methodBase = (MethodBase)list[num2].operand;
						key2 = ArgumentEnd(list, num2, methodBase, num3) + 1;
						type = methodBase.GetParameters()[num3].ParameterType;
					}
					else
					{
						key2 = ((item.Kind == "darkPassive") ? num2 : (num2 + 1));
						type = typeof(int);
					}
					string text2 = ((type == typeof(decimal)) ? "AdjustDecimal" : ((type == typeof(int)) ? "AdjustInt" : null));
					if (text2 == null)
					{
						throw new InvalidOperationException("Unexpected argument type: " + type);
					}
					List<CodeInstruction> list7 = Load(fieldInfo);
					list7.Add(new CodeInstruction(OpCodes.Ldstr, item.Attribute));
					list7.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), text2)));
					if (item.Kind == "darkPassive")
					{
						list7[0].labels.AddRange(list[num2].labels);
						list[num2].labels.Clear();
					}
					if (dictionary.ContainsKey(key2))
					{
						throw new InvalidOperationException("Adaptadores superpuestos");
					}
					dictionary.Add(key2, list7);
					num++;
				}
			}
			int num4 = ((item.Kind == "teslaPassive" || item.Kind == "options") ? 2 : item.Sites);
			if (item.Card == "Tracking" && TrackingUsesPercent)
			{
				num4 = 1;
			}
			if (num != num4)
			{
				throw new InvalidOperationException($"Unexpected signature in {item.Card}/{item.Attribute}: {num}/{num4}");
			}
		}
		List<CodeInstruction> list8 = new List<CodeInstruction>();
		for (int num5 = 0; num5 <= list.Count; num5++)
		{
			if (dictionary.TryGetValue(num5, out var value))
			{
				list8.AddRange(value);
			}
			if (num5 < list.Count)
			{
				list8.Add(hashSet.Contains(num5) ? new CodeInstruction(OpCodes.Nop) : list[num5]);
			}
		}
		return list8;
	}

	private static bool MatchesGeneric(MethodInfo m, string type, string method, string target)
	{
		if (m?.DeclaringType.Name == type && m.Name == method && m.IsGenericMethod)
		{
			return m.GetGenericArguments()[0].Name == target;
		}
		return false;
	}

	private static List<CodeInstruction> Load(FieldInfo self)
	{
		if (!(self == null))
		{
			return new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, self)
			};
		}
		return new List<CodeInstruction>
		{
			new CodeInstruction(OpCodes.Ldarg_0)
		};
	}

	internal static int ArgumentEnd(List<CodeInstruction> code, int callIndex, MethodBase call, int arg)
	{
		int num = callIndex - 1;
		for (int num2 = call.GetParameters().Length - 1; num2 > arg; num2--)
		{
			num = ExpressionStart(code, num) - 1;
		}
		return num;
	}

	private static int ExpressionStart(List<CodeInstruction> code, int end)
	{
		int num = 1;
		for (int num2 = end; num2 >= 0; num2--)
		{
			CodeInstruction codeInstruction = code[num2];
			if (codeInstruction.opcode.FlowControl == FlowControl.Branch || codeInstruction.opcode.FlowControl == FlowControl.Cond_Branch)
			{
				throw new InvalidOperationException("Branching argument: needs a specific adapter");
			}
			int num3 = Push(codeInstruction);
			int num4 = Pop(codeInstruction);
			num = num - num3 + num4;
			if (num == 0)
			{
				return num2;
			}
			if (num < 0)
			{
				throw new InvalidOperationException("Pila IL ambigua");
			}
		}
		throw new InvalidOperationException("Argument has no IL producer");
	}

	private static int Push(CodeInstruction c)
	{
		if (c.opcode == OpCodes.Newobj)
		{
			return 1;
		}
		if (c.operand is MethodInfo methodInfo && (c.opcode == OpCodes.Call || c.opcode == OpCodes.Callvirt))
		{
			return (!(methodInfo.ReturnType == typeof(void))) ? 1 : 0;
		}
		return c.opcode.StackBehaviourPush switch
		{
			StackBehaviour.Push0 => 0, 
			StackBehaviour.Push1_push1 => 2, 
			StackBehaviour.Varpush => throw new InvalidOperationException("Push variable"), 
			_ => 1, 
		};
	}

	private static int Pop(CodeInstruction c)
	{
		if (c.operand is MethodBase methodBase && (c.opcode == OpCodes.Call || c.opcode == OpCodes.Callvirt || c.opcode == OpCodes.Newobj))
		{
			return methodBase.GetParameters().Length + ((c.opcode != OpCodes.Newobj && !methodBase.IsStatic) ? 1 : 0);
		}
		switch (c.opcode.StackBehaviourPop)
		{
		case StackBehaviour.Pop0:
			return 0;
		case StackBehaviour.Pop1:
		case StackBehaviour.Popi:
		case StackBehaviour.Popref:
			return 1;
		case StackBehaviour.Pop1_pop1:
		case StackBehaviour.Popi_pop1:
		case StackBehaviour.Popi_popi:
		case StackBehaviour.Popi_popi8:
		case StackBehaviour.Popi_popr4:
		case StackBehaviour.Popi_popr8:
		case StackBehaviour.Popref_pop1:
		case StackBehaviour.Popref_popi:
			return 2;
		case StackBehaviour.Popref_popi_popi:
		case StackBehaviour.Popref_popi_popi8:
		case StackBehaviour.Popref_popi_popr4:
		case StackBehaviour.Popref_popi_popr8:
		case StackBehaviour.Popref_popi_popref:
		case StackBehaviour.Popref_popi_pop1:
			return 3;
		default:
			throw new InvalidOperationException("Unsupported IL pop: " + c.opcode);
		}
	}

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
				int num = Delta(card, attribute);
				if (attribute == "extra:hits" && card.GetType().Name == "Dismantle" && native == 1)
				{
					return native;
				}
				if (!(attribute == "extra:multiplier"))
				{
					return native + num;
				}
				return native * (1 + num);
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
			int num = Delta(card, attribute);
			if (!(attribute == "extra:multiplier"))
			{
				return native + (decimal)num;
			}
			return native * (1m + (decimal)num);
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
		int num = Delta(card, attribute);
		if (native || num <= 0)
		{
			return native;
		}
		return ((attribute == "extra:drawTolerance") ? PileType.Draw.GetPile(card.Owner).Cards.Count : PileType.Hand.GetPile(card.Owner).Cards.Count((CardModel c) => c.Type != CardType.Attack)) <= num;
	}

	private static Task PassiveStable(PlayerChoiceContext context, OrbModel orb, Creature target, CardModel card)
	{
		return Passive(context, orb, target, silent: false, card);
	}

	private static int Delta(CardModel card, string key)
	{
		return BidirectionalEffects.RawDelta(card.GetType().Name, key, CardStateStore.Get(card));
	}

	/// <summary>Combined build (Florescence): The Shot it creates deals more damage per exhausted card.</summary>
	private static void BuffShotFromFlorescence(TheShot shot, decimal count, CardModel card)
	{
		int delta = Delta(card, "extra:shotDamage");
		if (delta != 0)
		{
			shot.DynamicVars.ExtraDamage.BaseValue += delta;
		}
		shot.BuffFromFlorescence(count);
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
		int num = Delta(card, key);
		if (num == 0)
		{
			return await CardSelectCmd.FromCombatPile(context, pile, player, prefs, filter);
		}
		if (prefs.MaxSelect + num <= 0)
		{
			return Array.Empty<CardModel>();
		}
		CardModel[] array = (await CardSelectCmd.FromCombatPile(context, pile, player, OptionalPrefs(prefs, prefs.MaxSelect + num), filter)).ToArray();
		if (card is Cleanse)
		{
			CardModel[] array2 = array;
			foreach (CardModel card2 in array2)
			{
				await GameCompat.Exhaust(context, card2);
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
		int num = Delta(card, transform ? "extra:selection" : "extra:exhaustMaximum");
		if (num == 0)
		{
			return await CardSelectCmd.FromHand(context, player, prefs, filter, source);
		}
		if (prefs.MaxSelect + num <= 0)
		{
			return Array.Empty<CardModel>();
		}
		foreach (CardModel item in await CardSelectCmd.FromHand(context, player, OptionalPrefs(prefs, prefs.MaxSelect + num), filter, source))
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
	/// Combined build (Recall): lets you pick extra cards; the card itself puts the first one on top of the
	/// draw pile, and this puts the extras there too.
	/// </summary>
	private static async Task<IEnumerable<CardModel>> RetrieveToTop(PlayerChoiceContext context, CardPile pile, Player player, CardSelectorPrefs prefs, CardModel card)
	{
		int num = Delta(card, "extra:selection");
		if (num <= 0)
		{
			return await CardSelectCmd.FromCombatPile(context, pile, player, prefs);
		}
		CardModel[] picked = (await CardSelectCmd.FromCombatPile(context, pile, player, ExpandedPrefs(prefs, checked(prefs.MaxSelect + num)))).ToArray();
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
		int num = Delta(card, "extra:selection");
		if (num <= 0)
		{
			return await CardSelectCmd.FromCombatPile(context, pile, player, prefs, filter);
		}
		CardModel[] array = (await CardSelectCmd.FromCombatPile(context, pile, player, ExpandedPrefs(prefs, checked(prefs.MaxSelect + num)), filter)).ToArray();
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
		int num = Delta(source, "extra:generation");
		if (num <= 0)
		{
			return result;
		}
		bool colorless = source is ManifestAuthority || source is Largesse;
		IEnumerable<CardModel> source2 = (colorless ? ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(beneficiary.UnlockState, creator.RunState.CardMultiplayerConstraint) : creator.Character.CardPool.GetUnlockedCards(creator.UnlockState, creator.RunState.CardMultiplayerConstraint));
		if (source is BlotOutTheSky)
		{
			source2 = source2.Where((CardModel c) => c.Tags.Contains(CardTag.Shoot));
		}
		else if (!colorless)
		{
			source2 = source2.Where((CardModel c) => c.Type == generated.Type);
		}
		CardModel[] array = CardFactory.GetDistinctForCombat(beneficiary, source2.Where((CardModel c) => c.Id != generated.Id), num, creator.RunState.Rng.CombatCardGeneration).ToArray();
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

	private static int TextValue(CardModel card, ExpandedEffect effect, int value)
	{
		if (effect.Kind == "chooseGenerated")
		{
			return Math.Min(value, CardAttributes.CurrentValue(card, "extra:options"));
		}
		if (effect.Kind == "energyMultiplier" || effect.Kind == "blockMultiplier")
		{
			return value;
		}
		if (effect.Attribute != "extra:multiplier" && effect.Attribute != "extra:hangGrowth" && effect.Attribute != "extra:discount" && effect.Attribute != "extra:killReplays" && effect.Attribute != "extra:passive")
		{
			return value;
		}
		int num = Native(card, effect);
		decimal num2 = (decimal)num * (1m + (decimal)value - (decimal)num);
		if (!(effect.Attribute == "extra:hangGrowth"))
		{
			return (int)num2;
		}
		return (int)Math.Min(999999999m, num2);
	}

	internal static string DisplayValue(CardModel card, string attribute, int value)
	{
		if (AttributeKeys.IsVar(attribute) && SteppedValues.TryGet(card, AttributeKeys.VarName(attribute), out SteppedValues.Stepped stepped))
		{
			return SteppedValues.Display(stepped, value);
		}
		ExpandedEffect expandedEffect = ExpandedEffects.Find(card.GetType().Name, attribute);
		return FinalCardEffects.DisplayValue(attribute, (expandedEffect == null) ? value : TextValue(card, expandedEffect, value));
	}

	internal static string Rewrite(CardModel card, string text)
	{
		if (CardStateStore.Get(card) == null || !Enabled(card))
		{
			return text;
		}
		checked
		{
			foreach (ExpandedEffect item in from e in ExpandedEffects.For(card.GetType().Name)
				group e by e.Attribute into g
				select g.First())
			{
				int num = Delta(card, item.Attribute);
				if (num == 0 || (card is Armaments && card.IsUpgraded))
				{
					continue;
				}
				int num2 = TextValue(card, item, CardAttributes.CurrentValue(card, item.Attribute));
				if (item.Attribute == "extradown:quest")
				{
					num2 = card.DynamicVars[(item.Card == "Guilty") ? "Combats" : "Rooms"].IntValue;
				}
				if (item.Card == "Reflect")
				{
					num2 = CardAttributes.CurrentValue(card, "extra:reflectionPercent") * CardAttributes.CurrentValue(card, "extra:power");
				}
				string text2 = ExpandedEffectText.RewriteLine(text, item.Card, item.Attribute, item.Kind, (item.Attribute == "extra:sharedForge") ? (num2 * 25) : ((item.Kind == "x") ? num : num2));
				if (text2 != null)
				{
					text = text2;
				}
				else
				{
					if (AttributeKeys.IsVar(item.Attribute))
					{
						continue;
					}
					BidirectionalEffect bidirectionalEffect = BidirectionalEffects.Find(card.GetType().Name, item.Attribute);
					if (bidirectionalEffect != null)
					{
						string text3 = ((!(card is TrueGrit) || card.IsUpgraded) ? Lang.T(bidirectionalEffect.English) : Lang.T("Random Exhausts"));
						text = text + "\n" + text3 + ": " + CardAttributes.CurrentValue(card, item.Attribute) + Lang.T(" (adjusted).");
						continue;
					}
					string format = ((!(item.Card == "Eidolon")) ? ((item.Card == "Tracking" && TrackingUsesPercent) ? Lang.T("The damage bonus against Weak increases by {0} percentage points.") : Lang.T(item.ExtraEnglish)) : ((!EidolonUsesAutoPlay) ? Lang.T("Gain {0} additional Intangible when the requirement is met.") : Lang.T("Each automatic card is played {0} additional times.")));
					if (item.Attribute == "extra:blockGrowth")
					{
						format = Lang.T("At the end of your turn, increase your Block by {0}%.");
					}
					if (item.Attribute == "extra:sharedForge")
					{
						format = Lang.T("Allies receive {0}% of your Forge.");
					}
					if (item.Attribute == "extra:shadowMultiplier")
					{
						format = Lang.T("Boosted damage is multiplied by {0}.");
					}
					int num3 = ((item.Attribute == "extra:blockGrowth") ? (num * 10) : ((item.Attribute == "extra:sharedForge") ? (CardAttributes.CurrentValue(card, item.Attribute) * 25) : ((item.Attribute == "extra:shadowMultiplier") ? CardAttributes.CurrentValue(card, item.Attribute) : ((item.Kind == "chooseGenerated") ? Math.Max(0, unchecked(num2 - Native(card, item))) : Math.Abs(num)))));
					text = text + "\n" + string.Format(format, num3);
				}
			}
			return text;
		}
	}
}
