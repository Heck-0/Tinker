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

/// <summary>What the player picked in the Workshop: a card upgrade, or a relic upgrade.</summary>
internal sealed class WorkshopChoice
{
	public CardModel Card { get; init; }

	public UpgradeRequest Request { get; init; }

	public RelicModel Relic { get; init; }

	/// <summary>Relic purchase: the attribute's index in Catalog.For(Relic).</summary>
	public int RelicAttributeIndex { get; init; }

	public int RelicUnits { get; init; }

	/// <summary>Trait bought for <see cref="Card"/> (its id), instead of an upgrade.</summary>
	public string Trait { get; init; }

	/// <summary>Boon bought for <see cref="Relic"/> (its id), instead of an upgrade.</summary>
	public string Boon { get; init; }
}

/// <summary>
/// The Workshop page. Cards in the grid, relics in a column beside it, both shown in the same Before / After
/// slots. On top the upgrade type, in the middle the value change, at the bottom the upgrade size and its price
/// (green if affordable, red if not); the player's gold sits in the right-hand panel. One page per purchase:
/// <see cref="WorkshopFlow"/> opens it, applies the choice and opens it again.
/// </summary>
internal sealed class WorkshopScreen
{
	internal static readonly Color Ink = new Color("F4E8C8");
	internal static readonly Color NegativeInk = new Color("D9B3FF");
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
	private const string PanelShader = "\r\nshader_type canvas_item;\r\nuniform sampler2D paper : filter_linear, repeat_enable;\r\nuniform vec2 size = vec2(400.0, 400.0);\r\nuniform vec2 tex_px = vec2(200.0, 300.0);\r\nuniform float mean_lum = 0.75;\r\nuniform float radius = 16.0;\r\nuniform float roller_shadow = 0.0;\r\nuniform vec3 deep = vec3(0.090, 0.150, 0.340);\r\nuniform vec3 royal = vec3(0.215, 0.345, 0.660);\r\nuniform vec3 ink = vec3(0.055, 0.090, 0.200);\r\n\r\nfloat hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }\r\nfloat vnoise(vec2 p) {\r\n    vec2 i = floor(p);\r\n    vec2 f = fract(p);\r\n    vec2 u = f * f * (3.0 - 2.0 * f);\r\n    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);\r\n}\r\nfloat fbm(vec2 p) {\r\n    float v = 0.0;\r\n    float a = 0.5;\r\n    for (int i = 0; i < 5; i++) { v += a * vnoise(p); p *= 2.03; a *= 0.5; }\r\n    return v;\r\n}\r\n\r\nfloat rbox(vec2 p, vec2 b, float r) {\r\n    vec2 q = abs(p) - b + r;\r\n    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;\r\n}\r\n\r\nvoid fragment() {\r\n    vec2 p = UV * size;\r\n    float d = rbox(p - size * 0.5, size * 0.5, radius);\r\n    // Grano del papel del juego, repetido en espejo para que no haya costuras.\r\n    vec2 t = p / tex_px;\r\n    vec2 m = 1.0 - abs(mod(t, 2.0) - 1.0);\r\n    float l = dot(texture(paper, m).rgb, vec3(0.299, 0.587, 0.114));\r\n    vec3 col = royal * (1.0 + (l - mean_lum) * 3.2);\r\n    // Papel pintado a mano: manchas grandes, grano medio y fibras finas horizontales.\r\n    float mott = fbm(p / 170.0) - 0.5;\r\n    float mid = fbm(p / 38.0 + 7.3) - 0.5;\r\n    float fib = vnoise(vec2(p.x / 9.0, p.y / 1.7)) - 0.5;\r\n    col *= 1.0 + mott * 0.30 + mid * 0.14 + fib * 0.06;\r\n    // Viñeta suave hacia los bordes y sombra bajo los rodillos.\r\n    col = mix(deep, col, 0.45 + 0.55 * smoothstep(0.0, 70.0, -d));\r\n    if (roller_shadow > 0.0) {\r\n        float ys = min(p.y, size.y - p.y);\r\n        col *= mix(0.62, 1.0, smoothstep(roller_shadow * 0.8, roller_shadow * 1.9, ys));\r\n    }\r\n    col = mix(ink, col, smoothstep(3.0, 4.5, -d));\r\n    COLOR = vec4(col, 1.0 - smoothstep(-0.5, 0.5, d));\r\n}";
	private const string RollerShader = "\r\nshader_type canvas_item;\r\nuniform vec2 size = vec2(1420.0, 60.0);\r\nuniform vec3 blue = vec3(0.190, 0.310, 0.600);\r\nuniform vec3 ink = vec3(0.055, 0.090, 0.200);\r\nuniform float seed = 0.0;\r\n\r\nfloat hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }\r\nfloat vnoise(vec2 p) {\r\n    vec2 i = floor(p);\r\n    vec2 f = fract(p);\r\n    vec2 u = f * f * (3.0 - 2.0 * f);\r\n    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);\r\n}\r\nfloat fbm(vec2 p) {\r\n    float v = 0.0;\r\n    float a = 0.5;\r\n    for (int i = 0; i < 4; i++) { v += a * vnoise(p); p *= 2.03; a *= 0.5; }\r\n    return v;\r\n}\r\n\r\nvoid fragment() {\r\n    vec2 p = UV * size;\r\n    float s = p.x;\r\n    float R = size.y * 0.5 - 7.0;\r\n    // Grosor y eje que ondulan un poco a lo largo: enrollado a mano.\r\n    float r = R * (0.78 + 0.40 * fbm(vec2(s / 120.0 + seed, 1.3)));\r\n    // Las puntas, peor apretadas, abultan un poco más.\r\n    r *= 1.0 + 0.10 * (1.0 - smoothstep(0.0, 90.0, min(s, size.x - s)));\r\n    float c = size.y * 0.5 + 7.0 * (vnoise(vec2(s / 240.0 + seed, 7.1)) - 0.5);\r\n    float y = p.y - c;\r\n    // Se mira un poco desde la izquierda: la cara izquierda enseña la espiral de las vueltas y la\r\n    // derecha, que queda de espaldas, solo el canto del papel, algo desigual al enrollar a mano.\r\n    float x0 = 26.0;\r\n    float x1 = size.x - 12.0 - 5.0 * vnoise(vec2(seed + 9.0, p.y / 7.0)) - 2.0 * vnoise(vec2(seed + 13.0, p.y / 2.5));\r\n    float body = max(abs(y) - r, max(x0 - s, s - x1));\r\n    float rx = r * 0.40;\r\n    vec2 e = vec2((s - x0) / rx, y / r);\r\n    float face = (length(e) - 1.0) * rx;\r\n    float d = min(body, face);\r\n    if (d > 1.0) discard;\r\n\r\n    float grain = 1.0 + (fbm(p / 26.0 + seed) - 0.5) * 0.26 + (vnoise(vec2(p.x / 8.0, p.y / 1.5)) - 0.5) * 0.07;\r\n    float v = clamp(y / r, -1.0, 1.0);\r\n    // Luz suave desde arriba, sin brillo especular: papel mate.\r\n    float shade = 0.60 + 0.32 * sqrt(1.0 - v * v) - 0.13 * v;\r\n    vec3 col = blue * shade * grain;\r\n    // El canto se ve un poco más oscuro, como papel apilado.\r\n    col *= 1.0 - 0.18 * (1.0 - smoothstep(0.0, 7.0, x1 - s));\r\n    float edge = smoothstep(-2.6, -1.5, d);\r\n    if (face < 0.0) {\r\n        float q = length(e);\r\n        float a = atan(e.y, e.x) / 6.2831853;\r\n        float sp = fract(q * 3.6 - a + seed);\r\n        float line = 1.0 - smoothstep(0.02, 0.16, min(sp, 1.0 - sp));\r\n        col = blue * (1.05 + 0.15 * q) * grain;\r\n        col = mix(col, ink, line * 0.85 * smoothstep(0.12, 0.30, q));\r\n        col = mix(col, ink, 1.0 - smoothstep(0.08, 0.22, q));\r\n        edge = max(edge, smoothstep(-2.4, -1.3, face));\r\n    }\r\n    col = mix(col, ink, edge);\r\n    COLOR = vec4(col, 1.0 - smoothstep(0.0, 1.0, d));\r\n}";
	private static float _paperMean = 0.75f;

