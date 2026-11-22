using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Card;

public static class CardStateStore
{
	private static readonly ConditionalWeakTable<CardModel, CardMachineState> States = new ConditionalWeakTable<CardModel, CardMachineState>();

	public static CardMachineState Get(CardModel card)
	{
		if (!States.TryGetValue(card, out var value))
		{
			return null;
		}
		return value;
	}

	public static CardMachineState GetOrCreate(CardModel card)
	{
		return States.GetValue(card, (CardModel _) => new CardMachineState());
	}

	public static void Set(CardModel card, CardMachineState state)
	{
		States.Remove(card);
		if (state != null && !state.IsEmpty)
		{
			States.Add(card, state);
		}
	}

}
