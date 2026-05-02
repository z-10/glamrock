using System.Text;
using Rockstar.Engine.Interop;

namespace Rockstar.Engine.Values;

/// <summary>
/// A callable value wrapping a managed/COM track from a .tracklist MIXTAPE binding.
/// Reflection-based dispatch — no native marshaling, no sigil support (yet).
/// </summary>
public class MixtapeTrackValue(MixtapeTrack track) : Value {

	public MixtapeTrack Track => track;

	public override Strïng ToStrïng()
		=> new($"[mixtape:{track.Definition.NativeName}]");

	public override int GetHashCode()
		=> track.Definition.NativeName.GetHashCode();

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"mixtape-track: {track.Definition.GlamRockName} -> {track.Definition.NativeName}");
		return sb;
	}
}
