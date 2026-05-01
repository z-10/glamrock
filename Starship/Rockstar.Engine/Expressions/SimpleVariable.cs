namespace Rockstar.Engine.Expressions;

public class SimpleVariable : Variable {
	public SimpleVariable(string name) :base(name) {
		if (!IsValidSimpleVariableName(name)) throw new ArgumentException($"{name} is not a valid simple variable name");
	}
	public override string Key => Name.ToLower();

	private static bool IsValidSimpleVariableName(string name) {
		if (string.IsNullOrEmpty(name)) return false;

		for (var i = 0; i < name.Length; i++) {
			var ch = name[i];
			if (!char.IsLetterOrDigit(ch) && ch != '_') return false;
		}

		return true;
	}
}
