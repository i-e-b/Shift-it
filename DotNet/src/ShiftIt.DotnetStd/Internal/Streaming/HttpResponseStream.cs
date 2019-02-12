using System;
using System.IO;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using ShiftIt.Http;

namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// Wrapper around a http body stream
	/// </summary>
	public class HttpResponseStream : IHttpResponseStream
	{
		[NotNull]Stream _source;
		[NotNull]readonly object _lock;
        long _readSoFar;

		/// <summary>
		/// Returns true if all expected data has been read.
		/// Returns false if message should have more data.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		public bool Complete { get { return _readSoFar >= ExpectedLength; }}

		/// <summary>
		/// Timeout for reading.
		/// </summary>
		public TimeSpan Timeout { get; set; }

		/// <summary>
		/// Wrap a non-chunked http body stream, with an expected length
		/// </summary>
		public HttpResponseStream(Stream source, int expectedLength)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_lock = new object();
			ExpectedLength = expectedLength;
			_readSoFar = 0;

			Timeout = HttpClient.DefaultTimeout;
		}

        /// <summary>
        /// Length that server reported for the response.
        /// Tries to give decompressed length if response is compressed.
        /// 
        /// Due to frequent protocol violations, this is not 100% reliable.
        /// </summary>
        public long ExpectedLength { get; private set; }

        /// <summary>
        /// Read string up to the declared response length.
        /// If response is chunked, this will read until an empty chunk is received.
        /// </summary>
        public string ReadStringToLength(Action<long> receiveProgress = null)
		{
			return Encoding.UTF8.GetString(ReadBytesToLength(receiveProgress));
		}

		/// <summary>
		/// Read string while data is on the stream, waiting up to the timeout value for more data.
		/// If response is chunked, this will read the next chunk.
		/// </summary>
		public string ReadStringToTimeout(Action<long> receiveProgress = null)
		{
			return Encoding.UTF8.GetString(ReadBytesToTimeout(receiveProgress));
		}

		/// <summary>
		/// Read raw bytes up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		public byte[] ReadBytesToLength(Action<long> receiveProgress = null)
		{
			var ms = new MemoryStream((int)ExpectedLength);
			lock (_lock)
			{
				ExpectedLength = StreamTools.CopyBytesToLength(_source, ms, ExpectedLength, Timeout, receiveProgress);
			}
			_readSoFar += ms.Length;
			return ms.ToArray();
		}

		/// <summary>
		/// Read raw bytes while data is on the stream, waiting up to the timeout value for more data.
		/// </summary>
		public byte[] ReadBytesToTimeout(Action<long> receiveProgress = null)
		{
			var ms = new MemoryStream((int)ExpectedLength);
			StreamTools.CopyBytesToTimeout(_source, ms, receiveProgress);
			_readSoFar += ms.Length;
			return ms.ToArray();
		}

		/// <summary>
		/// Read raw bytes from the response into a buffer, returning number of bytes read.
		/// </summary>
		public int Read(byte[] buffer, int offset, int count)
		{
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
			return _source.Read(buffer, offset, count);
		}

		/// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		~HttpResponseStream()
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

		/// <summary>
		/// Internal dispose
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			var stream = Interlocked.Exchange(ref _source, null);
			if (stream == null) return;
			stream.Close();
		}
	}
}
