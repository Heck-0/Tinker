// The game's compiled code as the rule engine's input (Instr / MethodCode / ItemCode), read with Mono.Cecil.
// Shared by tools/rulecheck and tools/tests; the mod itself reads the same thing through Harmony (GameRules).
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Tinker.Common.Rules;

namespace Tinker.Tools;

public sealed class CecilCode
{
	private readonly Dictionary<string, TypeDefinition> _types;

	/// <summary>Types from mods (the extra DLLs), by class name.</summary>
	private readonly Dictionary<string, TypeDefinition> _modTypes = new Dictionary<string, TypeDefinition>();

	private readonly Dictionary<string, int> _powerTypes = new Dictionary<string, int>();

	/// <param name="modDlls">Mods whose cards, relics and powers to read too (e.g. HextechRunes.dll).</param>
	public CecilCode(string sts2Dll, params string[] modDlls)
	{
		ModuleDefinition module = ModuleDefinition.ReadModule(sts2Dll);
		_types = module.GetTypes().ToDictionary(t => t.FullName);
		foreach (string dll in modDlls)
		{
			foreach (TypeDefinition t in ModuleDefinition.ReadModule(dll).GetTypes())
			{
				_modTypes.TryAdd(t.Name, t);
			}
		}
	}

	public TypeDefinition Type(string fullName) => _types.TryGetValue(fullName, out TypeDefinition t) ? t : null;

	private TypeDefinition Find(string ns, string name) => Type(ns + name) ?? (_modTypes.TryGetValue(name, out TypeDefinition t) ? t : null);

	public TypeDefinition Card(string name) => Find("MegaCrit.Sts2.Core.Models.Cards.", name);

	public TypeDefinition Relic(string name) => Find("MegaCrit.Sts2.Core.Models.Relics.", name);

	public TypeDefinition Power(string name) => Find("MegaCrit.Sts2.Core.Models.Powers.", name);

	/// <summary>
	/// Every concrete card and relic class: the game's (<paramref name="mods"/> false) or the mods' (true), as
	/// ("card" | "relic", type), sorted by name.
	/// </summary>
	public IEnumerable<(string Kind, TypeDefinition Type)> Items(bool mods)
	{
		IEnumerable<TypeDefinition> types = mods ? _modTypes.Values : _types.Values;
		// Mock* are the game's test cards, not in a real run.
		foreach (TypeDefinition t in types.Where(t => t.IsClass && !t.IsAbstract && !t.IsNested && !t.Name.StartsWith("Mock", System.StringComparison.Ordinal)).OrderBy(t => t.Name, System.StringComparer.Ordinal))
		{
			string kind = Chain(t).Skip(1).Select(d => d.FullName).FirstOrDefault(n => n is "MegaCrit.Sts2.Core.Models.CardModel" or "MegaCrit.Sts2.Core.Models.RelicModel") switch
			{
				"MegaCrit.Sts2.Core.Models.CardModel" => "card",
				"MegaCrit.Sts2.Core.Models.RelicModel" => "relic",
				_ => null
			};
			if (kind != null)
			{
				yield return (kind, t);
			}
		}
	}

	/// <summary>Values that aren't numbers: never upgradable, so not listed.</summary>
	private static readonly HashSet<string> NotNumbers = new HashSet<string> { "BoolVar", "StringVar" };

	/// <summary>
	/// A card's or relic's values as its CanonicalVars getter creates them (nearest override up the class chain, and
	/// a static helper it calls, e.g. a mod's CreateDamageVars(1.2m)): name, DynamicVar class and, for PowerVar&lt;T&gt;,
	/// T. A value's name is the string its constructor gets, else the one its class passes to DynamicVar (DamageVar:
	/// "Damage"), else for PowerVar&lt;T&gt; the power's name.
	/// </summary>
	public List<VarInfo> Vars(TypeDefinition type)
	{
		List<VarInfo> vars = new List<VarInfo>();
		MethodDefinition getter = Chain(type).Select(d => d.Methods.FirstOrDefault(m => m.Name == "get_CanonicalVars" && m.HasBody)).FirstOrDefault(m => m != null);
		if (getter == null)
		{
			return vars;
		}
		AddVars(getter, vars, 0);
		return vars;
	}

