using Rockstar.Engine;
using Rockstar.Engine.Expressions;
using Rockstar.Engine.Values;

namespace Rockstar.Test;

public class ModuleTests(ITestOutputHelper output) : RockstarTestBase(output) {

	private static string GetModulesDir() {
		var dir = AppContext.BaseDirectory;
		while (dir != null && !File.Exists(Path.Combine(dir, "Rockstar.Test.csproj"))) {
			dir = Path.GetDirectoryName(dir);
		}
		return Path.Combine(dir ?? ".", "programs", "examples", "13-modules");
	}

	private static readonly string ModulesDir = GetModulesDir();

	private (Result Result, string Output) RunModuleSource(string source) {
		var filePath = Path.Combine(ModulesDir, "__inline_test__.rock");
		var program = Parser.Parse(source.Trim().ReplaceLineEndings());
		var env = new TestEnvironment(() => null);
		env.SourceFilePath = filePath;
		env.ModuleLoader = new ModuleLoader();
		var result = env.Execute(program);
		return (result, env.Output);
	}

	// --- Import All ---

	[Fact]
	public void ChannelImportsAllExports() {
		var (_, o) = RunModuleSource(@"
Channel Math Module
Shout Add taking 3, 4
Shout Multiply taking 5, 5
Shout The Pi");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("7");
		lines[1].ShouldBe("25");
		lines[2].ShouldBe("3.14159");
	}

	[Fact]
	public void ChannelDoesNotImportUnexported() {
		var (_, o) = RunModuleSource("Channel Math Module\nShout The Secret");
		o.Trim().ShouldBe("mysterious");
	}

	[Fact]
	public void BringAliasWorks() {
		var (_, o) = RunModuleSource("Bring Math Module\nShout Add taking 10, 20");
		o.Trim().ShouldBe("30");
	}

	// --- Selective Import ---

	[Fact]
	public void SelectiveImportWithPossessive() {
		var (_, o) = RunModuleSource("Channel Math Module's Add\nShout Add taking 3, 4");
		o.Trim().ShouldBe("7");
	}

	[Fact]
	public void SelectiveImportWithFrom() {
		var (_, o) = RunModuleSource("Channel Add from Math Module\nShout Add taking 100, 200");
		o.Trim().ShouldBe("300");
	}

	[Fact]
	public void SelectiveImportMultipleSymbols() {
		var (_, o) = RunModuleSource("Channel Math Module's Add and Multiply\nShout Add taking 1, 2\nShout Multiply taking 3, 4");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("3");
		lines[1].ShouldBe("12");
	}

	[Fact]
	public void SelectiveImportRejectsNonExport() {
		Should.Throw<Exception>(() => RunModuleSource("Channel The Hidden from Partial Exports"))
			.Message.ShouldContain("does not export");
	}

	// --- From Expression ---

	[Fact]
	public void FromExpressionCallsFunction() {
		var (_, o) = RunModuleSource(@"
Channel Math Module
Shout Add from Math Module taking 3, 4
Shout Multiply from Math Module taking 5, 5");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("7");
		lines[1].ShouldBe("25");
	}

	[Fact]
	public void FromExpressionAccessesVariable() {
		var (_, o) = RunModuleSource("Channel Math Module\nShout The Pi from Math Module");
		o.Trim().ShouldBe("3.14159");
	}

	[Fact]
	public void FromExpressionRejectsNonExport() {
		Should.Throw<Exception>(() => RunModuleSource("Channel Math Module\nShout The Secret from Math Module"))
			.Message.ShouldContain("does not export");
	}

	[Fact]
	public void FromWithoutChannelingThrowsError() {
		Should.Throw<Exception>(() => RunModuleSource("Shout Add from Math Module taking 3, 4"))
			.Message.ShouldContain("has not been channeled");
	}

	// --- Export Behavior ---

	[Fact]
	public void ExportsCollectFinalValues() {
		var tempDir = Path.Combine(Path.GetTempPath(), "glamrock-test-" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);
		try {
			File.WriteAllText(Path.Combine(tempDir, "late_assign.rock"),
				"X is 1\nLight X\nX is 42");
			var program = Parser.Parse("Channel Late Assign\nShout X");
			var env = new TestEnvironment(() => null);
			env.SourceFilePath = Path.Combine(tempDir, "main.rock");
			env.ModuleLoader = new ModuleLoader();
			env.Execute(program);
			env.Output.Trim().ShouldBe("42");
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void IgniteAliasExports() {
		var tempDir = Path.Combine(Path.GetTempPath(), "glamrock-test-" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);
		try {
			File.WriteAllText(Path.Combine(tempDir, "mod.rock"),
				"Greet takes Name\nGive back \"Hello, \" with Name\n\nIgnite Greet");
			var program = Parser.Parse("Channel Mod\nShout Greet taking \"World\"");
			var env = new TestEnvironment(() => null);
			env.SourceFilePath = Path.Combine(tempDir, "main.rock");
			env.ModuleLoader = new ModuleLoader();
			env.Execute(program);
			env.Output.Trim().ShouldBe("Hello, World");
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ShineAliasExports() {
		var tempDir = Path.Combine(Path.GetTempPath(), "glamrock-test-" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);
		try {
			File.WriteAllText(Path.Combine(tempDir, "mod.rock"), "X is 99\nShine X");
			var program = Parser.Parse("Channel Mod\nShout X");
			var env = new TestEnvironment(() => null);
			env.SourceFilePath = Path.Combine(tempDir, "main.rock");
			env.ModuleLoader = new ModuleLoader();
			env.Execute(program);
			env.Output.Trim().ShouldBe("99");
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void BeaconAliasExports() {
		var tempDir = Path.Combine(Path.GetTempPath(), "glamrock-test-" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);
		try {
			File.WriteAllText(Path.Combine(tempDir, "mod.rock"), "X is 77\nBeacon X");
			var program = Parser.Parse("Channel Mod\nShout X");
			var env = new TestEnvironment(() => null);
			env.SourceFilePath = Path.Combine(tempDir, "main.rock");
			env.ModuleLoader = new ModuleLoader();
			env.Execute(program);
			env.Output.Trim().ShouldBe("77");
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	// --- Module Loader ---

	[Fact]
	public void CircularImportThrowsError() {
		var tempDir = Path.Combine(Path.GetTempPath(), "glamrock-test-" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);
		try {
			File.WriteAllText(Path.Combine(tempDir, "alpha.rock"), "Channel Beta\nLight Alpha");
			File.WriteAllText(Path.Combine(tempDir, "beta.rock"), "Channel Alpha\nLight Beta");
			var source = File.ReadAllText(Path.Combine(tempDir, "alpha.rock")).ReplaceLineEndings();
			var program = Parser.Parse(source);
			var env = new TestEnvironment(() => null);
			env.SourceFilePath = Path.Combine(tempDir, "alpha.rock");
			env.ModuleLoader = new ModuleLoader();
			Should.Throw<InvalidOperationException>(() => env.Execute(program))
				.Message.ShouldContain("Circular import");
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ModuleCachingExecutesOnlyOnce() {
		var tempDir = Path.Combine(Path.GetTempPath(), "glamrock-test-" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);
		try {
			File.WriteAllText(Path.Combine(tempDir, "counter.rock"),
				"Shout \"loaded\"\nX is 42\nLight X");
			var program = Parser.Parse("Channel Counter\nChannel Counter\nShout X");
			var env = new TestEnvironment(() => null);
			env.SourceFilePath = Path.Combine(tempDir, "main.rock");
			env.ModuleLoader = new ModuleLoader();
			env.Execute(program);
			var lines = env.Output.Trim().Split(Environment.NewLine);
			lines.Count(l => l == "loaded").ShouldBe(1);
			lines.Last().ShouldBe("42");
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ModuleNotFoundThrowsError() {
		Should.Throw<FileNotFoundException>(() => RunModuleSource("Channel Nonexistent Module"));
	}

	[Fact]
	public void ModuleCannotSeeImporterScope() {
		var tempDir = Path.Combine(Path.GetTempPath(), "glamrock-test-" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);
		try {
			// Module tries to read importer's variable via 'let' (expression, not poetic)
			File.WriteAllText(Path.Combine(tempDir, "reader.rock"),
				"Let The Value be The Outsider\nLight The Value");
			var program = Parser.Parse("The Outsider is 999\nChannel Reader\nShout The Value");
			var env = new TestEnvironment(() => null);
			env.SourceFilePath = Path.Combine(tempDir, "main.rock");
			env.ModuleLoader = new ModuleLoader();
			env.Execute(program);
			// Module can't see importer's The Outsider, so The Value = mysterious
			env.Output.Trim().ShouldBe("mysterious");
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	// --- Scoped Channeling ---

	[Fact]
	public void ScopedChannelingBlock() {
		var (_, o) = RunModuleSource("Channeling Math Module\nShout Add taking 3, 4\nYeah\nShout \"done\"");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("7");
		lines[1].ShouldBe("done");
	}

	[Fact]
	public void ScopedChannelingOneLiner() {
		var (_, o) = RunModuleSource("Channeling Math Module Shout Add taking 3, 4");
		o.Trim().ShouldBe("7");
	}

	[Fact]
	public void ScopedBringingAlias() {
		var (_, o) = RunModuleSource("Bringing Math Module Shout Multiply taking 5, 5");
		o.Trim().ShouldBe("25");
	}

	[Fact]
	public void ScopedChannelingCleansUpSymbols() {
		var (_, o) = RunModuleSource("Channeling Math Module\nShout Add taking 3, 4\nYeah\nShout Add");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("7");
		lines[1].ShouldBe("mysterious");
	}

	[Fact]
	public void ScopedChannelingRestoresPreviousBinding() {
		var (_, o) = RunModuleSource("Add is 99\nChanneling Math Module\nShout Add taking 3, 4\nYeah\nShout Add");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("7");
		lines[1].ShouldBe("99");
	}

	[Fact]
	public void ScopedChannelingPreservesPermanentImport() {
		var (_, o) = RunModuleSource("Channel Math Module\nChanneling Math Module\nShout Add taking 1, 1\nYeah\nShout Add taking 2, 2");
		var lines = o.Trim().Split(Environment.NewLine);
		lines[0].ShouldBe("2");
		lines[1].ShouldBe("4");
	}

	[Fact]
	public void ScopedFromExpressionCleansUpAfterBlock() {
		Should.Throw<Exception>(() => RunModuleSource(
			"Channeling Math Module\nShout Add from Math Module taking 3, 4\nYeah\nShout Add from Math Module taking 1, 2"))
			.Message.ShouldContain("has not been channeled");
	}
}
