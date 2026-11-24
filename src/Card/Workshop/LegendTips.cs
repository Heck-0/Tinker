using System;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Tinker.Card;

internal static class LegendTips
{
	private static readonly PropertyInfo TitleProperty = typeof(HoverTip).GetProperty("Title");

	public static IHoverTip Make(string title, string description, Texture2D icon = null, bool debuff = false)
	{
		object obj = new HoverTip(new LocString("card_selection", "TO_TINKER"), description, icon);
		TitleProperty?.SetValue(obj, title);
		return Tinker.GameCompat113.WithIdentity((HoverTip)obj, "Ruleta:" + title, debuff);
	}

	public static void Attach(Control owner, Func<IHoverTip> tip, HoverTipAlignment alignment = HoverTipAlignment.Left)
	{
		owner.MouseFilter = Control.MouseFilterEnum.Stop;
		owner.MouseEntered += delegate
		{
			try
			{
				NHoverTipSet.CreateAndShow(owner, tip(), alignment);
			}
			catch (Exception ex)
			{
				ModEntry.Warn("Could not show the help: " + ex.Message);
			}
		};
		owner.MouseExited += delegate
		{
			Hide(owner);
		};
	}

	public static void Hide(Control owner)
	{
		try
		{
			NHoverTipSet.Remove(owner);
		}
		catch (Exception)
		{
		}
	}

	public static string Capitalize(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return char.ToUpperInvariant(text[0]) + text.Substring(1);
		}
		return text;
	}
}
