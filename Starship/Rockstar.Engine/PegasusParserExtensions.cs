using Pegasus.Common;

namespace Rockstar.Engine;

public static class PegasusParserExtensions {

	private static readonly char[] splitters = " ?!.,;".ToCharArray();

	public static string? ErrorToken(this Cursor cursor) {
		var line = GetLine(cursor.Subject, cursor.Line);
		if (line == null) return null;
		if (line.Length < cursor.Column) return null;
		var token = line[(cursor.Column - 1)..].Split(splitters).FirstOrDefault("");
		return token;
	}

	public static string FormatError(this Cursor cursor, FormatException ex)
		=> (cursor.ErrorToken() == null ? "Error" : $"Unexpected '{cursor.ErrorToken()}'")
			+ $" at line {cursor.Line} col {cursor.Column}"
			+ ": " + ex.Message;

	private static string? GetLine(string subject, int lineNumber) {
		if (lineNumber < 1) return null;

		var currentLine = 1;
		var start = 0;

		for (var i = 0; i < subject.Length; i++) {
			if (subject[i] != '\r' && subject[i] != '\n') continue;

			if (currentLine == lineNumber) {
				return subject[start..i];
			}

			if (subject[i] == '\r' && i + 1 < subject.Length && subject[i + 1] == '\n') i++;
			currentLine++;
			start = i + 1;
		}

		return currentLine == lineNumber ? subject[start..] : null;
	}
}
