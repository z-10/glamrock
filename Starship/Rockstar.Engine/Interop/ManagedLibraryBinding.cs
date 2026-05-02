using System.Reflection;
using System.Runtime.InteropServices;
using Rockstar.Engine.Values;

namespace Rockstar.Engine.Interop;

/// <summary>
/// Mixtape backend. Two modes:
///   1. Assembly mode — LibraryPath is a managed DLL. FEATURING is a fully-qualified
///      "Namespace.Type.Method" reference resolved against the loaded assembly.
///      Static methods only.
///   2. ProgID mode — LibraryPath starts with "@ProgID:". A single COM instance is
///      created via Type.GetTypeFromProgID. FEATURING uses dotted member chains:
///        "play"            → InvokeMethod on the root
///        "controls.play"   → GetProperty controls, InvokeMethod play
///        "URL="            → SetProperty URL on the root
///        "settings.volume=" → GetProperty settings, SetProperty volume
///      A bare member with no TAKES and a non-Nothing GIVES is treated as a getter;
///      otherwise it is invoked as a method.
/// </summary>
public class ManagedLibraryBinding : IDisposable {

	public TracklistFile Tracklist { get; }
	private readonly Assembly? assembly;
	private readonly object? comInstance;
	private readonly Dictionary<string, MixtapeTrack> tracks = new(StringComparer.OrdinalIgnoreCase);
	private bool disposed;

	public const string ProgIdPrefix = "@ProgID:";

	public ManagedLibraryBinding(TracklistFile tracklist) {
		Tracklist = tracklist;

		if (IsProgIdReference(tracklist.LibraryPath, out var progId)) {
			comInstance = CreateComInstance(progId);
		} else {
			assembly = LoadAssembly(tracklist);
		}

		foreach (var def in tracklist.Tracks) {
			tracks[def.GlamRockName] = new MixtapeTrack(def, comInstance, assembly);
		}
	}

	internal static bool IsProgIdReference(string libraryPath, out string progId) {
		if (libraryPath.StartsWith(ProgIdPrefix, StringComparison.OrdinalIgnoreCase)) {
			progId = libraryPath[ProgIdPrefix.Length..].Trim();
			return progId.Length > 0;
		}
		progId = "";
		return false;
	}

	private static object CreateComInstance(string progId) {
		if (!OperatingSystem.IsWindows()) {
			throw new PlatformNotSupportedException(
				$"COM ProgID '@ProgID:{progId}' requires Windows.");
		}
		var type = Type.GetTypeFromProgID(progId, throwOnError: false);
		if (type == null) {
			throw new InvalidOperationException(
				$"COM ProgID '{progId}' could not be resolved on this machine.");
		}
		return Activator.CreateInstance(type)
			?? throw new InvalidOperationException(
				$"Activator.CreateInstance returned null for ProgID '{progId}'.");
	}

	internal static string? ResolveRelativeAssemblyPath(TracklistFile tracklist) {
		if (Path.IsPathRooted(tracklist.LibraryPath) || tracklist.SourcePath == null) {
			return null;
		}
		var baseDir = Path.GetDirectoryName(tracklist.SourcePath);
		return string.IsNullOrEmpty(baseDir)
			? null
			: Path.GetFullPath(Path.Combine(baseDir, tracklist.LibraryPath));
	}

	private static Assembly LoadAssembly(TracklistFile tracklist) {
		var relative = ResolveRelativeAssemblyPath(tracklist);
		if (relative != null && File.Exists(relative)) {
			return Assembly.LoadFrom(relative);
		}
		if (File.Exists(tracklist.LibraryPath)) {
			return Assembly.LoadFrom(tracklist.LibraryPath);
		}
		// Treat as assembly simple name in the current load context.
		var simple = Path.GetFileNameWithoutExtension(tracklist.LibraryPath);
		return Assembly.Load(simple);
	}

	public MixtapeTrack? GetTrack(string glamRockName)
		=> tracks.GetValueOrDefault(glamRockName);

	public IReadOnlyDictionary<string, MixtapeTrack> Tracks => tracks;

	public void Dispose() {
		if (disposed) return;
		disposed = true;
		if (comInstance != null && OperatingSystem.IsWindows() && Marshal.IsComObject(comInstance)) {
			try { Marshal.FinalReleaseComObject(comInstance); }
			catch { /* COM may already be released */ }
		}
		GC.SuppressFinalize(this);
	}

	~ManagedLibraryBinding() => Dispose();
}

public class MixtapeTrack {

	public TrackDefinition Definition { get; }
	private readonly object? comInstance;
	private readonly Assembly? assembly;
	private readonly bool isComMode;

	public MixtapeTrack(TrackDefinition definition, object? comInstance, Assembly? assembly) {
		Definition = definition;
		this.comInstance = comInstance;
		this.assembly = assembly;
		isComMode = comInstance != null;
	}

