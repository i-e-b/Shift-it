using System;
using JetBrains.Annotations;

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
		/// <exception cref="ShiftIt.Http.TimeoutException">Timeouts while reading or writing sockets</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Generic socket exceptions</exception>
		[NotNull]IHttpResponse Request([NotNull]IHttpRequest request, Action<long> sendProgress = null);

		/// <summary>
		/// Issue a request to a server, and return the (IDisposable) response. Throws if the response status code was not a success.
		/// </summary>
		/// <exception cref="ShiftIt.Http.HttpTransferException">Response to the request was not a succesful HTTP status.</exception>
		/// <exception cref="ShiftIt.Http.TimeoutException">Timeouts while reading or writing sockets.</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Low level transport exception occured.</exception>
        [NotNull]IHttpResponse RequestOrThrow([NotNull]IHttpRequest request, Action<long> sendProgress = null);

	    /// <summary>
	    /// Request data from one resource and provide to another.
	    /// This is done in a memory-efficient manner.
	    /// </summary>
	    /// <param name="loadRequest">Request that will provide body data (should be a GET or POST)</param>
	    /// <param name="storeRequest">Request that will accept body data (should be a PUT or POST)</param>
	    /// <param name="sendProgress">Optional: action that is updated with bytes transferred. No guarantees as to when.</param>
	    /// <exception cref="ShiftIt.Http.HttpTransferException">Response to the request was not a succesful HTTP status.</exception>
	    /// <exception cref="System.Net.Sockets.SocketException">Low level transport exception occured.</exception>
	    /// <exception cref="ShiftIt.Http.TimeoutException">A timeout occured during transfer.</exception>
	    void CrossLoad([NotNull]IHttpRequest loadRequest, [NotNull]IHttpRequestBuilder storeRequest, Action<long> sendProgress = null);

		/// <summary>
		/// Request data from one resource and provide to another, calculating a 
		/// hash of the cross-loaded data.
		/// This is done in a memory-efficient manner.
		/// </summary>
		/// <param name="loadRequest">Request that will provide body data (should be a GET or POST)</param>
		/// <param name="storeRequest">Request that will accept body data (should be a PUT or POST)</param>
		/// <param name="hashAlgorithmName">Name of hash algorithm to use (should a name supported by System.Security.Cryptography.HashAlgorithm)</param>
		/// <exception cref="ShiftIt.Http.HttpTransferException">Response to the request was not a succesful HTTP status.</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Low level transport exception occured.</exception>
		/// <exception cref="ShiftIt.Http.TimeoutException">A timeout occured during transfer.</exception>
        [NotNull]byte[] CrossLoad([NotNull]IHttpRequest loadRequest, [NotNull]IHttpRequestBuilder storeRequest, string hashAlgorithmName);

		/// <summary>
		/// Connection and data transfer timeout
		/// </summary>
		TimeSpan Timeout { get; set; }
	}
}