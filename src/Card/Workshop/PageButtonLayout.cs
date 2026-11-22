using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace Tinker.Card;

internal static class PageButtonLayout
{
	[HarmonyPatch(typeof(NConfirmButton), "OnWindowChange")]
	private static class ConfirmPositionPatch
	{
		private static bool Prefix(NConfirmButton __instance)
		{
			if (!__instance.HasMeta("card_upgrader_local_position"))
			{
				return true;
			}
			Refresh(__instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(NBackButton), "OnWindowChange")]
	private static class BackPositionPatch
	{
		private static bool Prefix(NBackButton __instance)
		{
			if (!__instance.HasMeta("card_upgrader_local_position"))
			{
				return true;
			}
			Refresh(__instance);
			return false;
		}
	}

	private const string PositionKey = "card_upgrader_local_position";

	public static void Bind(NButton button, Vector2 position)
	{
		button.SetMeta("card_upgrader_local_position", position);
		button.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
	}

	public static void Refresh(NButton button)
	{
		if (button != null && GodotObject.IsInstanceValid(button) && button.HasMeta("card_upgrader_local_position") && button.IsInsideTree())
		{
			Vector2 vector = button.GetMeta("card_upgrader_local_position").AsVector2();
			bool flag = button is NBackButton;
			Vector2 vector2 = vector + new Vector2(flag ? (-180) : 180, 0f);
			Transform2D globalTransform = ((Control)button.GetParent()).GetGlobalTransform();
			Vector2 vector3 = (flag ? (globalTransform * vector) : vector);
			Vector2 vector4 = (flag ? (globalTransform * vector2) : vector2);
			Type type = button.GetType();
			(AccessTools.Field(type, "_moveTween")?.GetValue(button) as Tween)?.Kill();
			AccessTools.Field(type, "_showPos").SetValue(button, vector3);
			AccessTools.Field(type, "_hidePos").SetValue(button, vector4);
			button.Position = (button.IsEnabled ? vector : vector2);
		}
	}
}
