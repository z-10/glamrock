using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Channeling(Variable module, Variable? alias = null) : Statement {
	public Variable Module => module;
	public Variable? Alias => alias;

	public string ModulePath => Module.Name.ToLower().Replace(" ", "_");

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"channeling: {ModulePath}");
		if (alias != null) {
			sb.Append(prefix + INDENT).Append("as: ");
			alias.Print(sb, "");
		}
		return sb;
	}
}
