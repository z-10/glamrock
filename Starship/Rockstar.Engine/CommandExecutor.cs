namespace Rockstar.Engine;

public record CommandResult(string Stdout, string Stderr, int ExitCode);

public abstract class CommandExecutorBase {
	public abstract CommandResult Execute(string command);
}

public class ProcessCommandExecutor : CommandExecutorBase {
	public override CommandResult Execute(string command) {
		var isWindows = OperatingSystem.IsWindows();
		var psi = new System.Diagnostics.ProcessStartInfo {
			FileName = isWindows ? "cmd.exe" : "/bin/sh",
			Arguments = isWindows ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = System.Diagnostics.Process.Start(psi)
			?? throw new("Failed to start process");
		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();
		return new CommandResult(
			stdout.TrimEnd('\r', '\n'),
			stderr.TrimEnd('\r', '\n'),
			process.ExitCode
		);
	}
}
