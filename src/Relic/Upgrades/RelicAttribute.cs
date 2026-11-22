using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Relic;

public sealed record RelicAttribute(string Key, string Family, int Direction = 1, int Maximum = 10000, string? LabelKey = null)
{

	public string Label
	{
		get
		{
			if (!(LabelKey == "drawLoss"))
			{
				if (!(Key == "Discount") || Direction >= 0)
				{
					if (!(Key == "Stars") || Direction >= 0)
					{
						if (!(Key == "Gold") || Direction >= 0)
						{
							if (!(Key == "Energy") || Direction >= 0)
							{
								if (!(Key == "Cards") || Direction >= 0)
								{
									if (!(Key == "Damage") || Direction >= 0)
									{
										if (!(Key == "Turns") || !(Family == "duration"))
										{
											return Text.Attribute(Key);
										}
										return Text.T("Duración (turnos)", "Duration (turns)");
									}
									return Text.T("Daño propio", "Self damage");
								}
								return Text.T("Cartas necesarias", "Cards required");
							}
							return Text.T("Energía necesaria", "Energy required");
						}
						return Text.T("Oro pagado", "Gold paid");
					}
					return Text.T("Estrellas necesarias", "Stars required");
				}
				return Text.T("Precio (%)", "Price (%)");
			}
			return Text.T("Cartas no robadas", "Cards lost");
		}
	}

	public int Limit(RelicModel relic)
	{
		decimal baseValue = relic.DynamicVars[Key].BaseValue;
		// Combined build: no per-category bet limit (Catalog.MaxBet still stops at the minimum chance).
		return (int)Math.Max(0m, (Direction < 0) ? (baseValue - 1m) : ((decimal)Maximum - baseValue));
	}

	public decimal Value(RelicModel relic)
	{
		return relic.DynamicVars[Key].BaseValue;
	}
}
