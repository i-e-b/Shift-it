using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ShiftIt.Socket
{
	public class ExpectedLengthStream : IExpectedLengthStream
	{
		Stream _source;
		readonly int _expectedLength;
		int _readSoFar;
		readonly object _lock;

		public ExpectedLengthStream(Stream source, int expectedLength)
		{
			_lock = new Object();
			_readSoFar = 0;
			_source = source;
			_expectedLength = expectedLength;
		}

		public long ExpectedLength {
			get { return _expectedLength; }
		}

		public string ReadStringToLength()
		{
			var sb = new StringBuilder(_expectedLength);
			var buf = new byte[1024];
			lock (_lock)
			{
				int remaining;
				while ((remaining = _expectedLength - _readSoFar) > 0)
				{
					var len = remaining > 1024 ? 1024 : remaining;
					var got = _source.Read(buf, 0, len);
					_readSoFar += got;
					sb.Append(Encoding.UTF8.GetString(buf,0,got));
				}
			}
			return sb.ToString();
		}

		public string ReadStringToTimeout()
		{
			return new StreamReader(_source).ReadToEnd();
		}

		public byte[] ReadBytesToLength()
		{
			var ms = new MemoryStream(_expectedLength);
			var buf = new byte[1024];
			lock (_lock)
			{
				int remaining;
				while ((remaining = _expectedLength - _readSoFar) > 0)
				{
					var len = remaining > 1024 ? 1024 : remaining;
					var got = _source.Read(buf, 0, len);
					_readSoFar += got;
					ms.Write(buf, 0, got);
				}
			}
			return ms.ToArray();
		}

		public byte[] ReadBytesToTimeout()
		{
			var ms = new MemoryStream(_expectedLength);
			_source.CopyTo(ms);
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
	}
}
