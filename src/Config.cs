using System;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;

namespace Tinker;

/// <summary>
/// The settings menu (BaseLib mod config, saved as Tinker.cfg): preset, upgrade difficulty, the Workshop's gold
/// per upgrade, traits (cards) and boons (relics).
///
/// - Shared values (preset, difficulty) write to both halves through <see cref="Settings"/>.
/// - Traits and boons read and write the halves' own settings objects (CardSettings / Tinker.Relic.ModSettings).
/// </summary>
internal sealed class Config : SimpleModConfig
{
    /// <summary>BaseLib names the file after the class's root namespace: Tinker.cfg.</summary>
    public const string FileName = "Tinker";

    private static Control _container;

    [ConfigIgnore]
    public static Config Instance { get; private set; }

    public Config()
    {
        Instance = this;
        Settings.CopySharedToRelic();
        Settings.Refresh();
    }


    [ConfigSection("SectionGeneral")]
    [ConfigHoverTip(true)]
    public static SettingsPreset Preset
    {
        get => Settings.Preset;
        set
        {
            if (value != SettingsPreset.Custom && value != Settings.Preset)
            {
                Settings.ApplyPreset(value);
                Instance?.ConfigReloaded();
            }
        }
    }

    [ConfigSection("SectionPrices")]
    [ConfigSlider(25.0, 300.0, 5.0)]
    [SliderLabelFormat("{0} %")]
    [ConfigHoverTip(true)]
    public static int Difficulty
    {
        get => Settings.Difficulty;
        set { Settings.Difficulty = value; Changed(); }
    }

    // ---------------------------------------------------------------- the Workshop

    [ConfigSection("SectionWorkshop")]
    [ConfigSlider(5.0, 100.0, 5.0)]
    [ConfigHoverTip(true)]
    public static int WorkshopGoldRate
    {
        get => Card.WorkshopGoldRate;
        set { Card.WorkshopGoldRate = value; Changed(); }
    }

    [ConfigSection("SectionTraits")]
    [ConfigHoverTip(true)]
    public static bool TraitsEnabled
    {
        get => Card.TraitsEnabled;
        set { Card.TraitsEnabled = value; Changed(); }
    }

    [ConfigVisibleIf("TraitsEnabled", new object[] { })]
    [ConfigSlider(0.0, 500.0, 10.0)]
    [ConfigHoverTip(true)]
    public static int TraitPrice
    {
        get => Card.TraitPrice;
        set { Card.TraitPrice = value; Changed(); }
    }

    [ConfigVisibleIf("TraitsEnabled", new object[] { })]
    public static bool TraitInexhaustible
    {
        get => CardTrait("Inexhaustible");
        set { SetCardTrait("Inexhaustible", value); Changed(); }
    }

    [ConfigVisibleIf("TraitsEnabled", new object[] { })]
    public static bool TraitEcho
    {
        get => CardTrait("Echo");
        set { SetCardTrait("Echo", value); Changed(); }
    }

    [ConfigVisibleIf("TraitsEnabled", new object[] { })]
    public static bool TraitAwakening
    {
        get => CardTrait("Awakening");
        set { SetCardTrait("Awakening", value); Changed(); }
    }

    [ConfigVisibleIf("TraitsEnabled", new object[] { })]
    public static bool TraitLegacy
    {
        get => CardTrait("Legacy");
        set { SetCardTrait("Legacy", value); Changed(); }
    }

    [ConfigVisibleIf("TraitsEnabled", new object[] { })]
    public static bool TraitVenomous
    {
        get => CardTrait("Venomous");
        set { SetCardTrait("Venomous", value); Changed(); }
    }

    [ConfigVisibleIf("TraitsEnabled", new object[] { })]
    public static bool TraitLifesteal
    {
        get => CardTrait("Lifesteal");
        set { SetCardTrait("Lifesteal", value); Changed(); }
    }

    [ConfigSection("SectionBoons")]
    [ConfigHoverTip(true)]
    public static bool BoonsEnabled
    {
        get => Relic.BoonsEnabled;
        set { Relic.BoonsEnabled = value; Changed(); }
    }

    [ConfigSlider(0.0, 500.0, 10.0)]
    [ConfigHoverTip(true)]
    public static int BoonPrice
    {
        get => Relic.BoonPrice;
        set { value = Math.Clamp(value, 0, 9999); Relic.BoonPrice = value; Changed(); }
    }

