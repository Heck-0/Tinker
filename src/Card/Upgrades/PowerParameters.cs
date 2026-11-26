using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Tinker.Common;

namespace Tinker.Card;

internal static class PowerParameters
{
	private sealed class Parameters
	{
		internal readonly Dictionary<string, int> Values = new Dictionary<string, int>();
	}

	[HarmonyPatch(typeof(AbstractModel), "MutableClone")]
	internal static class CloneParameters
	{
		private static void Postfix(AbstractModel __instance, AbstractModel __result)
		{
			if (!(__instance is PowerModel key) || !(__result is PowerModel powerModel) || !Store.TryGetValue(key, out var value))
			{
				return;
			}
			Parameters parameters = new Parameters();
			foreach (KeyValuePair<string, int> value2 in value.Values)
			{
				parameters.Values.Add(value2.Key, value2.Value);
			}
			Store.Remove(powerModel);
			Store.Add(powerModel, parameters);
		}
	}

	[HarmonyPatch(typeof(PowerModel), "get_HoverTips")]
	internal static class ParameterTooltip
	{
		private static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			if (__result == null || !Store.TryGetValue(__instance, out var value))
			{
				return;
			}
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, int> pair in value.Values)
			{
				if (__instance.GetType().Name == "DoubleDamagePower" && pair.Key == "extra:shadowMultiplier" && Value(__instance, "shadow:boostTurns", 0) > 0)
				{
					list.Add(Lang.T("Damage multiplier") + ": " + FinalCardEffects.DisplayValue(pair.Key, pair.Value) + ".");
					continue;
				}
				ExpandedEffect expandedEffect = ExpandedEffects.Entries.FirstOrDefault((ExpandedEffect e) => e.Target == __instance.GetType().Name && e.Attribute == pair.Key);
				if (expandedEffect != null && pair.Value != ((!(__instance.GetType().Name == "DemesnePower")) ? expandedEffect.Native : 0))
				{
					list.Add((Lang.T(expandedEffect.English)) + ": " + FinalCardEffects.DisplayValue(pair.Key, pair.Value) + ".");
				}
			}
			if (list.Count != 0)
			{
				List<IHoverTip> list2 = __result.ToList();
				if (list2.Count > 0 && list2[0] is HoverTip hoverTip)
				{
					object obj = hoverTip;
					AccessTools.PropertySetter(typeof(HoverTip), "Description").Invoke(obj, new object[1] { hoverTip.Description + "\n" + string.Join("\n", list) });
					list2[0] = (HoverTip)obj;
					__result = list2;
				}
			}
		}
	}

	private static readonly ConditionalWeakTable<PowerModel, Parameters> Store = new ConditionalWeakTable<PowerModel, Parameters>();

	private static readonly HashSet<string> Ready = new HashSet<string>();

	internal static bool Enabled(string name)
	{
		return Ready.Contains(name);
	}

	private static Type Power(string name)
	{
		return typeof(PowerModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Powers." + name);
	}

	internal static void Install(Harmony harmony)
	{
		foreach (string item in (from e in ExpandedEffects.Entries
			where e.Kind == "capture"
			select e.Target).Distinct())
		{
			try
			{
				Type type = Power(item);
				if (type == null)
				{
					continue;
				}
				if (FinalCardEffects.InstallPower(harmony, item, type))
				{
					Ready.Add(item);
					continue;
				}
				switch (item)
				{
				case "EchoFormPower":
				case "SignalBoostPower":
				case "BurstPower":
				case "OneTwoPunchPower":
					harmony.Patch(AccessTools.DeclaredMethod(type, "ModifyCardPlayCount"), null, new HarmonyMethod(typeof(PowerParameters), "Replays"));
					break;
				case "DemesnePower":
					harmony.Patch(AccessTools.DeclaredMethod(type, "ModifyMaxEnergy"), null, new HarmonyMethod(typeof(PowerParameters), "Energy"));
					break;
				case "OrbitPower":
					Patch(harmony, AccessTools.PropertyGetter(type, "DisplayAmount"));
					Patch(harmony, MoveNext(type, "AfterEnergySpent"));
					break;
				case "AutomationPower":
					Patch(harmony, MoveNext(type, "AfterCardDrawn"));
					break;
				case "PanachePower":
					Patch(harmony, MoveNext(type, "AfterCardPlayed"));
					Patch(harmony, AccessTools.DeclaredMethod(type, "AfterSideTurnEnd"));
					break;
				case "LightningRodPower":
					Patch(harmony, MoveNext(type, "AfterEnergyReset"));
					break;
				case "IterationPower":
					Patch(harmony, MoveNext(type, "AfterCardDrawn"));
					break;
				case "PhantomBladesPower":
					Patch(harmony, AccessTools.DeclaredMethod(type, "ModifyDamageAdditive"));
					break;
				case "LethalityPower":
					Patch(harmony, AccessTools.DeclaredMethod(type, "ModifyDamageMultiplicative"));
					break;
				case "JugglingPower":
					Patch(harmony, MoveNext(type, (AccessTools.DeclaredMethod(type, "BeforeCardPlayed") != null) ? "BeforeCardPlayed" : "AfterCardPlayed"));
					break;
				case "CacophonyPower":
					Patch(harmony, MoveNext(type, "AfterCardDrawn"));
					break;
				case "ReflectPower":
					Patch(harmony, MoveNext(type, "AfterDamageReceived"));
					break;
				case "HellraiserPower":
					Patch(harmony, MoveNext(type, "AfterCardDrawnEarly"));
					break;
				case "ConquerorPower":
				case "UnmovablePower":
					harmony.Patch(AccessTools.DeclaredMethod(type, (item == "ConquerorPower") ? "ModifyDamageMultiplicative" : "ModifyBlockMultiplicative"), null, new HarmonyMethod(typeof(PowerParameters), "ConditionalMultiplier"));
					break;
				case "ColossusPower":
					harmony.Patch(AccessTools.DeclaredMethod(type, "ModifyDamageMultiplicative"), null, new HarmonyMethod(typeof(PowerParameters), "ProtectedDamage"));
					break;
				case "TankPower":
					harmony.Patch(AccessTools.DeclaredMethod(Power("GuardedPower"), "ModifyDamageMultiplicative"), null, new HarmonyMethod(typeof(PowerParameters), "GuardedDamage"));
					break;
				case "BeaconOfHopePower":
					Patch(harmony, MoveNext(type, "AfterBlockGained"));
					break;
				case "DebilitatePower":
					harmony.Patch(AccessTools.DeclaredMethod(type, "ModifyVulnerableMultiplier"), null, new HarmonyMethod(typeof(PowerParameters), "AmplifyVulnerable"));
					harmony.Patch(AccessTools.DeclaredMethod(type, "ModifyWeakMultiplier"), null, new HarmonyMethod(typeof(PowerParameters), "AmplifyWeak"));
					break;
				case "TyrannyPower":
				case "ToolsOfTheTradePower":
					harmony.Patch(AccessTools.DeclaredMethod(type, "ModifyHandDraw"), null, new HarmonyMethod(typeof(PowerParameters), "ExtraDraw"));
					break;
				}
				Ready.Add(item);
			}
			catch (Exception ex)
			{
				ModEntry.Warn("Parameter disabled for " + item + ": " + ex.GetBaseException().Message);
			}
		}
	}

	private static void Patch(Harmony h, MethodInfo m)
	{
		h.Patch(m, null, null, new HarmonyMethod(typeof(PowerParameters), "Transpile"));
	}

	private static MethodInfo MoveNext(Type type, string method)
	{
		return AccessTools.DeclaredMethod(AccessTools.DeclaredMethod(type, method).GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType, "MoveNext");
	}

	internal static void Attach(PowerModel power, CardModel card)
	{
		if (power == null || card == null)
		{
			return;
		}
		Parameters orCreateValue = Store.GetOrCreateValue(power);
		foreach (ExpandedEffect item in from e in ExpandedEffects.For(card.GetType().Name)
			where e.Kind == "capture" && e.Target == power.GetType().Name
			select e)
		{
			int num = Value(power, item.Attribute, (!(item.Target == "DemesnePower")) ? item.Native : 0);
			int currentValue = CardAttributes.CurrentValue(card, item.Attribute);
			int value2;
			if (item.Target == "DemesnePower")
			{
				currentValue = (orCreateValue.Values.TryGetValue(item.Attribute, out var value) ? value : 0) + (CardStateStore.Get(card)?.RawDelta(item.Attribute) ?? 0);
			}
			else if (orCreateValue.Values.TryGetValue(item.Attribute, out value2))
			{
				currentValue = ((AttributeDirection.SignOf(item.Attribute) < 0) ? Math.Min(value2, currentValue) : Math.Max(value2, currentValue));
			}
			orCreateValue.Values[item.Attribute] = currentValue;
			if (item.Target == "OutbreakPower" && power.DynamicVars.ContainsKey("Repeat"))
			{
				power.DynamicVars["Repeat"].BaseValue = currentValue;
			}
			if (currentValue != num)
			{
				if (item.Target == "RollingBoulderPower")
				{
					power.DynamicVars.Damage.BaseValue = currentValue;
				}
				if (item.Target == "DanseMacabrePower")
				{
					power.DynamicVars.Energy.BaseValue = currentValue;
				}
				if (item.Target == "PaleBlueDotPower")
				{
					power.DynamicVars["CardPlay"].BaseValue = currentValue;
				}
				if (item.Target == "OrbitPower")
				{
					power.DynamicVars.Energy.BaseValue = currentValue;
				}
				if (item.Target == "PanachePower")
				{
					power.DynamicVars["CardsLeft"].BaseValue = Math.Max(1, power.DynamicVars["CardsLeft"].IntValue + currentValue - num);
				}
				if (item.Target == "CacophonyPower")
				{
					power.DynamicVars.Cards.BaseValue = Math.Max(1, power.DynamicVars.Cards.IntValue + currentValue - num);
				}
				if (item.Target == "AutomationPower")
				{
					power.DynamicVars["BaseCards"].BaseValue = currentValue;
					Type nestedType = power.GetType().GetNestedType("Data", BindingFlags.NonPublic);
					object genericMethod = AccessTools.Method(typeof(PowerModel), "GetInternalData").MakeGenericMethod(nestedType).Invoke(power, null);
					FieldInfo fieldInfo = AccessTools.Field(nestedType, "cardsLeft");
					fieldInfo.SetValue(genericMethod, Math.Max(1, (int)fieldInfo.GetValue(genericMethod) + currentValue - num));
				}
			}
		}
		AccessTools.Method(typeof(PowerModel), "InvokeDisplayAmountChanged").Invoke(power, null);
	}

	internal static int Value(PowerModel power, string key, int native)
	{
		if (power == null || !Store.TryGetValue(power, out var value) || !value.Values.TryGetValue(key, out var value2))
		{
			return native;
		}
		return value2;
	}

	internal static void Set(PowerModel power, string key, int value)
	{
		Store.GetOrCreateValue(power).Values[key] = value;
	}

	internal static bool Has(PowerModel power, string key)
	{
		if (power != null && Store.TryGetValue(power, out var value))
		{
			return value.Values.ContainsKey(key);
		}
		return false;
	}

	private static void Replays(PowerModel __instance, int playCount, ref int __result)
	{
		if (__result > playCount)
		{
			__result = checked(__result + Value(__instance, "extra:replays", 1) - 1);
		}
	}

	private static void Energy(PowerModel __instance, decimal amount, ref decimal __result)
	{
		if (__result > amount)
		{
			__result += (decimal)Value(__instance, "var:Energy", 0);
		}
	}

	private static void ExtraDraw(PowerModel __instance, decimal count, ref decimal __result)
	{
		if (__result > count)
		{
			__result += (decimal)(Value(__instance, "extra:draw", 1) - 1);
		}
	}

	private static void ConditionalMultiplier(PowerModel __instance, ref decimal __result)
	{
		if (__result > 1m)
		{
			__result = Value(__instance, "extra:conditionalMultiplier", 2);
		}
	}

	private static int Protection(PowerModel p)
	{
		return Math.Min(90, Math.Max(0, Value(p, "extra:protectionPercent", 50)));
	}

	private static void AmplifyVulnerable(PowerModel __instance, decimal amount, ref decimal __result)
	{
		if (__result > amount)
		{
			__result = amount + (__result - amount) * (decimal)Value(__instance, "extra:debilitatePercent", 100) / 100m;
		}
	}

	private static void AmplifyWeak(PowerModel __instance, decimal amount, ref decimal __result)
	{
		if (__result < amount)
		{
			__result = Math.Max(0.1m, amount - (amount - __result) * (decimal)Value(__instance, "extra:debilitatePercent", 100) / 100m);
		}
	}

	private static void ProtectedDamage(PowerModel __instance, ref decimal __result)
	{
		if (__result < 1m)
		{
			__result = 1m - (decimal)Protection(__instance) / 100m;
		}
	}

	private static void GuardedDamage(PowerModel __instance, ref decimal __result)
	{
		if (!(__result >= 1m))
		{
			PowerModel powerModel = __instance.Applier?.Powers.FirstOrDefault((PowerModel p) => p.GetType().Name == "TankPower");
			if (powerModel != null)
			{
				__result = 1m - (decimal)Protection(powerModel) / 100m;
			}
		}
	}

	internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		List<CodeInstruction> list = instructions.ToList();
		List<CodeInstruction> list3 = new List<CodeInstruction>();
		FieldInfo fieldInfo = __originalMethod.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((FieldInfo f) => f.Name == "<>4__this" && typeof(PowerModel).IsAssignableFrom(f.FieldType));
		string name = (fieldInfo?.FieldType ?? __originalMethod.DeclaringType).Name;
		int num = 0;
		if (name == "ReflectPower")
		{
			Dictionary<int, List<CodeInstruction>> dictionary = new Dictionary<int, List<CodeInstruction>>();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].operand is MethodInfo methodInfo && methodInfo.DeclaringType == typeof(CreatureCmd) && methodInfo.Name == "Damage")
				{
					int key = ExpandedEffectPatches.ArgumentEnd(list, i, methodInfo, 2) + 1;
					dictionary.Add(key, new List<CodeInstruction>
					{
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, fieldInfo),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerParameters), "ReflectionDamage"))
					});
					num++;
				}
			}
			if (num != 1)
			{
				throw new InvalidOperationException("Unexpected Reflect signature: " + num);
			}
			for (int j = 0; j <= list.Count; j++)
			{
				if (dictionary.TryGetValue(j, out var value))
				{
					list3.AddRange(value);
				}
				if (j < list.Count)
				{
					list3.Add(list[j]);
				}
			}
			return list3;
		}
		for (int k = 0; k < list.Count; k++)
		{
			CodeInstruction codeInstruction = list[k];
			if (name == "BeaconOfHopePower" && codeInstruction.operand is MethodInfo methodInfo6 && methodInfo6.DeclaringType == typeof(decimal) && methodInfo6.Name == "op_Multiply")
			{
				list3.Add(codeInstruction);
				list3.Add(new CodeInstruction(OpCodes.Ldarg_0));
				list3.Add(new CodeInstruction(OpCodes.Ldfld, fieldInfo));
				list3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerParameters), "SharedBlock")));
				num++;
				continue;
			}
			bool flag = (name == "OrbitPower" && codeInstruction.LoadsConstant(4L)) || (name == "PanachePower" && codeInstruction.LoadsConstant(5L)) || (name == "AutomationPower" && codeInstruction.LoadsConstant(10L)) || (name == "JugglingPower" && codeInstruction.LoadsConstant(3L)) || (name == "CacophonyPower" && codeInstruction.LoadsConstant(33L));
			if (name == "LethalityPower" && codeInstruction.opcode == OpCodes.Ldloc_1 && k + 1 < list.Count && (list[k + 1].opcode == OpCodes.Ble || list[k + 1].opcode == OpCodes.Ble_S))
			{
				list3.Add(codeInstruction);
				list3.Add(new CodeInstruction(OpCodes.Ldarg_0));
				list3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerParameters), "AttackCoverage")));
				num++;
				continue;
			}
			bool flag2 = ((name == "IterationPower" && codeInstruction.LoadsConstant(1L)) || (name == "PhantomBladesPower" && codeInstruction.LoadsConstant(0L))) && k + 1 < list.Count && (list[k + 1].opcode == OpCodes.Ble || list[k + 1].opcode == OpCodes.Ble_S);
			bool flag3 = name == "LightningRodPower" && codeInstruction.operand is MethodInfo methodInfo7 && methodInfo7.DeclaringType == typeof(OrbCmd) && methodInfo7.Name == "Channel";
			bool flag4 = name == "HellraiserPower" && codeInstruction.operand is MethodInfo methodInfo8 && methodInfo8.DeclaringType == typeof(CardCmd) && methodInfo8.Name == "AutoPlay";
			if (flag || flag2 || flag3 || flag4)
			{
				CodeInstruction codeInstruction3 = new CodeInstruction(OpCodes.Ldarg_0);
				codeInstruction3.labels.AddRange(codeInstruction.labels);
				codeInstruction3.blocks.AddRange(codeInstruction.blocks);
				list3.Add(codeInstruction3);
				if (fieldInfo != null)
				{
					list3.Add(new CodeInstruction(OpCodes.Ldfld, fieldInfo));
				}
				MethodInfo method = AccessTools.Method(typeof(PowerParameters), flag4 ? "AutoPlay" : (flag3 ? "Channel" : (flag2 ? "Coverage" : "Threshold")));
				if (flag3)
				{
					method = method.MakeGenericMethod(((MethodInfo)codeInstruction.operand).GetGenericArguments());
				}
				list3.Add(new CodeInstruction(OpCodes.Call, method));
				num++;
			}
			else
			{
				list3.Add(codeInstruction);
			}
		}
		int num5 = ((!(name == "OrbitPower") || !(__originalMethod.Name == "get_DisplayAmount")) ? 1 : 2);
		if (num != num5)
		{
			throw new InvalidOperationException($"Unexpected parameter signature in {name}: {num}/{num5}");
		}
		return list3;
	}

	private static int Threshold(PowerModel p)
	{
		return Math.Max(1, Value(p, "extradown:threshold", (p.GetType().Name == "OrbitPower") ? 4 : ((p.GetType().Name == "PanachePower") ? 5 : ((p.GetType().Name == "JugglingPower") ? 3 : ((p.GetType().Name == "CacophonyPower") ? 33 : 10)))));
	}

	private static int AttackCoverage(int native, PowerModel power)
	{
		return checked(native + Value(power, "extra:coverage", 1) - 1);
	}

	private static int Coverage(PowerModel p)
	{
		return Value(p, "extra:coverage", 1) - ((p.GetType().Name == "PhantomBladesPower") ? 1 : 0);
	}

	private static decimal ReflectionDamage(decimal native, PowerModel p)
	{
		return native * (decimal)Value(p, "extra:reflectionPercent", 100) / 100m;
	}

	private static decimal SharedBlock(decimal native, PowerModel p)
	{
		return native * (decimal)Protection(p) / 50m;
	}

	private static Task AutoPlay(PlayerChoiceContext context, CardModel card, Creature target, AutoPlayType type, bool skipXCapture, bool skipCardPileVisuals, PowerModel power)
	{
		return ExpandedEffectPatches.AutoPlayWithBonus(context, card, target, type, skipXCapture, skipCardPileVisuals, Value(power, "extra:replays", 1) - 1);
	}

	private static async Task Channel<T>(PlayerChoiceContext context, Player player, PowerModel p) where T : OrbModel
	{
		for (int i = 0; i < Value(p, "extra:channels", 1); i++)
		{
			await OrbCmd.Channel<T>(context, player);
		}
	}
}
