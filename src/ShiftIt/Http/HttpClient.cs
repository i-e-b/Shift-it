using System;
using System.IO;
using System.Security.Cryptography;
using ShiftIt.Http.Internal;
using ShiftIt.Internal.Socket;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Http
{
	public class HttpClient : IHttpClient
	{
		public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);


		readonly IConnectableStreamSource _conn;
		readonly IHttpResponseParser _parser;

		public TimeSpan Timeout { get; set; }

		public HttpClient(IConnectableStreamSource conn, IHttpResponseParser parser)
		{
			_conn = conn;
			_parser = parser;
			Timeout = DefaultTimeout;
		}

		public HttpClient() : this(new SocketStreamFactory(), new HttpResponseParser()) { }

		public IHttpResponse Request(IHttpRequest request)
		{
			var socket = (request.Secure) 
				? _conn.ConnectSSL(request.Target, Timeout)
				: _conn.ConnectUnsecured(request.Target, Timeout);

			var Tx = new StreamWriter(socket);
			Tx.Write(request.RequestHead());
			Tx.Flush();

			if (request.DataStream != null)
			{
				if (request.DataLength > 0)
					HttpSingleResponseStream.CopyBytesToLength(request.DataStream, socket, request.DataLength, Timeout);
				else
					HttpSingleResponseStream.CopyBytesToTimeout(request.DataStream, socket);
			}

			socket.Flush();

			return _parser.Parse(socket, Timeout);
		}
		public void CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest)
		{
			using (var getTx = Request(loadRequest)) // get source
			{
				var storeRq = storeRequest.Data(getTx.RawBodyStream, getTx.BodyReader.ExpectedLength).Build();
				Request(storeRq).Dispose(); // write out to dest
			}
		}

		public byte[] CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest, string hashAlgorithmName)
		{
			var hash = HashAlgorithm.Create(hashAlgorithmName);
			using (var getTx = Request(loadRequest))
			{
				var hashStream = new HashingReadStream(getTx.RawBodyStream, hash);
				var storeRq = storeRequest.Data(hashStream, getTx.BodyReader.ExpectedLength).Build();
				Request(storeRq).Dispose();
				return hashStream.GetHashValue();
			}
		}
	}
}