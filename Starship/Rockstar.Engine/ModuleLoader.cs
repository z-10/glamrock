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

public class ModuleLoader {
	private readonly Dictionary<string, ModuleState> moduleStates = new();
	private readonly Dictionary<string, ModuleExports> moduleCache = new();
	private readonly Parser parser = new();

	public static string ResolvePath(string importPath, string? currentFilePath) {
		if (System.IO.Path.IsPathRooted(importPath)) {
			return NormalizePath(importPath);
		}

		var baseDir = currentFilePath != null
			? System.IO.Path.GetDirectoryName(currentFilePath)!
			: Directory.GetCurrentDirectory();

		var combined = System.IO.Path.Combine(baseDir, importPath);
		return NormalizePath(combined);
	}

	private static string NormalizePath(string path) {
		if (!path.EndsWith(".rock", StringComparison.OrdinalIgnoreCase)) {
			path += ".rock";
		}
		return System.IO.Path.GetFullPath(path);
	}

	public ModuleExports Load(string resolvedPath, IRockstarIO io) {
		if (moduleCache.TryGetValue(resolvedPath, out var cached)) {
			return cached;
		}

		if (moduleStates.TryGetValue(resolvedPath, out var state) && state == ModuleState.Loading) {
			throw new InvalidOperationException(
				$"Circular import detected: {resolvedPath} is already being loaded");
		}

		if (!File.Exists(resolvedPath)) {
			throw new FileNotFoundException(
				$"Module not found: {resolvedPath}");
		}

		moduleStates[resolvedPath] = ModuleState.Loading;

		try {
			var source = File.ReadAllText(resolvedPath).ReplaceLineEndings();
			var program = parser.Parse(source);

			// Execute in isolated root environment with its own scope
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
