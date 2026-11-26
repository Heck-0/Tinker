using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Tinker.Tests;

/// <summary>Rules for the code itself.</summary>
public class SourceTests
{
	// Spanish left from the original mods: accented letters, or common Spanish words, in strings and comments.
	private static readonly Regex SpanishWord = new Regex(
		@"[áéíóúñ¿¡«»]|\b(debe|ser|los|las|del|una|para|cuando|que|por|con|carta|cartas|reliquia|reliquias|turno|turnos|daño|oro|aviso|avisos|tensión|entre|mejora|mejoras|está|valor|valores|número|números|también|desde|hasta|según|ningún|ninguna|ajustes|cargados|preajuste|carga|guardado|partida|jugador|enemigo|enemigos)\b",
		RegexOptions.IgnoreCase);

	private static readonly Regex StringOrComment = new Regex(@"\$?@?""(?:[^""\\]|\\.)*""|//[^\n]*");

	[Fact]
	public void NoSpanishInTheCode()
	{
		List<string> hits = new List<string>();
		foreach (var (path, text) in Repo.SourceFiles().Where(f => !f.Path.EndsWith("/Spanish.cs")))
		{
			string[] lines = text.Split('\n');
			for (int i = 0; i < lines.Length; i++)
			{
				foreach (Match m in StringOrComment.Matches(lines[i]))
				{
					if (SpanishWord.IsMatch(m.Value))
					{
						hits.Add($"{path}:{i + 1}: {m.Value.Trim()}");
					}
				}
			}
		}
		Assert.True(hits.Count == 0, hits.Count + " Spanish strings or comments:\n" + string.Join("\n", hits));
	}
}
