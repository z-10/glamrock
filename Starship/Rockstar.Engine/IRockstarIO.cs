namespace Rockstar.Engine;

public interface IRockstarIO {
	public string? Read();
	public void Write(string s);
	public void WriteLine(string s);
	public void WriteLine() => WriteLine(String.Empty);

	public void WriteError(ParserException ex, string source) {
		this.WriteLine(ex.Message);
		this.WriteLine();
		var lines = SplitLines("\n" + source);
		var digits = lines.Length.ToString().Length;
		for (var i = ex.Line - 2; i < ex.Line + 2; i++) {
			if (i < 0 || i >= lines.Length) continue;
			this.WriteLine($"{i.ToString().PadLeft(digits, ' ')}: {lines[i]}");
			if (i == ex.Line) this.WriteLine(String.Empty.PadRight(digits + 1 + ex.Column, ' ') + "^");
		}
	}

	private static string[] SplitLines(string source) {
		var lines = new List<string>();
		var start = 0;

		for (var i = 0; i < source.Length; i++) {
			if (source[i] != '\r' && source[i] != '\n') continue;

			lines.Add(source[start..i]);
			if (source[i] == '\r' && i + 1 < source.Length && source[i + 1] == '\n') i++;
			start = i + 1;
		}

		lines.Add(source[start..]);
		return [.. lines];
	}
}
