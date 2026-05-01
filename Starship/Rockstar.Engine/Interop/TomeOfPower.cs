using System.Text;
using Rockstar.Engine.Values;

namespace Rockstar.Engine.Interop;

/// <summary>
/// Built-in album: Tome of Power - handle-based file I/O helpers.
///
/// Usage in GlamRock:
///   Know Tome of Power
///   Let the tome be Open The Tome taking "notes.txt", "w+"
///   Write The Tome taking the tome, "hello"
///   Seek The Tome taking the tome, 0, 0
///   Shout Read The Tome taking the tome
///   Seal The Tome taking the tome
/// </summary>
public static class TomeOfPower {
	private static readonly Dictionary<int, TomeHandle> handles = new();
	private static int nextHandle = 1;
	private static readonly Lock gate = new();

	public static BuiltinAlbum Create() {
		var album = new BuiltinAlbum("Tome of Power");

		album.AddTrack("Open The Tome", OpenTheTome,
			parameters: [
				new InteropParam(InteropType.String),
				new InteropParam(InteropType.String),
			],
			returnType: InteropType.Number
		);

		album.AddTrack("Read The Tome", ReadTheTome,
			parameters: [ new InteropParam(InteropType.Number) ],
			returnType: InteropType.String
		);

		album.AddTrack("Read The Line", ReadTheLine,
			parameters: [ new InteropParam(InteropType.Number) ],
			returnType: InteropType.String
		);

		album.AddTrack("Write The Tome", WriteTheTome,
			parameters: [
				new InteropParam(InteropType.Number),
				new InteropParam(InteropType.String),
			],
			returnType: InteropType.Number
		);

		album.AddTrack("Write The Line", WriteTheLine,
			parameters: [
				new InteropParam(InteropType.Number),
				new InteropParam(InteropType.String),
			],
			returnType: InteropType.Number
		);

		album.AddTrack("Seek The Tome", SeekTheTome,
			parameters: [
				new InteropParam(InteropType.Number),
				new InteropParam(InteropType.Number),
				new InteropParam(InteropType.Number),
			],
			returnType: InteropType.Number
		);

		album.AddTrack("Tell The Tome", TellTheTome,
			parameters: [ new InteropParam(InteropType.Number) ],
			returnType: InteropType.Number
		);

		album.AddTrack("Tome Exhausted", ReachTheEnd,
			parameters: [ new InteropParam(InteropType.Number) ],
			returnType: InteropType.Boolean
		);

		album.AddTrack("Seal The Tome", SealTheTome,
			parameters: [ new InteropParam(InteropType.Number) ],
			returnType: InteropType.Boolean
		);

		return album;
	}

	private static Value OpenTheTome(Value[] args, RockstarEnvironment env) {
		var path = ResolvePath(env, GetString(args, 0));
		var mode = GetString(args, 1);
		var handle = CreateHandle(path, mode);
		return new Numbër(handle);
	}

	private static Value ReadTheTome(Value[] args, RockstarEnvironment env) {
		var handle = GetHandle(args, 0);
		return new Strïng(handle.ReadToEnd());
	}

	private static Value ReadTheLine(Value[] args, RockstarEnvironment env) {
		var handle = GetHandle(args, 0);
		return new Strïng(handle.ReadLine());
	}

	private static Value WriteTheTome(Value[] args, RockstarEnvironment env) {
		var handle = GetHandle(args, 0);
		var text = GetString(args, 1);
		return new Numbër(handle.Write(text));
	}

	private static Value WriteTheLine(Value[] args, RockstarEnvironment env) {
		var handle = GetHandle(args, 0);
		var text = GetString(args, 1) + Environment.NewLine;
		return new Numbër(handle.Write(text));
	}

	private static Value SeekTheTome(Value[] args, RockstarEnvironment env) {
		var handle = GetHandle(args, 0);
		var offset = GetInteger(args, 1);
		var whence = GetInteger(args, 2);
		return new Numbër(handle.Seek(offset, whence));
	}

	private static Value TellTheTome(Value[] args, RockstarEnvironment env) {
		var handle = GetHandle(args, 0);
		return new Numbër(handle.Position);
	}

	private static Value ReachTheEnd(Value[] args, RockstarEnvironment env) {
		var handle = GetHandle(args, 0);
		return new Booleän(handle.AtEnd());
	}

	private static Value SealTheTome(Value[] args, RockstarEnvironment env) {
		var handleId = GetInteger(args, 0);
		lock (gate) {
			if (!handles.Remove(handleId)) {
				return Booleän.False;
			}
		}
		return Booleän.True;
	}

	private static TomeHandle GetHandle(Value[] args, int index) {
		var handleId = GetInteger(args, index);
		lock (gate) {
			if (!handles.TryGetValue(handleId, out var handle)) {
				throw new InvalidOperationException($"Tome handle {handleId} is not open");
			}
			return handle;
		}
	}

	private static int CreateHandle(string path, string mode) {
		var handle = new TomeHandle(path, mode);
		lock (gate) {
			var id = nextHandle++;
			handles[id] = handle;
			return id;
		}
	}

