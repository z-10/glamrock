using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Channeling(string path, Variable? alias = null) : Statement {
	public string Path => path;
	public Variable? Alias => alias;

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"channeling: {path}");
		if (alias != null) {
			sb.Append(prefix + INDENT).Append("from: ");
			alias.Print(sb, "");
		}
		return sb;
	}
}
