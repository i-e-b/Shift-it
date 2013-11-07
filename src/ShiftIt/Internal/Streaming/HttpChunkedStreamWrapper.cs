using System;
using System.Globalization;
using System.IO;
using System.Text;
using ShiftIt.Internal.Socket;

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

		public override void Flush() { }

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}


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

			StreamTools.CopyBytesToLength(_source, ms, length, _timeout);

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

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead
		{
			get { return _source.CanRead; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override long Length
		{
			get { return 0; }
		}

		public override long Position { get; set; }

		public bool IsComplete()
		{
			return _complete && _buffer.Available() <= 0;
		}
	}
}