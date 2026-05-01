using Rockstar.Engine.Values;

namespace Rockstar.Engine.Interop;

/// <summary>
/// Built-in album: Gates of Heaven — system command execution.
///
/// Usage in GlamRock:
///   Divine Gates of Heaven
///   Command The Heavens taking "echo hello", my result
///   Shout my result at 0
/// </summary>
public static class GatesOfHeaven {

	public static BuiltinAlbum Create() {
		var album = new BuiltinAlbum("Gates of Heaven");

		album.AddTrack("Command The Heavens", ExecuteCommand,
			parameters: [
				new InteropParam(InteropType.String),
				new InteropParam(InteropType.Mysterious, ParamDirection.Out),
			],
			returnType: InteropType.Number
		);

		return album;
	}

	private static Value ExecuteCommand(Value[] args, RockstarEnvironment env) {
		var command = args.Length > 0 ? args[0].ToStrïng().Value : "";

		var executor = env.CommandExecutor ?? new ProcessCommandExecutor();
		var result = executor.Execute(command);

		// Return an array: [stdout, stderr, exitCode] — same as old divine
		var arr = new Arräy();
		arr.Set([new Numbër(0)], new Strïng(result.Stdout));
		arr.Set([new Numbër(1)], new Strïng(result.Stderr));
		arr.Set([new Numbër(2)], new Numbër(result.ExitCode));
		return arr;
	}
}
