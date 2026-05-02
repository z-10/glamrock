namespace Rockstar.Engine.Interop;

public enum InteropType {
	Number,      // int32
	String,      // wchar* (Unicode)
	Boolean,     // int (0/1)
	Nothing,     // void
	Mysterious,  // IntPtr / void* (opaque handle)
}

public enum ParamDirection {
	In,
	Out,  // sigil — native function writes into this
}

public record InteropParam(InteropType Type, ParamDirection Direction = ParamDirection.In) {
	public bool IsSigil => Direction == ParamDirection.Out;
}

public record TrackDefinition(
	string GlamRockName,    // multi-word Rockstar-style name: "Create Image"
	string NativeName,      // actual export: "GdipCreateBitmapFromFile"
	InteropParam[] Parameters,
	InteropType ReturnType
);

public enum TracklistKind {
	Album,   // native DLL (P/Invoke)
	Mixtape, // .NET assembly (reflection) — v2
}

public record TracklistFile(
	TracklistKind Kind,
	string LibraryPath,            // DLL/SO path from ALBUM/MIXTAPE line
	TrackDefinition[] Tracks,
	string? SourcePath = null
);
