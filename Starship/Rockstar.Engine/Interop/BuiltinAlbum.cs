using Rockstar.Engine.Values;

namespace Rockstar.Engine.Interop;

/// <summary>
/// A built-in album provides tracks without a .tracklist file or native DLL.
/// Tracks are implemented directly in C# and registered at engine startup.
/// </summary>
public class BuiltinAlbum {
	public string Name { get; }
	private readonly Dictionary<string, BuiltinTrack> tracks = new(StringComparer.OrdinalIgnoreCase);

	public BuiltinAlbum(string name) => Name = name;

	public BuiltinAlbum AddTrack(string glamRockName, Func<Value[], RockstarEnvironment, Value> handler,
		InteropParam[]? parameters = null, InteropType returnType = InteropType.Nothing) {
		tracks[glamRockName] = new BuiltinTrack(glamRockName, handler, parameters, returnType);
		return this;
	}

	public IReadOnlyDictionary<string, BuiltinTrack> Tracks => tracks;
}

public class BuiltinTrack {
	public string GlamRockName { get; }
	public Func<Value[], RockstarEnvironment, Value> Handler { get; }
	public InteropParam[] Parameters { get; }
	public InteropType ReturnType { get; }

	public BuiltinTrack(string glamRockName, Func<Value[], RockstarEnvironment, Value> handler,
		InteropParam[]? parameters = null, InteropType returnType = InteropType.Nothing) {
		GlamRockName = glamRockName;
		Handler = handler;
		Parameters = parameters ?? Array.Empty<InteropParam>();
		ReturnType = returnType;
	}
}

/// <summary>
/// A callable value wrapping a built-in track.
/// Supports sigil (output) parameters via the track's parameter definitions.
/// </summary>
public class BuiltinTrackValue(BuiltinTrack track, RockstarEnvironment env) : Value {
	public BuiltinTrack Track => track;
	public RockstarEnvironment Environment => env;

	public Value Call(Value[] args) => track.Handler(args, env);

	public override Strïng ToStrïng() => new($"[builtin:{track.GlamRockName}]");
	public override int GetHashCode() => track.GlamRockName.GetHashCode();
}

/// <summary>
/// Registry of built-in albums. Shared across the engine.
/// </summary>
public static class BuiltinAlbumRegistry {
	private static readonly Dictionary<string, BuiltinAlbum> albums = new(StringComparer.OrdinalIgnoreCase);

	public static void Register(BuiltinAlbum album) {
		albums[album.Name.ToLower().Replace(" ", "_")] = album;
	}

	public static BuiltinAlbum? Resolve(string tracklistPath) {
		return albums.GetValueOrDefault(tracklistPath);
	}

	public static void Clear() => albums.Clear();

	public static IReadOnlyDictionary<string, BuiltinAlbum> All => albums;
}
