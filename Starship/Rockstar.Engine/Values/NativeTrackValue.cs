using System.Text;
using Rockstar.Engine.Expressions;
using Rockstar.Engine.Interop;

namespace Rockstar.Engine.Values;

/// <summary>
/// A callable value wrapping a native function from a .tracklist binding.
/// Unlike Closure/Functiön, this handles its own argument marshaling
/// and supports sigil (output) parameters.
/// </summary>
public class NativeTrackValue(NativeTrack track) : Value {

	public NativeTrack Track => track;

	public override Strïng ToStrïng()
		=> new($"[native:{track.Definition.NativeName}]");

	public override int GetHashCode()
		=> track.Definition.NativeName.GetHashCode();

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"native-track: {track.Definition.GlamRockName} -> {track.Definition.NativeName}");
		return sb;
	}
}