	private void AddVars(MethodDefinition getter, List<VarInfo> vars, int depth)
	{
		List<Instr> body = Convert(getter);
		var raw = getter.Body.Instructions;
		for (int i = 0; i < raw.Count; i++)
		{
			if (depth == 0 && raw[i].OpCode.Code == Code.Call && raw[i].Operand is MethodReference helper && helper.ReturnType.Name.StartsWith("IEnumerable", System.StringComparison.Ordinal)
				&& helper.Resolve() is MethodDefinition hd && hd.HasBody && hd.IsStatic)
			{
				AddVars(hd, vars, depth + 1);
				continue;
			}
			if (raw[i].OpCode.Code != Code.Newobj || raw[i].Operand is not MethodReference ctor)
			{
				continue;
			}
			TypeReference varType = ctor.DeclaringType;
			TypeDefinition varDef = (varType is GenericInstanceType gi ? gi.ElementType : varType) as TypeDefinition ?? Type(Strip(varType.FullName));
			if (varDef == null || !Chain(varDef).Any(d => d.FullName == "MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar"))
			{
				continue;
			}
			string cls = Clean(varDef.Name);
			if (NotNumbers.Contains(cls))
			{
				continue;
			}
			string power = (varType is GenericInstanceType g && cls == "PowerVar") ? Clean(g.GenericArguments[0].Name) : null;
			string name = null;
			if (ctor.Parameters.Count > 0 && ctor.Parameters[0].ParameterType.FullName == "System.String")
			{
				(int s, int e) arg = RuleEngine.ArgSpan(body, i, 0);
				if (arg.s >= 0 && arg.s == arg.e && body[arg.s].Op == "ldstr")
				{
					name = body[arg.s].Str;
				}
			}
			name ??= DefaultName(varDef, ctor) ?? power;
			if (name != null && vars.All(v => v.Name != name))
			{
				vars.Add(new VarInfo { Name = name, Class = cls, Power = power });
			}
		}
	}

	private static string Strip(string fullName)
	{
		int i = fullName.IndexOf('<');
		return (i < 0) ? fullName : fullName.Substring(0, i);
	}