    [ConfigHoverTip(true)]
    public static bool BoonStrength
    {
        get => Relic.BoonStrength;
        set { Relic.BoonStrength = value; Changed(); }
    }

    [ConfigHoverTip(true)]
    public static bool BoonDexterity
    {
        get => Relic.BoonDexterity;
        set { Relic.BoonDexterity = value; Changed(); }
    }

    [ConfigHoverTip(true)]
    public static bool BoonBlock
    {
        get => Relic.BoonBlock;
        set { Relic.BoonBlock = value; Changed(); }
    }

    [ConfigHoverTip(true)]
    public static bool BoonHeal
    {
        get => Relic.BoonHeal;
        set { Relic.BoonHeal = value; Changed(); }
    }

    [ConfigHoverTip(true)]
    public static bool BoonEnergy
    {
        get => Relic.BoonEnergy;
        set { Relic.BoonEnergy = value; Changed(); }
    }

    [ConfigHoverTip(true)]
    public static bool BoonDraw
    {
        get => Relic.BoonDraw;
        set { Relic.BoonDraw = value; Changed(); }
    }

    // ---------------------------------------------------------------- plumbing

    /// <summary>Card Upgrader's settings object (applied live; in co-op the host's copy is used instead).</summary>
    private static CardSettings Card => Tinker.Card.Settings.Local;

    /// <summary>Relic Upgrader's settings object (frozen into each new run). Re-read every time: presets replace it.</summary>
    private static Tinker.Relic.ModSettings Relic => Tinker.Relic.Settings.Local;

    private static bool CardTrait(string id) => !Card.DisabledTraits.Contains(id);

    private static void SetCardTrait(string id, bool on)
    {
        if (on)
        {
            Card.DisabledTraits.Remove(id);
        }
        else
        {
            Card.DisabledTraits.Add(id);
        }
    }

    /// <summary>The font the menu uses, so the card graphs match it.</summary>
    private static Font FindFont(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is RichTextLabel richText && richText.HasThemeFontOverride("normal_font"))
            {
                return richText.GetThemeFont("normal_font");
            }
            if (child is Label label && label.HasThemeFontOverride("font"))
            {
                return label.GetThemeFont("font");
            }
            Font font = FindFont(child);
            if (font != null)
            {
                return font;
            }
        }
        return null;
    }

    /// <summary>Puts a control at the top of a collapsible section (or at the end of the menu if it isn't found).</summary>
    private static void InsertIntoSection(Control root, string section, Control control)
    {
        Node node = root.FindChild("SectionLabel_" + section, recursive: true, owned: false);
        while (node != null && node is not NConfigCollapsibleSection)
        {
            node = node.GetParent();
        }
        if (node is NConfigCollapsibleSection { ContentContainer: not null } collapsible)
        {
            collapsible.ContentContainer.AddChild(control);
            collapsible.ContentContainer.MoveChild(control, 0);
        }
        else
        {
            root.AddChild(control);
            GD.PushWarning("[Tinker] Section " + section + " not found; graph added at the end.");
        }
    }

    /// <summary>After any value change: resync both halves and refresh the preset shown.</summary>
    private static void Changed()
    {
        SettingsPreset before = Settings.Preset;
        Settings.Refresh();
        if (Settings.Preset != before)
        {
            Instance?.ConfigReloaded();
        }
    }

    protected override void RestoreDefaultsNoConfirm()
    {
        Settings.RestoreDefaults();
        Save();
        ConfigReloaded();
    }

    public override void SetupConfigUI(Control container)
    {
        Text.Install();
        base.SetupConfigUI(container);
        _container = container;

        MegaRichTextLabel note = ModConfig.CreateRawLabelControl(Lang.T(
            "Los precios del taller salen de la dificultad y del oro por mejora. Las reglas de las reliquias se guardan con cada partida, así que sus cambios se aplican a partidas nuevas. En cooperativo manda el anfitrión.",
            "Workshop prices come from the difficulty and the gold per upgrade. Relic rules are saved with each run, so changes to them apply to new runs. Co-op uses the host's settings."), 23);
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        note.CustomMinimumSize = new Vector2(700f, 80f);
        container.AddChild(note);
        container.MoveChild(note, 0);
    }
}
