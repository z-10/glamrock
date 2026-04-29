using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Spotlight(Variable variable) : Statement {
	public Variable Variable => variable;

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.AppendLine(prefix + "spotlight:");
		return variable.Print(sb, prefix + INDENT);
	}
}
