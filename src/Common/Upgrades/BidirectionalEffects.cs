using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

public static class BidirectionalEffects
{
	public static readonly IReadOnlyList<BidirectionalEffect> Entries = Build().ToArray();

	private static IEnumerable<BidirectionalEffect> Build()
	{
		string[] array = new string[4] { "Acrobatics", "DaggerThrow", "Survivor", "Prepared" };
		foreach (string card in array)
		{
			yield return E(card, "extradown:discard", "extra:discard", "Discards");
		}
		yield return E("HiddenDaggers", "vardown:Cards", "var:Cards", "Discards");
		yield return E("ThinkingAhead", "extradown:putBack", "extra:putBack", "Cards put back");
		array = new string[2] { "Glimmer", "PhotonCut" };
		foreach (string card2 in array)
		{
			yield return E(card2, "vardown:PutBack", "var:PutBack", "Cards put back");
		}
		array = new string[2] { "Headbutt", "CosmicIndifference" };
		foreach (string card3 in array)
		{
			yield return E(card3, "extra:selection", "extradown:selection", "Maximum cards retrieved to draw pile");
		}
		yield return E("Rebound", "extra:returnCount", "extradown:returnCount", "Returns this turn");
		BidirectionalEffect bidirectionalEffect = E("Nostalgia", "extra:returnCount", "extradown:returnCount", "Returns per turn");
		bidirectionalEffect.Family = "persistentRepeat";
		yield return bidirectionalEffect;
		array = new string[5] { "Brand", "BurningPact", "Scavenge", "Cleanse", "TrueGrit" };
		foreach (string card4 in array)
		{
			yield return E(card4, "extra:exhaustMaximum", "extradown:exhaustMaximum", "Maximum Exhausts");
		}
		yield return E("Purity", "var:Cards", "vardown:Cards", "Maximum Exhausts");
		yield return E("Cinder", "extradown:randomExhaust", "extra:randomExhaust", "Random Exhausts");
	}

	private static BidirectionalEffect E(string card, string canonical, string reverse, string en)
	{
		return new BidirectionalEffect
		{
			Card = card,
			Canonical = canonical,
			Reverse = reverse,
			English = en
		};
	}

	public static IEnumerable<BidirectionalEffect> For(string card)
	{
		return Entries.Where((BidirectionalEffect e) => e.Card == card);
	}

	public static BidirectionalEffect Find(string card, string key)
	{
		return For(card).FirstOrDefault((BidirectionalEffect e) => e.Canonical == key || e.Reverse == key);
	}

	public static string Axis(string card, string key)
	{
		return Find(card, key)?.Canonical ?? key;
	}

	public static bool KnownExtra(string key)
	{
		if (!AttributeKeys.IsVar(key))
		{
			return Entries.Any((BidirectionalEffect e) => e.Canonical == key || e.Reverse == key);
		}
		return false;
	}

	public static int RawDelta(string card, string key, CardMachineState state)
	{
		BidirectionalEffect bidirectionalEffect = Find(card, key);
		if (bidirectionalEffect != null)
		{
			return checked((state?.RawDelta(bidirectionalEffect.Canonical) ?? 0) + (state?.RawDelta(bidirectionalEffect.Reverse) ?? 0));
		}
		return state?.RawDelta(key) ?? 0;
	}

	public static int Gain(string card, string key, CardMachineState state)
	{
		if (state == null)
		{
			return 0;
		}
		string axis = Axis(card, key);
		return state.Ledgers.Where((KeyValuePair<string, AttributeLedger> e) => Axis(card, e.Key) == axis).Sum((KeyValuePair<string, AttributeLedger> e) => e.Value.Gain);
	}

	public static void ConstrainToBase(BidirectionalEffect pair, CardMachineState state, int nativeBase)
	{
		int num = Math.Max(0, -checked(nativeBase + RawDelta(pair.Card, pair.Canonical, state)));
		if (num > 0 && state.Ledgers.TryGetValue(pair.Down, out var value))
		{
			value.Gain -= Math.Min(value.Gain, num);
		}
	}
}
