using Rockstar.Engine;
using Rockstar.Engine.Interop;
using Rockstar.Engine.Statements;
using Rockstar.Engine.Values;

namespace Rockstar.Test;

public class BuiltinAlbumTests(ITestOutputHelper output) : RockstarTestBase(output) {

	// --- Keyword Remapping ---

	[Fact]
	public void DivineIsParsedAsInvoke() {
		var program = Parser.Parse("Divine Gates of Heaven\n");
		program.Blocks[0].Statements[0].ShouldBeOfType<Invoke>();
	}

	[Fact]
	public void KnowIsParsedAsInvoke() {
		var program = Parser.Parse("Know Gates of Heaven\n");
		var invoke = program.Blocks[0].Statements[0].ShouldBeOfType<Invoke>();
		invoke.TracklistPath.ShouldBe("gates_of_heaven");
	}

	[Fact]
	public void ConjureIsParsedAsChanneling() {
		var program = Parser.Parse("Conjure Gates of Heaven\n");
		var channeling = program.Blocks[0].Statements[0].ShouldBeOfType<Channeling>();
		channeling.ModulePath.ShouldBe("gates_of_heaven");
	}

	// --- Built-in Album Registry ---

	[Fact]
	public void GatesOfHeavenIsRegistered() {
		// Trigger static constructor by referencing the type
		_ = RockstarEnvironment.VERSION;
		var album = BuiltinAlbumRegistry.Resolve("gates_of_heaven");
		album.ShouldNotBeNull();
		album.Name.ShouldBe("Gates of Heaven");
		album.Tracks.ContainsKey("Command The Heavens").ShouldBeTrue();
	}

	[Fact]
	public void TomeOfPowerIsRegistered() {
		_ = RockstarEnvironment.VERSION;
		var album = BuiltinAlbumRegistry.Resolve("tome_of_power");
		album.ShouldNotBeNull();
		album.Name.ShouldBe("Tome of Power");
		album.Tracks.ContainsKey("Open The Tome").ShouldBeTrue();
		album.Tracks.ContainsKey("Read The Tome").ShouldBeTrue();
		album.Tracks.ContainsKey("Write The Tome").ShouldBeTrue();
		album.Tracks.ContainsKey("Seal The Tome").ShouldBeTrue();
		album.Tracks.ContainsKey("Tome Exhausted").ShouldBeTrue();
	}

	// --- Gates of Heaven Execution ---

	[Fact]
	public void CommandTheHeavensExecutesShellViaKnow() {
		var (_, o) = RunWithBuiltin("Know Gates of Heaven\nCommand The Heavens taking \"echo hello\", my result\nShout my result at 0");
		o.Trim().ShouldBe("hello");
	}

	[Fact]
	public void CommandTheHeavensExecutesShellViaDivine() {
		var (_, o) = RunWithBuiltin("Divine Gates of Heaven\nCommand The Heavens taking \"echo hello\", my result\nShout my result at 0");
		o.Trim().ShouldBe("hello");
	}

	[Fact]
	public void CommandTheHeavensWorksViaConjure() {
		var (_, o) = RunWithBuiltin("Conjure Gates of Heaven\nCommand The Heavens taking \"echo hello\", my result\nShout my result at 0");
		o.Trim().ShouldBe("hello");
	}

	[Fact]
	public void CommandTheHeavensReturnsArray() {
		var (_, o) = RunWithBuiltin(@"
Know Gates of Heaven
Let my result be Command The Heavens taking ""echo test""
Shout my result at 0
Shout my result at 2");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("test");
		lines[1].ShouldBe("0");
	}

	[Fact]
	public void TomeOfPowerCanWriteSeekAndRead() {
		var tempDir = CreateTempDir();
		var path = ToRockPath(Path.Combine(tempDir, "spellbook.txt"));
		try {
			var (_, output) = RunWithBuiltin($@"
Know Tome of Power
Let the tome be Open The Tome taking ""{path}"", ""w+""
Write The Tome taking the tome, ""fire and thunder""
Seek The Tome taking the tome, 0, 0
Shout Read The Tome taking the tome
Seal The Tome taking the tome");

			output.Trim().ShouldBe("fire and thunder");
			File.ReadAllText(Path.Combine(tempDir, "spellbook.txt")).ShouldBe("fire and thunder");
		} finally {
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public void TomeOfPowerCanReadLinesAndTellPosition() {
		var tempDir = CreateTempDir();
		var filePath = Path.Combine(tempDir, "verses.txt");
		File.WriteAllText(filePath, "first line\nsecond line\n");
		var path = ToRockPath(filePath);
		try {
			var (_, output) = RunWithBuiltin($@"
Know Tome of Power
Let the tome be Open The Tome taking ""{path}"", ""r""
Shout Read The Line taking the tome
Shout Tell The Tome taking the tome
Shout Tome Exhausted taking the tome
Shout Read The Line taking the tome
Shout Tome Exhausted taking the tome
Seal The Tome taking the tome");

			var lines = output.Trim().Split(Environment.NewLine);
			lines[0].ShouldBe("first line");
			lines[1].ShouldBe("11");
			lines[2].ShouldBe("false");
			lines[3].ShouldBe("second line");
			lines[4].ShouldBe("true");
		} finally {
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public void TomeOfPowerResolvesRelativePathsAgainstSourceFile() {
		var tempDir = CreateTempDir();
		var sourcePath = Path.Combine(tempDir, "song.rock");
		var env = new TestEnvironment(() => null) {
			SourceFilePath = sourcePath
		};

		try {
			var program = Parser.Parse(@"
Know Tome of Power
Let the tome be Open The Tome taking ""notes.txt"", ""w+""
Write The Line taking the tome, ""sigil""
Seek The Tome taking the tome, 0, 0
Shout Read The Tome taking the tome
Seal The Tome taking the tome".Trim().ReplaceLineEndings());

			env.Execute(program);
			env.Output.Trim().ShouldBe("sigil");
			File.ReadAllText(Path.Combine(tempDir, "notes.txt")).ShouldBe($"sigil{Environment.NewLine}");
		} finally {
			Directory.Delete(tempDir, recursive: true);
		}
	}

	private (Result Result, string Output) RunWithBuiltin(string source) {
		var program = Parser.Parse(source.Trim().ReplaceLineEndings());
		var env = new TestEnvironment(() => null);
		var result = env.Execute(program);
		return (result, env.Output);
	}

	private static string CreateTempDir() {
		var tempDir = Path.Combine(Path.GetTempPath(), $"glamrock-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		return tempDir;
	}

	private static string ToRockPath(string path) => path.Replace("\\", "/");
}
