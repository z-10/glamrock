using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Rockstar.Engine.Values;

namespace Rockstar.Engine.Interop;

/// <summary>
/// Wraps a single native function from a loaded DLL,
/// handling type marshaling between GlamRock values and native types.
/// </summary>
public class NativeTrack {
	public TrackDefinition Definition { get; }
	private readonly IntPtr functionPointer;
	private readonly Type delegateType;
	private readonly Delegate nativeDelegate;

	public NativeTrack(TrackDefinition definition, IntPtr libraryHandle) {
		Definition = definition;

		functionPointer = NativeLibrary.GetExport(libraryHandle, definition.NativeName);
		delegateType = BuildDelegateType(definition);
		nativeDelegate = Marshal.GetDelegateForFunctionPointer(functionPointer, delegateType);
	}

	/// <summary>
	/// Call this track with GlamRock values. Returns the result and fills sigil outputs.
	/// sigilOutputs[i] receives the output value for each sigil parameter.
	/// </summary>
	public Value Call(Value[] args, out Dictionary<int, Value> sigilOutputs) {
		sigilOutputs = new();
		var nativeArgs = new object?[Definition.Parameters.Length];
		var sigilPtrs = new Dictionary<int, GCHandle>();
		var sigilStrings = new Dictionary<int, StringBuilder>();
		var invokeParameters = delegateType.GetMethod("Invoke")!.GetParameters();

		try {
			for (int i = 0; i < Definition.Parameters.Length; i++) {
				var param = Definition.Parameters[i];
				var arg = i < args.Length ? args[i] : Nüll.Instance;

				if (param.IsSigil) {
					if (param.Type == InteropType.String) {
						var buffer = new StringBuilder(4096);
						sigilStrings[i] = buffer;
						nativeArgs[i] = buffer;
					} else {
						// Allocate a pinned IntPtr for the native function to write into
						var box = new IntPtr[1];
						var handle = GCHandle.Alloc(box, GCHandleType.Pinned);
						sigilPtrs[i] = handle;
						nativeArgs[i] = handle.AddrOfPinnedObject();
					}
				} else {
					nativeArgs[i] = MarshalToNative(arg, param.Type);
				}
			}

			for (int i = 0; i < nativeArgs.Length; i++) {
				nativeArgs[i] = CoerceNativeArgument(nativeArgs[i], invokeParameters[i].ParameterType);
			}

			object? result;
			try {
				result = nativeDelegate.DynamicInvoke(nativeArgs);
			} catch (Exception ex) {
				var expected = string.Join(", ", delegateType.GetMethod("Invoke")!.GetParameters()
					.Select(p => p.ParameterType.Name));
				var actual = string.Join(", ", nativeArgs.Select(a => a?.GetType().Name ?? "null"));
				throw new InvalidOperationException(
					$"Native call '{Definition.NativeName}' failed. Expected [{expected}], got [{actual}].",
					ex
				);
			}

			// Read back sigil outputs
			foreach (var (idx, handle) in sigilPtrs) {
				var box = (IntPtr[])handle.Target!;
				sigilOutputs[idx] = new Numbër((decimal)box[0]);
			}

			foreach (var (idx, buffer) in sigilStrings) {
				var text = buffer.ToString();
				var terminator = text.IndexOf('\0');
				if (terminator >= 0) {
					text = text[..terminator];
				}
				sigilOutputs[idx] = new Str\u00efng(text);
			}

			return MarshalFromNative(result, Definition.ReturnType);
		} finally {
			// Free pinned handles
			foreach (var handle in sigilPtrs.Values) {
				handle.Free();
			}
			// Free marshaled strings
			for (int i = 0; i < nativeArgs.Length; i++) {
				if (!Definition.Parameters[i].IsSigil &&
					Definition.Parameters[i].Type == InteropType.String &&
					nativeArgs[i] is IntPtr ptr && ptr != IntPtr.Zero) {
					Marshal.FreeHGlobal(ptr);
				}
			}
		}
	}

	private static object? CoerceNativeArgument(object? value, Type expectedType) {
		if (expectedType == typeof(StringBuilder)) {
			return value;
		}

		if (expectedType == typeof(IntPtr)) {
			return value switch {
				null => IntPtr.Zero,
				IntPtr ptr => ptr,
				int i => new IntPtr(i),
				long l => new IntPtr(l),
				decimal d => new IntPtr((long)d),
				_ => value
			};
		}

		if (expectedType == typeof(int)) {
			return value switch {
				null => 0,
				int i => i,
				bool b => b ? 1 : 0,
				IntPtr ptr => ptr.ToInt32(),
				long l => checked((int)l),
				decimal d => (int)d,
				_ => Convert.ToInt32(value)
			};
		}

		return value;
	}

	private static object? MarshalToNative(Value value, InteropType type) => type switch {
		InteropType.Number => (int)(value is Numbër n ? n.Value : 0m),
		InteropType.Boolean => (value is Booleän b && b.Truthy) ? 1 : 0,
		InteropType.String => value is Strïng s
			? Marshal.StringToHGlobalUni(s.Value)
			: IntPtr.Zero,
		InteropType.Mysterious => value is Numbër num
			? new IntPtr((long)num.Value)
			: IntPtr.Zero,
		InteropType.Nothing => IntPtr.Zero,
		_ => throw new ArgumentException($"Cannot marshal GlamRock value to {type}")
	};

