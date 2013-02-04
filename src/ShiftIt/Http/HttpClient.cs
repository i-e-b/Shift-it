using System;
using System.IO;
using ShiftIt.Http.Internal;
using ShiftIt.Internal.Socket;

namespace ShiftIt.Http
{
	public interface IHttpClient
	{
		IHttpResponse Request(IHttpRequest request);
		void CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest);
	}

	public class HttpClient : IHttpClient
	{
		readonly IConnectableStreamSource _conn;
		readonly IHttpResponseParser _parser;

		public TimeSpan Timeout { get; set; }

		public HttpClient(IConnectableStreamSource conn, IHttpResponseParser parser)
		{
			_conn = conn;
			_parser = parser;
			Timeout = TimeSpan.FromSeconds(5);
		}

		public HttpClient() : this(new SocketStreamFactory(), new HttpResponseParser()) { }

		public IHttpResponse Request(IHttpRequest request)
		{
			var socket = _conn.Connect(request.Target, Timeout);
			var Tx = new StreamWriter(socket);
			Tx.Write(request.RequestHead());
			Tx.Flush();

			if (request.DataStream != null)
			{
				if (request.DataLength > 0)
					ExpectedLengthStream.CopyBytesToLength(request.DataStream, socket, request.DataLength);
				else
					ExpectedLengthStream.CopyBytesToTimeout(request.DataStream, socket);
			}

			socket.Flush();

			return _parser.Parse(socket);
		}

		public void CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest)
		{
			using (var getTx = Request(loadRequest)) // get source
			{
				var storeRq = storeRequest.Data(getTx.RawBodyStream, getTx.BodyReader.ExpectedLength).Build();
				Request(storeRq).Dispose(); // write out to dest
			}
		}
	}
}