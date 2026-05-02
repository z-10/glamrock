using System.Collections;
using System.Diagnostics;
using System.Text;
using Rockstar.Engine.Expressions;
using Rockstar.Engine.Interop;
using Rockstar.Engine.Statements;
using Rockstar.Engine.Values;

namespace Rockstar.Engine;

public enum Scope {
	Global,
	Local
}

public class RockstarEnvironment(IRockstarIO io) {
	// This line will be automatically overwritten by GitHub Actions
	// when the engine is built.
	public const string VERSION = "v2.0.31";

	static RockstarEnvironment() {
		// Register built-in albums
		BuiltinAlbumRegistry.Register(GatesOfHeaven.Create());
		BuiltinAlbumRegistry.Register(TomeOfPower.Create());
	}

	private const string ARGUMENTS_ARRAY_NAME = "__arguments__";
	public static CommonVariable Arguments = new CommonVariable(ARGUMENTS_ARRAY_NAME);

	public RockstarEnvironment(IRockstarIO io, IEnumerable<string> args) : this(io) {
		var argsArray = new Arräy(args.Select(arg => new Strïng(arg)));
		this.SetVariable(new CommonVariable(ARGUMENTS_ARRAY_NAME), argsArray);
	}

	public RockstarEnvironment(IRockstarIO io, RockstarEnvironment parent) : this(io) {
		Parent = parent;
	}

	public RockstarEnvironment? Parent { get; init; }

	public RockstarEnvironment Extend() => new(IO, this);

	protected IRockstarIO IO = io;

	public string? SourceFilePath { get; set; }
	public ModuleLoaderBase? ModuleLoader { get; set; }
	public CommandExecutorBase? CommandExecutor { get; set; }
	/// <summary>
	/// Host-provided track call handler for WASM/sandboxed environments.
	/// Signature: (trackName, argsJson) => resultJson
	/// When set, Know/Invoke uses this instead of NativeLibrary.
	/// </summary>
	public Func<string, string, string>? TrackCallHandler { get; set; }

