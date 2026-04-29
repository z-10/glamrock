using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class Shining(Variable variable) : Statement {
	public Variable Variable => variable;

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.AppendLine(prefix + "shining:");
		return variable.Print(sb, prefix + INDENT);
	}
}
