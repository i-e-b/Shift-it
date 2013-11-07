using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using ShiftIt.Http;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Internal.Socket
{
	
	/// <summary>
	/// Wrapper around a chunked http body stream
	/// </summary>
	public class HttpChunkedResponseStream : IHttpResponseStream
	{
		Stream _source;
		readonly object _lock;

		/// <summary>
		/// Timeout for reading.
		/// </summary>
		public TimeSpan Timeout { get; set; }

		/// <summary>
		/// Wrap a chunked http response
		/// </summary>
		public HttpChunkedResponseStream(Stream source)
		{
			_source = source;

			_lock = new object();
			Timeout = HttpClient.DefaultTimeout;
			Complete = false;
		}

		/// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		~HttpChunkedResponseStream()
		{
			Dispose(false);
		}

		/// <summary>
		/// Returns true if all expected data has been read.
		/// Returns false if message should have more data.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		public bool Complete { get; protected set; }

		/// <summary>
		/// Length that server reported for the response.
		/// Tries to give decompressed length if response is compressed.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		public long ExpectedLength { get { return 0; } }

		/// <summary>
		/// Read string up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		public string ReadStringToLength()
		{
			return Encoding.UTF8.GetString(ReadBytesToLength());
		}

		/// <summary>
		/// Read string while data is on the stream, waiting up to the timeout value for more data.
		/// If response is chunked, this will read the next chunk.
		/// </summary>
		public string ReadStringToTimeout()
		{
			return Encoding.UTF8.GetString(ReadBytesToTimeout());
		}

		byte[] ReadNextChunk()
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

				StreamTools.CopyBytesToLength(_source, ms, length, Timeout);

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

		/// <summary>
		/// Read raw bytes up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
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

		/// <summary>
		/// Read raw bytes while data is on the stream, waiting up to the timeout value for more data.
		/// </summary>
		public byte[] ReadBytesToTimeout()
		{
			Func<int> now = () => Environment.TickCount;
			int[] lastData = {now()};
			Func<int> waiting = () => now() - lastData[0];
			var ms = new MemoryStream();
			
			while (!Complete)
			{
				var ch = ReadNextChunk();

				if (ch.Length > 0)
				{
					ms.Write(ch, 0, ch.Length);
					lastData[0] = now();
				}
				else if (waiting() > Timeout.TotalMilliseconds) break;
			}

			return ms.ToArray();
		}

		/// <summary>
		/// Read raw bytes from the response into a buffer, returning number of bytes read.
		/// </summary>
		public int Read(byte[] buffer, int offset, int count)
		{
			return _source.Read(buffer, offset, count);
		}

		/// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Internal dispose
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			var sock = Interlocked.Exchange(ref _source, null);
			if (sock == null) return;
			sock.Dispose();
		}
	}
}
