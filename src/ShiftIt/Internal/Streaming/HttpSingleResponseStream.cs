using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using ShiftIt.Http;

namespace ShiftIt.Internal.Socket
{
	/// <summary>
	/// Wrapper around a non-chunked http body stream
	/// </summary>
	public class HttpSingleResponseStream : IHttpResponseStream
	{
		Stream _source;
		readonly int _expectedLength;
		readonly object _lock;
		int _firstByte;
		long _readSoFar;

		/// <summary>
		/// Returns true if all expected data has been read.
		/// Returns false if message should have more data.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		public bool Complete { get { return _readSoFar >= _expectedLength; }}

		/// <summary>
		/// Timeout for reading.
		/// </summary>
		public TimeSpan Timeout { get; set; }

		/// <summary>
		/// Wrap a non-chunked http body stream, with an expected length
		/// </summary>
		public HttpSingleResponseStream(Stream source, int expectedLength)
		{
			_lock = new Object();
			_expectedLength = expectedLength;
			_firstByte = -1;
			_readSoFar = 0;

			Timeout = HttpClient.DefaultTimeout;
			VerifiedLength = true;
			_source = TryDirtyCompressedStreamReading(source, ref _expectedLength);
		}
		
		/// <summary>
		/// True if length should be treated as reliable.
		/// If false, the length field will likely be inaccurate
		/// </summary>
		protected bool VerifiedLength { get; private set; }

		/// <summary>
		/// Length that server reported for the response.
		/// Tries to give decompressed length if response is compressed.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		public long ExpectedLength
		{
			get {
				if (_firstByte >= 0) return _expectedLength + 1;
				return _expectedLength;
			}
		}

		/// <summary>
		/// Read string up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		public string ReadStringToLength()
		{
			if (!VerifiedLength) return ReadStringToTimeout();
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

		/// <summary>
		/// Read raw bytes up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		public byte[] ReadBytesToLength()
		{
			if (!VerifiedLength) return ReadBytesToTimeout();
			var ms = new MemoryStream(_expectedLength);
			lock (_lock)
			{
				if (_firstByte >= 0) ms.WriteByte((byte)_firstByte);
				StreamTools.CopyBytesToLength(_source, ms, _expectedLength, Timeout);
			}
			_readSoFar += ms.Length;
			return ms.ToArray();
		}

		/// <summary>
		/// Read raw bytes while data is on the stream, waiting up to the timeout value for more data.
		/// </summary>
		public byte[] ReadBytesToTimeout()
		{
			var ms = new MemoryStream(_expectedLength);
			if (_firstByte >= 0) ms.WriteByte((byte)_firstByte);
			StreamTools.CopyBytesToTimeout(_source, ms);
			_readSoFar += ms.Length;
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
		~HttpSingleResponseStream()
		{
			Dispose(false);
		}

		/// <summary>
		/// Close and dispose the underlying stream
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var sock = Interlocked.Exchange(ref _source, null);
			if (sock == null) return;
			sock.Dispose();
		}

		/// <summary>
		/// This pile of dirt digs out the compressed stream length if it can.
		/// Blame the bad .Net api!
		/// </summary>
		Stream TryDirtyCompressedStreamReading(Stream ins, ref int expectedLength)
		{
			VerifiedLength = false;
			return ins;
			/*DeflateStream s = null;
			try
			{
				if (ins is GZipStream)
				{
					var g = ins.GetType().GetField("deflateStream", BindingFlags.Instance | BindingFlags.NonPublic);
					if (g == null) return ins;
					s = (DeflateStream)g.GetValue(ins);
				}
				else s = ins as DeflateStream;

				if (s == null) return ins;
				// need to pull a byte to get the deflate stream to decode...
				_firstByte = s.ReadByte();

				var f = s.GetType().GetField("inflater", BindingFlags.Instance | BindingFlags.NonPublic);
				if (f == null) return ins;
				var fo = f.GetValue(s);

				var avf = fo.GetType().GetProperty("AvailableOutput");
				var avv = avf.GetValue(fo, new object[0]);

				var reportedLength = (int)avv;
				if (reportedLength > expectedLength) expectedLength = reportedLength;
			}
			catch (InvalidDataException) // This is probably not a valid compressed stream
			{
				if (s == null || s.BaseStream == null) return ins;
				// TODO: set a flag that to note that we did this!

				Stream sx = s.BaseStream;
				int b;
				while ((b = sx.ReadByte()) >= 0)
				{
					Console.Write((char)b);
				}

				return s.BaseStream;
			}
			catch (Exception)
			{
				VerifiedLength = false;
			}
			return ins;*/
		}
	}
}
