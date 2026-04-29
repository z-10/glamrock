using System.Text;
using Rockstar.Engine.Expressions;
using Rockstar.Engine.Statements;

namespace Rockstar.Engine.Values;

public class FunctionCall : Statement {
	public Variable? Function { get; }
	public Expression? FunctionExpression { get; }
	public List<Expression> Args { get; }

	public FunctionCall(Variable function, IEnumerable<Expression>? args = default) {
		Function = function;
		Args = (args ?? []).ToList();
	}

	public FunctionCall(Expression functionExpr, IEnumerable<Expression>? args = default) {
		FunctionExpression = functionExpr;
		Args = (args ?? []).ToList();
	}

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		if (Function != null) {
			sb.Append(prefix).AppendLine($"function call: {Function.Name}");
		} else {
			sb.Append(prefix).AppendLine("function call:");
			FunctionExpression!.Print(sb, prefix + INDENT);
		}
		foreach (var arg in Args) arg.Print(sb, prefix + INDENT);
		return sb;
	}

	public override string ToString() => Function != null 
		? $"call: {Function.Key}({String.Join(", ", Args.Select(a => a.ToString()))})"
		: $"call: expr({String.Join(", ", Args.Select(a => a.ToString()))})";
}