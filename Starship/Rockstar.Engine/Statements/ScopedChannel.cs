using System.Text;
using Rockstar.Engine.Expressions;

namespace Rockstar.Engine.Statements;

public class ScopedChannel(Variable module, Block body) : Statement {
	public Variable Module => module;
	public Block Body => body;

	public string ModulePath => Module.Name.ToLower().Replace(" ", "_");

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"channeling: {ModulePath}");
		sb.Append(prefix + INDENT).AppendLine("body:");
		foreach (var stmt in body.Statements) stmt.Print(sb, prefix + INDENT + INDENT);
		return sb;
	}
}
