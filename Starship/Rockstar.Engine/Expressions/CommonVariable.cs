namespace Rockstar.Engine.Expressions;

public class CommonVariable(string name) : Variable(name) {
	public override string Key => NormalizedName.ToLower();
}

public class ProperVariable : Variable {
	public ProperVariable(string name) : base(name) {
		if (!IsValidProperVariableName(name)) throw new ArgumentException($"'{name}' is not a valid proper variable name");
	}

	public override string Key => NormalizedName.ToUpperInvariant();

	private static bool IsValidProperVariableName(string name) {
		var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length < 2) return false;

		var nounCount = 0;
		for (var i = 0; i < parts.Length; i++) {
			var part = parts[i];

			if (IsConnector(part)) {
				if (i == 0 || i == parts.Length - 1) return false;
				continue;
			}

			if (!IsProperNoun(part)) return false;
			nounCount++;
		}

		if (nounCount < 2) return false;

		for (var i = 1; i < parts.Length; i++) {
			if (IsConnector(parts[i]) && IsConnector(parts[i - 1])) return false;
		}

		return true;
	}

	private static bool IsConnector(string value)
		=> value.Equals("of", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("the", StringComparison.OrdinalIgnoreCase);

	private static bool IsProperNoun(string value) {
		if (value.Length == 2 && char.IsUpper(value[0]) && value[1] == '.') return true;
		if (value.Length == 0 || !char.IsUpper(value[0])) return false;

		for (var i = 1; i < value.Length; i++) {
			var ch = value[i];
			if (!char.IsLetterOrDigit(ch) && ch != '_') return false;
		}

		return true;
	}
}
