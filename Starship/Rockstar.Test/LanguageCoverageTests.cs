using Rockstar.Engine.Values;

namespace Rockstar.Test;

public class LanguageCoverageTests(ITestOutputHelper output) : RockstarTestBase(output) {
	private static string Lines(params string[] lines)
		=> string.Join(Environment.NewLine, lines) + Environment.NewLine;

	[Fact]
	public void CoversLiteralAliasesAndPoeticForms() {
		var (_, output) = RunProgram("""
		                            Shout true
		                            Shout yes
		                            Shout ok
		                            Shout right
		                            Shout false
		                            Shout no
		                            Shout wrong
		                            Shout lies
		                            Shout null
		                            Shout nothing
		                            Shout nowhere
		                            Shout nobody
		                            Shout gone
		                            Shout empty
		                            Shout silent
		                            Shout silence
		                            Shout mysterious
		                            Let the number be like ice cold beer
		                            Shout the number
		                            The lyric says We are the champions
		                            Shout the lyric
		                            The ninja holds 72
		                            Shout the ninja
		                            """);

		output.ShouldBe(Lines(
			"true", "true", "true", "true",
			"false", "false", "false", "false",
			"null", "null", "null", "null", "null",
			"", "", "",
			"mysterious",
			"344",
			"We are the champions",
			"H"));
	}

	[Theory]
	[InlineData("My heart is 10")]
	[InlineData("My heart was 10")]
	[InlineData("My heart are 10")]
	[InlineData("My heart were 10")]
	[InlineData("My heart am 10")]
	[InlineData("My heart's 10")]
	[InlineData("My heart're 10")]
	[InlineData("Put 10 into my heart")]
	[InlineData("Let my heart be 10")]
	[InlineData("Let my heart = 10")]
	[InlineData("My heart = 10")]
	public void CoversAssignmentForms(string assignment) {
		var (_, output) = RunProgram($"""
		                             {assignment}
		                             Shout my heart
		                             """);

		output.ShouldBe(Lines("10"));
	}

	[Fact]
	public void CoversOperatorsAliasesAndPrecedence() {
		var (_, output) = RunProgram("""
		                            Shout 2 plus 3
		                            Shout 5 with 7
		                            Shout 10 minus 4
		                            Shout 10 without 3
		                            Shout 6 times 7
		                            Shout 3 of 4
		                            Shout 12 divided by 3
		                            Shout 12 over 4
		                            Shout 12 between 2
		                            Shout 2 plus 3 times 4
		                            Shout true and "right"
		                            Shout false or "fallback"
		                            Shout true nor false
		                            Shout not false
		                            Shout non-true
		                            Shout 2 is as low as 2
		                            Shout 3 is as high as 2
		                            Shout 4 is greater than 3
		                            Shout 4 is higher than 3
		                            Shout 4 is bigger than 3
		                            Shout 4 is stronger than 3
		                            Shout 2 is lower than 3
		                            Shout 2 is smaller than 3
		                            Shout 2 is weaker than 3
		                            Shout 2 is exactly 2
		                            Shout 2 isn't exactly "2"
		                            Shout 2 != 3
		                            """);

		output.ShouldBe(Lines(
			"5", "12", "6", "7", "42", "12", "4", "3", "6", "14",
			"right", "fallback", "false", "true", "false",
			"true", "true", "true", "true", "true", "true",
			"true", "true", "true", "true", "true", "true"));
	}

	[Fact]
	public void CoversInputOutputDebugAndDumpStatements() {
		var (_, output) = RunProgram("""
		                            Print "print"
		                            Shout "shout"
		                            Say "say"
		                            Scream "scream"
		                            Whisper "whisper"
		                            Write "write"
		                            Listen to the answer
		                            Shout the answer
		                            Listen
		                            Shout it
		                            Debug the answer
		                            @dump
		                            """, new Queue<string>(["heard", "ignored"]));

		output.ShouldContain(Lines("print", "shout", "say", "scream", "whisper"));
		output.ShouldContain("writeheard");
		output.ShouldContain("DEBUG: the answer: str");
		output.ShouldContain("======== DUMP ========");
	}

	[Fact]
	public void CoversConditionalsLoopsAndBlockEndings() {
		var (_, output) = RunProgram("""
		                            Let the count be 0
		                            While the count is lower than 5
		                              Build the count up
		                              If the count is 2
		                                Take it higher
		                              Yeah
		                              If the count is 4
		                                Break it down
		                              Yeah
		                              Shout the count
		                            Baby
		                            Until the count is 5
		                              Build the count up
		                            Oh
		                            Shout the count
		                            If the count is 5, shout "one-line"
		                            If the count is 0
		                              Shout "wrong"
		                            Otherwise
		                              Shout "else"
		                            Yeah
		                            """);

		output.ShouldBe(Lines("1", "3", "5", "one-line", "else"));
	}

