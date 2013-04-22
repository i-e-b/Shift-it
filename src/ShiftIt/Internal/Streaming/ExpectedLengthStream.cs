using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ShiftIt.Internal.Socket
{
	public class ExpectedLengthStream : IExpectedLengthStream
	{
		Stream _source;
		readonly int _expectedLength;
		readonly object _lock;
		int _firstByte;
		const int BufferSize = 4096;

		public ExpectedLengthStream(Stream source, int expectedLength)
		{
			_lock = new Object();
			_source = source;
			_expectedLength = expectedLength;
			_firstByte = -1;

			VerifiedLength = true;
			TryDirtyCompressedStreamReading(source, ref _expectedLength);
		}
		
		protected bool VerifiedLength { get; private set; }
		public long ExpectedLength
		{
			get {
				if (_firstByte >= 0) return _expectedLength + 1;
				return _expectedLength;
			}
		}

		public string ReadStringToLength()
		{
			if (!VerifiedLength) return ReadStringToTimeout();
			return Encoding.UTF8.GetString(ReadBytesToLength());
		}

		public string ReadStringToTimeout()
		{
			return Encoding.UTF8.GetString(ReadBytesToTimeout());
		}

		public byte[] ReadBytesToLength()
		{
			if (!VerifiedLength) return ReadBytesToTimeout();
			var ms = new MemoryStream(_expectedLength);
			lock (_lock)
			{
				if (_firstByte >= 0) ms.WriteByte((byte)_firstByte);
				CopyBytesToLength(_source, ms, _expectedLength, TimeSpan.FromSeconds(5));
			}
			return ms.ToArray();
		}

		public byte[] ReadBytesToTimeout()
		{
			var ms = new MemoryStream(_expectedLength);
			if (_firstByte >= 0) ms.WriteByte((byte)_firstByte);
			CopyBytesToTimeout(_source, ms);
			return ms.ToArray();
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			return _source.Read(buffer, offset, count);
		}

		~ExpectedLengthStream()
		{
			Dispose();
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
			var lastData = now();
			Func<int> waiting = () => now() - lastData;

			while ((remaining = length - read) > 0)
			{
				int len = remaining > BufferSize ? BufferSize : (int)remaining;
				var got = source.Read(buf, 0, len);

				if (got > 0) lastData = now();
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

		/// <summary>
		/// This pile of dirt digs out the compressed stream length if it can.
		/// Blame the bad .Net api!
		/// </summary>
		void TryDirtyCompressedStreamReading(Stream ins, ref int expectedLength)
		{
			try
			{
				DeflateStream s;
				if (ins is GZipStream)
				{
					var g = ins.GetType().GetField("deflateStream", BindingFlags.Instance | BindingFlags.NonPublic);
					if (g == null) return;
					s = (DeflateStream)g.GetValue(ins);
				}
				else s = ins as DeflateStream;

				if (s == null) return;
				// need to pull a byte to get the deflate stream to decode...
				_firstByte = s.ReadByte();

				var f = s.GetType().GetField("inflater", BindingFlags.Instance | BindingFlags.NonPublic);
				if (f == null) return;
				var fo = f.GetValue(s);

				var avf = fo.GetType().GetProperty("AvailableOutput");
				var avv = avf.GetValue(fo, new object[0]);

				var reportedLength = (int)avv;
				if (reportedLength > expectedLength) expectedLength = reportedLength;
			}
			catch (Exception)
			{
				VerifiedLength = false;
			}
		}

	}
}
