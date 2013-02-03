using System;
using System.IO;
using ShiftIt.Http;
using ShiftIt.Http.Internal;
using ShiftIt.Socket;

namespace ShiftIt
{
	public interface IHttpClient
	{
		IHttpResponse Request(IHttpRequest request);
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
			var txt = new StreamWriter(socket);
			txt.Write(request.RequestHead());
			txt.Flush();

			if (request.DataStream != null)
				request.DataStream.CopyTo(socket);

			return _parser.Parse(socket);
		}
	}
}