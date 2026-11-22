using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace Tinker.Card;

public static class Loc
{
	private static Dictionary<string, Dictionary<string, string>> Spanish()
	{
		return new Dictionary<string, Dictionary<string, string>>
		{
			["relics"] = new Dictionary<string, string>
			{
				["TINKERERS_DIAL.title"] = "Dial del Artífice",
				["TINKERERS_DIAL.description"] = "Haz clic para abrir el [gold]taller[/gold]: compra mejoras para tus cartas y reliquias con oro. Fuera de combate.",
				["TINKERERS_DIAL.flavor"] = "Nadie recuerda quién lo calibró. Nadie se atreve a hacerlo de nuevo."
			},
			["settings_ui"] = new Dictionary<string, string>
			{
				["RULETA-SECTION_GENERAL.title"] = "General",
				["RULETA-PRESET.title"] = "Preajuste",
				["RULETA-PRESET.hover.title"] = "Preajuste",
				["RULETA-PRESET.hover.desc"] = "Normal es el mod tal cual. Fácil y Difícil cambian varios valores a la vez. Si tocas algo, pasa a Personalizado. En cooperativo se usa la configuración del anfitrión.",
				["RULETA-SECTION_ODDS.title"] = "Precios",
				["RULETA-SECTION_TRAITS.title"] = "Propiedades",
				["RULETA-TRAITS_ENABLED.title"] = "Propiedades al comprar",
				["RULETA-TRAITS_ENABLED.hover.title"] = "Propiedades al comprar",
				["RULETA-TRAITS_ENABLED.hover.desc"] = "Si las quitas, comprar una mejora solo cambia el número.",
				["RULETA-TRAIT_BASE.title"] = "Probabilidad base",
				["RULETA-TRAIT_BASE.hover.title"] = "Probabilidad base",
				["RULETA-TRAIT_BASE.hover.desc"] = "Probabilidad de propiedad en cada compra. Las mejoras más caras dan más.",
				["RULETA-TRAIT_MAX_ON_SUCCESS.title"] = "Probabilidad máxima",
				["RULETA-TRAIT_MAX_ON_SUCCESS.hover.title"] = "Probabilidad máxima",
				["RULETA-TRAIT_MAX_ON_SUCCESS.hover.desc"] = "Probabilidad máxima de propiedad en una sola compra.",
				["RULETA-TRAIT_BLOODTHIRST.title"] = "Sed de sangre",
				["RULETA-TRAIT_FORTIFY.title"] = "Fortificación",
				["RULETA-TRAIT_INEXHAUSTIBLE.title"] = "Inagotable",
				["RULETA-TRAIT_ECHO.title"] = "Eco",
				["RULETA-TRAIT_AWAKENING.title"] = "Despertar",
				["RULETA-TRAIT_LEGACY.title"] = "Legado",
				["RULETA-TRAIT_VENOMOUS.title"] = "Ponzoña",
				["RULETA-TRAIT_LIFESTEAL.title"] = "Vampirismo",
				["RULETA-PRESET.Normal"] = "Normal",
				["RULETA-PRESET.Easy"] = "Fácil",
				["RULETA-PRESET.Hard"] = "Difícil",
				["RULETA-PRESET.Custom"] = "Personalizado",
			},
			["card_selection"] = new Dictionary<string, string>
			{
				["TO_TINKER"] = "Elige una carta para [gold]Ajustar[/gold].",
				["UPGRADER_GENERATED_CARDS"] = "Elige {Amount} cartas.",
				["UPGRADER_GENERATED_OPTIONAL"] = "Elige hasta {Amount} cartas."
			},
			["cards"] = new Dictionary<string, string> { ["FERAL.description"] = "{FeralPower:plural:La primera vez|Las primeras {FeralPower:diff()} veces} que juegues\nun Ataque de coste 0\u00a0{energyPrefix:energyIcons(1)}\ncada turno, devuélvelo a tu [gold]Mano[/gold]." }
		};
	}