	/// <summary>The name a DynamicVar subclass's constructor gives the value (its first string constant), or null.</summary>
	private static string DefaultName(TypeDefinition varDef, MethodReference ctor)
	{
		MethodDefinition c = varDef.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == ctor.Parameters.Count
			&& m.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(ctor.Parameters.Select(p => p.ParameterType.Name)));
		return c?.Body?.Instructions.FirstOrDefault(x => x.OpCode.Code == Code.Ldstr)?.Operand as string;
	}

	/// <summary>
	/// A power's type, as the game would report it: 1 buff, -1 debuff, 0 none / unknown. From the nearest Type getter
	/// up the class chain (a mod's power often inherits it); a getter that asks IsPositive (TemporaryStrengthPower
	/// and its kind) is answered from the nearest IsPositive override.
	/// </summary>
	public int PowerType(string name)
	{
		if (!_powerTypes.TryGetValue(name, out int t))
		{
			t = 0;
			TypeDefinition power = Power(name);
			MethodDefinition getter = Chain(power).Select(d => d.Methods.FirstOrDefault(m => m.Name == "get_Type" && m.HasBody)).FirstOrDefault(m => m != null);
			if (getter != null && getter.Body.Instructions.Any(i => i.Operand is MethodReference mr && mr.Name == "get_IsPositive"))
			{
				MethodDefinition positive = Chain(power).Select(d => d.Methods.FirstOrDefault(m => m.Name == "get_IsPositive" && m.HasBody)).FirstOrDefault(m => m != null);
				Instruction value = positive?.Body.Instructions.FirstOrDefault(i => i.OpCode.Code is Code.Ldc_I4_0 or Code.Ldc_I4_1);
				t = (value == null) ? 0 : (value.OpCode.Code == Code.Ldc_I4_1) ? 1 : -1;
			}
			else
			{
				Instruction constant = getter?.Body.Instructions.FirstOrDefault(i => i.OpCode.Code is Code.Ldc_I4_1 or Code.Ldc_I4_2);
				if (constant != null)
				{
					t = (constant.OpCode.Code == Code.Ldc_I4_1) ? 1 : -1;
				}
			}
			_powerTypes[name] = t;
		}
		return t;
	}

	/// <summary>The type and its base classes, as far as they're in the loaded DLLs.</summary>
	private IEnumerable<TypeDefinition> Chain(TypeDefinition type)
	{
		for (TypeDefinition d = type; d != null; )
		{
			yield return d;
			TypeReference b = d.BaseType is GenericInstanceType g ? g.ElementType : d.BaseType;
			d = (b == null) ? null : (b as TypeDefinition) ?? Type(b.FullName);
		}
	}

	/// <summary>
	/// What the rule engine reads for a card, relic or power. A mod's class is marked modded and also brings its base
	/// classes from the same mod, as GameRules does in the game.
	/// </summary>
	public ItemCode ItemFor(TypeDefinition type)
	{
		ItemCode item = Item(type);
		if (_types.ContainsKey(type.FullName))
		{
			return item;
		}
		item.Modded = true;
		// A base class from the same DLL is a TypeDefinition; the game's (RelicModel, ...) is only a reference.
		for (TypeReference b = type.BaseType; b != null; )
		{
			if ((b is GenericInstanceType g ? g.ElementType : b) is not TypeDefinition bd || bd.Module != type.Module)
			{
				break;
			}
			AddMethods(bd, null, item.Methods);
			b = bd.BaseType;
		}
		return item;
	}

	/// <summary>The type's methods (and its compiler-made nested types' methods, under their owner's name).</summary>
	public static ItemCode Item(TypeDefinition type)
	{
		ItemCode item = new ItemCode { Name = type.Name };
		AddMethods(type, null, item.Methods);
		return item;
	}

	public static void AddMethods(TypeDefinition t, string owner, List<MethodCode> into)
	{
		foreach (MethodDefinition m in t.Methods.Where(m => m.HasBody))
		{
			into.Add(new MethodCode { Name = m.Name, Owner = owner ?? m.Name, Body = Convert(m) });
		}
		foreach (TypeDefinition n in t.NestedTypes)
		{
			Match om = Regex.Match(n.Name, @"^<(\w+)>");
			AddMethods(n, om.Success ? om.Groups[1].Value : owner, into);
		}
	}

	private static string Clean(string n)
	{
		int i = n.IndexOf('`');
		return (i < 0) ? n : n.Substring(0, i);
	}

	public static List<Instr> Convert(MethodDefinition m)
	{
		var body = m.Body.Instructions;
		var index = new Dictionary<Instruction, int>();
		for (int i = 0; i < body.Count; i++)
		{
			index[body[i]] = i;
		}
		var list = new List<Instr>();
		foreach (Instruction ins in body)
		{
			var x = new Instr { Op = ins.OpCode.Name };
			switch (ins.Operand)
			{
				case string s: x.Str = s; break;
				case sbyte sb: x.Num = sb; break;
				case int iv: x.Num = iv; break;
				case long lv: x.Num = lv; break;
				case float fv: x.Num = fv; break;
				case double dv: x.Num = dv; break;
				case Instruction t: x.Target = index[t]; break;
				case Instruction[] ts: x.Target = ts.Length > 0 ? index[ts[0]] : null; break;
				case FieldReference f: x.Str = f.Name; x.Type = Clean(f.DeclaringType.Name); break;
				case ParameterDefinition p: x.Str = p.Name; break;
				case MethodReference mr:
					x.Type = Clean(mr.DeclaringType.Name);
					x.Method = mr.Name;
					x.ParamTypes = mr.Parameters.Select(p => Clean(p.ParameterType.Name)).ToArray();
					if (mr is GenericInstanceMethod g)
					{
						x.GenericArgs = g.GenericArguments.Select(a => Clean(a.Name)).ToArray();
					}
					break;
			}
			// ldc.i4.N / ldarg.N short forms
			Match mm = Regex.Match(ins.OpCode.Name, @"^ldc\.i4\.(m1|\d)$");
			if (mm.Success)
			{
				x.Num = mm.Groups[1].Value == "m1" ? -1 : int.Parse(mm.Groups[1].Value);
			}
			Match am = Regex.Match(ins.OpCode.Name, @"^ldarg\.(\d)$");
			if (am.Success)
			{
				int a = int.Parse(am.Groups[1].Value) - (m.HasThis ? 1 : 0);
				if (a >= 0 && a < m.Parameters.Count)
				{
					x.Str = m.Parameters[a].Name;
				}
			}
			int pops = RuleEngine.StackCount(ins.OpCode.StackBehaviourPop.ToString());
			int pushes = RuleEngine.StackCount(ins.OpCode.StackBehaviourPush.ToString());
			if (ins.Operand is MethodReference sig)
			{
				bool ctor = ins.OpCode.Code == Code.Newobj;
				if (pops < 0) pops = sig.Parameters.Count + (sig.HasThis && !ctor ? 1 : 0);
				if (pushes < 0) pushes = ctor ? 1 : (sig.ReturnType.FullName == "System.Void" ? 0 : 1);
				if (ctor) { pops = sig.Parameters.Count; pushes = 1; }
			}
			if (pops < 0) pops = ins.OpCode.Code == Code.Ret ? (m.ReturnType.FullName == "System.Void" ? 0 : 1) : 0;
			if (pushes < 0) pushes = 0;
			x.Pops = pops;
			x.Pushes = pushes;
			list.Add(x);
		}
		return list;
	}
}
