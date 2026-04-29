using System.Text;

namespace Rockstar.Engine.Expressions;

public class ModuleLookup(Variable member, Variable module) : Expression {
	public Variable Member => member;
	public Variable Module => module;

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine("module lookup:");
		sb.Append(prefix + INDENT).Append("member: ");
		member.Print(sb, "");
		sb.Append(prefix + INDENT).Append("from: ");
		module.Print(sb, "");
		return sb;
	}
}