	private static Dictionary<string, Dictionary<string, string>> English()
	{
		return new Dictionary<string, Dictionary<string, string>>
		{
			["relics"] = new Dictionary<string, string>
			{
				["TINKERERS_DIAL.title"] = "Tinkerer's Dial",
				["TINKERERS_DIAL.description"] = "Click to open the [gold]Workshop[/gold]: buy upgrades for your cards and relics with gold. Not during combat.",
				["TINKERERS_DIAL.flavor"] = "Nobody remembers who calibrated it. Nobody dares to do it again."
			},
			["settings_ui"] = new Dictionary<string, string>
			{
				["RULETA-SECTION_GENERAL.title"] = "General",
				["RULETA-PRESET.title"] = "Preset",
				["RULETA-PRESET.hover.title"] = "Preset",
				["RULETA-PRESET.hover.desc"] = "Normal is the mod as designed. Easy and Hard change several values at once. Touch anything and it becomes Custom. In co-op, the host's settings are used.",
				["RULETA-SECTION_ODDS.title"] = "Prices",
				["RULETA-SECTION_TRAITS.title"] = "Traits",
				["RULETA-TRAITS_ENABLED.title"] = "Traits on purchase",
				["RULETA-TRAITS_ENABLED.hover.title"] = "Traits on purchase",
				["RULETA-TRAITS_ENABLED.hover.desc"] = "Turn them off and buying an upgrade only changes the number.",
				["RULETA-TRAIT_BASE.title"] = "Base chance",
				["RULETA-TRAIT_BASE.hover.title"] = "Base chance",
				["RULETA-TRAIT_BASE.hover.desc"] = "Chance of a trait on each purchase. Pricier upgrades give a better chance.",
				["RULETA-TRAIT_MAX_ON_SUCCESS.title"] = "Maximum trait chance",
				["RULETA-TRAIT_MAX_ON_SUCCESS.hover.title"] = "Maximum trait chance",
				["RULETA-TRAIT_MAX_ON_SUCCESS.hover.desc"] = "Highest trait chance on a single purchase.",
				["RULETA-TRAIT_BLOODTHIRST.title"] = "Bloodthirst",
				["RULETA-TRAIT_FORTIFY.title"] = "Fortify",
				["RULETA-TRAIT_INEXHAUSTIBLE.title"] = "Inexhaustible",
				["RULETA-TRAIT_ECHO.title"] = "Echo",
				["RULETA-TRAIT_AWAKENING.title"] = "Awakening",
				["RULETA-TRAIT_LEGACY.title"] = "Legacy",
				["RULETA-TRAIT_VENOMOUS.title"] = "Venomous",
				["RULETA-TRAIT_LIFESTEAL.title"] = "Lifesteal",
				["RULETA-PRESET.Normal"] = "Normal",
				["RULETA-PRESET.Easy"] = "Easy",
				["RULETA-PRESET.Hard"] = "Hard",
				["RULETA-PRESET.Custom"] = "Custom",
			},
			["card_selection"] = new Dictionary<string, string>
			{
				["TO_TINKER"] = "Choose a card to [gold]Tinker[/gold] with.",
				["UPGRADER_GENERATED_CARDS"] = "Choose {Amount} cards.",
				["UPGRADER_GENERATED_OPTIONAL"] = "Choose up to {Amount} cards."
			}
		};
	}

	internal static Dictionary<string, Dictionary<string, string>> TablesFor(string language)
	{
		bool flag = Lines.IsSpanish(language);
		Dictionary<string, Dictionary<string, string>> obj = (flag ? Spanish() : English());
		FinalCardEffects.AddTexts(obj, flag);
		return Lang.Zh(obj, language);
	}

	public static void Install()
	{
		MergeInto(LocManager.Instance);
	}

	private static void MergeInto(LocManager manager)
	{
		if (manager != null)
		{
			Lang.Merge(manager, TablesFor(manager.Language), ModEntry.Warn);
		}
	}

	internal static void MergeIntoTables(Dictionary<string, LocTable> tables, string language)
	{
		Lang.Merge(tables, TablesFor(language));
	}
}
