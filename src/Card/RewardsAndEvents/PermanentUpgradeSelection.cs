using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace Tinker.Card;

internal static class PermanentUpgradeSelection
{
	private static readonly ConditionalWeakTable<NDeckUpgradeSelectScreen, NConfirmButton> Buttons = new ConditionalWeakTable<NDeckUpgradeSelectScreen, NConfirmButton>();

	private static CardSelectorPrefs Prefs(NDeckUpgradeSelectScreen screen)
	{
		return (CardSelectorPrefs)AccessTools.Field(typeof(NDeckUpgradeSelectScreen), "_prefs").GetValue(screen);
	}

	internal static bool Applies(CardSelectorPrefs prefs)
	{
		if (prefs.Prompt?.LocEntryKey == "UPGRADER_PERMANENT_UPGRADE" && prefs.MinSelect == 0)
		{
			return prefs.MaxSelect > 1;
		}
		return false;
	}

	internal static void Install(Harmony harmony)
	{
		harmony.Patch(AccessTools.DeclaredMethod(typeof(NDeckUpgradeSelectScreen), "_Ready"), null, new HarmonyMethod(typeof(PermanentUpgradeSelection), "Ready"));
		string[] array = new string[2] { "OnCardClicked", "CancelSelection" };
		foreach (string name in array)
		{
			harmony.Patch(AccessTools.DeclaredMethod(typeof(NDeckUpgradeSelectScreen), name), null, new HarmonyMethod(typeof(PermanentUpgradeSelection), "Refresh"));
		}
		harmony.Patch(AccessTools.DeclaredMethod(typeof(NDeckUpgradeSelectScreen), "ConfirmSelection"), new HarmonyMethod(typeof(PermanentUpgradeSelection), "Confirm"));
	}

	private static IEnumerable<CardModel> Selected(NDeckUpgradeSelectScreen screen)
	{
		return (IEnumerable<CardModel>)AccessTools.Field(typeof(NDeckUpgradeSelectScreen), "_selectedCards").GetValue(screen);
	}

	private static void Ready(NDeckUpgradeSelectScreen __instance)
	{
		if (Applies(Prefs(__instance)))
		{
			NConfirmButton nConfirmButton = ResourceLoader.Load<PackedScene>("res://scenes/ui/confirm_button.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<NConfirmButton>(PackedScene.GenEditState.Disabled);
			nConfirmButton.Name = "UpgraderConfirmSelection";
			nConfirmButton.ZIndex = 100;
			__instance.AddChild(nConfirmButton, forceReadableName: false, Node.InternalMode.Disabled);
			nConfirmButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
			{
				Confirm(__instance);
			}));
			Buttons.Add(__instance, nConfirmButton);
			Refresh(__instance);
		}
	}

	private static void Refresh(NDeckUpgradeSelectScreen __instance)
	{
		if (Buttons.TryGetValue(__instance, out var value))
		{
			string[] array = new string[2] { "_singlePreviewConfirmButton", "_multiPreviewConfirmButton" };
			foreach (string name in array)
			{
				((NConfirmButton)AccessTools.Field(typeof(NDeckUpgradeSelectScreen), name).GetValue(__instance)).Disable();
			}
			if (Selected(__instance).Any())
			{
				value.Enable();
			}
			else
			{
				value.Disable();
			}
		}
	}

	private static bool Confirm(NDeckUpgradeSelectScreen __instance)
	{
		CardSelectorPrefs prefs = Prefs(__instance);
		if (!Applies(prefs))
		{
			return true;
		}
		CardModel[] array = Selected(__instance).Distinct().ToArray();
		if (array.Length == 0 || array.Length > prefs.MaxSelect || array.Any((CardModel c) => !c.IsUpgradable))
		{
			return false;
		}
		if (((TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(typeof(NDeckUpgradeSelectScreen), "_completionSource").GetValue(__instance)).TrySetResult(array))
		{
			NOverlayStack.Instance.Remove(__instance);
		}
		return false;
	}
}
