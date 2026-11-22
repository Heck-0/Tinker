using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tinker.Card;

internal static class BalanceFile
{
	public const string FileName = "balance.jsonc";

	private static string _text;

	public static BalanceConfig Current { get; private set; } = new BalanceConfig();

	public static string Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BalanceFile).Assembly.Location) ?? ".", "balance.jsonc");

	public static BalanceConfig Fresh()
	{
		BalanceConfig balanceConfig = new BalanceConfig();
		if (_text != null)
		{
			balanceConfig.ApplyOverrides(_text);
		}
		return balanceConfig;
	}

	public static string Reload()
	{
		BalanceConfig balanceConfig = new BalanceConfig();
		string path = Path;
		if (!File.Exists(path))
		{
			Current = balanceConfig;
			ModEntry.Warn("No hay balance.jsonc junto al mod; se usan los valores por defecto.");
			return "Sin balance.jsonc: valores por defecto.";
		}
		List<string> list;
		try
		{
			_text = File.ReadAllText(path);
			list = balanceConfig.ApplyOverrides(_text);
		}
		catch (Exception ex)
		{
			list = new List<string> { "no se pudo leer: " + ex.Message };
		}
		Current = balanceConfig;
		Settings.Invalidate();
		foreach (string item in list)
		{
			ModEntry.Warn("balance.jsonc: " + item);
		}
		ModEntry.Info("Balance cargado:" + Environment.NewLine + Describe(balanceConfig));
		if (list.Count != 0)
		{
			return $"Balance recargado con {list.Count} aviso(s); los valores erróneos se ignoran (ver registro).";
		}
		return "Balance recargado sin avisos (detalle en el registro).";
	}

	public static string Describe(BalanceConfig config)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append($"  probabilidad entre {config.MinChance:0.##} y {config.MaxChance:0.##}");
		foreach (KeyValuePair<string, AttributeBalance> family in config.Families)
		{
			AttributeBalance value = family.Value;
			stringBuilder.Append(Environment.NewLine).Append($"  {family.Key,-9} peso {value.Weight:0.##}, curva {value.Curve:0.##}, suelo {value.ReferenceFloor:0.##}");
		}
		return stringBuilder.ToString();
	}
}
