using Rockstar.Engine;
using Rockstar.Engine.Interop;
using Rockstar.Engine.Values;

namespace Rockstar.Test;

public class TracklistParserTests(ITestOutputHelper output) : RockstarTestBase(output) {

	private readonly TracklistParser parser = new();

	// --- Parser Tests ---

	[Fact]
	public void ParsesAlbumWithTracks() {
		var result = parser.Parse(@"
ALBUM gdiplus.dll

TRACK Create Image FEATURING GdipCreateBitmapFromFile
  TAKES string, sigil
  GIVES number

TRACK Dispose FEATURING GdipDisposeImage
  TAKES mysterious
  GIVES nothing
");

		result.Kind.ShouldBe(TracklistKind.Album);
		result.LibraryPath.ShouldBe("gdiplus.dll");
		result.Tracks.Length.ShouldBe(2);

		result.Tracks[0].GlamRockName.ShouldBe("Create Image");
		result.Tracks[0].NativeName.ShouldBe("GdipCreateBitmapFromFile");
		result.Tracks[0].Parameters.Length.ShouldBe(2);
		result.Tracks[0].Parameters[0].Type.ShouldBe(InteropType.String);
		result.Tracks[0].Parameters[0].Direction.ShouldBe(ParamDirection.In);
		result.Tracks[0].Parameters[1].Type.ShouldBe(InteropType.Mysterious);
		result.Tracks[0].Parameters[1].Direction.ShouldBe(ParamDirection.Out);
		result.Tracks[0].ReturnType.ShouldBe(InteropType.Number);

		result.Tracks[1].GlamRockName.ShouldBe("Dispose");
		result.Tracks[1].NativeName.ShouldBe("GdipDisposeImage");
		result.Tracks[1].Parameters[0].Type.ShouldBe(InteropType.Mysterious);
		result.Tracks[1].ReturnType.ShouldBe(InteropType.Nothing);
	}

	[Fact]
	public void ParsesMixtape() {
		var result = parser.Parse(@"
MIXTAPE System.IO.dll

TRACK Read All Text FEATURING System.IO.File.ReadAllText
  TAKES string
  GIVES string
");
		result.Kind.ShouldBe(TracklistKind.Mixtape);
		result.LibraryPath.ShouldBe("System.IO.dll");
		result.Tracks[0].GlamRockName.ShouldBe("Read All Text");
	}

	[Fact]
	public void AcceptsUmlautTypeNames() {
		var result = parser.Parse(@"
ALBUM test.dll

TRACK My Func FEATURING myFunc
  TAKES Strïng, Mysteriöus, Booleän
  GIVES Numbër
");
		result.Tracks[0].Parameters[0].Type.ShouldBe(InteropType.String);
		result.Tracks[0].Parameters[1].Type.ShouldBe(InteropType.Mysterious);
		result.Tracks[0].Parameters[2].Type.ShouldBe(InteropType.Boolean);
		result.Tracks[0].ReturnType.ShouldBe(InteropType.Number);
	}

	[Fact]
	public void AcceptsNullAliases() {
		var result = parser.Parse(@"
ALBUM test.dll

TRACK A FEATURING a
  GIVES nothing

TRACK B FEATURING b
  GIVES nüll

TRACK C FEATURING c
  GIVES gone

TRACK D FEATURING d
  GIVES nowhere
");
		foreach (var track in result.Tracks) {
			track.ReturnType.ShouldBe(InteropType.Nothing);
		}
	}

	[Fact]
	public void SigilMapsToOutMysterous() {
		var result = parser.Parse(@"
ALBUM test.dll

TRACK Get Value FEATURING getValue
  TAKES sigil
  GIVES number
");
		var p = result.Tracks[0].Parameters[0];
		p.Type.ShouldBe(InteropType.Mysterious);
		p.Direction.ShouldBe(ParamDirection.Out);
		p.IsSigil.ShouldBeTrue();
	}

	[Fact]
	public void TrackWithNoTakesHasEmptyParams() {
		var result = parser.Parse(@"
ALBUM test.dll

TRACK Get Time FEATURING getTime
  GIVES number
");
		result.Tracks[0].Parameters.Length.ShouldBe(0);
	}

	[Fact]
	public void TrackWithNoGivesDefaultsToNothing() {
		var result = parser.Parse(@"
ALBUM test.dll

TRACK Do Thing FEATURING doThing
  TAKES number
");
		result.Tracks[0].ReturnType.ShouldBe(InteropType.Nothing);
	}

	[Fact]
	public void ThrowsOnMissingAlbumOrMixtape() {
		Should.Throw<FormatException>(() => parser.Parse(@"
TRACK Foo FEATURING foo
  GIVES number
"));
	}

	[Fact]
	public void ThrowsOnUnknownType() {
		Should.Throw<FormatException>(() => parser.Parse(@"
ALBUM test.dll

TRACK Foo FEATURING foo
  TAKES blah
  GIVES number
"));
	}

	[Fact]
	public void ThrowsOnMissingFeaturing() {
		Should.Throw<FormatException>(() => parser.Parse(@"
ALBUM test.dll

TRACK Foo
  GIVES number
"));
	}

	[Fact]
	public void ThrowsOnNoTracks() {
		Should.Throw<FormatException>(() => parser.Parse(@"
ALBUM test.dll
"));
	}

	// --- Grammar Tests ---

	[Fact]
	public void InvokeParses() {
		var program = Parser.Parse("Know GDI\n");
		program.Blocks[0].Statements[0].ShouldBeOfType<Engine.Statements.Invoke>();
		((Engine.Statements.Invoke)program.Blocks[0].Statements[0]).TracklistPath.ShouldBe("gdi");
	}

	[Fact]
	public void InvokeWithMultiWordParses() {
		var program = Parser.Parse("Invoke GDI Wrapper\n");
		var invoke = program.Blocks[0].Statements[0].ShouldBeOfType<Engine.Statements.Invoke>();
		invoke.TracklistPath.ShouldBe("gdi_wrapper");
	}

	[Fact]
	public void KnowIsAlias() {
		var p1 = Parser.Parse("Know GDI\n");
		var p2 = Parser.Parse("Invoke GDI\n");
		p1.Blocks[0].Statements[0].ShouldBeOfType<Engine.Statements.Invoke>();
		p2.Blocks[0].Statements[0].ShouldBeOfType<Engine.Statements.Invoke>();
	}
}
