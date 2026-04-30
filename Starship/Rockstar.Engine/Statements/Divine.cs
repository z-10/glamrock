using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Divine(Expression command, Variable? target = null) : Statement {
	public Expression Command => command;
	public Variable? Target => target;

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine("divine:");
		command.Print(sb, prefix + INDENT);
		if (target != null) {
			sb.Append(prefix + INDENT).Append("into: ");
			target.Print(sb, "");
		}
		return sb;
	}
}
