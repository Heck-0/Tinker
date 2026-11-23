using System.Collections.Generic;
using System.Linq;
using System;

namespace Tinker.Common;

// Traits (exceptional properties): definitions, requirements and the catalog.

public sealed class ExceptionalDefinition
{
	public string Id { get; set; }

	public ExceptionalRequirement Requires { get; set; }


}

public enum ExceptionalRequirement
{
	Damage,
	NativeExhaust,
	Playable,
	PaidCost,
	DealtDamage
}

public static class ExceptionalCatalog
{
	public const int AwakeningPlays = 1;

	public static IReadOnlyList<ExceptionalDefinition> Default { get; } = new ExceptionalDefinition[6]
	{
		new ExceptionalDefinition
		{
			Id = "Inexhaustible",
			Requires = ExceptionalRequirement.NativeExhaust
		},
		new ExceptionalDefinition
		{
			Id = "Echo",
			Requires = ExceptionalRequirement.Playable
		},
		new ExceptionalDefinition
		{
			Id = "Awakening",
			Requires = ExceptionalRequirement.PaidCost
		},
		new ExceptionalDefinition
		{
			Id = "Legacy",
			Requires = ExceptionalRequirement.Damage
		},
		new ExceptionalDefinition
		{
			Id = "Venomous",
			Requires = ExceptionalRequirement.DealtDamage
		},
		new ExceptionalDefinition
		{
			Id = "Lifesteal",
			Requires = ExceptionalRequirement.DealtDamage
		}
	};

	public static ExceptionalDefinition Find(string id)
	{
		return Default.FirstOrDefault((ExceptionalDefinition d) => d.Id == id);
	}

	public static List<ExceptionalDefinition> Applicable(ISet<ExceptionalRequirement> meets)
	{
		return Default.Where((ExceptionalDefinition d) => meets.Contains(d.Requires) || (d.Requires == ExceptionalRequirement.DealtDamage && meets.Contains(ExceptionalRequirement.Damage))).ToList();
	}


}
