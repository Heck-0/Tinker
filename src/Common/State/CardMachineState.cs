using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tinker.Common;

/// <summary>
/// A card's Workshop upgrades: units bought per attribute. Saved as JSON under CardUpgraderState:
/// {"v":2,"a":{"var:Damage":3}}. Version 1 (the roulette's gain/loss ledgers) isn't read; a trait ("p", removed in
/// 1.8.0) is ignored.
/// </summary>
public sealed class CardMachineState
{
	public const int FormatVersion = 2;

	private const int MaxStoredUnits = 1000000;

	private readonly SortedDictionary<string, AttributeLedger> _ledgers = new SortedDictionary<string, AttributeLedger>(StringComparer.Ordinal);

	public IReadOnlyDictionary<string, AttributeLedger> Ledgers => _ledgers;

	public bool IsEmpty => _ledgers.Count == 0;

	public bool RemoveAttribute(string attribute)
	{
		return _ledgers.Remove(attribute);
	}

	public int RawDelta(string attribute)
	{
		if (!_ledgers.TryGetValue(attribute, out var value))
		{
			return 0;
		}
		return value.Gain * AttributeDirection.SignOf(attribute);
	}

	/// <summary>A "lower is better" value can't go below zero: caps its units at the card's base value.</summary>
	public void ConstrainToBase(string attribute, int nativeBase)
	{
		if (nativeBase < 0)
		{
			throw new ArgumentOutOfRangeException("nativeBase");
		}
		if (AttributeDirection.SignOf(attribute) < 0 && _ledgers.TryGetValue(attribute, out var value))
		{
			value.Gain = Math.Min(value.Gain, nativeBase);
			Prune();
		}
	}

	public void ConstrainCostToBase(int nativeCost)
	{
		if (nativeCost < 0)
		{
			throw new ArgumentOutOfRangeException("nativeCost");
		}
		if (_ledgers.TryGetValue("cost", out var value))
		{
			value.Gain = Math.Min(value.Gain, nativeCost);
			Prune();
		}
	}

	/// <summary>Adds bought units; returns the raw change to apply to the card (negative for "lower is better").</summary>
	public int ApplySuccess(string attribute, int powerUnits)
	{
		if (powerUnits <= 0)
		{
			throw new ArgumentOutOfRangeException("powerUnits");
		}
		ValidateAttribute(attribute);
		if (!_ledgers.TryGetValue(attribute, out var ledger))
		{
			ledger = new AttributeLedger();
			_ledgers[attribute] = ledger;
		}
		int gain = checked(ledger.Gain + powerUnits);
		if (gain > MaxStoredUnits)
		{
			throw new InvalidOperationException("Machine state limit reached.");
		}
		ledger.Gain = gain;
		return powerUnits * AttributeDirection.SignOf(attribute);
	}

	private static void ValidateAttribute(string attribute)
	{
		if (string.IsNullOrEmpty(attribute))
		{
			throw new ArgumentException("Empty attribute.", "attribute");
		}
		if (!(attribute == "cost") && !FixedEffects.Known(attribute) && !ExpandedEffects.Known(attribute) && !EnchantmentEffects.Known(attribute) && !AdvancedEnchantmentEffects.Known(attribute) && !AttributeKeys.IsVar(attribute) && (!attribute.StartsWith("self:", StringComparison.Ordinal) || attribute.Length <= "self:".Length) && (!attribute.StartsWith("kw+:", StringComparison.Ordinal) || attribute.Length <= "kw+:".Length))
		{
			throw new ArgumentException("Unknown machine attribute.", "attribute");
		}
	}

	public CardMachineState Clone()
	{
		CardMachineState cardMachineState = new CardMachineState();
		foreach (KeyValuePair<string, AttributeLedger> ledger in _ledgers)
		{
			cardMachineState._ledgers[ledger.Key] = new AttributeLedger
			{
				Gain = ledger.Value.Gain
			};
		}
		return cardMachineState;
	}

	private void Prune()
	{
		foreach (string item in (from kv in _ledgers
			where kv.Value.Gain == 0
			select kv.Key).ToList())
		{
			_ledgers.Remove(item);
		}
	}

	public string ToJson()
	{
		JsonObject attributes = new JsonObject();
		foreach (KeyValuePair<string, AttributeLedger> ledger in _ledgers)
		{
			attributes[ledger.Key] = ledger.Value.Gain;
		}
		JsonObject jsonObject = new JsonObject
		{
			["v"] = FormatVersion,
			["a"] = attributes
		};
		return jsonObject.ToJsonString();
	}

	public static CardMachineState FromJson(string json)
	{
		try
		{
			JsonObject jsonObject = (JsonNode.Parse(json) as JsonObject) ?? throw new FormatException("empty machine state");
			int version = (int)(jsonObject["v"] ?? throw new FormatException("missing version"));
			if (version != FormatVersion)
			{
				throw new FormatException("unsupported state version: " + version);
			}
			JsonObject obj = (jsonObject["a"] as JsonObject) ?? throw new FormatException("missing attributes");
			CardMachineState cardMachineState = new CardMachineState();
			foreach (KeyValuePair<string, JsonNode> item in obj)
			{
				ValidateAttribute(item.Key);
				int gain = (int)(item.Value ?? throw new FormatException("malformed attribute: " + item.Key));
				if (gain < 0 || gain > MaxStoredUnits)
				{
					throw new FormatException("values out of range in " + item.Key);
				}
				cardMachineState._ledgers[item.Key] = new AttributeLedger
				{
					Gain = gain
				};
			}
			cardMachineState.Prune();
			return cardMachineState;
		}
		catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is OverflowException || ex is ArgumentException)
		{
			throw new FormatException("invalid machine state structure", ex);
		}
	}
}
