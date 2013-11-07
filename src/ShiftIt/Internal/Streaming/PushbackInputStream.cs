using System;
using System.Collections.Generic;
using System.IO;

namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// A stream wrapper that has an 'UnRead' method.
	/// </summary>
	public class PushbackInputStream:Stream
	{
		readonly Stream _baseStream;
		long _position;
		readonly Queue<byte> _unreadBuffer;

		public PushbackInputStream(Stream baseStream)
		{
			_baseStream = baseStream;
			_position = _baseStream.Position;
			_unreadBuffer = new Queue<byte>();
		}

		/// <summary>
		/// Read from the underlying stream, returning actual length read.
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			var remains = count;
			var pos = offset;
			while (remains > 0 && _unreadBuffer.Count > 0)
			{
				// write to buffer, advance offset
				remains--;
				buffer[pos] = _unreadBuffer.Dequeue();
				pos++;
			}
			//any left, read from stream
			if (remains > 0) pos += _baseStream.Read(buffer, pos, remains);
			var total = pos - offset;
			_position += total;
			return total;
		}
		
		/// <summary>
		/// Push data back onto the buffer, changing the Position
		/// and allowing the data to be read again.
		/// 
		/// If the underlying stream supports seeking, a seek-back will
		/// be used instead.
		/// </summary>
		/// <param name="buffer">buffer used for matching 'Read' call</param>
		/// <param name="offset">offset used for matching 'Read' call</param>
		/// <param name="length">length *returned* by matching 'Read' call</param>
		public void UnRead(byte[] buffer, int offset, int length)
		{
			if (_baseStream.CanSeek)
			{
				_position = _baseStream.Seek(-length, SeekOrigin.Current);
				return;
			}
			var end = offset+length;
			for (var i = offset; i < end; i++)
			{
				_unreadBuffer.Enqueue(buffer[i]);
				_position--;
			}
		}

		/// <summary>
		/// Read position. 'UnRead'ing data back onto the buffer
		/// will reduce the position.
		/// Write position is not supported.
		/// </summary>
		public override long Position
		{
			get { return _position; }
			set { throw new NotSupportedException("Changing position of buffered stream is not supported"); }
		}

		/// <summary>
		/// Passed to the underlying stream if it supports seeking.
		/// Otherwise, 
		///  - Reverse seeks will try to move within the UnRead buffer or throw an ArgumentException.
		///  - Forward seeks will read more data into the UnRead buffer and continue from seek'd position.
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			if (_baseStream.CanSeek)
			{
				_position = _baseStream.Seek(offset, origin);
				return _position;
			}
			return 0;
		}

		#region Pass-through methods
		/// <summary>
		/// Flush is called directly on the underlying stream and
		/// has no effect on push-back buffering.
		/// </summary>
		public override void Flush()
		{
			_baseStream.Flush();
		}

		/// <summary>
		/// Passed directly to underlying stream
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes. </param><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>2</filterpriority>
		public override void SetLength(long value)
		{
			_baseStream.SetLength(value);
		}

		/// <summary>
		/// Write is passed directly through to underlying stream.
		/// </summary>
		public override void Write(byte[] buffer, int offset, int count)
		{
			_baseStream.Write(buffer, offset, count);
		}

		public override bool CanRead { get { return _baseStream.CanRead; } }
		public override bool CanSeek { get { return _baseStream.CanSeek; } }
		public override bool CanWrite { get { return _baseStream.CanWrite; } }
		public override long Length { get { return _baseStream.Length; } }
		#endregion

		
	}
}