	public Value Call(Value[] args) {
		var nativeArgs = new object?[args.Length];
		for (int i = 0; i < args.Length; i++) {
			var paramType = i < Definition.Parameters.Length
				? Definition.Parameters[i].Type
				: InteropType.Mysterious;
			nativeArgs[i] = MixtapeMarshal.ToNative(args[i], paramType);
		}

		object? result = isComMode
			? InvokeCom(nativeArgs)
			: InvokeStatic(nativeArgs);

		return MixtapeMarshal.FromNative(result, Definition.ReturnType);
	}

	private object? InvokeCom(object?[] args) {
		var (target, member, isSetter) = ResolveComTarget();
		var type = target.GetType();

		if (isSetter) {
			type.InvokeMember(member,
				BindingFlags.SetProperty,
				binder: null, target, args);
			return null;
		}

		// Treat parameterless track with a non-Nothing return as a property getter,
		// falling back to method invocation if no such property exists.
		if (args.Length == 0 && Definition.ReturnType != InteropType.Nothing) {
			try {
				return type.InvokeMember(member,
					BindingFlags.GetProperty,
					binder: null, target, null);
			} catch (MissingMemberException) {
				// fall through to method dispatch
			}
		}

		return type.InvokeMember(member,
			BindingFlags.InvokeMethod,
			binder: null, target, args);
	}

	private (object Target, string Member, bool IsSetter) ResolveComTarget() {
		var featuring = Definition.NativeName.Trim();
		var isSetter = featuring.EndsWith('=');
		if (isSetter) featuring = featuring[..^1];

		var parts = featuring.Split('.');
		object target = comInstance!;
		var targetType = target.GetType();
		for (int i = 0; i < parts.Length - 1; i++) {
			var next = targetType.InvokeMember(parts[i],
				BindingFlags.GetProperty,
				binder: null, target, null);
			if (next == null) {
				throw new InvalidOperationException(
					$"COM property '{parts[i]}' returned null while resolving '{Definition.NativeName}'.");
			}
			target = next;
			targetType = target.GetType();
		}
		return (target, parts[^1], isSetter);
	}

	private object? InvokeStatic(object?[] args) {
		var featuring = Definition.NativeName;
		var lastDot = featuring.LastIndexOf('.');
		if (lastDot <= 0) {
			throw new InvalidOperationException(
				$"Mixtape FEATURING '{featuring}' must be a fully-qualified Type.Method reference.");
		}
		var typeName = featuring[..lastDot];
		var methodName = featuring[(lastDot + 1)..];

		var type = assembly!.GetType(typeName)
			?? Type.GetType(typeName)
			?? throw new TypeLoadException(
				$"Mixtape type '{typeName}' not found in assembly '{assembly.GetName().Name}'.");

		var argTypes = args.Select(a => a?.GetType() ?? typeof(object)).ToArray();
		var method = type.GetMethod(methodName,
			BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase,
			binder: null, argTypes, modifiers: null);

		method ??= type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
			.FirstOrDefault(m =>
				string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase) &&
				m.GetParameters().Length == args.Length);

		if (method == null) {
			throw new MissingMethodException(
				$"Static method '{methodName}' on type '{typeName}' not found with {args.Length} arguments.");
		}

		var coerced = CoerceForMethod(args, method.GetParameters());
		return method.Invoke(null, coerced);
	}

	private static object?[] CoerceForMethod(object?[] args, ParameterInfo[] parameters) {
		var result = new object?[args.Length];
		for (int i = 0; i < args.Length; i++) {
			result[i] = i < parameters.Length
				? MixtapeMarshal.CoerceToParameterType(args[i], parameters[i].ParameterType)
				: args[i];
		}
		return result;
	}
}

internal static class MixtapeMarshal {

	public static object? ToNative(Value value, InteropType type) => type switch {
		InteropType.Number => value is Numbër n ? (object)n.Value : 0m,
		InteropType.Boolean => value is Booleän b && b.Truthy,
		InteropType.String => value is Strïng s ? s.Value : "",
		InteropType.Mysterious => value switch {
			Numbër n => (object)(long)n.Value,
			Strïng s => s.Value,
			Booleän b => b.Truthy,
			Nüll => null!,
			_ => null!,
		},
		InteropType.Nothing => null,
		_ => null,
	};

	public static Value FromNative(object? result, InteropType type) {
		if (result == null) {
			return type == InteropType.String ? new Strïng("") : Nüll.Instance;
		}
		return type switch {
			InteropType.Number => new Numbër(Convert.ToDecimal(result)),
			InteropType.Boolean => new Booleän(Convert.ToBoolean(result)),
			InteropType.String => new Strïng(Convert.ToString(result) ?? ""),
			InteropType.Mysterious => result switch {
				IConvertible c when result is not string => new Numbër(Convert.ToDecimal(c)),
				_ => new Strïng(Convert.ToString(result) ?? ""),
			},
			InteropType.Nothing => Nüll.Instance,
			_ => Nüll.Instance,
		};
	}

	public static object? CoerceToParameterType(object? value, Type targetType) {
		if (value == null) {
			return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
				? Activator.CreateInstance(targetType)
				: null;
		}
		if (targetType.IsInstanceOfType(value)) return value;
		if (targetType == typeof(object)) return value;
		try {
			return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
		} catch {
			return value;
		}
	}
}