	private static ImageTexture _paperGrain;

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

	private GridContainer _propertyList;

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
		PlaceCentered(MakeLabel(40, Ink, Lang.T("Taller", "Workshop")), new Vector2(735f, 90f), new Vector2(660f, 56f));
		PlaceCentered(MakeLabel(27, Ink, Lines.Before), new Vector2(LeftSlotCenter.X, 122f), new Vector2(260f, 37f));
		PlaceCentered(MakeLabel(27, Ink, Lines.After), new Vector2(RightSlotCenter.X, 122f), new Vector2(260f, 37f));
		_leftHint = MakeLabel(26, Ink, Lang.T("Elige una carta o una reliquia", "Pick a card or a relic"));
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

	/// <summary>The right-hand panel: the player's gold like the top bar's counter, then the traits / boons for sale.</summary>
	private void BuildPanel()
	{
		ColorRect panel = BluePanel(new Vector2(1452f, 64f), new Vector2(462f, 536f), 16f);
		_canvas.AddChild(panel, forceReadableName: false, Node.InternalMode.Disabled);
		GoldOutline(panel, new Vector2(231f, 268f), new Vector2(434f, 510f), 12f, 2.5f);
		HBoxContainer gold = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2(0f, 60f),
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
			Position = new Vector2(52f, 318f),
			Size = new Vector2(358f, 2f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_extrasHeading = MakeLabel(28, GoldInk, "");
		_extrasHeading.HorizontalAlignment = HorizontalAlignment.Center;
		_extrasHeading.Position = new Vector2(0f, 326f);
		_extrasHeading.Size = new Vector2(462f, 40f);
		panel.AddChild(_extrasHeading, forceReadableName: false, Node.InternalMode.Disabled);
		_extrasLine = MakeLabel(18, Ink, "");
		_extrasLine.HorizontalAlignment = HorizontalAlignment.Center;
		_extrasLine.Position = new Vector2(30f, 364f);
		_extrasLine.Size = new Vector2(402f, 28f);
		panel.AddChild(_extrasLine, forceReadableName: false, Node.InternalMode.Disabled);
		_propertyList = new GridContainer
		{
			Columns = 2,
			Position = new Vector2(46f, 396f),
			Size = new Vector2(370f, 116f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_propertyList.AddThemeConstantOverride("h_separation", 10);
		_propertyList.AddThemeConstantOverride("v_separation", 4);
		panel.AddChild(_propertyList, forceReadableName: false, Node.InternalMode.Disabled);
		_propertyDetail = MakeLabel(15, Ink, "");
		_propertyDetail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_propertyDetail.HorizontalAlignment = HorizontalAlignment.Center;
		_propertyDetail.Position = new Vector2(46f, 396f);
		_propertyDetail.Size = new Vector2(370f, 100f);
		panel.AddChild(_propertyDetail, forceReadableName: false, Node.InternalMode.Disabled);
	}

	
	/// <summary>The relic column beside the card grid: every relic that can be upgraded.</summary>
	private void BuildRelicColumn()
	{
		ColorRect panel = BluePanel(new Vector2(1505f, 612f), new Vector2(240f, 456f), 12f);
		_canvas.AddChild(panel, forceReadableName: false, Node.InternalMode.Disabled);
		GoldOutline(panel, new Vector2(120f, 228f), new Vector2(226f, 442f), 10f, 2f);
		Label heading = MakeLabel(22, Ink, Lang.T("Reliquias", "Relics"));
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
			Label none = MakeLabel(16, FadedInk, Lang.T("Ninguna reliquia se puede mejorar.", "None of your relics can be upgraded."));
			none.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			none.HorizontalAlignment = HorizontalAlignment.Center;
			none.CustomMinimumSize = new Vector2(204f, 80f);
			list.AddChild(none, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	public Task<WorkshopChoice> ChooseAsync()
	{
		_choice = new TaskCompletionSource<WorkshopChoice>();
		List<CardModel> cards = _player.Deck.Cards.Where(WorkshopOptions.CardUsable).ToList();
		_grid.SetCards(cards, PileType.Deck, new List<SortingOrders> { SortingOrders.Ascending });
		_back.Enable();
		if (cards.Count == 0 && _relicRows.Count == 0)
		{
			_leftHint.Text = Lang.T("Nada se puede mejorar todavía.", "Nothing can be upgraded yet.");
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
			ModEntry.Warn("No se pudo volver a elegir la carta o reliquia: " + ex.Message);
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
		_costLabel.Text = (_spend == int.MaxValue) ? Lang.T("Demasiado caro", "Too expensive") : Lang.T($"{_spend:N0} de oro", $"{_spend:N0} gold");
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
		return LegendTips.Make(Lang.T("Precio", "Price"), Lang.T(
			"Lo que cuesta esta mejora: verde si te llega el oro, rojo si no. Las mejoras grandes, y las de cartas o reliquias ya mejoradas, cuestan más.",
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

	private static ImageTexture PaperGrain()
	{
		if (_paperGrain != null && GodotObject.IsInstanceValid(_paperGrain))
		{
			return _paperGrain;
		}
		try
		{
			Texture2D texture2D = ResourceLoader.Load<Texture2D>("res://images/atlases/ui_atlas.sprites/map/map_legend.tres", null, ResourceLoader.CacheMode.Reuse);
			Image image;
			Rect2I rect2I;
			if (texture2D is AtlasTexture atlasTexture)
			{
				image = atlasTexture.Atlas.GetImage();
				rect2I = new Rect2I((Vector2I)atlasTexture.Region.Position, (Vector2I)atlasTexture.Region.Size);
			}
			else
			{
				image = texture2D.GetImage();
				rect2I = new Rect2I(0, 0, image.GetWidth(), image.GetHeight());
			}
			if (image.IsCompressed())
			{
				image.Decompress();
			}
			Image region = image.GetRegion(new Rect2I(rect2I.Position + rect2I.Size / 4, rect2I.Size / 2));
			double num = 0.0;
			int num2 = 0;
			for (int i = 0; i < region.GetHeight(); i += 3)
			{
				for (int j = 0; j < region.GetWidth(); j += 3)
				{
					Color pixel = region.GetPixel(j, i);
					num += 0.299 * (double)pixel.R + 0.587 * (double)pixel.G + 0.114 * (double)pixel.B;
					num2++;
				}
			}
			_paperMean = ((num2 > 0) ? ((float)(num / (double)num2)) : 0.75f);
			_paperGrain = ImageTexture.CreateFromImage(region);
		}
		catch (Exception ex)
		{
			ModEntry.Warn("No se pudo leer el papel del mapa: " + ex.Message);
			Image image2 = Image.CreateEmpty(4, 4, useMipmaps: false, Image.Format.Rgba8);
			image2.Fill(new Color(0.75f, 0.75f, 0.75f));
			_paperGrain = ImageTexture.CreateFromImage(image2);
			_paperMean = 0.75f;
		}
		return _paperGrain;
	}

	internal static ColorRect BluePanel(Vector2 position, Vector2 size, float radius, float rollerShadow = 0f)
	{
		ImageTexture imageTexture = PaperGrain();
		ShaderMaterial shaderMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = "\r\nshader_type canvas_item;\r\nuniform sampler2D paper : filter_linear, repeat_enable;\r\nuniform vec2 size = vec2(400.0, 400.0);\r\nuniform vec2 tex_px = vec2(200.0, 300.0);\r\nuniform float mean_lum = 0.75;\r\nuniform float radius = 16.0;\r\nuniform float roller_shadow = 0.0;\r\nuniform vec3 deep = vec3(0.090, 0.150, 0.340);\r\nuniform vec3 royal = vec3(0.215, 0.345, 0.660);\r\nuniform vec3 ink = vec3(0.055, 0.090, 0.200);\r\n\r\nfloat hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }\r\nfloat vnoise(vec2 p) {\r\n    vec2 i = floor(p);\r\n    vec2 f = fract(p);\r\n    vec2 u = f * f * (3.0 - 2.0 * f);\r\n    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);\r\n}\r\nfloat fbm(vec2 p) {\r\n    float v = 0.0;\r\n    float a = 0.5;\r\n    for (int i = 0; i < 5; i++) { v += a * vnoise(p); p *= 2.03; a *= 0.5; }\r\n    return v;\r\n}\r\n\r\nfloat rbox(vec2 p, vec2 b, float r) {\r\n    vec2 q = abs(p) - b + r;\r\n    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;\r\n}\r\n\r\nvoid fragment() {\r\n    vec2 p = UV * size;\r\n    float d = rbox(p - size * 0.5, size * 0.5, radius);\r\n    // Grano del papel del juego, repetido en espejo para que no haya costuras.\r\n    vec2 t = p / tex_px;\r\n    vec2 m = 1.0 - abs(mod(t, 2.0) - 1.0);\r\n    float l = dot(texture(paper, m).rgb, vec3(0.299, 0.587, 0.114));\r\n    vec3 col = royal * (1.0 + (l - mean_lum) * 3.2);\r\n    // Papel pintado a mano: manchas grandes, grano medio y fibras finas horizontales.\r\n    float mott = fbm(p / 170.0) - 0.5;\r\n    float mid = fbm(p / 38.0 + 7.3) - 0.5;\r\n    float fib = vnoise(vec2(p.x / 9.0, p.y / 1.7)) - 0.5;\r\n    col *= 1.0 + mott * 0.30 + mid * 0.14 + fib * 0.06;\r\n    // Viñeta suave hacia los bordes y sombra bajo los rodillos.\r\n    col = mix(deep, col, 0.45 + 0.55 * smoothstep(0.0, 70.0, -d));\r\n    if (roller_shadow > 0.0) {\r\n        float ys = min(p.y, size.y - p.y);\r\n        col *= mix(0.62, 1.0, smoothstep(roller_shadow * 0.8, roller_shadow * 1.9, ys));\r\n    }\r\n    col = mix(ink, col, smoothstep(3.0, 4.5, -d));\r\n    COLOR = vec4(col, 1.0 - smoothstep(-0.5, 0.5, d));\r\n}"
			}
		};
		shaderMaterial.SetShaderParameter("paper", imageTexture);
		shaderMaterial.SetShaderParameter("size", size);
		shaderMaterial.SetShaderParameter("tex_px", new Vector2(imageTexture.GetWidth(), imageTexture.GetHeight()));
		shaderMaterial.SetShaderParameter("mean_lum", _paperMean);
		shaderMaterial.SetShaderParameter("radius", radius);
		shaderMaterial.SetShaderParameter("roller_shadow", rollerShadow);
		return new ColorRect
		{
			Material = shaderMaterial,
			Position = position,
			Size = size,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	internal static void GoldOutline(Control parent, Vector2 center, Vector2 size, float radius, float width, float gapHalfWidth = 0f)
	{
		Vector2 vector = size / 2f;
		Line2D line2D = new Line2D
		{
			Width = width,
			DefaultColor = Gold,
			JointMode = Line2D.LineJointMode.Round,
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round,
			Position = center,
			Antialiased = true
		};
		line2D.AddPoint(new Vector2(gapHalfWidth, 0f - vector.Y));
		Vector2[] array = new Vector2[4]
		{
			new Vector2(vector.X - radius, 0f - vector.Y + radius),
			new Vector2(vector.X - radius, vector.Y - radius),
			new Vector2(0f - vector.X + radius, vector.Y - radius),
			new Vector2(0f - vector.X + radius, 0f - vector.Y + radius)
		};
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j <= 8; j++)
			{
				float s = Mathf.DegToRad(-90f + (float)i * 90f + (float)j * 90f / 8f);
				line2D.AddPoint(array[i] + new Vector2(Mathf.Cos(s), Mathf.Sin(s)) * radius);
			}
		}
		line2D.AddPoint(new Vector2(0f - gapHalfWidth, 0f - vector.Y));
		parent.AddChild(line2D, forceReadableName: false, Node.InternalMode.Disabled);
		if (!(gapHalfWidth <= 0f))
		{
			float[] array2 = new float[2]
			{
				gapHalfWidth,
				0f - gapHalfWidth
			};
			foreach (float x in array2)
			{
				Vector2 vector2 = center + new Vector2(x, 0f - vector.Y);
				parent.AddChild(new Polygon2D
				{
					Polygon = new Vector2[4]
					{
						vector2 + new Vector2(0f, -7f),
						vector2 + new Vector2(7f, 0f),
						vector2 + new Vector2(0f, 7f),
						vector2 + new Vector2(-7f, 0f)
					},
					Color = Gold
				}, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
	}

	private void Roller(Vector2 center, float length, float seed)
	{
		_canvas.AddChild(RollerRect(center, length, seed), forceReadableName: false, Node.InternalMode.Disabled);
	}

	internal static ColorRect RollerRect(Vector2 center, float length, float seed, float height = 58f)
	{
		ShaderMaterial shaderMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = "\r\nshader_type canvas_item;\r\nuniform vec2 size = vec2(1420.0, 60.0);\r\nuniform vec3 blue = vec3(0.190, 0.310, 0.600);\r\nuniform vec3 ink = vec3(0.055, 0.090, 0.200);\r\nuniform float seed = 0.0;\r\n\r\nfloat hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }\r\nfloat vnoise(vec2 p) {\r\n    vec2 i = floor(p);\r\n    vec2 f = fract(p);\r\n    vec2 u = f * f * (3.0 - 2.0 * f);\r\n    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);\r\n}\r\nfloat fbm(vec2 p) {\r\n    float v = 0.0;\r\n    float a = 0.5;\r\n    for (int i = 0; i < 4; i++) { v += a * vnoise(p); p *= 2.03; a *= 0.5; }\r\n    return v;\r\n}\r\n\r\nvoid fragment() {\r\n    vec2 p = UV * size;\r\n    float s = p.x;\r\n    float R = size.y * 0.5 - 7.0;\r\n    // Grosor y eje que ondulan un poco a lo largo: enrollado a mano.\r\n    float r = R * (0.78 + 0.40 * fbm(vec2(s / 120.0 + seed, 1.3)));\r\n    // Las puntas, peor apretadas, abultan un poco más.\r\n    r *= 1.0 + 0.10 * (1.0 - smoothstep(0.0, 90.0, min(s, size.x - s)));\r\n    float c = size.y * 0.5 + 7.0 * (vnoise(vec2(s / 240.0 + seed, 7.1)) - 0.5);\r\n    float y = p.y - c;\r\n    // Se mira un poco desde la izquierda: la cara izquierda enseña la espiral de las vueltas y la\r\n    // derecha, que queda de espaldas, solo el canto del papel, algo desigual al enrollar a mano.\r\n    float x0 = 26.0;\r\n    float x1 = size.x - 12.0 - 5.0 * vnoise(vec2(seed + 9.0, p.y / 7.0)) - 2.0 * vnoise(vec2(seed + 13.0, p.y / 2.5));\r\n    float body = max(abs(y) - r, max(x0 - s, s - x1));\r\n    float rx = r * 0.40;\r\n    vec2 e = vec2((s - x0) / rx, y / r);\r\n    float face = (length(e) - 1.0) * rx;\r\n    float d = min(body, face);\r\n    if (d > 1.0) discard;\r\n\r\n    float grain = 1.0 + (fbm(p / 26.0 + seed) - 0.5) * 0.26 + (vnoise(vec2(p.x / 8.0, p.y / 1.5)) - 0.5) * 0.07;\r\n    float v = clamp(y / r, -1.0, 1.0);\r\n    // Luz suave desde arriba, sin brillo especular: papel mate.\r\n    float shade = 0.60 + 0.32 * sqrt(1.0 - v * v) - 0.13 * v;\r\n    vec3 col = blue * shade * grain;\r\n    // El canto se ve un poco más oscuro, como papel apilado.\r\n    col *= 1.0 - 0.18 * (1.0 - smoothstep(0.0, 7.0, x1 - s));\r\n    float edge = smoothstep(-2.6, -1.5, d);\r\n    if (face < 0.0) {\r\n        float q = length(e);\r\n        float a = atan(e.y, e.x) / 6.2831853;\r\n        float sp = fract(q * 3.6 - a + seed);\r\n        float line = 1.0 - smoothstep(0.02, 0.16, min(sp, 1.0 - sp));\r\n        col = blue * (1.05 + 0.15 * q) * grain;\r\n        col = mix(col, ink, line * 0.85 * smoothstep(0.12, 0.30, q));\r\n        col = mix(col, ink, 1.0 - smoothstep(0.08, 0.22, q));\r\n        edge = max(edge, smoothstep(-2.4, -1.3, face));\r\n    }\r\n    col = mix(col, ink, edge);\r\n    COLOR = vec4(col, 1.0 - smoothstep(0.0, 1.0, d));\r\n}"
			}
		};
		shaderMaterial.SetShaderParameter("size", new Vector2(length, height));
		shaderMaterial.SetShaderParameter("seed", seed);
		return new ColorRect
		{
			Material = shaderMaterial,
			Position = center - new Vector2(length / 2f, height / 2f),
			Size = new Vector2(length, height),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
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
			ModEntry.Warn("No se pudo previsualizar la reliquia mejorada: " + ex.Message);
			return relic;
		}
	}

	/// <summary>Right panel: the selected card's traits or relic's boons for sale (fixed price, one each).</summary>
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
		if (_relic != null)
		{
			RefreshBoons();
		}
		else if (_card != null)
		{
			RefreshTraits();
		}
		else
		{
			_extrasHeading.Text = Lang.T("Propiedades y bufos", "Traits and boons");
			_extrasLine.Text = Lang.T("Elige una carta o una reliquia", "Pick a card or a relic");
		}
	}

	private void RefreshTraits()
	{
		_extrasHeading.Text = Lang.T("Propiedades", "Traits");
		ExceptionalDefinition owned = ExceptionalEffects.PropertyOf(_card);
		if (owned != null)
		{
			ShowOwned(Lines.PropertyAlready(ExceptionalEffects.Name(owned)), ExceptionalEffects.Explain(owned, CardStateStore.Get(_card)?.PropertyGrowth ?? 0));
			return;
		}
		if (!Settings.Active.TraitsEnabled)
		{
			_extrasLine.Text = Lines.TraitsOff;
			return;
		}
		List<ExceptionalDefinition> forSale = ExceptionalEffects.ApplicableTo(_card);
		if (forSale.Count == 0)
		{
			_extrasLine.Text = Lines.PropertyNone;
			return;
		}
		int price = WorkshopOptions.TraitPrice;
		ShowEachPrice(price);
		CardModel card = _card;
		foreach (ExceptionalDefinition def in forSale)
		{
			AddForSale(ExceptionalEffects.Name(def), ExceptionalEffects.Explain(def, 0), price, new WorkshopChoice
			{
				Card = card,
				Trait = def.Id
			});
		}
	}

	private void RefreshBoons()
	{
		_extrasHeading.Text = Lang.T("Bufos", "Boons");
		string owned = Tinker.Relic.Store.For(_relic).Boon;
		if (owned != null)
		{
			string name = Tinker.Relic.Boons.Name(owned);
			ShowOwned(Lang.T("Tiene el bufo " + name + ".", "Has the " + name + " boon."), Tinker.Relic.Boons.Description(owned));
			return;
		}
		string[] forSale = Tinker.Relic.Boons.ForSale(_relic);
		if (forSale.Length == 0)
		{
			_extrasLine.Text = Lang.T("Bufos desactivados en los ajustes", "Boons are turned off in the settings");
			return;
		}
		int price = WorkshopOptions.BoonPrice;
		ShowEachPrice(price);
		RelicModel relic = _relic;
		foreach (string boon in forSale)
		{
			AddForSale(Tinker.Relic.Boons.Name(boon), Tinker.Relic.Boons.Description(boon), price, new WorkshopChoice
			{
				Relic = relic,
				RelicAttributeIndex = -1,
				Boon = boon
			});
		}
	}

	private void ShowOwned(string line, string detail)
	{
		_extrasLine.Text = line;
		_extrasLine.AddThemeColorOverride("font_color", GoldInk);
		_propertyDetail.Text = detail;
	}

	/// <summary>The price line above the buttons: green if the player can afford one, red if not.</summary>
	private void ShowEachPrice(int price)
	{
		_extrasLine.Text = Lang.T($"{price:N0} de oro cada una", $"{price:N0} gold each");
		_extrasLine.AddThemeColorOverride("font_color", (price <= _gold) ? AffordableInk : UnaffordableInk);
	}

	/// <summary>One trait / boon button: hover for what it does, click to buy (disabled if too expensive).</summary>
	private void AddForSale(string name, string description, int price, WorkshopChoice choice)
	{
		bool affordable = price <= _gold;
		Button button = new Button
		{
			Text = "✦ " + name,
			ClipText = true,
			Alignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(180f, 34f),
			FocusMode = Control.FocusModeEnum.None,
			Disabled = !affordable
		};
		button.AddThemeFontOverride("font", ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres", null, ResourceLoader.CacheMode.Reuse));
		button.AddThemeFontSizeOverride("font_size", 17);
		button.AddThemeColorOverride("font_color", new Color("F3C14F"));
		button.AddThemeColorOverride("font_hover_color", Ink);
		button.AddThemeColorOverride("font_pressed_color", Ink);
		button.AddThemeColorOverride("font_disabled_color", new Color(FadedInk.R, FadedInk.G, FadedInk.B, 0.45f));
		StyleRow(button, selected: false);
		LegendTips.Attach(button, () => LegendTips.Make(name, LegendTips.Capitalize(description)));
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

	private void UpdatePreview()
	{
		CardModel cardModel = (CardModel)_card.MutableClone();
		CardMachineState cardMachineState = CardStateStore.Get(cardModel)?.Clone() ?? new CardMachineState();
		int num = cardMachineState.ApplySuccess(Attribute, _units);
		CardStateStore.Set(cardModel, cardMachineState);
		if (AttributeKeys.IsVar(Attribute) && cardModel.DynamicVars.TryGetValue(AttributeKeys.VarName(Attribute), out DynamicVar value))
		{
			value.UpgradeValueBy(SteppedValues.TryGet(cardModel, AttributeKeys.VarName(Attribute), out SteppedValues.Stepped stepped) ? num * stepped.Step : num);
		}
		else if (Attribute == "cost")
		{
			cardModel.EnergyCost.UpgradeBy(num);
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
