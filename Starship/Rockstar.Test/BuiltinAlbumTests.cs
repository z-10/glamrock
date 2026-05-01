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
		program.Blocks[0].Statements[0].ShouldBeOfType<Invoke>();
	}

	[Fact]
	public void ConjureIsParsedAsChanneling() {
		var program = Parser.Parse("Conjure Math Module\n");
		program.Blocks[0].Statements[0].ShouldBeOfType<Channeling>();
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

	private (Result Result, string Output) RunWithBuiltin(string source) {
		var program = Parser.Parse(source.Trim().ReplaceLineEndings());
		var env = new TestEnvironment(() => null);
		var result = env.Execute(program);
		return (result, env.Output);
	}
}
