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

		public override int Read(byte[] buffer, int offset, int count)
		{
			var len = _source.Read(buffer, offset, count);
			if (len > 0)
			{
				_hash.TransformBlock(buffer, offset, len, buffer, offset);
			}
			return len;
		}

		protected override void Dispose(bool disposing)
		{
			var lstream = Interlocked.Exchange(ref _source, null);
			if (lstream != null) { lstream.Dispose(); }
			var lhash = Interlocked.Exchange(ref _hash, null);
			if (lhash != null) { lhash.Clear(); }
		}

		public override bool CanRead
		{
			get { return _source.CanRead; }
		}

		#region Unsupported Junk
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) { throw new InvalidOperationException("Not supported"); }
		public override void SetLength(long value) { throw new InvalidOperationException("Not supported"); }
		public override void Write(byte[] buffer, int offset, int count) { throw new InvalidOperationException("Not supported"); }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return false; } }
		public override long Length { get { return 0; } }
		public override long Position { get; set; }
		#endregion
	}
}
