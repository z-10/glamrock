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

	public abstract string ResolvePath(string importPath, string? currentFilePath);

	public ModuleExports Load(string resolvedPath, IRockstarIO io) {
		if (moduleCache.TryGetValue(resolvedPath, out var cached)) {
			return cached;
		}

		if (moduleStates.TryGetValue(resolvedPath, out var state) && state == ModuleState.Loading) {
			throw new InvalidOperationException(
				$"Circular import detected: {resolvedPath} is already being loaded");
		}

		moduleStates[resolvedPath] = ModuleState.Loading;

		try {
			var source = LoadSource(resolvedPath);
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

	protected abstract string LoadSource(string resolvedPath);
}

public class ModuleLoader : ModuleLoaderBase {
	public override string ResolvePath(string importPath, string? currentFilePath) {
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

	protected override string LoadSource(string resolvedPath) {
		if (!File.Exists(resolvedPath)) {
			throw new FileNotFoundException($"Module not found: {resolvedPath}");
		}
		return File.ReadAllText(resolvedPath).ReplaceLineEndings();
	}
}
