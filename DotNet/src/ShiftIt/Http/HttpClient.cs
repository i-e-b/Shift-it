using System;
using System.IO;
using System.Security.Cryptography;
using ShiftIt.Internal.Http;
using ShiftIt.Internal.Socket;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Http
{
	/// <summary>
	/// Standard Http client for Shift-it
	/// </summary>
	public class HttpClient : IHttpClient
	{
		/// <summary>
		/// Default conenction and data timeout (5 seconds)
		/// </summary>
		public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

		readonly IConnectableStreamSource _conn;
		readonly IHttpResponseParser _parser;

		/// <summary>
		/// Connection and data transfer timeout
		/// </summary>
		public TimeSpan Timeout { get; set; }

		/// <summary>
		/// Create a new HttpClient with a specified connection and parser
		/// </summary>
		public HttpClient(IConnectableStreamSource conn, IHttpResponseParser parser)
		{
			_conn = conn;
			_parser = parser;
			Timeout = DefaultTimeout;
		}

		/// <summary>
		/// Start a new HttpClient
		/// </summary>
		public HttpClient() : this(new SocketStreamFactory(), new HttpResponseParser()) { }

		/// <summary>
		/// Issue a request to a server, and return the (IDisposable) response.
		/// </summary>
		/// <exception cref="ShiftIt.Http.TimeoutException">Timeouts while reading or writing sockets.</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Low level transport exception occured.</exception>
		public IHttpResponse Request(IHttpRequest request, Action<long> sendProgress = null)
		{
			var socket = (request.Secure) 
				? _conn.ConnectSSL(request.Target, Timeout)
				: _conn.ConnectUnsecured(request.Target, Timeout);

			var Tx = new StreamWriter(socket);
			Tx.Write(request.RequestHead);
			Tx.Flush();

			if (request.DataStream != null)
			{
				if (request.DataLength > 0)
					StreamTools.CopyBytesToLength(request.DataStream, socket, request.DataLength, Timeout, sendProgress);
				else
					StreamTools.CopyBytesToTimeout(request.DataStream, socket, sendProgress);
			}

			socket.Flush();

			return _parser.Parse(socket, Timeout);
		}

		/// <summary>
		/// Issue a request to a server, and return the (IDisposable) response. Throws if the response status code was not a success.
		/// </summary>
		/// <exception cref="ShiftIt.Http.HttpTransferException">Response to the request was not a succesful HTTP status.</exception>
		/// <exception cref="ShiftIt.Http.TimeoutException">Timeouts while reading or writing sockets.</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Low level transport exception occured.</exception>
		public IHttpResponse RequestOrThrow(IHttpRequest request, Action<long> sendProgress = null)
		{
			var response = Request(request);
			if (response.StatusClass == StatusClass.Success) return response;

			response.Dispose();
			throw new HttpTransferException(response.Headers, request.Target, response.StatusCode, response.StatusMessage);
		}

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
	    public void CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest, Action<long> sendProgress = null)
		{
			using (var getTx = RequestOrThrow(loadRequest)) // get source
			{
				var storeRq = storeRequest.Build(getTx.RawBodyStream, getTx.BodyReader.ExpectedLength);
				using (RequestOrThrow(storeRq)) // dispose of the response stream
				{
				}
			}
		}

		/// <summary>
		/// Request data from one resource and provide to another, calculating a 
		/// hash of the cross-loaded data. This is done in a memory-efficient manner.
		/// If either source or destination return a non-success result (including redirects)
		/// an exception will be thrown
		/// </summary>
		/// <param name="loadRequest">Request that will provide body data (should be a GET or POST)</param>
		/// <param name="storeRequest">Request that will accept body data (should be a PUT or POST)</param>
		/// <param name="hashAlgorithmName">Name of hash algorithm to use (should be a name supported by <see cref="System.Security.Cryptography.HashAlgorithm"/>)</param>
		/// <exception cref="ShiftIt.Http.HttpTransferException">Response to the request was not a succesful HTTP status.</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Low level transport exception occured.</exception>
		/// <exception cref="ShiftIt.Http.TimeoutException">A timeout occured during transfer.</exception>
		public byte[] CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest, string hashAlgorithmName)
		{
			var hash = HashAlgorithm.Create(hashAlgorithmName);
			using (var getTx = RequestOrThrow(loadRequest))
			{
				var hashStream = new HashingReadStream(getTx.RawBodyStream, hash);
				var storeRq = storeRequest.Build(hashStream, getTx.BodyReader.ExpectedLength);
				using (RequestOrThrow(storeRq)) // dispose of the response stream
				{
				}
				return hashStream.GetHashValue();
			}
		}
	}
}