using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tinker.Tests;

/// <summary>Paths: the repository (found from the test binary) and the installed game.</summary>
internal static class Repo
{
	public static string Root { get; } = FindRoot();

	public static string Src => Path.Combine(Root, "src");

	public static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "data", name);

	/// <summary>The game's sts2.dll, or null if the game isn't installed where expected.</summary>
	public static string GameDll
	{
		get
		{
			string dir = Environment.GetEnvironmentVariable("STS2_DIR") ?? @"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2";
			string dll = Path.Combine(dir, "data_sts2_windows_x86_64", "sts2.dll");
			return File.Exists(dll) ? dll : null;
		}
	}

	/// <summary>The mod's own C# files (relative path, text).</summary>
	public static IEnumerable<(string Path, string Text)> SourceFiles()
	{
		return Directory.GetFiles(Src, "*.cs", SearchOption.AllDirectories)
			.OrderBy(p => p, StringComparer.Ordinal)
			.Select(p => (Path.GetRelativePath(Root, p).Replace('\\', '/'), File.ReadAllText(p)));
	}

	private static string FindRoot()
	{
		string dir = AppContext.BaseDirectory;
		while (dir != null && !File.Exists(Path.Combine(dir, "Tinker.csproj")))
		{
			dir = Path.GetDirectoryName(dir);
		}
		return dir ?? throw new InvalidOperationException("Tinker.csproj not found above " + AppContext.BaseDirectory);
	}
}
