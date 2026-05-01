using Rockstar.Engine;

namespace Rockstar.Test;

public class UnifiedAlbumLoadingTests(ITestOutputHelper output) : RockstarTestBase(output) {

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), $"glamrock-unified-{Guid.NewGuid():N}");
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static (Result Result, string Output) RunIn(string tempDir, string source,
		Func<string, string, string>? trackHandler = null) {
		var sourcePath = Path.Combine(tempDir, "main.rock");
		var program = new Parser().Parse(source.Trim().ReplaceLineEndings());
		var env = new TestEnvironment(() => null) {
			SourceFilePath = sourcePath,
			ModuleLoader = new ModuleLoader()
		};
		if (trackHandler != null) env.TrackCallHandler = trackHandler;
		env.Execute(program);
		return (Result.Unknown, env.Output);
	}

	[Fact]
	public void KnowLoadsRockModule() {
		var dir = CreateTempDir();
		try {
			File.WriteAllText(Path.Combine(dir, "math_helpers.rock"),
				"Add takes X and Y\nGive back X with Y\n\nLight Add");
			var (_, o) = RunIn(dir, "Know Math Helpers\nShout Add taking 7, 8");
			o.Trim().ShouldBe("15");
		} finally {
			Directory.Delete(dir, true);
		}
	}

	[Fact]
	public void ChannelLoadsTracklistViaHostHandler() {
		var dir = CreateTempDir();
		try {
			File.WriteAllText(Path.Combine(dir, "fake_album.tracklist"),
				"ALBUM fake.dll\n\nTRACK Echo Number FEATURING echoNumber\n  TAKES number\n  GIVES number\n");

			string Handler(string trackName, string argsJson) => "{\"result\":42}";

			var (_, o) = RunIn(dir, "Channel Fake Album\nShout Echo Number taking 5", Handler);
			o.Trim().ShouldBe("42");
		} finally {
			Directory.Delete(dir, true);
		}
	}

	[Fact]
	public void TracklistOverridesRockOnNameCollision() {
		var dir = CreateTempDir();
		try {
			File.WriteAllText(Path.Combine(dir, "mixed.rock"),
				"Greet takes Name\nGive back \"rock-\" with Name\n\nLight Greet");
			File.WriteAllText(Path.Combine(dir, "mixed.tracklist"),
				"ALBUM fake.dll\n\nTRACK Greet FEATURING greet\n  TAKES string\n  GIVES string\n");

			string Handler(string trackName, string argsJson) => "{\"result\":\"tracklist-wins\"}";

			var (_, o) = RunIn(dir, "Know Mixed\nShout Greet taking \"x\"", Handler);
			o.Trim().ShouldBe("tracklist-wins");
		} finally {
			Directory.Delete(dir, true);
		}
	}

	[Fact]
	public void RockOverridesBuiltinOnNameCollision() {
		var dir = CreateTempDir();
		try {
			File.WriteAllText(Path.Combine(dir, "tome_of_power.rock"),
				"Open The Tome takes path and mode\nGive back \"shadow-tome\"\n\nLight Open The Tome");

			var (_, o) = RunIn(dir, "Know Tome of Power\nShout Open The Tome taking \"x\", \"r\"");
			o.Trim().ShouldBe("shadow-tome");
		} finally {
			Directory.Delete(dir, true);
		}
	}

	[Fact]
	public void SelectiveImportFiltersMergedSet() {
		var dir = CreateTempDir();
		try {
			File.WriteAllText(Path.Combine(dir, "two_funcs.rock"),
				"Alpha takes X\nGive back X with 1\n\nBeta takes Y\nGive back Y with 2\n\nLight Alpha\nLight Beta");

			var (_, o) = RunIn(dir, "Channel Two Funcs's Alpha\nShout Alpha taking 10\nShout Beta");
			var lines = o.Trim().Split(Environment.NewLine);
			lines[0].ShouldBe("11");
			lines[1].ShouldBe("mysterious");
		} finally {
			Directory.Delete(dir, true);
		}
	}

	[Fact]
	public void MissingAlbumThrowsFileNotFound() {
		var dir = CreateTempDir();
		try {
			Should.Throw<FileNotFoundException>(() =>
				RunIn(dir, "Know Nonexistent Album"));
		} finally {
			Directory.Delete(dir, true);
		}
	}
}
