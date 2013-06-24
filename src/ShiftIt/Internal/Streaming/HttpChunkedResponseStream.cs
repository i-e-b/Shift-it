using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using ShiftIt.Http;

namespace ShiftIt.Internal.Socket
{
	public class HttpChunkedResponseStream : IHttpResponseStream
	{
		Stream _source;
		readonly object _lock;
		const int BufferSize = 4096;

		public TimeSpan Timeout { get; set; }

		public HttpChunkedResponseStream(Stream source)
		{
			_source = source;

			_lock = new object();
			Timeout = HttpClient.DefaultTimeout;
			Complete = false;
		}

		~HttpChunkedResponseStream()
		{
			Dispose();
		}

		public bool Complete { get; protected set; }
		
		public long ExpectedLength { get { return 0; } }

		public string ReadStringToLength()
		{
			return Encoding.UTF8.GetString(ReadBytesToLength());
		}

		public string ReadStringToTimeout()
		{
			return Encoding.UTF8.GetString(ReadBytesToTimeout());
		}

		public byte[] ReadNextChunk()
		{
			lock (_lock)
			{
				if (Complete) return new byte[0];
				var ms = new MemoryStream();

				// assuming we at the beginning of a chunk. Not thread safe!

				var length = ReadChunkLength(_source);
				if (length == 0)
				{
					Complete = true;
					return new byte[0];
				}

				var maybeNewline = _source.ReadByte();
				if (maybeNewline >= 0 && maybeNewline != '\r' && maybeNewline != '\n')
				{
					ms.WriteByte((byte) maybeNewline);
					length--;
				}

				CopyBytesToLength(_source, ms, length, Timeout);

				// Should end with CRLF:
				// TODO: make this resilient to LF-only output
				_source.ReadByte();
				_source.ReadByte();

				return ms.ToArray();
			}
		}

		/// <summary>
		/// Read chunk length. Throws if chunk length not available.
		/// Should leave a spare '\n' char on stream if protocol correct.
		/// </summary>
		static long ReadChunkLength(Stream source)
		{
			var sb = new StringBuilder();
 			for (;;)
 			{
				var cint = source.ReadByte();

				if (cint == -1) return 0; // unexpected end of stream.

				var inp = (char)cint;

				if (Char.IsLetterOrDigit(inp)) sb.Append(inp);
				else if (inp == '\r' || inp == '\n') break;
				else throw new InvalidDataException("Unexpected character in chunk length: " + ((int)inp));
 			}

			long result;
			long.TryParse(sb.ToString(), NumberStyles.HexNumber, null, out result);

			return result;
		}

		public byte[] ReadBytesToLength()
		{
			var ms = new MemoryStream();
			while (!Complete)
			{
				var chunk = ReadNextChunk();
				ms.Write(chunk, 0, chunk.Length);
			}
			return ms.ToArray();
		}

		public byte[] ReadBytesToTimeout()
		{
			return ReadNextChunk();
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			return _source.Read(buffer, offset, count);
		}

		public void Dispose()
		{
			var sock = Interlocked.Exchange(ref _source, null);
			if (sock == null) return;
			sock.Dispose();
		}

		public static void CopyBytesToLength(Stream source, Stream dest, long length, TimeSpan timeout)
		{
			long read = 0;
			var buf = new byte[BufferSize];
			long remaining;

			Func<int> now = () => Environment.TickCount;
			int[] lastData = {now()};
			Func<int> waiting = () => now() - lastData[0];

			while ((remaining = length - read) > 0)
			{
				var len = remaining > BufferSize ? BufferSize : (int)remaining;
				var got = source.Read(buf, 0, len);

				if (got > 0) lastData[0] = now();
				else if (waiting() > timeout.TotalMilliseconds) throw new TimeoutException("Timeout while reading from result stream");

				read += got;
				dest.Write(buf, 0, got);
			}
		}
		public static void CopyBytesToTimeout(Stream source, Stream dest)
		{
			try { source.CopyTo(dest); }
			catch (TimeoutException) { }
		}
	}
}
