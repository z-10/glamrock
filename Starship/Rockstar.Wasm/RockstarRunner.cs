using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rockstar.Engine;

namespace Rockstar.Wasm;

public class WasmIO(Action<string> output, Queue<string> input) : IRockstarIO {
	public string? Read() => input.TryDequeue(out var s) ? s : null;
	public void Write(string s) => output(s);
	public void WriteLine(string s) => output(s + Environment.NewLine);
}

public class WasmModuleLoader : ModuleLoaderBase {
	private readonly Func<string, string?> resolveModule;

	public WasmModuleLoader(Func<string, string?> resolveModule) {
		this.resolveModule = resolveModule;
	}

	public override string ResolvePath(string importPath, string? currentFilePath) => importPath;

	public override string? TryReadSource(string resolvedPath) {
		var source = resolveModule(resolvedPath);
		return source?.ReplaceLineEndings();
	}
}

public class WasmCommandExecutor : CommandExecutorBase {
	private readonly Func<string, string> executeCommand;

	public WasmCommandExecutor(Func<string, string> executeCommand) {
		this.executeCommand = executeCommand;
	}

	public override CommandResult Execute(string command) {
		try {
			var stdout = executeCommand(command);
			return new CommandResult(stdout ?? "", "", 0);
		} catch (Exception ex) {
			return new CommandResult("", ex.Message, 1);
		}
	}
}

public partial class RockstarRunner {

	[JSExport]
	public static Task<string> Status() {
		var status = $"Rockstar (Starship {RockstarEnvironment.VERSION} on {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier})";
		Console.WriteLine(status);
		return Task.Run(() => status);
	}

	private static readonly Parser parser = new();

	[JSExport]
	public static Task<string> Run(string source,
		[JSMarshalAs<JSType.Function<JSType.String>>] Action<string> output, string? input = null, string? args = null) {
		return RunWithModules(source, output, null, null, null, input, args);
	}

	[JSExport]
	public static Task<string> RunWithModules(string source,
		[JSMarshalAs<JSType.Function<JSType.String>>] Action<string> output,
		[JSMarshalAs<JSType.Function<JSType.String, JSType.String>>] Func<string, string?>? moduleResolver = null,
		[JSMarshalAs<JSType.Function<JSType.String, JSType.String>>] Func<string, string>? commandExecutor = null,
		[JSMarshalAs<JSType.Function<JSType.String, JSType.String, JSType.String>>] Func<string, string, string>? trackHandler = null,
		string? input = null, string? args = null) {
		Console.WriteLine("Running GlamRock program");
		var inputQueue = new Queue<string>((input ?? "").Split(Environment.NewLine));
		var argList = Regex.Split((args ?? ""), "\\s+");
		return Task.Run(() => {
			IRockstarIO io = new WasmIO(output, inputQueue);
			var env = new RockstarEnvironment(io, argList);
			if (moduleResolver != null) {
				env.ModuleLoader = new WasmModuleLoader(moduleResolver);
				env.SourceFilePath = "main.rock";
			}
			if (commandExecutor != null) {
				env.CommandExecutor = new WasmCommandExecutor(commandExecutor);
			}
			if (trackHandler != null) {
				env.TrackCallHandler = trackHandler;
			}
			try {
				var program = parser.Parse(source);
				var result = env.Execute(program);
				return result?.Value?.ToString() ?? "";
			} catch (ParserException ex) {
				io.WriteError(ex, source);
				return "";
			}
		});
	}

	[JSExport]
	public static Task<string> Parse(string source) {
		Console.WriteLine("Parsing Rockstar program");
		return Task.Run(() => parser.Parse(source).ToString());
	}
}