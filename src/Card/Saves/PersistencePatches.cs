using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Tinker.Card;

public static class PersistencePatches
{
	[HarmonyPatch(typeof(CardModel), "UpgradeInternal")]
	public static class UpgradeCostPatch
	{
		public static void Prefix(CardModel __instance, out int __state)
		{
			__state = CardStateStore.Get(__instance)?.RawDelta("cost") ?? 0;
			if (__state != 0)
			{
				CardAttributes.ApplyRaw(__instance, "cost", -__state);
			}
		}

		public static void Postfix(CardModel __instance, int __state)
		{
			if (__state != 0)
			{
				CardMachineState cardMachineState = CardStateStore.Get(__instance);
				cardMachineState.ConstrainCostToBase(CardAttributes.CurrentValue(__instance, "cost"));
				CardAttributes.ApplyRaw(__instance, "cost", cardMachineState.RawDelta("cost"));
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), "ToSerializable")]
	public static class ToSerializablePatch
	{
		public static void Postfix(CardModel __instance, SerializableCard __result)
		{
			CardMachineState cardMachineState = CardStateStore.Get(__instance);
			if (cardMachineState != null && !cardMachineState.IsEmpty)
			{
				SavedProperties savedProperties;
				if (__result.Props == null)
				{
					savedProperties = (__result.Props = new SavedProperties());
				}
				savedProperties = __result.Props;
				if (savedProperties.strings == null)
				{
					savedProperties.strings = new List<SavedProperties.SavedProperty<string>>();
				}
				__result.Props.strings.RemoveAll((SavedProperties.SavedProperty<string> p) => p.name == PropName);
				__result.Props.strings.Add(new SavedProperties.SavedProperty<string>(PropName, cardMachineState.ToJson()));
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), "FromSerializable")]
	public static class FromSerializablePatch
	{
		public static void Postfix(SerializableCard save, CardModel __result)
		{
			string text = save?.Props?.strings?.FirstOrDefault((SavedProperties.SavedProperty<string> p) => p.name == PropName).value;
			if (string.IsNullOrEmpty(text) || __result == null)
			{
				return;
			}
			try
			{
				CardMachineState state = CardMachineState.FromJson(text);
				CardAttributes.ApplyWhole(__result, state);
				CardStateStore.Set(__result, state);
			}
			catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is OverflowException)
			{
				ModEntry.Error($"Unreadable machine state on {save.Id}; the card stays normal. {ex.Message}");
			}
		}
	}

	[HarmonyPatch(typeof(AbstractModel), "MutableClone")]
	public static class ClonePatch
	{
		public static void Postfix(AbstractModel __instance, AbstractModel __result)
		{
			if (__instance is CardModel card && __result is CardModel cardModel)
			{
				CardMachineState cardMachineState = CardStateStore.Get(card);
				if (cardMachineState != null)
				{
					CardStateStore.Set(cardModel, cardMachineState.Clone());
				}
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), "DowngradeInternal")]
	public static class DowngradePatch
	{
		public static void Postfix(CardModel __instance)
		{
			CardMachineState cardMachineState = CardStateStore.Get(__instance);
			if (cardMachineState != null)
			{
				CardAttributes.ApplyWhole(__instance, cardMachineState);
			}
		}
	}

	[HarmonyPatch(typeof(SavedProperties), "Serialize")]
	public static class NetSerializePatch
	{
		private static bool? _hasNetId;

		private static bool HasNetId()
		{
			if (!_hasNetId.HasValue)
			{
				try
				{
					GameCompat.NetIdForPropertyName(PropName);
					_hasNetId = true;
				}
				catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
				{
					_hasNetId = false;
					ModEntry.Error("Card state has no network id; it won't reach the other player. " + ex.Message);
				}
			}
			return _hasNetId.Value;
		}

		public static void Prefix(SavedProperties __instance, out List<SavedProperties.SavedProperty<string>> __state)
		{
			__state = null;
			if (!HasNetId() && __instance.strings != null && !__instance.strings.All<SavedProperties.SavedProperty<string>>((SavedProperties.SavedProperty<string> p) => p.name != PropName))
			{
				__state = __instance.strings;
				List<SavedProperties.SavedProperty<string>> list = __state.Where((SavedProperties.SavedProperty<string> p) => p.name != PropName).ToList();
				__instance.strings = ((list.Count > 0) ? list : null);
			}
		}

		public static Exception Finalizer(SavedProperties __instance, List<SavedProperties.SavedProperty<string>> __state, Exception __exception)
		{
			if (__state != null)
			{
				__instance.strings = __state;
			}
			return __exception;
		}
	}

	public const string PropName = "CardUpgraderState";
}