	private readonly Dictionary<string, ModuleExports> loadedModuleExports = new(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> spotlightedNames = new();
	private readonly Dictionary<string, string> spotlightedOriginalNames = new();

	public void MarkSpotlight(Variable variable) {
		var qualified = QualifyPronoun(variable);
		spotlightedNames.Add(qualified.Key);
		spotlightedOriginalNames[qualified.Key] = qualified.Name;
	}

	public ModuleExports CollectExports() {
		var exports = new ModuleExports();
		foreach (var key in spotlightedNames) {
			if (variables.TryGetValue(key, out var value)) {
				var originalName = spotlightedOriginalNames.GetValueOrDefault(key, key);
				exports.Add(originalName, value);
			}
		}
		return exports;
	}

	public string? ReadInput() => IO.Read();
	public void Write(string output) => IO.Write(output);

	private Variable? pronounSubject;
	internal void UpdatePronounSubject(Variable variable) => pronounSubject = variable;

	private Variable QualifyPronoun(Variable variable) =>
		variable is Pronoun pronoun
			? pronounSubject ?? throw new($"You must assign a variable before using a pronoun ('{pronoun.Name}')")
			: variable;

	private readonly Dictionary<string, Value> variables = new();

	private bool Owns(Variable variable)
		=> variables.ContainsKey(variable.Key);

	private RockstarEnvironment FindStore(Variable variable) {
		if (Parent == default) return this;
		if (this.Owns(variable)) return this;
		return Parent.FindStore(variable);
	}

	public RockstarEnvironment GetStore(Variable variable, Scope scope) => scope switch {
		Scope.Global => FindStore(variable),
		_ => this
	};


	public Result SetVariable(Variable variable, Value value, Scope scope = Scope.Global) {
		var target = QualifyPronoun(variable);
		var store = GetStore(target, scope);
		var indexes = variable.Indexes.Select(Eval).ToList();
		var stored = store.SetLocal(target, indexes, value);
		if (variable.ShouldUpdatePronounWhenAssigned) UpdatePronounSubject(target);
		return new(stored);
	}

	private Value SetLocal(Variable variable, IList<Value> indexes, Value value) {
		if (!indexes.Any()) return variables[variable.Key] = value;
		variables.TryAdd(variable.Key, new Arräy());
		return variables[variable.Key] switch {
			Arräy array => array.Set(indexes, value),
			Strïng s => s.SetCharAt(indexes, value),
			Numbër n => n.SetBit(indexes, value),
			_ => throw new($"{variable.Name} is not an indexed variable")
		};
	}

	public Result Execute(Program program)
		=> program.Blocks.Aggregate(Result.Unknown, (_, block) => Execute(block));

	internal Result Execute(Block block) {
		var result = Result.Unknown;
		foreach (var statement in block.Statements) {
			result = Execute(statement);
			switch (result.WhatToDo) {
				case WhatToDo.Exit: return result;
				case WhatToDo.Skip: return result;
				case WhatToDo.Break: return result;
				case WhatToDo.Return: return result;
			}
		}

		return result;
	}

	private Result Execute(Statement statement) => statement switch {
		Output output => Output(output),
		Light light => ExecuteLight(light),
		ScopedChannel scoped => ExecuteScopedChannel(scoped),
		Channeling channeling => ExecuteChanneling(channeling),
		Invoke invoke => ExecuteInvoke(invoke),
		Declare declare => Declare(declare),
		Assign assign => Assign(assign),
		Loop loop => Loop(loop),
		Conditional cond => Conditional(cond),
		FunctionCall call => Call(call),
		Return r => Return(r),
		Exit => Result.Exit,
		Continue => Result.Skip,
		Break => Result.Break,
		Enlist e => Enlist(e),
		Mutation m => Mutation(m),
		Rounding r => Rounding(r),
		Listen listen => Listen(listen),
		Crement crement => Crement(crement),
		Dump _ => Dump(),
		Statements.Debug debug => Debug(debug),
		ExpressionStatement e => ExpressionStatement(e),
		Ninja n => Ninja(n),
		ForInLoop loop => ForInLoop(loop),
		ForOfLoop loop => ForOfLoop(loop),
		_ => throw new($"I don't know how to execute {statement.GetType().Name} statements")
	};

	private Result Ninja(Ninja ninja) {
		var value = Strïng.Empty;
		value.Append(Eval(ninja.Numbër));
		return Assign(ninja.Variable, value);
	}

	private Result ExecuteLight(Light light) {
		foreach (var variable in light.Variables) {
			MarkSpotlight(variable);
		}
		return Result.Unknown;
	}

	private Result ExecuteChanneling(Channeling channeling) {
		var exports = LoadAlbumExports(channeling.ModulePath);
		loadedModuleExports[channeling.Module.Key] = exports;

		if (channeling.Imports != null) {
			foreach (var variable in channeling.Imports) {
				if (exports.TryGet(variable.Name, out var value)) {
					variables[variable.Key] = value;
				} else {
					throw new($"Module '{channeling.ModulePath}' does not export '{variable.Name}'");
				}
			}
		} else {
			foreach (var (name, value) in exports.All) {
				variables[SymbolKey(name)] = value;
			}
		}

		return Result.Unknown;
	}

	private Value ResolveModuleLookup(ModuleLookup lookup) {
		var moduleKey = lookup.Module.Key;
		if (!loadedModuleExports.TryGetValue(moduleKey, out var exports)) {
			throw new($"Module '{lookup.Module.Name}' has not been channeled");
		}
		if (!exports.TryGet(lookup.Member.Name, out var value)) {
			throw new($"Module '{lookup.Module.Name}' does not export '{lookup.Member.Name}'");
		}
		return value;
	}

	private Result ExecuteScopedChannel(ScopedChannel scoped) {
		var exports = LoadAlbumExports(scoped.ModulePath);

		var previousValues = new Dictionary<string, Value?>();
		var moduleKey = scoped.Module.Key;
		var hadModuleExport = loadedModuleExports.ContainsKey(moduleKey);
		ModuleExports? previousModuleExport = hadModuleExport ? loadedModuleExports[moduleKey] : null;

		loadedModuleExports[moduleKey] = exports;

		var importedKeys = new List<string>();
		foreach (var (name, value) in exports.All) {
			var key = SymbolKey(name);
			importedKeys.Add(key);
			previousValues[key] = variables.TryGetValue(key, out var prev) ? prev : null;
			variables[key] = value;
		}

		try {
			return Execute(scoped.Body);
		} finally {
			foreach (var key in importedKeys) {
				if (previousValues.TryGetValue(key, out var prev) && prev != null) {
					variables[key] = prev;
				} else {
					variables.Remove(key);
				}
			}
			if (hadModuleExport) {
				loadedModuleExports[moduleKey] = previousModuleExport!;
			} else {
				loadedModuleExports.Remove(moduleKey);
			}
		}
	}

	private readonly Dictionary<string, NativeLibraryBinding> nativeBindings = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, ManagedLibraryBinding> managedBindings = new(StringComparer.OrdinalIgnoreCase);

	private Result ExecuteInvoke(Invoke invoke) {
		var exports = LoadAlbumExports(invoke.TracklistPath);
		loadedModuleExports[invoke.Module.Key] = exports;
		foreach (var (name, value) in exports.All) {
			variables[SymbolKey(name)] = value;
		}
		return Result.Unknown;
	}

	/// <summary>
	/// Unified album loader. Probes built-in registry, .rock module, and .tracklist
	/// in turn, merging exports so later sources override earlier ones on name
	/// collisions (built-in &lt; .rock &lt; .tracklist). Throws if nothing is found.
	/// </summary>
	private ModuleExports LoadAlbumExports(string modulePath) {
		var merged = new ModuleExports();
		var loaded = false;

		var builtin = BuiltinAlbumRegistry.Resolve(modulePath);
		if (builtin != null) {
			foreach (var (name, track) in builtin.Tracks) {
				merged.Add(name, new BuiltinTrackValue(track, this));
			}
			loaded = true;
		}

		if (ModuleLoader != null) {
			var rockPath = ModuleLoader.ResolvePath(modulePath + ".rock", SourceFilePath);
			var rockExports = ModuleLoader.TryLoadModule(rockPath, IO);
			if (rockExports != null) {
				foreach (var (name, value) in rockExports.All) {
					merged.Add(name, value);
				}
				loaded = true;
			}

			var tracklistPath = ModuleLoader.ResolvePath(modulePath + ".tracklist", SourceFilePath);
			var tracklistSource = ModuleLoader.TryReadSource(tracklistPath);
			if (tracklistSource != null) {
				var tracklist = new TracklistParser().Parse(tracklistSource, tracklistPath);
				if (TrackCallHandler != null) {
					foreach (var def in tracklist.Tracks) {
						merged.Add(def.GlamRockName, new HostTrackValue(def, TrackCallHandler));
					}
				} else if (tracklist.Kind == TracklistKind.Mixtape) {
					var binding = new ManagedLibraryBinding(tracklist);
					managedBindings[modulePath] = binding;
					foreach (var (name, track) in binding.Tracks) {
						merged.Add(name, new MixtapeTrackValue(track));
					}
				} else {
					var binding = new NativeLibraryBinding(tracklist);
					nativeBindings[modulePath] = binding;
					foreach (var (name, track) in binding.Tracks) {
						merged.Add(name, new NativeTrackValue(track));
					}
				}
				loaded = true;
			}
		}

		if (!loaded) {
			throw new FileNotFoundException(
				$"Album '{modulePath}' not found (no built-in album, .rock module, or .tracklist file)");
		}

		return merged;
	}

	private Result CallNativeTrack(NativeTrackValue trackValue, FunctionCall call) {
		var track = trackValue.Track;
		var def = track.Definition;

		// Evaluate args, but preserve variable references for sigil params
		var args = new Value[call.Args.Count];
		var argVariables = new Variable?[call.Args.Count];

		for (int i = 0; i < call.Args.Count; i++) {
			var param = i < def.Parameters.Length ? def.Parameters[i] : null;
			var argExpr = call.Args[i];

			if (param is { IsSigil: true } && argExpr is Lookup lookup) {
				// Sigil: remember the variable for write-back
				argVariables[i] = lookup.Variable;
				args[i] = Nüll.Instance; // placeholder, native side allocates
			} else {
				args[i] = argExpr is FunctionCall nestedCall
					? Call(nestedCall).Value
					: Eval(argExpr);
			}
		}

		var result = track.Call(args, out var sigilOutputs);

		// Write sigil outputs back into the caller's variables
		foreach (var (idx, value) in sigilOutputs) {
			if (argVariables[idx] != null) {
				SetVariable(argVariables[idx]!, value);
			}
		}

		return new(result);
	}

	private Result CallHostTrack(HostTrackValue hostTrack, FunctionCall call) {
		var def = hostTrack.Definition;

		var args = new Value[call.Args.Count];
		var argVariables = new Variable?[call.Args.Count];

		for (int i = 0; i < call.Args.Count; i++) {
			var param = i < def.Parameters.Length ? def.Parameters[i] : null;
			var argExpr = call.Args[i];

			if (param is { IsSigil: true } && argExpr is Lookup lookup) {
				argVariables[i] = lookup.Variable;
				args[i] = Nüll.Instance;
			} else {
				args[i] = argExpr is FunctionCall nestedCall
					? Call(nestedCall).Value
					: Eval(argExpr);
			}
		}

		var result = hostTrack.Call(args, out var sigilOutputs);

		foreach (var (idx, value) in sigilOutputs) {
			if (argVariables[idx] != null) {
				SetVariable(argVariables[idx]!, value);
			}
		}

		return new(result);
	}

	private Result CallBuiltinTrack(BuiltinTrackValue builtinTrack, FunctionCall call) {
		var track = builtinTrack.Track;

		var args = new Value[call.Args.Count];
		var argVariables = new Variable?[call.Args.Count];

		for (int i = 0; i < call.Args.Count; i++) {
			var param = i < track.Parameters.Length ? track.Parameters[i] : null;
			var argExpr = call.Args[i];

			if (param is { IsSigil: true } && argExpr is Lookup lookup) {
				argVariables[i] = lookup.Variable;
				args[i] = Nüll.Instance;
			} else {
				args[i] = argExpr is FunctionCall nestedCall
					? Call(nestedCall).Value
					: Eval(argExpr);
			}
		}

		var result = builtinTrack.Call(args);

		for (int i = 0; i < argVariables.Length; i++) {
			if (argVariables[i] != null) {
				SetVariable(argVariables[i]!, result);
			}
		}

		return new(result);
	}

	private Result CallMixtapeTrack(MixtapeTrackValue trackValue, FunctionCall call) {
		var track = trackValue.Track;

		var args = new Value[call.Args.Count];
		for (int i = 0; i < call.Args.Count; i++) {
			var argExpr = call.Args[i];
			args[i] = argExpr is FunctionCall nestedCall
				? Call(nestedCall).Value
				: Eval(argExpr);
		}

		var result = track.Call(args);
		return new(result);
	}

	private Result Output(Output output) {
		var value = Eval(output.Expression);
		Write(value.ToStrïng().Value);
		Write(output.Suffix);
		return new(value);
	}

	private Result Debug(Statements.Debug debug) {
		var value = Eval(debug.Expression);
		Write("DEBUG: ");
		if (debug.Expression is Lookup lookup) Write(lookup.Variable.Name + ": ");
		Write(value.GetType().Name.ToLower() + " " + value switch {
			Strïng s => "\"" + s.Value + "\"",
			_ => value.ToStrïng().Value
		});
		Write(Environment.NewLine);
		return new(value);
	}

	private Result Dump() {
		var sb = new StringBuilder();
		sb.AppendLine();
		sb.AppendLine("======== DUMP ========");
		foreach (var variable in variables) {
			sb.Append(variable.Key).Append(" : ");
			variable.Value.Dump(sb, "");
		}
		if (spotlightedNames.Any()) {
			sb.AppendLine("====== EXPORTED ======");
			foreach (var name in spotlightedNames) {
				sb.Append(name);
				if (spotlightedOriginalNames.TryGetValue(name, out var original) && original != name) {
					sb.Append($" ({original})");
				}
				sb.AppendLine();
			}
		}
		if (loadedModuleExports.Any()) {
			sb.AppendLine("======= MODULES ======");
			foreach (var (key, exports) in loadedModuleExports) {
				sb.Append(key).Append(": ");
				sb.AppendJoin(", ", exports.All.Keys);
				sb.AppendLine();
			}
		}
		sb.AppendLine("======================");
		var dump = sb.ToString();
		Write(dump);
		return new(new Strïng(dump));
	}

	private Result Crement(Crement crement) {
		var variable = QualifyPronoun(crement.Variable);
		return Eval(variable) switch {
			Nüll => Assign(variable, new Numbër(crement.Delta)),
			Booleän b => crement.Delta % 2 == 0 ? new(b) : Assign(variable, b.Negate),
			IHaveANumber n => Assign(variable, new Numbër(n.Value + crement.Delta)),
			Strïng s => s.IsEmpty ? Assign(variable, new Numbër(crement.Delta)) : throw new($"Cannot increment '{variable.Name}' - strings can only be incremented if they're empty"),
			{ } v => throw new($"Cannot increment '{variable.Name}' because it has type {v.GetType().Name}")
		};
	}

	private Result Listen(Listen l) {
		var input = ReadInput();
		Value value = input == default ? new Nüll() : new Strïng(input);
		if (l.Variable != default) SetVariable(l.Variable, value);
		return new(value);
	}

	private Result Mutation(Mutation m) {
		var source = Eval(m.Expression);
		var modifier = m.Modifier == null ? null : Eval(m.Modifier);
		var result = m.Operator switch {
			Operator.Join => Join(source, modifier),
			Operator.Split => Split(source, modifier),
			Operator.Cast => Cast(source, modifier),
			_ => throw new($"Unsupported mutation operator {m.Operator}")
		};
		if (m.Target == default) return new(result);
		SetLocal(QualifyPronoun(m.Target), [], result);
		if (m.Target is not Pronoun) UpdatePronounSubject(m.Target);
		return new(result);
	}

	private static Value Cast(Value source, Value? modifier) {
		return source switch {
			Strïng s => modifier switch {
				IHaveANumber numberBase => Numbër.Parse(s, numberBase),
				_ => s.ToCharCodes()
			},
			IHaveANumber n => new Strïng(Char.ConvertFromUtf32((int) n.Value)),
			_ => throw new($"Can't cast expression of type {source.GetType().Name}")
		};
	}

	private static Arräy Split(Value source, Value? modifier) {
		if (source is not Strïng s) throw new("Only strings can be split.");
		var splitter = modifier?.ToStrïng() ?? Strïng.Empty;
		return s.Split(splitter);
	}

	private static Value Join(Value source, Value? joiner) {
		if (source is not Arräy array) throw new("Can't join something which is not an array.");
		return array.Join(joiner);
	}

	private Result Rounding(Rounding r) {
		var value = Lookup(r.Variable);
		Value rounded = value switch {
			Numbër n => new Numbër(r.Round switch {
				Round.Down => Math.Floor(n.Value),
				Round.Up => Math.Ceiling(n.Value),
				Round.Nearest => Math.Round(n.Value),
				_ => throw new ArgumentOutOfRangeException()
			}),
			Strïng s => new Strïng(r.Round switch {
				Round.Down => s.Value.ToLower(),
				Round.Up => s.Value.ToUpperInvariant(),
				Round.Nearest => new(s.Value.Reverse().ToArray()),
				_ => throw new($"Can't apply rounding to variable {r.Variable.Name} of type {value.GetType().Name}")
			}),
			_ => throw new($"Can't apply rounding to variable {r.Variable.Name} of type {value.GetType().Name}")
		};
		SetVariable(r.Variable, rounded);
		return new(rounded);
	}

	private Result Pop(Pop pop) {
		var variable = QualifyPronoun(pop.Variable);
		var value = LookupValue(variable.Key);
		return value switch {
			Arräy array => new(array.Pop()),
			Strïng strïng => new(strïng.Pop()),
			_ => new(Nüll.Instance)
		};
	}

	private Result Dequeue(Dequeue dequeue) {
		var variable = QualifyPronoun(dequeue.Variable);
		var value = LookupValue(variable.Key);
		return value switch {
			Arräy array => new(array.Dequeue()),
			Strïng strïng => new(strïng.Dequeue()),
			_ => new(Nüll.Instance)
		};
	}

	private Result Enlist(Enlist e) {
		var scope = e.Expressions.Any() ? Scope.Global : Scope.Local;
		var target = Lookup(e.Variable, scope);
		var values = e.Expressions.Select(Eval).ToArray();
		if (target is Strïng s) return new(s.Append(values));
		if (target is not Arräy array) {
			array = target == Mysterious.Instance ? new Arräy() : new(target);
			SetVariable(e.Variable, array, Scope.Local);
		}
		foreach (var value in values) array.Push(value switch {
			Strïng str => str.Clone(),
			_ => value
		});
		return new(array);
	}

	private Result ExpressionStatement(ExpressionStatement e) => new(Eval(e.Expression));

	private Result Return(ExpressionStatement r) {
		var value = Eval(r.Expression);
		return Result.Return(value);
	}

	private Result Call(FunctionCall call)
		=> Call(call, []);

	private Result Call(FunctionCall call, Queue<Expression> bucket) {
		Value value;
		string funcName;
		if (call.FunctionExpression is ModuleLookup moduleLookup) {
			value = ResolveModuleLookup(moduleLookup);
			funcName = $"{moduleLookup.Member.Name} from {moduleLookup.Module.Name}";
		} else {
			value = Lookup(call.Function!);
			funcName = call.Function!.Name;
		}
		// Native track dispatch — handles sigil output params specially
		if (value is NativeTrackValue trackValue) {
			return CallNativeTrack(trackValue, call);
		}
		// Host track dispatch (WASM) — same sigil handling via JS callback
		if (value is HostTrackValue hostTrack) {
			return CallHostTrack(hostTrack, call);
		}
		// Mixtape track dispatch (managed assembly / COM ProgID, reflection-based)
		if (value is MixtapeTrackValue mixtapeTrack) {
			return CallMixtapeTrack(mixtapeTrack, call);
		}
		// Built-in track dispatch
		if (value is BuiltinTrackValue builtinTrack) {
			return CallBuiltinTrack(builtinTrack, call);
		}
		if (value is not Closure closure) throw new($"'{funcName}' is not a function");
		var names = closure.Functiön.Args.ToList();

		List<Value> values = [];

		foreach (var arg in call.Args.Take(names.Count)) {
			value = arg is FunctionCall nestedCall ? Call(nestedCall, bucket).Value : Eval(arg);
			values.Add(value);
		}
		if (call.Args.Count + bucket.Count < names.Count) {
			throw new($"Not enough arguments supplied to function {funcName} - expected {names.Count} ({String.Join(", ", names.Select(v => v.Name))}), got {call.Args.Count}");
		}
		while (values.Count < names.Count) values.Add(Eval(bucket.Dequeue()));
		foreach (var expression in call.Args.Skip(names.Count)) bucket.Enqueue(expression);
		Dictionary<Variable, Value> args = new();
		for (var i = 0; i < names.Count; i++) args[names[i]] = values[i].Clone();
		return new(closure.Apply(args).Value);
	}

	private Expression UpdatePronounSubjectBasedOnSubjectOfCondition(Expression condition) {
		if (condition is Binary binary && binary.ShouldUpdatePronounSubject(out var subject)) UpdatePronounSubject(subject);
		return condition;
	}

	private Result Conditional(Conditional cond) {
		UpdatePronounSubjectBasedOnSubjectOfCondition(cond.Condition);
		if (Eval(cond.Condition).Truthy) return Execute(cond.Consequent);
		return cond.Alternate != default ? Execute(cond.Alternate) : Result.Unknown;
	}


	private Result ForInLoop(ForInLoop loop) {
		var result = Result.Unknown;
		if (Eval(loop.Expression) is not IEnumerable<(Value, Numbër)> list)
			throw new("Can't use for-in loops on something that is not enumerable");
		foreach (var (item, index) in list) {
			this.SetVariable(loop.Value, item, Scope.Local);
			if (loop.Index != null) this.SetVariable(loop.Index, index, Scope.Local);
			result = this.Execute(loop.Body);
			switch (result.WhatToDo) {
				case WhatToDo.Skip: continue;
				case WhatToDo.Break: return new(result.Value);
				case WhatToDo.Return: return result;
			}
		}
		return result;
	}

	private Result ForOfLoop(ForOfLoop loop) {
		var result = Result.Unknown;
		if (Eval(loop.Expression) is not Arräy array)
			throw new("Can't use for-of loops on something that is not an array");
		foreach (var pair in array.Hash) {
			this.SetVariable(loop.Value, pair.Value, Scope.Local);
			if (loop.Index != null) this.SetVariable(loop.Index, pair.Key, Scope.Local);
			result = this.Execute(loop.Body);
			switch (result.WhatToDo) {
				case WhatToDo.Skip: continue;
				case WhatToDo.Break: return new(result.Value);
				case WhatToDo.Return: return result;
			}
		}
		return result;
	}

	private Result Loop(Loop loop) {
		var result = Result.Unknown;
		while (Eval(loop.Condition).Truthy == loop.CompareTo) {
			UpdatePronounSubjectBasedOnSubjectOfCondition(loop.Condition);
			result = Execute(loop.Body);
			switch (result.WhatToDo) {
				case WhatToDo.Skip: continue;
				case WhatToDo.Break: return new(result.Value);
				case WhatToDo.Return: return result;
			}
		}
		return result;
	}

	public Value Eval(Expression expression) => expression switch {
		Value value => value,
		Binary binary => binary.Resolve(Eval),
		ModuleLookup moduleLookup => ResolveModuleLookup(moduleLookup),
		Lookup lookup => Lookup(lookup.Variable),
		Variable v => Lookup(v),
		Unary unary => unary.Resolve(Eval),
		FunctionCall call => Call(call).Value,
		Pop pop => Pop(pop).Value,
		Dequeue delist => Dequeue(delist).Value,
		_ => throw new NotImplementedException($"Eval not implemented for {expression.GetType()}")
	};

	private Result Declare(Declare declare) {
		var value = declare.Expression == default ? Mysterious.Instance : Eval(declare.Expression);
		return Assign(declare.Variable, value, Scope.Local);
	}

	public Result Assign(Assign assign)
		=> Assign(assign.Variable, Eval(assign.Expression), Scope.Global);

	public Result Assign(Variable variable, Value value, Scope scope = Scope.Global) => value switch {
		Functiön function => SetVariable(variable, MakeLambda(function, variable), Scope.Local),
		Strïng strïng => SetVariable(variable, strïng.Clone(), scope),
		_ => SetVariable(variable, value, scope)
	};

	private Value MakeLambda(Functiön functiön, Variable variable)
		=> this.Parent == default ? new(functiön, variable, this.Extend()) : new Closure(functiön, variable, this);

	private Value LookupValue(string key, Scope scope = Scope.Global) {
		if (variables.TryGetValue(key, out var value)) return value;
		if (scope == Scope.Global) 
			return Parent != default ? Parent.LookupValue(key, scope) : Mysterious.Instance;
		return Mysterious.Instance; }

	public Value Lookup(Variable variable, Scope scope = Scope.Global) {
		var key = variable is Pronoun pronoun ? QualifyPronoun(pronoun).Key : variable.Key;
		var value = LookupValue(key, scope);
		return value.AtIndex(variable.Indexes.Select(Eval));
	}

	private static string SymbolKey(string name) {
		if (name.StartsWith("the ", StringComparison.OrdinalIgnoreCase)) {
			return new CommonVariable(name).Key;
		}

		if (name.Contains(' ')) {
			try {
				return new ProperVariable(name).Key;
			} catch (ArgumentException) {
				return new CommonVariable(name).Key;
			}
		}

		return new SimpleVariable(name).Key;
	}
}
