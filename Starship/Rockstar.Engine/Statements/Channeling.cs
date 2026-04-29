using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Channeling(Variable module, List<Variable>? imports = null) : Statement {
	public Variable Module => module;
	public List<Variable>? Imports => imports;

	public string ModulePath => Module.Name.ToLower().Replace(" ", "_");

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"channel: {ModulePath}");
		if (imports != null) {
			foreach (var v in imports) {
				sb.Append(prefix + INDENT).Append("import: ");
				v.Print(sb, "");
			}
		}
		return sb;
	}
}
