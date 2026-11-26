using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace Tinker.Card;

// The right-hand panel's enchantments for sale.
internal sealed partial class WorkshopScreen
{
	/// <summary>Right panel: the enchantments for sale for the selected card (fixed price, one per card).</summary>
	private void RefreshExtras()
	{
		foreach (Node child in _propertyList.GetChildren())
		{
			if (child is Control owner)
			{
				LegendTips.Hide(owner);
			}
			child.QueueFree();
		}
		_propertyDetail.Text = "";
		_extrasLine.AddThemeColorOverride("font_color", Ink);
		_extrasHeading.Text = Lang.T("Enchantments");
		if (_card != null)
		{
			RefreshEnchantments();
		}
		else
		{
			_extrasLine.Text = Lang.T((_relic != null) ? "Only cards can be enchanted" : "Pick a card");
		}
	}

	private void RefreshEnchantments()
	{
		EnchantmentModel owned = _card.Enchantment;
		if (owned != null)
		{
			ShowOwned(Lang.T("Has " + ShopEnchantments.Name(owned, owned.Amount)), ShopEnchantments.Description(owned, owned.Amount));
			return;
		}
		if (!Settings.Active.EnchantmentsEnabled)
		{
			_extrasLine.Text = Lang.T("Enchantments are turned off in the settings");
			return;
		}
		List<(EnchantmentModel Model, int Amount)> forSale = ShopEnchantments.For(_card);
		if (forSale.Count == 0)
		{
			_extrasLine.Text = Lang.T("None fit this card");
			return;
		}
		_extrasLine.Text = Lang.T("One per card");
		CardModel card = _card;
		foreach ((EnchantmentModel model, int amount) in forSale)
		{
			int price = ShopEnchantments.Price(model);
			AddForSale(ShopEnchantments.Name(model, amount), ShopEnchantments.Description(model, amount), price, new WorkshopChoice
			{
				Card = card,
				Enchantment = model.GetType().Name,
				EnchantmentAmount = amount
			});
		}
	}

	private void ShowOwned(string line, string detail)
	{
		_extrasLine.Text = line;
		_extrasLine.AddThemeColorOverride("font_color", GoldInk);
		_propertyDetail.Text = detail;
	}

	/// <summary>One enchantment button: hover for what it does, click to buy (disabled if too expensive).</summary>
	private void AddForSale(string name, string description, int price, WorkshopChoice choice)
	{
		bool affordable = price <= _gold;
		Button button = new Button
		{
			Text = "✦ " + name,
			ClipText = true,
			Alignment = HorizontalAlignment.Left,
			CustomMinimumSize = new Vector2(370f, 30f),
			MouseFilter = Control.MouseFilterEnum.Pass,
			FocusMode = Control.FocusModeEnum.None,
			Disabled = !affordable
		};
		button.AddThemeFontOverride("font", ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres", null, ResourceLoader.CacheMode.Reuse));
		button.AddThemeFontSizeOverride("font_size", 16);
		button.AddThemeColorOverride("font_color", new Color("F3C14F"));
		button.AddThemeColorOverride("font_hover_color", Ink);
		button.AddThemeColorOverride("font_pressed_color", Ink);
		button.AddThemeColorOverride("font_disabled_color", new Color(FadedInk.R, FadedInk.G, FadedInk.B, 0.45f));
		StyleRow(button, selected: false);
		string priceLine = Lang.T($"{price:N0} gold");
		Label priceLabel = MakeLabel(16, affordable ? AffordableInk : UnaffordableInk, priceLine);
		priceLabel.HorizontalAlignment = HorizontalAlignment.Right;
		priceLabel.VerticalAlignment = VerticalAlignment.Center;
		priceLabel.Position = new Vector2(0f, 0f);
		priceLabel.Size = new Vector2(358f, 30f);
		priceLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		button.AddChild(priceLabel, forceReadableName: false, Node.InternalMode.Disabled);
		LegendTips.Attach(button, () => LegendTips.Make(name, LegendTips.Capitalize(description) + "\n\n" + ((price <= _gold) ? "[green]" : "[red]") + priceLine + ((price <= _gold) ? "[/green]" : "[/red]")));
		button.Pressed += delegate
		{
			if (_choice != null && !_choice.Task.IsCompleted && price <= _gold)
			{
				SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
				_choice.TrySetResult(choice);
			}
		};
		_propertyList.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
	}
}
