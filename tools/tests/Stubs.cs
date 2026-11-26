// Stand-ins for the few game / Godot types the tested code touches (Lang.cs). Only what it calls.
using System.Collections.Generic;

namespace MegaCrit.Sts2.Core.Localization
{
	public class LocTable
	{
		public Dictionary<string, string> Merged { get; } = new Dictionary<string, string>();

		public void MergeWith(Dictionary<string, string> d)
		{
			foreach (KeyValuePair<string, string> pair in d)
			{
				Merged[pair.Key] = pair.Value;
			}
		}
	}

	public class LocManager
	{
		public static LocManager Instance;

		public string Language = "eng";

		private readonly Dictionary<string, LocTable> _tables = new Dictionary<string, LocTable>();

		public LocTable GetTable(string name)
		{
			if (!_tables.TryGetValue(name, out LocTable table))
			{
				_tables[name] = table = new LocTable();
			}
			return table;
		}
	}
}

namespace Godot
{
	public static class GD
	{
		public static void PushWarning(string s)
		{
		}
	}
}
