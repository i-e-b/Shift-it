using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// Wraps a stream in a hash algorithm
	/// </summary>
	public class HashingReadStream : Stream
	{
		Stream _source;
		HashAlgorithm _hash;

		/// <summary>
		/// Create a new hashing readstream
		/// </summary>
		public HashingReadStream(Stream source, HashAlgorithm hash)
		{
			_source = source;
			_hash = hash;
		}

		/// <summary>
		/// Call after stream is complete to get hash value
		/// </summary>
		public byte[] GetHashValue()
		{
			_hash.TransformFinalBlock(new byte[0], 0, 0);
			return _hash.Hash;
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param><param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream. </param><param name="count">The maximum number of bytes to be read from the current stream. </param><exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. </exception><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support reading. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
		public override int Read(byte[] buffer, int offset, int count)
		{
			var len = _source.Read(buffer, offset, count);
			if (len > 0)
			{
				_hash.TransformBlock(buffer, offset, len, buffer, offset);
			}
			return len;
		}

		/// <summary>
		/// Releases the unmanaged resources used by the underlying stream and optionally releases the managed resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			var lstream = Interlocked.Exchange(ref _source, null);
			if (lstream != null) { lstream.Dispose(); }
			var lhash = Interlocked.Exchange(ref _hash, null);
			if (lhash != null) { lhash.Clear(); }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <returns>
		/// true if the stream supports reading; otherwise, false.
		/// </returns>
		public override bool CanRead
		{
			get { return _source.CanRead; }
		}

		#region Unsupported Junk
		/**<summary> Not Supported </summary>*/ public override void Flush() { }
		/**<summary> Not Supported </summary>*/ public override long Seek(long offset, SeekOrigin origin) { throw new InvalidOperationException("Not supported"); }
		/**<summary> Not Supported </summary>*/ public override void SetLength(long value) { throw new InvalidOperationException("Not supported"); }
		/**<summary> Not Supported </summary>*/ public override void Write(byte[] buffer, int offset, int count) { throw new InvalidOperationException("Not supported"); }
		/**<summary> Not Supported </summary>*/ public override bool CanSeek { get { return false; } }
		/**<summary> Not Supported </summary>*/ public override bool CanWrite { get { return false; } }
		/**<summary> Not Supported </summary>*/ public override long Length { get { return 0; } }
		/**<summary> Not Supported </summary>*/ public override long Position { get; set; }
		#endregion
	}
}
