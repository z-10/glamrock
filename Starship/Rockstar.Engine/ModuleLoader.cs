using Rockstar.Engine.Values;

namespace Rockstar.Engine;

public class ModuleExports {
	private readonly Dictionary<string, Value> exports = new(StringComparer.OrdinalIgnoreCase);

	public void Add(string name, Value value) => exports[name] = value;

	public bool TryGet(string name, out Value value) => exports.TryGetValue(name, out value!);

	public IReadOnlyDictionary<string, Value> All => exports;
}

public enum ModuleState {
	Loading,
	Loaded,
	Failed
}

public abstract class ModuleLoaderBase {
	protected readonly Dictionary<string, ModuleState> moduleStates = new();
	protected readonly Dictionary<string, ModuleExports> moduleCache = new();
	protected readonly Parser parser = new();

	/// <summary>
	/// Resolve an import path to an absolute (or host-canonical) path.
	/// Does NOT manipulate the file extension — callers pass the full filename
	/// they expect (e.g. "math_module.rock" or "gdi.tracklist").
	/// </summary>
	public abstract string ResolvePath(string importPath, string? currentFilePath);

	/// <summary>
	/// Read source for an already-resolved path. Returns null if the source
	/// does not exist (callers use this to probe optional sidecar files).
	/// </summary>
	public abstract string? TryReadSource(string resolvedPath);

	public ModuleExports? TryLoadModule(string resolvedPath, IRockstarIO io) {
		if (moduleCache.TryGetValue(resolvedPath, out var cached)) {
			return cached;
		}

		if (moduleStates.TryGetValue(resolvedPath, out var state) && state == ModuleState.Loading) {
			throw new InvalidOperationException(
				$"Circular import detected: {resolvedPath} is already being loaded");
		}

		var source = TryReadSource(resolvedPath);
		if (source == null) return null;

		moduleStates[resolvedPath] = ModuleState.Loading;

		try {
			var program = parser.Parse(source);

			var moduleEnv = new RockstarEnvironment(io) {
				SourceFilePath = resolvedPath,
				ModuleLoader = this
			};

			moduleEnv.Execute(program);

			var exports = moduleEnv.CollectExports();
			moduleCache[resolvedPath] = exports;
			moduleStates[resolvedPath] = ModuleState.Loaded;
			return exports;
		} catch {
			moduleStates[resolvedPath] = ModuleState.Failed;
			throw;
		}
	}
}

public class ModuleLoader : ModuleLoaderBase {
	public override string ResolvePath(string importPath, string? currentFilePath) {
		if (System.IO.Path.IsPathRooted(importPath)) {
			return System.IO.Path.GetFullPath(importPath);
		}

		var baseDir = currentFilePath != null
			? System.IO.Path.GetDirectoryName(currentFilePath)!
			: Directory.GetCurrentDirectory();

		return System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, importPath));
	}

	public override string? TryReadSource(string resolvedPath) {
		if (!File.Exists(resolvedPath)) return null;
		return File.ReadAllText(resolvedPath).ReplaceLineEndings();
	}
}
