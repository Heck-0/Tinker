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

// The IL patch: finds each expanded effect's value in a card's code and routes it through the methods in ExpandedEffectPatches.Calls.cs.
internal static partial class ExpandedEffectPatches
{
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
			for (int i = 0; i < list.Count; i++)
			{
				MethodInfo methodInfo = list[i].operand as MethodInfo;
				int num3 = -1;
				bool flag = false;
				string text = null;
				if (item.Kind == "redraw" && methodInfo?.DeclaringType == typeof(CardPileCmd) && methodInfo.Name == "Draw")
				{
					text = "DrawAndRedraw";
				}
				if (item.Kind == "generatedStats" && methodInfo?.DeclaringType == typeof(CardCmd) && methodInfo.Name == "Transform" && methodInfo.GetParameters()[1].ParameterType == typeof(CardModel))
				{
					int key = ArgumentEnd(list, i, methodInfo, 1) + 1;
					List<CodeInstruction> load = Load(fieldInfo);
					load.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FinalCardEffects), "BuffGenerated")));
					dictionary.Add(key, load);
					num++;
					continue;
				}
				if (item.Kind == "targetCost" && methodInfo?.DeclaringType.Name == "CardEnergyCost" && (methodInfo.Name == "SetThisCombat" || methodInfo.Name == "SetThisTurnOrUntilPlayed"))
				{
					num3 = 0;
				}
				if (item.Kind == "tolerance" && list[i].opcode == OpCodes.Ret)
				{
					List<CodeInstruction> load2 = Load(fieldInfo);
					load2[0].labels.AddRange(list[i].labels);
					list[i].labels.Clear();
					load2.Add(new CodeInstruction(OpCodes.Ldstr, item.Attribute));
					load2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), "AdjustPlayability")));
					dictionary.Add(i, load2);
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
					List<CodeInstruction> load3 = Load(fieldInfo);
					load3[0].labels.AddRange(list[i].labels);
					list[i].labels.Clear();
					MethodInfo method = AccessTools.Method(typeof(ExpandedEffectPatches), text);
					if (text == "ApplyPower")
					{
						method = method.MakeGenericMethod(methodInfo.GetGenericArguments());
					}
					if (text == "RandomExhaustResult")
					{
						method = method.MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments());
					}
					load3.Add(new CodeInstruction(OpCodes.Call, method));
					dictionary.Add(i, load3);
					hashSet.Add(i);
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
					List<CodeInstruction> load4 = Load(fieldInfo);
					load4.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), "ShouldChannelGlass")));
					dictionary.Add(i + 1, load4);
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
				if (item.Kind == "rewardChoices" && list[i].opcode == OpCodes.Newobj && list[i].operand is ConstructorInfo constructorInfo && constructorInfo.DeclaringType == typeof(CardReward))
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
				if (item.Kind == "literal" && list[i].LoadsConstant(item.Native))
				{
					flag = true;
				}
				if (item.Kind == "darkPassive" && list[i].opcode == OpCodes.Stfld && list[i].operand is FieldInfo fieldInfo2 && fieldInfo2.Name.StartsWith("<triggerCount>", StringComparison.Ordinal))
				{
					flag = true;
				}
				if (item.Kind == "teslaPassive" && methodInfo?.DeclaringType == typeof(OrbCmd) && methodInfo.Name == "Passive")
				{
					if (num == 0)
					{
						List<CodeInstruction> load5 = Load(fieldInfo);
						load5[0].labels.AddRange(list[i].labels);
						list[i].labels.Clear();
						load5.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), (methodInfo.GetParameters().Length == 3) ? "PassiveStable" : "Passive")));
						dictionary.Add(i, load5);
						hashSet.Add(i);
					}
					num++;
				}
				else if (num3 >= 0 || flag)
				{
					int key2;
					Type type;
					if (num3 >= 0)
					{
						MethodBase methodBase = (MethodBase)list[i].operand;
						key2 = ArgumentEnd(list, i, methodBase, num3) + 1;
						type = methodBase.GetParameters()[num3].ParameterType;
					}
					else
					{
						key2 = ((item.Kind == "darkPassive") ? i : (i + 1));
						type = typeof(int);
					}
					string text2 = ((type == typeof(decimal)) ? "AdjustDecimal" : ((type == typeof(int)) ? "AdjustInt" : null));
					if (text2 == null)
					{
						throw new InvalidOperationException("Unexpected argument type: " + type);
					}
					List<CodeInstruction> load6 = Load(fieldInfo);
					load6.Add(new CodeInstruction(OpCodes.Ldstr, item.Attribute));
					load6.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExpandedEffectPatches), text2)));
					if (item.Kind == "darkPassive")
					{
						load6[0].labels.AddRange(list[i].labels);
						list[i].labels.Clear();
					}
					if (dictionary.ContainsKey(key2))
					{
						throw new InvalidOperationException("Adaptadores superpuestos");
					}
					dictionary.Add(key2, load6);
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
		List<CodeInstruction> list9 = new List<CodeInstruction>();
		for (int j = 0; j <= list.Count; j++)
		{
			if (dictionary.TryGetValue(j, out var value))
			{
				list9.AddRange(value);
			}
			if (j < list.Count)
			{
				list9.Add(hashSet.Contains(j) ? new CodeInstruction(OpCodes.Nop) : list[j]);
			}
		}
		return list9;
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
		for (int i = call.GetParameters().Length - 1; i > arg; i--)
		{
			num = ExpressionStart(code, num) - 1;
		}
		return num;
	}

	private static int ExpressionStart(List<CodeInstruction> code, int end)
	{
		int num = 1;
		for (int i = end; i >= 0; i--)
		{
			CodeInstruction codeInstruction = code[i];
			if (codeInstruction.opcode.FlowControl == FlowControl.Branch || codeInstruction.opcode.FlowControl == FlowControl.Cond_Branch)
			{
				throw new InvalidOperationException("Branching argument: needs a specific adapter");
			}
			int push = Push(codeInstruction);
			int pop = Pop(codeInstruction);
			num = num - push + pop;
			if (num == 0)
			{
				return i;
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
}
