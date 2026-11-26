using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Tinker.Common;

namespace Tinker.Card;

// Card text for expanded effects: the numbers shown and the rewritten description lines.
internal static partial class ExpandedEffectPatches
{
	private static int TextValue(CardModel card, ExpandedEffect effect, int value)
	{
		if (effect.Kind == "chooseGenerated")
		{
			return Math.Min(value, CardAttributes.CurrentValue(card, "extra:options"));
		}
		if (effect.Kind == "energyMultiplier" || effect.Kind == "blockMultiplier")
		{
			return value;
		}
		if (effect.Attribute != "extra:multiplier" && effect.Attribute != "extra:hangGrowth" && effect.Attribute != "extra:discount" && effect.Attribute != "extra:killReplays" && effect.Attribute != "extra:passive")
		{
			return value;
		}
		int native = Native(card, effect);
		decimal num2 = (decimal)native * (1m + (decimal)value - (decimal)native);
		if (!(effect.Attribute == "extra:hangGrowth"))
		{
			return (int)num2;
		}
		return (int)Math.Min(999999999m, num2);
	}

	internal static string DisplayValue(CardModel card, string attribute, int value)
	{
		if (AttributeKeys.IsVar(attribute) && SteppedValues.TryGet(card, AttributeKeys.VarName(attribute), out SteppedValues.Stepped stepped))
		{
			return SteppedValues.Display(stepped, value);
		}
		ExpandedEffect expandedEffect = ExpandedEffects.Find(card.GetType().Name, attribute);
		return FinalCardEffects.DisplayValue(attribute, (expandedEffect == null) ? value : TextValue(card, expandedEffect, value));
	}

	internal static string Rewrite(CardModel card, string text)
	{
		if (CardStateStore.Get(card) == null || !Enabled(card))
		{
			return text;
		}
		checked
		{
			foreach (ExpandedEffect item in from e in ExpandedEffects.For(card.GetType().Name)
				group e by e.Attribute into g
				select g.First())
			{
				int delta = Delta(card, item.Attribute);
				if (delta == 0 || (card is Armaments && card.IsUpgraded))
				{
					continue;
				}
				int textValue = TextValue(card, item, CardAttributes.CurrentValue(card, item.Attribute));
				if (item.Attribute == "extradown:quest")
				{
					textValue = card.DynamicVars[(item.Card == "Guilty") ? "Combats" : "Rooms"].IntValue;
				}
				if (item.Card == "Reflect")
				{
					textValue = CardAttributes.CurrentValue(card, "extra:reflectionPercent") * CardAttributes.CurrentValue(card, "extra:power");
				}
				string rewritten = ExpandedEffectText.RewriteLine(text, item.Card, item.Attribute, item.Kind, (item.Attribute == "extra:sharedForge") ? (textValue * 25) : ((item.Kind == "x") ? delta : textValue));
				if (rewritten != null)
				{
					text = rewritten;
				}
				else
				{
					if (AttributeKeys.IsVar(item.Attribute))
					{
						continue;
					}
					BidirectionalEffect bidirectionalEffect = BidirectionalEffects.Find(card.GetType().Name, item.Attribute);
					if (bidirectionalEffect != null)
					{
						string text3 = ((!(card is TrueGrit) || card.IsUpgraded) ? Lang.T(bidirectionalEffect.English) : Lang.T("Random Exhausts"));
						text = text + "\n" + text3 + ": " + CardAttributes.CurrentValue(card, item.Attribute) + Lang.T(" (adjusted).");
						continue;
					}
					string format = ((!(item.Card == "Eidolon")) ? ((item.Card == "Tracking" && TrackingUsesPercent) ? Lang.T("The damage bonus against Weak increases by {0} percentage points.") : Lang.T(item.ExtraEnglish)) : ((!EidolonUsesAutoPlay) ? Lang.T("Gain {0} additional Intangible when the requirement is met.") : Lang.T("Each automatic card is played {0} additional times.")));
					if (item.Attribute == "extra:blockGrowth")
					{
						format = Lang.T("At the end of your turn, increase your Block by {0}%.");
					}
					if (item.Attribute == "extra:sharedForge")
					{
						format = Lang.T("Allies receive {0}% of your Forge.");
					}
					if (item.Attribute == "extra:shadowMultiplier")
					{
						format = Lang.T("Boosted damage is multiplied by {0}.");
					}
					int num3 = ((item.Attribute == "extra:blockGrowth") ? (delta * 10) : ((item.Attribute == "extra:sharedForge") ? (CardAttributes.CurrentValue(card, item.Attribute) * 25) : ((item.Attribute == "extra:shadowMultiplier") ? CardAttributes.CurrentValue(card, item.Attribute) : ((item.Kind == "chooseGenerated") ? Math.Max(0, unchecked(textValue - Native(card, item))) : Math.Abs(delta)))));
					text = text + "\n" + string.Format(format, num3);
				}
			}
			return text;
		}
	}
}
