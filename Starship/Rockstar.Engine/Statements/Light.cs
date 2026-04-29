using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Light(List<Variable> variables) : Statement {
	public List<Variable> Variables => variables;

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.AppendLine(prefix + "light:");
		foreach (var v in variables) v.Print(sb, prefix + INDENT);
		return sb;
	}
}
