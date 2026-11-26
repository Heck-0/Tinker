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

// Building the page: the card grid, relic column, right-hand panel, stepper rows and labels.
internal sealed partial class WorkshopScreen
{
	/// <summary>The right-hand panel: the player's gold like the top bar's counter, then the enchantments for sale.</summary>
	private void BuildPanel()
	{
		ColorRect panel = BluePanel(new Vector2(1452f, 64f), new Vector2(462f, 536f), 16f);
		_canvas.AddChild(panel, forceReadableName: false, Node.InternalMode.Disabled);
		GoldOutline(panel, new Vector2(231f, 268f), new Vector2(434f, 510f), 12f, 2.5f);
		HBoxContainer gold = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2(0f, 36f),
			Size = new Vector2(462f, 90f)
		};
		gold.AddThemeConstantOverride("separation", 12);
		gold.AddChild(new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>("res://images/packed/sprite_fonts/gold_icon.png", null, ResourceLoader.CacheMode.Reuse),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(80f, 80f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		}, forceReadableName: false, Node.InternalMode.Disabled);
		Label amount = MakeLabel(60, GoldInk, _gold.ToString());
		amount.VerticalAlignment = VerticalAlignment.Center;
		gold.AddChild(amount, forceReadableName: false, Node.InternalMode.Disabled);
		panel.AddChild(gold, forceReadableName: false, Node.InternalMode.Disabled);
		panel.AddChild(new ColorRect
		{
			Color = new Color(Gold.R, Gold.G, Gold.B, 0.55f),
			Position = new Vector2(52f, 140f),
			Size = new Vector2(358f, 2f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_extrasHeading = MakeLabel(28, GoldInk, "");
		_extrasHeading.HorizontalAlignment = HorizontalAlignment.Center;
		_extrasHeading.Position = new Vector2(0f, 148f);
		_extrasHeading.Size = new Vector2(462f, 40f);
		panel.AddChild(_extrasHeading, forceReadableName: false, Node.InternalMode.Disabled);
		_extrasLine = MakeLabel(18, Ink, "");
		_extrasLine.HorizontalAlignment = HorizontalAlignment.Center;
		_extrasLine.Position = new Vector2(30f, 186f);
		_extrasLine.Size = new Vector2(402f, 28f);
		panel.AddChild(_extrasLine, forceReadableName: false, Node.InternalMode.Disabled);
		// One row per enchantment (name, price on the right); scrolls when a card fits more than nine.
		ScrollContainer scroll = new ScrollContainer
		{
			Position = new Vector2(40f, 218f),
			Size = new Vector2(382f, 296f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		panel.AddChild(scroll, forceReadableName: false, Node.InternalMode.Disabled);
		_propertyList = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(370f, 0f),
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		_propertyList.AddThemeConstantOverride("separation", 3);
		scroll.AddChild(_propertyList, forceReadableName: false, Node.InternalMode.Disabled);
		_propertyDetail = MakeLabel(15, Ink, "");
		_propertyDetail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_propertyDetail.HorizontalAlignment = HorizontalAlignment.Center;
		_propertyDetail.Position = new Vector2(46f, 222f);
		_propertyDetail.Size = new Vector2(370f, 280f);
		panel.AddChild(_propertyDetail, forceReadableName: false, Node.InternalMode.Disabled);
	}

	/// <summary>The relic column beside the card grid: every relic that can be upgraded.</summary>
	private void BuildRelicColumn()
	{
		ColorRect panel = BluePanel(new Vector2(1505f, 612f), new Vector2(240f, 456f), 12f);
		_canvas.AddChild(panel, forceReadableName: false, Node.InternalMode.Disabled);
		GoldOutline(panel, new Vector2(120f, 228f), new Vector2(226f, 442f), 10f, 2f);
		Label heading = MakeLabel(22, Ink, Lang.T("Relics"));
		heading.HorizontalAlignment = HorizontalAlignment.Center;
		heading.Position = new Vector2(0f, 12f);
		heading.Size = new Vector2(240f, 30f);
		panel.AddChild(heading, forceReadableName: false, Node.InternalMode.Disabled);
		ScrollContainer scroll = new ScrollContainer
		{
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			Position = new Vector2(12f, 48f),
			Size = new Vector2(216f, 396f)
		};
		VBoxContainer list = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(204f, 0f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		list.AddThemeConstantOverride("separation", 4);
		scroll.AddChild(list, forceReadableName: false, Node.InternalMode.Disabled);
		panel.AddChild(scroll, forceReadableName: false, Node.InternalMode.Disabled);
		Font font = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres", null, ResourceLoader.CacheMode.Reuse);
		foreach (RelicModel relic in _player.Relics.Where(WorkshopOptions.RelicUsable))
		{
			Button row = new Button
			{
				Text = relic.Title.GetFormattedText(),
				Icon = relic.Icon,
				ExpandIcon = true,
				ClipText = true,
				Alignment = HorizontalAlignment.Left,
				CustomMinimumSize = new Vector2(204f, 46f),
				FocusMode = Control.FocusModeEnum.None
			};
			row.AddThemeConstantOverride("icon_max_width", 36);
			row.AddThemeFontOverride("font", font);
			row.AddThemeFontSizeOverride("font_size", 17);
			row.AddThemeColorOverride("font_color", Ink);
			row.AddThemeColorOverride("font_hover_color", GoldInk);
			row.AddThemeColorOverride("font_pressed_color", GoldInk);
			StyleRow(row, selected: false);
			RelicModel target = relic;
			row.Pressed += delegate
			{
				SelectRelic(target);
			};
			list.AddChild(row, forceReadableName: false, Node.InternalMode.Disabled);
			_relicRows.Add((relic, row));
		}
		if (_relicRows.Count == 0)
		{
			Label none = MakeLabel(16, FadedInk, Lang.T("None of your relics can be upgraded."));
			none.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			none.HorizontalAlignment = HorizontalAlignment.Center;
			none.CustomMinimumSize = new Vector2(204f, 80f);
			list.AddChild(none, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private RelicSlot BuildRelicSlot(Vector2 center)
	{
		Control root = new Control
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = center - SlotSize / 2f,
			Size = SlotSize,
			Visible = false
		};
		_canvas.AddChild(root, forceReadableName: false, Node.InternalMode.Disabled);
		TextureRect icon = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2((SlotSize.X - 110f) / 2f, 16f),
			Size = new Vector2(110f, 110f)
		};
		root.AddChild(icon, forceReadableName: false, Node.InternalMode.Disabled);
		Label title = MakeLabel(22, Ink, "");
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.VerticalAlignment = VerticalAlignment.Center;
		title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		title.Position = new Vector2(10f, 128f);
		title.Size = new Vector2(SlotSize.X - 20f, 54f);
		root.AddChild(title, forceReadableName: false, Node.InternalMode.Disabled);
		RichTextLabel body = new RichTextLabel
		{
			BbcodeEnabled = true,
			ScrollActive = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2(14f, 186f),
			Size = new Vector2(SlotSize.X - 28f, SlotSize.Y - 196f)
		};
		Font font = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres", null, ResourceLoader.CacheMode.Reuse);
		foreach (string name in new string[4] { "normal_font", "bold_font", "italics_font", "bold_italics_font" })
		{
			body.AddThemeFontOverride(name, font);
			body.AddThemeFontSizeOverride(name + "_size", 17);
		}
		body.AddThemeColorOverride("default_color", Ink);
		body.AddThemeColorOverride("font_outline_color", Outline);
		body.AddThemeConstantOverride("outline_size", 3);
		root.AddChild(body, forceReadableName: false, Node.InternalMode.Disabled);
		return new RelicSlot { Root = root, Icon = icon, Title = title, Body = body };
	}

	private static void ShowRelic(RelicSlot slot, RelicModel relic, string extra)
	{
		slot.Icon.Texture = relic.BigIcon;
		slot.Title.Text = relic.Title.GetFormattedText();
		string text = Markup(relic.DynamicDescription.GetFormattedText());
		if (!string.IsNullOrEmpty(extra))
		{
			text = text + "\n\n[color=#F2C24E]" + extra + "[/color]";
		}
		slot.Body.Text = "[center]" + text + "[/center]";
		slot.Root.Visible = true;
	}

	private NCard AddCard(CardModel model, Vector2 center)
	{
		NCard nCard = NCard.Create(model);
		_canvas.AddChild(nCard, forceReadableName: false, Node.InternalMode.Disabled);
		nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
		nCard.Position = center;
		nCard.Scale = new Vector2(0.7f, 0.7f);
		return nCard;
	}

	private void PlaceCentered(Label label, Vector2 center, Vector2 size)
	{
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.Size = size;
		label.Position = center - size / 2f;
		_canvas.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private Control StepperRow(Label label, Action left, Action right, float labelWidth)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.Alignment = BoxContainer.AlignmentMode.Center;
		hBoxContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
		hBoxContainer.AddThemeConstantOverride("separation", 14);
		hBoxContainer.AddChild(ArrowButton("res://images/atlases/ui_atlas.sprites/settings_tiny_left_arrow.tres", left), forceReadableName: false, Node.InternalMode.Disabled);
		label.CustomMinimumSize = new Vector2(labelWidth, 0f);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		hBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(ArrowButton("res://images/atlases/ui_atlas.sprites/settings_tiny_right_arrow.tres", right), forceReadableName: false, Node.InternalMode.Disabled);
		return hBoxContainer;
	}

	private TextureButton ArrowButton(string texture, Action onPressed)
	{
		TextureButton button = new TextureButton
		{
			TextureNormal = ResourceLoader.Load<Texture2D>(texture, null, ResourceLoader.CacheMode.Reuse),
			StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
			IgnoreTextureSize = true,
			CustomMinimumSize = new Vector2(44f, 44f),
			SelfModulate = Ink
		};
		button.MouseEntered += delegate
		{
			button.SelfModulate = GoldInk;
		};
		button.MouseExited += delegate
		{
			button.SelfModulate = Ink;
		};
		button.Pressed += onPressed;
		return button;
	}

	/// <summary>Relic list rows: transparent, with a gold frame when selected and a faint one on hover.</summary>
	private static void StyleRow(Button row, bool selected)
	{
		StyleBoxFlat normal = (selected ? RowBox(new Color(RowGold, 0.14f), new Color(RowGold, 0.8f), 2) : RowBox(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0f), 0));
		StyleBoxFlat hover = (selected ? normal : RowBox(new Color(1f, 1f, 1f, 0.06f), new Color(RowGold, 0.3f), 1));
		row.AddThemeStyleboxOverride("normal", normal);
		row.AddThemeStyleboxOverride("hover", hover);
		row.AddThemeStyleboxOverride("pressed", normal);
		row.AddThemeStyleboxOverride("disabled", normal);
		row.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
	}

	private static StyleBoxFlat RowBox(Color fill, Color border, int width)
	{
		return new StyleBoxFlat
		{
			BgColor = fill,
			BorderColor = border,
			BorderWidthLeft = width,
			BorderWidthRight = width,
			BorderWidthTop = width,
			BorderWidthBottom = width,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
	}

	/// <summary>The game's description colour tags as BBCode colours, for a RichTextLabel.</summary>
	private static string Markup(string value)
	{
		foreach ((string tag, string hex) in new (string, string)[4]
		{
			("green", "80d4a0"),
			("red", "ff9494"),
			("gold", "eacb83"),
			("blue", "8ecbff")
		})
		{
			value = value.Replace("[" + tag + "]", "[color=#" + hex + "]").Replace("[/" + tag + "]", "[/color]");
		}
		return value;
	}

	internal static Label MakeLabel(int fontSize, Color color, string text)
	{
		Label label = new Label();
		label.Text = text;
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.AddThemeFontOverride("font", ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres", null, ResourceLoader.CacheMode.Reuse));
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_outline_color", Outline);
		label.AddThemeConstantOverride("outline_size", Math.Max(3, fontSize / 7));
		return label;
	}

	private static T Instantiate<T>(string scene) where T : Node
	{
		return ResourceLoader.Load<PackedScene>(scene, null, ResourceLoader.CacheMode.Reuse).Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private void ResizeCanvas()
	{
		PageLayout scrollLayout = new PageLayout(_root.Size.X, _root.Size.Y);
		float scale = scrollLayout.Scale;
		_canvas.Scale = new Vector2(scale, scale);
		_canvas.Position = new Vector2(scrollLayout.X, scrollLayout.Y);
		PageButtonLayout.Refresh(_confirm);
		PageButtonLayout.Refresh(_back);
	}

	private void BuildParchment()
	{
		ColorRect colorRect = BluePanel(new Vector2(80f, 31f), new Vector2(1330f, 569f), 6f, 30f);
		colorRect.MouseFilter = Control.MouseFilterEnum.Stop;
		_canvas.AddChild(colorRect, forceReadableName: false, Node.InternalMode.Disabled);
		GoldOutline(_canvas, new Vector2(745f, 319f), new Vector2(1270f, 458f), 18f, 3f, 105f);
		Roller(new Vector2(742f, 31f), 1404f, 3.7f);
		Roller(new Vector2(748f, 600f), 1412f, 11.2f);
	}
}
