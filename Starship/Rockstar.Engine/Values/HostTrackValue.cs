using System.Text;
using System.Text.Json;
using Rockstar.Engine.Expressions;
using Rockstar.Engine.Interop;
using Rockstar.Engine.Values;

namespace Rockstar.Engine.Values;

/// <summary>
/// A callable value that delegates track calls to a host-provided handler
/// (e.g. JS callback in WASM). Used instead of NativeTrackValue when
/// real P/Invoke is unavailable.
/// </summary>
public class HostTrackValue(
	TrackDefinition definition,
	Func<string, string, string> callHandler
) : Value {

	public TrackDefinition Definition => definition;

	public Value Call(Value[] args, out Dictionary<int, Value> sigilOutputs) {
		sigilOutputs = new();

		// Serialize args to JSON for the host
		var jsonArgs = new List<object?>();
		for (int i = 0; i < definition.Parameters.Length && i < args.Length; i++) {
			var param = definition.Parameters[i];
			if (param.IsSigil) {
				jsonArgs.Add(null); // sigil — host will fill this
			} else {
				jsonArgs.Add(MarshalToHost(args[i], param.Type));
			}
		}

		var argsJson = JsonSerializer.Serialize(jsonArgs);
		var resultJson = callHandler(definition.GlamRockName, argsJson);

		// Parse result: { "result": ..., "sigils": { "1": 42, "3": 99 } }
		using var doc = JsonDocument.Parse(resultJson);
		var root = doc.RootElement;

		// Read sigil outputs
		if (root.TryGetProperty("sigils", out var sigils)) {
			foreach (var prop in sigils.EnumerateObject()) {
				if (int.TryParse(prop.Name, out var idx)) {
					sigilOutputs[idx] = MarshalFromHost(prop.Value, InteropType.Mysterious);
				}
			}
		}

		// Read return value
		if (root.TryGetProperty("result", out var result)) {
			return MarshalFromHost(result, definition.ReturnType);
		}

		return Nüll.Instance;
	}

	private static object? MarshalToHost(Value value, InteropType type) => type switch {
		InteropType.Number => value is Numbër n ? (double)n.Value : 0.0,
		InteropType.Boolean => value is Booleän b && b.Truthy,
		InteropType.String => value is Strïng s ? s.Value : "",
		InteropType.Mysterious => value is Numbër num ? (double)num.Value : 0.0,
		InteropType.Nothing => null,
		_ => null
	};

	private static Value MarshalFromHost(JsonElement elem, InteropType type) {
		if (elem.ValueKind == JsonValueKind.Null || elem.ValueKind == JsonValueKind.Undefined)
			return Nüll.Instance;

		return type switch {
			InteropType.Number => new Numbër((decimal)elem.GetDouble()),
			InteropType.Boolean => new Booleän(elem.GetBoolean()),
			InteropType.String => new Strïng(elem.GetString() ?? ""),
			InteropType.Mysterious => new Numbër((decimal)elem.GetDouble()),
			_ => Nüll.Instance
		};
	}

	public override Strïng ToStrïng()
		=> new($"[host-track:{definition.NativeName}]");

	public override int GetHashCode()
		=> definition.NativeName.GetHashCode();

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"host-track: {definition.GlamRockName}");
		return sb;
	}
}
