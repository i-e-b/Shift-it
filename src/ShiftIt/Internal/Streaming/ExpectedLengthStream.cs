using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ShiftIt.Internal.Socket
{
	public class ExpectedLengthStream : IExpectedLengthStream
	{
		Stream _source;
		readonly int _expectedLength;
		readonly object _lock;
		const int BufferSize = 4096;

		public ExpectedLengthStream(Stream source, int expectedLength)
		{
			_lock = new Object();
			_source = source;
			_expectedLength = expectedLength;
		}

		public long ExpectedLength
		{
			get { return _expectedLength; }
		}

		public string ReadStringToLength()
		{
			return Encoding.UTF8.GetString(ReadBytesToLength());
		}

		public string ReadStringToTimeout()
		{
			return new StreamReader(_source).ReadToEnd();
		}

		public byte[] ReadBytesToLength()
		{
			var ms = new MemoryStream(_expectedLength);
			lock (_lock)
			{
				CopyBytesToLength(_source, ms, _expectedLength);
			}
			return ms.ToArray();
		}

		public byte[] ReadBytesToTimeout()
		{
			var ms = new MemoryStream(_expectedLength);
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

		public static void CopyBytesToLength(Stream source, Stream dest, long length)
		{
			long read = 0;
			var buf = new byte[BufferSize];
			long remaining;
			while ((remaining = length - read) > 0)
			{
				int len = remaining > BufferSize ? BufferSize : (int)remaining;
				var got = source.Read(buf, 0, len);
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
