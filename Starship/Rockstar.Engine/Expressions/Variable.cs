using System.Text;

namespace Rockstar.Engine.Expressions;

public abstract class Variable(string name) : Expression {

	public string Name => name;

	protected string NormalizedName { get; init; } = NormalizeName(name);

	public override string ToString()
		=> $"{GetType().Name.ToLower()}: {Key}"
			+ (Indexes.Any() ? "[" + String.Join("][", Indexes.Select(i => i.ToString())) + "]" : "");

	public override StringBuilder Print(StringBuilder sb, string prefix) {
		sb.Append(prefix).AppendLine($"variable: {name}");
		switch (Indexes.Count) {
			case 0: return sb;
			case 1:
				sb.Append(prefix).AppendLine("index:");
				break;
			default:
				sb.Append(prefix).AppendLine("indexes:");
				break;
		}
		foreach (var index in Indexes) index.Print(sb, prefix + INDENT);
		return sb;
	}

	private static string NormalizeName(string value) {
		var sb = new StringBuilder(value.Length);
		var pendingSeparator = false;

		foreach (var ch in value) {
			if (char.IsWhiteSpace(ch)) {
				pendingSeparator = sb.Length > 0;
				continue;
			}

			if (pendingSeparator) {
				sb.Append('_');
				pendingSeparator = false;
			}

			sb.Append(ch);
		}

		return sb.ToString();
	}

	public abstract string Key { get; }

	public IEnumerable<Variable> Concat(IEnumerable<Variable> tail)
		=> new List<Variable> { this }.Concat(tail);

	public List<Expression> Indexes { get; } = [];

	public bool ShouldUpdatePronounWhenAssigned
		=> this is not Pronoun;

	public Variable AtIndex(Expression index) {
		Indexes.Add(index);
		return this;
	}

	public Variable AtIndex(IEnumerable<Expression> indexes) {
		Indexes.AddRange(indexes);
		return this;
	}

	public string PrintIndexes() {
		if (Indexes.Any()) return ("[" + String.Join("][", Indexes.Select(i => i.ToString()).ToArray()) + "]");
		return String.Empty;
	}
}
