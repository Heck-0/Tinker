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

/// <summary>
/// The Workshop page. Cards in the grid, relics in a column beside it, both shown in the same Before / After
/// slots. On top the upgrade type, in the middle the value change, at the bottom the upgrade size and its price
/// (green if affordable, red if not); the player's gold sits in the right-hand panel. One page per purchase:
/// <see cref="WorkshopFlow"/> opens it, applies the choice and opens it again.
/// </summary>
internal sealed partial class WorkshopScreen
{
	internal static readonly Color Ink = new Color("F4E8C8");

	internal static readonly Color GoldInk = new Color("F2C24E");

	internal static readonly Color FadedInk = new Color(0.957f, 0.91f, 0.784f, 0.62f);

	internal static readonly Color Gold = new Color("E0A92E");

	internal static readonly Color AffordableInk = new Color("74DBA0");

	internal static readonly Color UnaffordableInk = new Color("FF8F8F");

	private static readonly Color Outline = new Color("0E1733");

	private static readonly Vector2 LeftSlotCenter = new Vector2(375f, 308f);

	private static readonly Vector2 RightSlotCenter = new Vector2(1100f, 308f);

	private static readonly Vector2 SlotSize = new Vector2(250f, 330f);

	private static readonly Color RowGold = new Color("efc851");

	/// <summary>Centre of the middle column (between the Before and After slots).</summary>
	private static readonly Vector2 Middle = new Vector2(735f, 305f);

	private readonly Player _player;

	private readonly int _gold;

	private readonly Control _root;

	private readonly Control _canvas;

	private readonly NCardGrid _grid;

	private readonly Label _leftHint;

	private readonly Label _rightHint;

	private readonly Label _centerBig;

	private readonly Control _controls;

	private readonly Label _attributeLabel;

	private readonly Label _amountLabel;

	private readonly Control _typeRow;

	private readonly Label _costLabel;

	private Label _extrasHeading;

	private Label _extrasLine;

	private VBoxContainer _propertyList;

	private Label _propertyDetail;

	private readonly NConfirmButton _confirm;

	private readonly NBackButton _back;

	private NCard _currentCard;

	private NCard _previewCard;

	private CardModel _card;

	private RelicModel _relic;

	private Tinker.Relic.RelicAttribute[] _relicOptions = Array.Empty<Tinker.Relic.RelicAttribute>();

	private readonly List<(RelicModel Relic, Button Row)> _relicRows = new List<(RelicModel, Button)>();

	private readonly RelicSlot _relicBefore;

	private readonly RelicSlot _relicAfter;

	private List<string> _attributes = new List<string>();

	private int _attributeIndex;

	/// <summary>The upgrade size being bought.</summary>
	private int _units = 1;

	/// <summary>Its price (0 = pick the cheapest step).</summary>
	private int _spend;

	private CardModel _preselect;

	private RelicModel _preselectRelic;

	private TaskCompletionSource<WorkshopChoice> _choice;

	private bool _closed;

	/// <summary>A relic shown in the Before / After slot: big icon, name and description.</summary>
	private sealed class RelicSlot
	{
		public Control Root;

		public TextureRect Icon;

		public Label Title;

		public RichTextLabel Body;
	}

	internal static WorkshopScreen Current { get; private set; }

	private string Attribute => (_attributes.Count != 0) ? _attributes[_attributeIndex] : null;

	/// <summary>Opens the Workshop page for <paramref name="player"/>, reselecting the last card or relic if it can still be upgraded.</summary>
	/// <param name="gold">The player's gold (shown in the panel and checked against prices).</param>
	public static WorkshopScreen Open(Player player, int gold, CardModel preselect = null, RelicModel preselectRelic = null)
	{
		return new WorkshopScreen(player, gold)
		{
			_preselect = preselect,
			_preselectRelic = preselectRelic
		};
	}

