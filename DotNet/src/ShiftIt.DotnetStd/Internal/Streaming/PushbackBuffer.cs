using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// Buffer reader helper for streams
	/// </summary>
	public class PushbackBuffer
	{
		[CanBeNull]readonly Stream _baseStream;
		[NotNull]readonly Queue<byte> _unreadBuffer;

		/// <summary>
		/// Create a new read buffer, over an existing stream
		/// </summary>
		public PushbackBuffer(Stream baseStream)
		{
			_baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
			_unreadBuffer = new Queue<byte>();
		}

		/// <summary>
		/// Create a new read buffer, with no underlying stream
		/// </summary>
		public PushbackBuffer()
		{
			_baseStream = null;
			_unreadBuffer = new Queue<byte>();
		}

		/// <summary>
		/// Read from the underlying stream, returning actual length read.
		/// </summary>
		public int Read(byte[] buffer, int offset, int count)
		{
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
			var remains = count;
			var pos = offset;
			while (remains > 0 && _unreadBuffer.Count > 0)
			{
				// write to buffer, advance offset
				remains--;
				buffer[pos] = _unreadBuffer.Dequeue();
				pos++;
			}
			// any left, read from stream
			if (remains > 0 && _baseStream != null) pos += _baseStream.Read(buffer, pos, remains);
			var total = pos - offset;
			return total;
		}
		
		/// <summary>
		/// Push data back onto the buffer, changing the Position
		/// and allowing the data to be read again.
		/// </summary>
		public void UnRead(byte[] buffer, int offset, int length)
		{
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
			var end = offset+length;
			for (var i = offset; i < end; i++)
			{
				_unreadBuffer.Enqueue(buffer[i]);
			}
		}

		/// <summary>
		/// Size of buffer, not including data not yet read from base stream
		/// </summary>
		public long Available()
		{
			return _unreadBuffer.Count;
		}
	}
}