	[Fact]
	public void CoversFunctionsCallsClosuresAndListSeparators() {
		var (_, output) = RunProgram("""
		                            Add takes X, Y & Z
		                            Give back X with Y with Z

		                            Joiner takes First, Second and Third
		                            Give back First with Second with Third

		                            The clock takes nothing
		                            Give back "now"

		                            Make Adder takes X
		                              Inner takes Y
		                                Give back X with Y
		                              Yeah
		                              Give back Inner
		                            Yeah

		                            Shout Add taking 1, 2, 3
		                            Shout Joiner taking "a", "b", "c"
		                            Call The clock into the time
		                            Shout the time
		                            Let Add Five be Make Adder taking 5
		                            Shout Add Five taking 7
		                            """);

		output.ShouldBe(Lines("6", "abc", "now", "12"));
	}

	[Fact]
	public void CoversArraysIndexingAndForLoops() {
		var (_, output) = RunProgram("""
		                            Rock the list
		                            Rock the list with 1, 2, and 3
		                            Push 4 into the list
		                            The list at "name" is "Tommy"
		                            Shout the list at 0
		                            Shout the list + 0
		                            For value and index in the list
		                              Shout index with ":" with value
		                            Yeah
		                            For value and key of the list
		                              Shout key with ":" with value
		                            Yeah
		                            Roll the list into the first
		                            Pop the list into the second
		                            Shout the first
		                            Shout the second
		                            The word is "abc"
		                            The word at 1 is "Z"
		                            Shout the word
		                            The bitfield is 0
		                            The bitfield at 2 is true
		                            Shout the bitfield
		                            """);

		output.ShouldBe(Lines(
			"1",
			"4",
			"0:1",
			"1:2",
			"2:3",
			"3:4",
			"name:Tommy",
			"1",
			"4",
			"aZc",
			"4"));
	}

	[Fact]
	public void CoversMutationsConversionsAndRounding() {
		var (_, output) = RunProgram("""
		                            Split "a,b,c" into the parts using ","
		                            Join the parts into the joined using "|"
		                            Shout the joined
		                            Cast "65" into the codes
		                            Shout the codes at 0
		                            Cast 65 into the letter
		                            Shout the letter
		                            The number is 1.2
		                            Turn up the number
		                            Shout the number
		                            Turn the number down
		                            Shout the number
		                            The word is "Rock"
		                            Turn the word down
		                            Shout the word
		                            Turn the word up
		                            Shout the word
		                            Turn the word around
		                            Shout the word
		                            """);

		output.ShouldBe(Lines(
			"a|b|c",
			"54",
			"A",
			"2",
			"2",
			"rock",
			"ROCK",
			"KCOR"));
	}

	[Fact]
	public void CoversUnifiedAlbumLoadingCommandsAndBuiltins() {
		var tempDir = Path.Combine(Path.GetTempPath(), $"glamrock-coverage-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);
		try {
			File.WriteAllText(Path.Combine(tempDir, "coverage_album.rock"), """
			                                                                  Coverage Track takes X
			                                                                  Give back X with "-rock"

			                                                                  Light Coverage Track
			                                                                  """);
			File.WriteAllText(Path.Combine(tempDir, "host_album.tracklist"), """
			                                                                 ALBUM host.dll

			                                                                 TRACK Host Track FEATURING hostTrack
			                                                                   TAKES string
			                                                                   GIVES string
			                                                                 """);

			string Handler(string trackName, string argsJson) => """{"result":"host-track"}""";
			var sourcePath = Path.Combine(tempDir, "main.rock");
			var program = Parser.Parse("""
			                           Know Coverage Album
			                           Shout Coverage Track taking "loaded"
			                           Channel Host Album
			                           Shout Host Track taking "ignored"
			                           Divine Tome of Power
			                           Let the tome be Open The Tome taking "coverage.txt", "w+"
			                           Write The Line taking the tome, "file"
			                           Seek The Tome taking the tome, 0, 0
			                           Shout Read The Line taking the tome
			                           Seal The Tome taking the tome
			                           """);
			var env = new TestEnvironment(() => null) {
				SourceFilePath = sourcePath,
				ModuleLoader = new ModuleLoader(),
				TrackCallHandler = Handler
			};

			env.Execute(program);

			env.Output.ShouldBe(Lines("loaded-rock", "host-track", "file"));
			File.ReadAllText(Path.Combine(tempDir, "coverage.txt")).ShouldBe(Lines("file"));
		} finally {
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[Theory]
	[InlineData("Conjure Gates of Heaven")]
	[InlineData("Channel Gates of Heaven")]
	[InlineData("Bring Gates of Heaven")]
	[InlineData("Know Gates of Heaven")]
	[InlineData("Invoke Gates of Heaven")]
	[InlineData("Divine Gates of Heaven")]
	public void CoversAllAlbumLoadCommandAliasesAgainstBuiltins(string command) {
		var program = Parser.Parse($"""
		                           {command}
		                           Let my result be Command The Heavens taking "ignored"
		                           Shout my result at 0
		                           Shout my result at 2
		                           """);
		var env = new TestEnvironment(() => null) {
			CommandExecutor = new StubCommandExecutor()
		};

		env.Execute(program);

		env.Output.ShouldBe(Lines("alias-output", "0"));
	}

	private sealed class StubCommandExecutor : CommandExecutorBase {
		public override CommandResult Execute(string command) => new("alias-output", "", 0);
	}
}
