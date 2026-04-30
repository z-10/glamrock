namespace Rockstar.Engine.Interop;

public class TracklistParser {

	private static readonly Dictionary<string, InteropType> TypeMap = new(StringComparer.OrdinalIgnoreCase) {
		["number"] = InteropType.Number,
		["numbër"] = InteropType.Number,
		["string"] = InteropType.String,
		["strïng"] = InteropType.String,
		["boolean"] = InteropType.Boolean,
		["booleän"] = InteropType.Boolean,
		["nothing"] = InteropType.Nothing,
		["nüll"] = InteropType.Nothing,
		["null"] = InteropType.Nothing,
		["nowhere"] = InteropType.Nothing,
		["nobody"] = InteropType.Nothing,
		["gone"] = InteropType.Nothing,
		["mysterious"] = InteropType.Mysterious,
		["mysteriöus"] = InteropType.Mysterious,
	};

	public TracklistFile Parse(string source, string? filename = null) {
		var lines = source.ReplaceLineEndings("\n").Split('\n')
			.Select(l => l.Trim())
			.Where(l => l.Length > 0)
			.ToList();

		TracklistKind? kind = null;
		string? libraryPath = null;
		var tracks = new List<TrackDefinition>();
		var i = 0;

		while (i < lines.Count) {
			var line = lines[i];

			if (line.StartsWith("ALBUM ", StringComparison.OrdinalIgnoreCase)) {
				kind = TracklistKind.Album;
				libraryPath = line[6..].Trim();
				i++;
			} else if (line.StartsWith("MIXTAPE ", StringComparison.OrdinalIgnoreCase)) {
				kind = TracklistKind.Mixtape;
				libraryPath = line[8..].Trim();
				i++;
			} else if (line.StartsWith("TRACK ", StringComparison.OrdinalIgnoreCase)) {
				var track = ParseTrack(lines, ref i, filename);
				tracks.Add(track);
			} else {
				throw new FormatException(
					$"Unexpected line in tracklist{Location(filename, i)}: '{line}'");
			}
		}

		if (kind == null || libraryPath == null) {
			throw new FormatException(
				$"Tracklist{Location(filename)} must start with ALBUM or MIXTAPE");
		}

		if (tracks.Count == 0) {
			throw new FormatException(
				$"Tracklist{Location(filename)} has no TRACK definitions");
		}

		return new TracklistFile(kind.Value, libraryPath, tracks.ToArray());
	}

	private TrackDefinition ParseTrack(List<string> lines, ref int i, string? filename) {
		var trackLine = lines[i];

		// TRACK <GlamRock Name> FEATURING <NativeFunction>
		var featuringIdx = trackLine.IndexOf(" FEATURING ", StringComparison.OrdinalIgnoreCase);
		if (featuringIdx < 0) {
			throw new FormatException(
				$"TRACK line missing FEATURING{Location(filename, i)}: '{trackLine}'");
		}

		var glamRockName = trackLine[6..featuringIdx].Trim();
		var nativeName = trackLine[(featuringIdx + 11)..].Trim();

		if (string.IsNullOrEmpty(glamRockName)) {
			throw new FormatException(
				$"TRACK has no GlamRock name{Location(filename, i)}");
		}
		if (string.IsNullOrEmpty(nativeName)) {
			throw new FormatException(
				$"TRACK has no native function name{Location(filename, i)}");
		}

		i++;

		InteropParam[]? parameters = null;
		InteropType returnType = InteropType.Nothing;

		// Parse optional TAKES and GIVES lines
		while (i < lines.Count) {
			var line = lines[i];

			if (line.StartsWith("TAKES ", StringComparison.OrdinalIgnoreCase)) {
				parameters = ParseParamList(line[6..], filename, i);
				i++;
			} else if (line.StartsWith("GIVES ", StringComparison.OrdinalIgnoreCase)) {
				returnType = ParseType(line[6..].Trim(), filename, i);
				i++;
			} else {
				break; // next TRACK or end
			}
		}

		return new TrackDefinition(
			glamRockName,
			nativeName,
			parameters ?? Array.Empty<InteropParam>(),
			returnType
		);
	}

	private InteropParam[] ParseParamList(string text, string? filename, int lineNum) {
		var parts = text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0);
		return parts.Select(p => ParseParam(p, filename, lineNum)).ToArray();
	}

	private InteropParam ParseParam(string text, string? filename, int lineNum) {
		var lower = text.ToLowerInvariant();
		if (lower == "sigil") {
			return new InteropParam(InteropType.Mysterious, ParamDirection.Out);
		}
		var type = ParseType(text, filename, lineNum);
		return new InteropParam(type);
	}

	private InteropType ParseType(string text, string? filename, int lineNum) {
		var trimmed = text.Trim();
		if (TypeMap.TryGetValue(trimmed, out var type)) {
			return type;
		}
		throw new FormatException(
			$"Unknown type '{trimmed}'{Location(filename, lineNum)}. " +
			$"Valid types: {string.Join(", ", TypeMap.Keys.Distinct())}");
	}

	private static string Location(string? filename, int? lineNum = null) {
		if (filename == null && lineNum == null) return "";
		var parts = new List<string>();
		if (filename != null) parts.Add(filename);
		if (lineNum != null) parts.Add($"line {lineNum + 1}");
		return $" ({string.Join(", ", parts)})";
	}
}
