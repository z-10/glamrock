using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Invoke(Variable module) : Statement {
	public Variable Module => module;

	public string TracklistPath => Module.Name.ToLower().Replace(" ", "_");

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"invoke: {TracklistPath}");
		return sb;
	}
}
