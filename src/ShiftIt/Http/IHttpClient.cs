using System;

namespace ShiftIt.Http
{
	public interface IHttpClient
	{
		/// <summary>
		/// Issue a request to a server, and return the (IDisposable) response.
		/// </summary>
		/// <exception cref="System.TimeoutException">Timeouts while reading or writing sockets</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Generic socket exceptions</exception>
		IHttpResponse Request(IHttpRequest request);
		void CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest);

		byte[] CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest, string hashAlgorithmName);
		TimeSpan Timeout { get; set; }
	}
}