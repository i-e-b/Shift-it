using System;

namespace ShiftIt.Http
{
	/// <summary>
	/// Contract for HttpClient.
	/// </summary>
	public interface IHttpClient
	{
		/// <summary>
		/// Issue a request to a server, and return the (IDisposable) response.
		/// </summary>
		/// <exception cref="System.TimeoutException">Timeouts while reading or writing sockets</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Generic socket exceptions</exception>
		IHttpResponse Request(IHttpRequest request);

		/// <summary>
		/// Request data from one resource and provide to another.
		/// This is done in a memory-efficient manner.
		/// </summary>
		/// <param name="loadRequest">Request that will provide body data (should be a GET or POST)</param>
		/// <param name="storeRequest">Request that will accept body data (should be a PUT or POST)</param>
		void CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest);


		/// <summary>
		/// Request data from one resource and provide to another, calculating a 
		/// hash of the cross-loaded data.
		/// This is done in a memory-efficient manner.
		/// </summary>
		/// <param name="loadRequest">Request that will provide body data (should be a GET or POST)</param>
		/// <param name="storeRequest">Request that will accept body data (should be a PUT or POST)</param>
		/// <param name="hashAlgorithmName">Name of hash algorithm to use (should a name supported by System.Security.Cryptography.HashAlgorithm)</param>
		byte[] CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest, string hashAlgorithmName);

		/// <summary>
		/// Connection and data transfer timeout
		/// </summary>
		TimeSpan Timeout { get; set; }
	}
}