using System;

namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// Wrapper around a HTTP response body.
	/// All decompression and decoding is handled.
	/// </summary>
	public interface IHttpResponseStream : IDisposable
	{
		/// <summary>
		/// Length that server reported for the response.
		/// Tries to give decompressed length if response is compressed.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		long ExpectedLength { get; }

		/// <summary>
		/// Returns true if all expected data has been read.
		/// Returns false if message should have more data.
		/// 
		/// Due to frequent protocol violations, this is not 100% reliable.
		/// </summary>
		bool Complete { get; }

		/// <summary>
		/// Timeout for reading.
		/// </summary>
		TimeSpan Timeout { get; set; }

		/// <summary>
		/// Read string up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		string ReadStringToLength(Action<long> receiveProgress = null);

		/// <summary>
		/// Read string while data is on the stream, waiting up to the timeout value for more data.
		/// If response is chunked, this will read the next chunk.
		/// </summary>
		string ReadStringToTimeout(Action<long> receiveProgress = null);

		
		/// <summary>
		/// Read raw bytes up to the declared response length.
		/// If response is chunked, this will read until an empty chunk is received.
		/// </summary>
		byte[] ReadBytesToLength(Action<long> receiveProgress = null);
		
		/// <summary>
		/// Read raw bytes while data is on the stream, waiting up to the timeout value for more data.
		/// </summary>
		byte[] ReadBytesToTimeout(Action<long> receiveProgress = null);

		/// <summary>
		/// Read raw bytes from the response into a buffer, returning number of bytes read.
		/// </summary>
		int Read(byte[] buffer, int offset, int count);
	}
}