	private WorkshopScreen(Player player, int gold)
	{
		_player = player;
		_gold = gold;
		Current = this;
		Control globalUi = NRun.Instance?.GlobalUi;
		_root = new Control
		{
			Name = "TinkerWorkshopScreen",
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		globalUi?.AddChild(_root, forceReadableName: false, Node.InternalMode.Disabled);
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.Size = globalUi?.Size ?? _root.GetViewportRect().Size;
		ColorRect backdrop = new ColorRect
		{
			Color = StsColors.ninetyPercentBlack,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_root.AddChild(backdrop, forceReadableName: false, Node.InternalMode.Disabled);
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		backdrop.Size = _root.Size;
		_canvas = new Control
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Size = new Vector2(1920f, 1080f)
		};
		_root.AddChild(_canvas, forceReadableName: false, Node.InternalMode.Disabled);
		// Cards: the deck grid, narrowed to leave room for the relic column.
		_grid = Instantiate<NCardGrid>("res://scenes/cards/card_grid.tscn");
		_canvas.AddChild(_grid, forceReadableName: false, Node.InternalMode.Disabled);
		_grid.Position = new Vector2(0f, 600f);
		_grid.Size = new Vector2(1500f, 480f);
		_grid.ClipContents = true;
		_grid.Connect(NCardGrid.SignalName.HolderPressed, Callable.From(delegate(NCardHolder h)
		{
			SelectCard(h?.CardModel);
		}));
		BuildParchment();
		GoldOutline(_canvas, LeftSlotCenter, SlotSize, 14f, 3f);
		GoldOutline(_canvas, RightSlotCenter, SlotSize, 14f, 3f);
		PlaceCentered(MakeLabel(40, Ink, Lang.T("Workshop")), new Vector2(735f, 90f), new Vector2(660f, 56f));
		PlaceCentered(MakeLabel(27, Ink, Lines.Before), new Vector2(LeftSlotCenter.X, 122f), new Vector2(260f, 37f));
		PlaceCentered(MakeLabel(27, Ink, Lines.After), new Vector2(RightSlotCenter.X, 122f), new Vector2(260f, 37f));
		_leftHint = MakeLabel(26, Ink, Lang.T("Pick a card or a relic"));
		PlaceCentered(_leftHint, LeftSlotCenter, new Vector2(SlotSize.X - 30f, 120f));
		_rightHint = MakeLabel(40, Ink, "?");
		PlaceCentered(_rightHint, RightSlotCenter, new Vector2(SlotSize.X - 30f, 120f));

		// Top: which upgrade. Middle: the value change. Bottom: how big, and its price.
		_attributeLabel = MakeLabel(30, Ink, "");
		_typeRow = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2(Middle.X - 230f, 150f),
			Size = new Vector2(460f, 56f),
			Visible = false
		};
		_typeRow.AddChild(StepperRow(_attributeLabel, delegate
		{
			ChangeAttribute(-1);
		}, delegate
		{
			ChangeAttribute(1);
		}, 200f), forceReadableName: false, Node.InternalMode.Disabled);
		_canvas.AddChild(_typeRow, forceReadableName: false, Node.InternalMode.Disabled);
		_centerBig = MakeLabel(54, Ink, "–");
		PlaceCentered(_centerBig, Middle + new Vector2(0f, -12f), new Vector2(360f, 70f));
		_amountLabel = MakeLabel(30, GoldInk, "");
		_controls = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2(Middle.X - 345f, 404f),
			Size = new Vector2(690f, 56f),
			Visible = false
		};
		_controls.AddChild(StepperRow(_amountLabel, delegate
		{
			ChangeSize(-1);
		}, delegate
		{
			ChangeSize(1);
		}, 160f), forceReadableName: false, Node.InternalMode.Disabled);
		_canvas.AddChild(_controls, forceReadableName: false, Node.InternalMode.Disabled);
		_costLabel = MakeLabel(28, AffordableInk, "");
		PlaceCentered(_costLabel, new Vector2(Middle.X, 488f), new Vector2(320f, 40f));
		_costLabel.Visible = false;
		LegendTips.Attach(_costLabel, PriceTip);
		_relicBefore = BuildRelicSlot(LeftSlotCenter);
		_relicAfter = BuildRelicSlot(RightSlotCenter);
		BuildPanel();
		BuildRelicColumn();
		_confirm = Instantiate<NConfirmButton>("res://scenes/ui/confirm_button.tscn");
		PageButtonLayout.Bind(_confirm, new Vector2(1760f, 726f));
		_canvas.AddChild(_confirm, forceReadableName: false, Node.InternalMode.Disabled);
		_confirm.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			Confirm();
		}));
		_back = Instantiate<NBackButton>("res://scenes/ui/back_button.tscn");
		PageButtonLayout.Bind(_back, new Vector2(-40f, 726f));
		_canvas.AddChild(_back, forceReadableName: false, Node.InternalMode.Disabled);
		_back.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(delegate
		{
			Cancel();
		}));
		ResizeCanvas();
		_root.Resized += ResizeCanvas;
		_root.Modulate = StsColors.transparentWhite;
		_root.CreateTween().TweenProperty(_root, "modulate:a", 1f, 0.25);
		SfxCmd.Play("event:/sfx/ui/map/map_open");
	}

	public Task<WorkshopChoice> ChooseAsync()
	{
		_choice = new TaskCompletionSource<WorkshopChoice>();
		List<CardModel> cards = _player.Deck.Cards.Where(WorkshopOptions.CardUsable).ToList();
		_grid.SetCards(cards, PileType.Deck, new List<SortingOrders> { SortingOrders.Ascending });
		_back.Enable();
		if (cards.Count == 0 && _relicRows.Count == 0)
		{
			_leftHint.Text = Lang.T("Nothing can be upgraded yet.");
		}
		Refresh();
		try
		{
			if (_preselect != null && cards.Contains(_preselect))
			{
				SelectCard(_preselect);
			}
			else if (_preselectRelic != null && _relicRows.Any(r => r.Relic == _preselectRelic))
			{
				SelectRelic(_preselectRelic);
			}
		}
		catch (Exception ex)
		{
			ModEntry.Warn("Could not reselect the card or relic: " + ex.Message);
		}
		return _choice.Task;
	}

	public void Close()
	{
		if (Current == this)
		{
			Current = null;
		}
		if (_closed)
		{
			return;
		}
		_closed = true;
		_choice?.TrySetResult(null);
		if (!GodotObject.IsInstanceValid(_root))
		{
			return;
		}
		foreach (Node child in _propertyList.GetChildren())
		{
			if (child is Control owner)
			{
				LegendTips.Hide(owner);
			}
		}
		LegendTips.Hide(_costLabel);
		SfxCmd.Play("event:/sfx/ui/map/map_close");
		_root.MouseFilter = Control.MouseFilterEnum.Ignore;
		Tween tween = _root.CreateTween();
		tween.TweenProperty(_root, "modulate:a", 0f, 0.2);
		tween.TweenCallback(Callable.From(delegate
		{
			_root.QueueFreeSafely();
		}));
	}

	private void SelectCard(CardModel card)
	{
		if (card == null || _choice == null || _choice.Task.IsCompleted)
		{
			return;
		}
		if (_card != null)
		{
			_grid.UnhighlightCard(_card);
		}
		_card = card;
		_grid.HighlightCard(card);
		if (_relic != null)
		{
			_relic = null;
			_relicOptions = Array.Empty<Tinker.Relic.RelicAttribute>();
			foreach (var (_, row) in _relicRows)
			{
				StyleRow(row, selected: false);
			}
			_relicBefore.Root.Visible = false;
			_relicAfter.Root.Visible = false;
			if (_currentCard != null)
			{
				_currentCard.Visible = true;
			}
		}
		_attributes = CardAttributes.SupportedFor(card).ToList();
		_attributeIndex = 0;
		_units = 1;
		_spend = 0;
		SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
		if (_currentCard == null)
		{
			_currentCard = AddCard(card, LeftSlotCenter);
			_previewCard = AddCard(card, RightSlotCenter);
		}
		else
		{
			_currentCard.Model = card;
			_currentCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
		}
		_leftHint.Visible = false;
		_rightHint.Visible = false;
		Refresh();
	}

	/// <summary>Picks a relic, deselecting any card.</summary>
	private void SelectRelic(RelicModel relic)
	{
		if (relic == null || _choice == null || _choice.Task.IsCompleted)
		{
			return;
		}
		if (_card != null)
		{
			_grid.UnhighlightCard(_card);
			_card = null;
		}
		_relic = relic;
		_relicOptions = Tinker.Relic.Catalog.For(relic);
		_attributes = _relicOptions.Select((Tinker.Relic.RelicAttribute a) => a.Key).ToList();
		_attributeIndex = 0;
		_units = 1;
		_spend = 0;
		foreach (var (r, row) in _relicRows)
		{
			StyleRow(row, r == relic);
		}
		if (_currentCard != null)
		{
			_currentCard.Visible = false;
		}
		if (_previewCard != null)
		{
			_previewCard.Visible = false;
		}
		_leftHint.Visible = false;
		_rightHint.Visible = false;
		SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
		Refresh();
	}

	/// <summary>The bottom arrows: the next bigger or smaller upgrade that costs a different amount.</summary>
	private void ChangeSize(int delta)
	{
		if ((_card == null && _relic == null) || Attribute == null)
		{
			return;
		}
		int next = Math.Max(1, Math.Min(MaxUnits(), _units + delta));
		if (next != _units)
		{
			_units = next;
			SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
			Refresh();
		}
	}

	private void Refresh()
	{
		bool selected = (_card != null || _relic != null) && Attribute != null;
		_controls.Visible = selected;
		_typeRow.Visible = selected;
		_costLabel.Visible = selected;
		if (!selected)
		{
			_confirm.Disable();
			_centerBig.Text = "–";
			RefreshExtras();
			return;
		}
		if (_relic != null)
		{
			RefreshRelic();
			return;
		}
		ResolveSpend();
		int before = CardAttributes.CurrentValue(_card, Attribute);
		int after = before + _units * AttributeDirection.SignOf(Attribute);
		_attributeLabel.Text = FixedEffectPatches.AttributeName(_card, Attribute);
		_attributeLabel.AddThemeColorOverride("font_color", Ink);
		string change = ExpandedEffectPatches.DisplayValue(_card, Attribute, before) + " → " + ExpandedEffectPatches.DisplayValue(_card, Attribute, after);
		ShowSpend(change, AttributeDirection.SignOf(Attribute));
		UpdatePreview();
		RefreshExtras();
	}

	private void RefreshRelic()
	{
		Tinker.Relic.RelicAttribute attribute = _relicOptions[_attributeIndex];
		ResolveSpend();
		decimal before = attribute.Value(_relic);
		decimal after = attribute.After(before, _units);
		_attributeLabel.Text = attribute.Label;
		_attributeLabel.AddThemeColorOverride("font_color", Ink);
		ShowSpend($"{before} → {after}", attribute.Direction);
		ShowRelic(_relicBefore, _relic, null);
		ShowRelic(_relicAfter, UpgradedPreview(_relic, attribute, after), null);
		RefreshExtras();
	}

	/// <summary>Middle: the value change. Bottom: the upgrade size (+1, +3…) and its price, green if affordable, red if not.</summary>
	private void ShowSpend(string change, int sign)
	{
		bool affordable = _spend <= _gold;
		_centerBig.Text = change;
		_centerBig.AddThemeColorOverride("font_color", Ink);
		_amountLabel.Text = ((sign < 0) ? "−" : "+") + _units;
		_costLabel.Text = (_spend == int.MaxValue) ? Lang.T("Too expensive") : Lang.T($"{_spend:N0} gold");
		_costLabel.AddThemeColorOverride("font_color", affordable ? AffordableInk : UnaffordableInk);
		if (affordable)
		{
			_confirm.Enable();
		}
		else
		{
			_confirm.Disable();
		}
	}

	private IHoverTip PriceTip()
	{
		return LegendTips.Make(Lang.T("Price"), Lang.T(
			"What this upgrade costs: green if you have enough gold, red if not. Bigger upgrades, and upgrades on cards or relics that are already upgraded, cost more."));
	}

	private void Confirm()
	{
		if (_relic != null && _relicOptions.Length > 0)
		{
			SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
			_choice?.TrySetResult(new WorkshopChoice
			{
				Relic = _relic,
				RelicAttributeIndex = Array.IndexOf(Tinker.Relic.Catalog.For(_relic), _relicOptions[_attributeIndex]),
				RelicUnits = _units
			});
		}
		else if (_card != null && Attribute != null)
		{
			SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
			_choice?.TrySetResult(new WorkshopChoice
			{
				Card = _card,
				Request = Request(_units)
			});
		}
	}

	private void Cancel()
	{
		SfxCmd.Play("event:/sfx/ui/clicks/ui_back");
		_choice?.TrySetResult(null);
	}

	/// <summary>
	/// A copy of the relic with the upgraded value, so the After slot shows the new description. Anything that
	/// can't be copied shows the relic as it is.
	/// </summary>
	private static RelicModel UpgradedPreview(RelicModel relic, Tinker.Relic.RelicAttribute attribute, decimal value)
	{
		if (!relic.DynamicVars.ContainsKey(attribute.Key))
		{
			return relic;
		}
		try
		{
			RelicModel copy = (RelicModel)relic.MutableClone();
			copy.DynamicVars[attribute.Key].BaseValue = value;
			return copy;
		}
		catch (Exception ex)
		{
			ModEntry.Warn("Could not preview the upgraded relic: " + ex.Message);
			return relic;
		}
	}

	/// <summary>The biggest size the bottom selector offers: what the value allows, at most 999. Gold doesn't limit it; unaffordable sizes show their price in red.</summary>
	private int MaxUnits()
	{
		return Math.Max(1, (_relic != null) ? Tinker.Relic.Catalog.MaxUnits(_relic, _relicOptions[_attributeIndex]) : CardUnitCap());
	}

	/// <summary>The gold price of the selected upgrade at this size.</summary>
	private int Price(int units)
	{
		return (_relic != null) ? WorkshopOptions.RelicPrice(_player, _relic, _relicOptions[_attributeIndex], units) : WorkshopOptions.CardPrice(_card, Attribute, units);
	}

	/// <summary>The most units the card's value allows (before any price limit).</summary>
	private int CardUnitCap()
	{
		int current = CardAttributes.CurrentValue(_card, Attribute);
		return Math.Min(999, (AttributeDirection.SignOf(Attribute) < 0) ? (current - CardAttributes.Minimum(_card, Attribute)) : (CardAttributes.Maximum(_card, Attribute) - current));
	}

	/// <summary>Keeps the size within what the value allows and prices it.</summary>
	private void ResolveSpend()
	{
		_units = Math.Max(1, Math.Min(MaxUnits(), _units));
		_spend = Price(_units);
	}

	private void UpdatePreview()
	{
		CardModel cardModel = (CardModel)_card.MutableClone();
		CardMachineState cardMachineState = CardStateStore.Get(cardModel)?.Clone() ?? new CardMachineState();
		int rawChange = cardMachineState.ApplySuccess(Attribute, _units);
		CardStateStore.Set(cardModel, cardMachineState);
		if (AttributeKeys.IsVar(Attribute) && cardModel.DynamicVars.TryGetValue(AttributeKeys.VarName(Attribute), out DynamicVar value))
		{
			value.UpgradeValueBy(SteppedValues.TryGet(cardModel, AttributeKeys.VarName(Attribute), out SteppedValues.Stepped stepped) ? rawChange * stepped.Step : rawChange);
		}
		else if (Attribute == "cost")
		{
			cardModel.EnergyCost.UpgradeBy(rawChange);
		}
		_previewCard.Model = cardModel;
		_previewCard.Visible = true;
		_previewCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Upgrade);
	}

	private UpgradeRequest Request(int units)
	{
		return CardCost.BuildRequest(_card, Attribute, units);
	}

	private void ChangeAttribute(int delta)
	{
		if (_attributes.Count >= 2)
		{
			_attributeIndex = (_attributeIndex + delta + _attributes.Count) % _attributes.Count;
			_units = 1;
			_spend = 0;
			SfxCmd.Play("event:/sfx/ui/clicks/ui_click");
			Refresh();
		}
	}
}
