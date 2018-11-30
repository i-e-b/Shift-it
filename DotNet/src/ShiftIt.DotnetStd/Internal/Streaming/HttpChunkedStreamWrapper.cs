using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// Wrap a HTTP chunked stream to get a de-chunked output.
	/// </summary>
	public class HttpChunkedStreamWrapper: Stream, ISelfTerminatingStream
	{
		readonly Stream _source;
		readonly PushbackBuffer _buffer;
		readonly TimeSpan _timeout;
		bool _complete;
		const byte CR = 0x0D;
		const byte LF = 0x0A;

		/// <summary>
		/// Wrap a stream to de-chunk.
		/// </summary>
		public HttpChunkedStreamWrapper(Stream source, TimeSpan timeout)
		{
			_source = source;
			_timeout = timeout;
			_complete = false;
			_buffer = new PushbackBuffer();
		}

		/// <summary>
		/// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
		/// </summary>
		/// <filterpriority>1</filterpriority>
		public override void Close()
		{
			_source.Close();
		}

		/**<summary>Not used</summary>*/public override void Flush() { }

		/**<summary>Not used</summary>*/public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/**<summary>Not used</summary>*/public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Read bytes from stream, reading and joining chunks as needed.
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count > _buffer.Available() && !_complete)
			{
				var more = ReadNextChunk();
				_buffer.UnRead(more, 0, more.Length);
			}
			return _buffer.Read(buffer, offset, count);
		}
		
		byte[] ReadNextChunk()
		{
			if (_complete) return new byte[0];
			var ms = new MemoryStream();

			// assuming we at the beginning of a chunk. Not thread safe!
			var length = ReadChunkLength(_source);
			if (length == 0)
			{
				_complete = true;
				return new byte[0];
			}

			var maybeNewline = _source.ReadByte();
			if (maybeNewline >= 0 && maybeNewline != '\r' && maybeNewline != '\n')
			{
				ms.WriteByte((byte)maybeNewline);
				length--;
			}

			StreamTools.CopyBytesToLength(_source, ms, length, _timeout, null);

			// Should end with CRLF:
			if (!SkipCRLF(_source)) throw new InvalidDataException("HTTP Chunk did not end in CRLF");

			return ms.ToArray();
		}

		/// <summary>
		/// Will skip one of CRLF, LFCR, CRCR, LFLF, LF, CR
		/// because HTTP servers are tricky.
		/// </summary>
		bool SkipCRLF(Stream source)
		{
			var cr = source.ReadByte();
			if (cr < 0) return false;
			if ((cr != CR) && (cr != LF)) return false;
			var lf = source.ReadByte();
			if (lf >= 0 && lf != LF && lf != CR) _buffer.UnRead(new []{(byte)lf}, 0, 1);
			return true;
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

		/**<summary>Not used</summary>*/public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		/**<summary>Not used</summary>*/public override bool CanRead
		{
			get { return _source.CanRead; }
		}

		/**<summary>Not used</summary>*/public override bool CanSeek
		{
			get { return false; }
		}

		/**<summary>Not used</summary>*/public override bool CanWrite
		{
			get { return false; }
		}

		/**<summary>Not used</summary>*/public override long Length
		{
			get { return 0; }
		}

		/**<summary>Not used</summary>*/public override long Position { get; set; }

		/// <summary>
		/// Returns true if the stream has terminated,
		/// false otherwise.
		/// </summary>
		public bool IsComplete()
		{
			return _complete && _buffer.Available() <= 0;
		}
	}
}