	private static Value MarshalFromNative(object? result, InteropType type) => type switch {
		InteropType.Number => new Numbër(Convert.ToDecimal(result ?? 0)),
		InteropType.Boolean => new Booleän(Convert.ToInt32(result ?? 0) != 0),
		InteropType.String => result is IntPtr sPtr && sPtr != IntPtr.Zero
			? new Strïng(Marshal.PtrToStringUni(sPtr) ?? "")
			: new Strïng(""),
		InteropType.Mysterious => result is IntPtr ptr
			? new Numbër((decimal)(long)ptr)
			: new Numbër(0m),
		InteropType.Nothing => Nüll.Instance,
		_ => Nüll.Instance
	};

	/// <summary>
	/// Builds a dynamic delegate type matching the native function signature.
	/// </summary>
	private static Type BuildDelegateType(TrackDefinition definition) {
		var paramTypes = definition.Parameters.Select(p => p.IsSigil
			? p.Type == InteropType.String
				? typeof(StringBuilder)
				: typeof(IntPtr)  // non-string sigil passes pointer to output location
			: GetManagedType(p.Type)
		).ToArray();

		var returnType = GetManagedType(definition.ReturnType);

		// Use Reflection.Emit to create a delegate type at runtime
		var assemblyName = new AssemblyName("GlamRockInteropDelegates");
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
			assemblyName, AssemblyBuilderAccess.Run);
		var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

		var typeBuilder = moduleBuilder.DefineType(
			$"NativeDelegate_{definition.NativeName}_{Guid.NewGuid():N}",
			TypeAttributes.Public | TypeAttributes.Sealed,
			typeof(MulticastDelegate));
		var unmanagedFunctionPointerCtor = typeof(UnmanagedFunctionPointerAttribute).GetConstructor([typeof(CallingConvention)])!;
		var charSetField = typeof(UnmanagedFunctionPointerAttribute).GetField(nameof(UnmanagedFunctionPointerAttribute.CharSet))!;
		typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
			unmanagedFunctionPointerCtor,
			[CallingConvention.Winapi],
			[charSetField],
			[CharSet.Unicode]));

		// Constructor
		var ctorBuilder = typeBuilder.DefineConstructor(
			MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
			CallingConventions.Standard,
			new[] { typeof(object), typeof(IntPtr) });
		ctorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

		// Invoke method
		var invokeBuilder = typeBuilder.DefineMethod(
			"Invoke",
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
			returnType,
			paramTypes);
		var marshalAsCtor = typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)])!;
		var lpwStrAttribute = new CustomAttributeBuilder(marshalAsCtor, [UnmanagedType.LPWStr]);
		for (var i = 0; i < paramTypes.Length; i++) {
			if (paramTypes[i] == typeof(string) || paramTypes[i] == typeof(StringBuilder)) {
				var parameter = invokeBuilder.DefineParameter(i + 1, ParameterAttributes.None, null);
				parameter.SetCustomAttribute(lpwStrAttribute);
			}
		}
		invokeBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

		return typeBuilder.CreateType();
	}

	private static Type GetManagedType(InteropType type) => type switch {
		InteropType.Number => typeof(int),
		InteropType.Boolean => typeof(int),
		InteropType.String => typeof(IntPtr),  // we marshal strings manually
		InteropType.Mysterious => typeof(IntPtr),
		InteropType.Nothing => typeof(void),
		_ => typeof(IntPtr)
	};
}

/// <summary>
/// Loads a native library and provides access to its tracks.
/// </summary>
public class NativeLibraryBinding : IDisposable {
	public TracklistFile Tracklist { get; }
	private readonly IntPtr libraryHandle;
	private readonly Dictionary<string, NativeTrack> tracks = new(StringComparer.OrdinalIgnoreCase);
	private bool disposed;

	public NativeLibraryBinding(TracklistFile tracklist) {
		Tracklist = tracklist;
		libraryHandle = LoadLibrary(tracklist);

		foreach (var def in tracklist.Tracks) {
			tracks[def.GlamRockName] = new NativeTrack(def, libraryHandle);
		}
	}

	internal static string? ResolveRelativeLibraryPath(TracklistFile tracklist) {
		if (Path.IsPathRooted(tracklist.LibraryPath) || tracklist.SourcePath == null) {
			return null;
		}

		var baseDir = Path.GetDirectoryName(tracklist.SourcePath);
		return string.IsNullOrEmpty(baseDir)
			? null
			: Path.GetFullPath(Path.Combine(baseDir, tracklist.LibraryPath));
	}

	private static IntPtr LoadLibrary(TracklistFile tracklist) {
		var relativePath = ResolveRelativeLibraryPath(tracklist);
		if (relativePath != null) {
			try {
				return NativeLibrary.Load(relativePath);
			} catch (DllNotFoundException) when (!File.Exists(relativePath)) {
				// Fall back to the platform loader for system libraries like gdi32.dll.
			}
		}

		return NativeLibrary.Load(tracklist.LibraryPath);
	}

	public NativeTrack? GetTrack(string glamRockName)
		=> tracks.GetValueOrDefault(glamRockName);

	public IReadOnlyDictionary<string, NativeTrack> Tracks => tracks;

	public void Dispose() {
		if (disposed) return;
		disposed = true;
		NativeLibrary.Free(libraryHandle);
		GC.SuppressFinalize(this);
	}

	~NativeLibraryBinding() => Dispose();
}
