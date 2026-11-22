using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tinker.Common;

/// <summary>
/// A card's Workshop upgrades: units bought per attribute, plus its trait. Saved as JSON under CardUpgraderState:
/// {"v":2,"a":{"var:Damage":3},"p":{"id":"Echo","g":0}}. Version 1 (the roulette's gain/loss ledgers) isn't read.
/// </summary>
public sealed class CardMachineState
{
	public const int FormatVersion = 2;

	private const int MaxStoredUnits = 1000000;

	private readonly SortedDictionary<string, AttributeLedger> _ledgers = new SortedDictionary<string, AttributeLedger>(StringComparer.Ordinal);

	public IReadOnlyDictionary<string, AttributeLedger> Ledgers => _ledgers;

	public bool IsEmpty => _ledgers.Count == 0 && Property == null;

	public string Property { get; private set; }

	public int PropertyGrowth { get; private set; }

	public void GrantProperty(string id)
	{
		if (Property != null)
		{
			throw new InvalidOperationException("La copia ya tiene una propiedad excepcional.");
		}
		if (ExceptionalCatalog.Find(id) == null)
		{
			throw new ArgumentException("Propiedad desconocida: " + id, "id");
		}
		Property = id;
		PropertyGrowth = 0;
	}

	public void AddPropertyGrowth(int amount)
	{
		if (Property == null)
		{
			throw new InvalidOperationException("La copia no tiene propiedad excepcional.");
		}
		if (amount <= 0)
		{
			throw new ArgumentOutOfRangeException("amount");
		}
		int num = checked(PropertyGrowth + amount);
		if (num > MaxStoredUnits)
		{
			throw new InvalidOperationException("Límite de crecimiento alcanzado.");
		}
		PropertyGrowth = num;
	}

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
			throw new InvalidOperationException("Límite de estado de máquina alcanzado.");
		}
		ledger.Gain = gain;
		return powerUnits * AttributeDirection.SignOf(attribute);
	}

	private static void ValidateAttribute(string attribute)
	{
		if (string.IsNullOrEmpty(attribute))
		{
			throw new ArgumentException("Atributo vacío.", "attribute");
		}
		if (!(attribute == "cost") && !FixedEffects.Known(attribute) && !ExpandedEffects.Known(attribute) && !EnchantmentEffects.Known(attribute) && !AdvancedEnchantmentEffects.Known(attribute) && !AttributeKeys.IsVar(attribute) && (!attribute.StartsWith("self:", StringComparison.Ordinal) || attribute.Length <= "self:".Length) && (!attribute.StartsWith("kw+:", StringComparison.Ordinal) || attribute.Length <= "kw+:".Length))
		{
			throw new ArgumentException("Atributo de máquina desconocido.", "attribute");
		}
	}

	public CardMachineState Clone()
	{
		CardMachineState cardMachineState = new CardMachineState
		{
			Property = Property,
			PropertyGrowth = PropertyGrowth
		};
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
		if (Property != null)
		{
			jsonObject["p"] = new JsonObject
			{
				["id"] = Property,
				["g"] = PropertyGrowth
			};
		}
		return jsonObject.ToJsonString();
	}

	public static CardMachineState FromJson(string json)
	{
		try
		{
			JsonObject jsonObject = (JsonNode.Parse(json) as JsonObject) ?? throw new FormatException("estado de máquina vacío");
			int version = (int)(jsonObject["v"] ?? throw new FormatException("falta la versión"));
			if (version != FormatVersion)
			{
				throw new FormatException("versión de estado no soportada: " + version);
			}
			JsonObject obj = (jsonObject["a"] as JsonObject) ?? throw new FormatException("faltan los atributos");
			CardMachineState cardMachineState = new CardMachineState();
			foreach (KeyValuePair<string, JsonNode> item in obj)
			{
				ValidateAttribute(item.Key);
				int gain = (int)(item.Value ?? throw new FormatException("atributo mal formado: " + item.Key));
				if (gain < 0 || gain > MaxStoredUnits)
				{
					throw new FormatException("valores fuera de rango en " + item.Key);
				}
				cardMachineState._ledgers[item.Key] = new AttributeLedger
				{
					Gain = gain
				};
			}
			if (jsonObject["p"] is JsonObject jsonObject2)
			{
				string text = (string?)jsonObject2["id"];
				int growth = (int)(jsonObject2["g"] ?? ((JsonNode)0));
				if (growth < 0 || growth > MaxStoredUnits)
				{
					throw new FormatException("crecimiento de propiedad inválido");
				}
				if (text != null && ExceptionalCatalog.Find(text) != null)
				{
					cardMachineState.Property = text;
					cardMachineState.PropertyGrowth = growth;
				}
			}
			cardMachineState.Prune();
			return cardMachineState;
		}
		catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is OverflowException || ex is ArgumentException)
		{
			throw new FormatException("estructura de estado de máquina inválida", ex);
		}
	}
}