	private static string ResolvePath(RockstarEnvironment env, string path) {
		if (Path.IsPathRooted(path)) {
			return Path.GetFullPath(path);
		}

		var baseDir = env.SourceFilePath != null
			? Path.GetDirectoryName(env.SourceFilePath)!
			: Directory.GetCurrentDirectory();

		return Path.GetFullPath(Path.Combine(baseDir, path));
	}

	private static string GetString(Value[] args, int index)
		=> index < args.Length ? args[index].ToStrïng().Value : "";

	private static int GetInteger(Value[] args, int index)
		=> index < args.Length && args[index] is IHaveANumber n ? n.IntegerValue : 0;

	private sealed class TomeHandle {
		private readonly string path;
		private readonly bool canRead;
		private readonly bool canWrite;
		private readonly bool append;
		private readonly Lock sync = new();

		public TomeHandle(string path, string mode) {
			this.path = path;

			switch (mode.Trim().ToLowerInvariant()) {
				case "r":
					RequireFile(path);
					canRead = true;
					break;
				case "r+":
					RequireFile(path);
					canRead = true;
					canWrite = true;
					break;
				case "w":
					canWrite = true;
					InitializeFile(truncate: true);
					break;
				case "w+":
					canRead = true;
					canWrite = true;
					InitializeFile(truncate: true);
					break;
				case "a":
					canWrite = true;
					append = true;
					InitializeFile(truncate: false);
					Position = GetLength();
					break;
				case "a+":
					canRead = true;
					canWrite = true;
					append = true;
					InitializeFile(truncate: false);
					Position = GetLength();
					break;
				default:
					throw new InvalidOperationException($"Unsupported tome mode '{mode}'. Use r, r+, w, w+, a, or a+.");
			}
		}

		public long Position { get; private set; }

		public string ReadToEnd() {
			EnsureReadable();
			lock (sync) {
				using var stream = Open(FileAccess.Read);
				stream.Position = Position;
				var bytes = new byte[stream.Length - stream.Position];
				var count = stream.Read(bytes, 0, bytes.Length);
				var startedAtZero = Position == 0;
				Position = stream.Position;
				return Decode(bytes, count, startedAtZero);
			}
		}

		public string ReadLine() {
			EnsureReadable();
			lock (sync) {
				using var stream = Open(FileAccess.Read);
				stream.Position = Position;
				var startedAtZero = Position == 0;
				using var buffer = new MemoryStream();

				while (true) {
					var next = stream.ReadByte();
					if (next < 0) {
						Position = stream.Position;
						return Decode(buffer.ToArray(), (int)buffer.Length, startedAtZero);
					}

					if (next == '\n') {
						Position = stream.Position;
						var bytes = buffer.ToArray();
						var count = bytes.Length;
						if (count > 0 && bytes[count - 1] == '\r') {
							count--;
						}
						return Decode(bytes, count, startedAtZero);
					}

					buffer.WriteByte((byte)next);
				}
			}
		}

		public int Write(string text) {
			EnsureWritable();
			lock (sync) {
				using var stream = Open(canRead ? FileAccess.ReadWrite : FileAccess.Write);
				stream.Position = append ? stream.Length : Position;
				using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
				writer.Write(text);
				writer.Flush();
				Position = stream.Position;
				return text.Length;
			}
		}

		public long Seek(int offset, int whence) {
			lock (sync) {
				var length = GetLength();
				var origin = whence switch {
					0 => 0L,
					1 => Position,
					2 => length,
					_ => throw new InvalidOperationException($"Unsupported seek origin {whence}. Use 0, 1, or 2.")
				};

				var next = origin + offset;
				if (next < 0) {
					throw new InvalidOperationException("Tome position cannot be negative");
				}

				Position = next;
				return Position;
			}
		}

		public bool AtEnd() {
			EnsureReadable();
			lock (sync) {
				return Position >= GetLength();
			}
		}

		private void EnsureReadable() {
			if (!canRead) {
				throw new InvalidOperationException("This tome was not opened for reading");
			}
		}

		private void EnsureWritable() {
			if (!canWrite) {
				throw new InvalidOperationException("This tome was not opened for writing");
			}
		}

		private void InitializeFile(bool truncate) {
			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir)) {
				Directory.CreateDirectory(dir);
			}

			using var stream = new FileStream(
				path,
				truncate ? FileMode.Create : FileMode.OpenOrCreate,
				FileAccess.ReadWrite,
				FileShare.ReadWrite);
			if (truncate) {
				stream.SetLength(0);
			}
		}

		private static void RequireFile(string path) {
			if (!File.Exists(path)) {
				throw new FileNotFoundException($"Tome not found: {path}");
			}
		}

		private FileStream Open(FileAccess access) => new(
			path,
			FileMode.OpenOrCreate,
			access,
			FileShare.ReadWrite);

		private long GetLength() {
			using var stream = Open(FileAccess.Read);
			return stream.Length;
		}

		private static string Decode(byte[] bytes, int count, bool stripBom) {
			var text = Encoding.UTF8.GetString(bytes, 0, count);
			return stripBom ? text.TrimStart('\uFEFF') : text;
		}
	}
}
