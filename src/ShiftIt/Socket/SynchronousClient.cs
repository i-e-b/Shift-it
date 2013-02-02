using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ShiftIt.Socket;

namespace ShiftIt.Http
{
	/// <summary>
	/// SynchronousClient class
	/// </summary>
	public class SynchronousClient : ISynchronousClient
	{
		/// <summary>
		/// Calls the target site using an HTTP/1.0 GET.
		/// </summary>
		public string GetString(string url)
		{
				// we don't support SSL!
				if (url.StartsWith("https://"))
					return null;

				var uri = new Uri(url, UriKind.Absolute);

				// build request
				string request = String.Format(
					@"GET {0} HTTP/1.1
HOST: {1}
Accept: text/*
Cache-Control: no-cache
Content-Length: 0

", uri.PathAndQuery, uri.Host);

				string response = RawImmediate(uri.Host, 80, request);

				// check for HTTP header end
				int entireHeaderLength;// length of header block
				if (-1 == (entireHeaderLength = response.IndexOf("\r\n\r\n", StringComparison.Ordinal)))
					return null;

				// parse out header data
				var headerLines = response.Substring(0, entireHeaderLength).Replace("\r\n", "\n").Split('\n');// header block converted to string []
				if (0 == headerLines.Length)
					return null;

				// get response status code
				string[] parts = headerLines[0].Split(' ');
				int status = Int32.Parse(parts[1]);				// HTTP response code
				if (200 != status)
					return null;

				// turn header array collection into a collection
				var headers = new Dictionary<string, string>();				// HTTP headers
				foreach (string hdr in headerLines)
				{
					int pos;
					if (-1 == (pos = hdr.IndexOf(' ')))
						continue;
					if (hdr.StartsWith("HTTP"))
						continue;
					headers.Add(hdr.Substring(0, pos), hdr.Substring(pos + 1));
				}

				return response.Substring(entireHeaderLength + 4);
			
		}

		public string RawImmediate(string host, int port, string rawRequest)
		{
			using (var state = ConnectSocketState(host, port))
			{
				SynchronousSendAndReceive(state, rawRequest);
				return state.ResultData.ToString();
			}
		}

		static StatefulSocket ConnectSocketState(string host, int port)
		{
			var state = new StatefulSocket(
				new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
				{Blocking = true});
			state.Socket.Connect(host, port);
			return state;
		}

		static void SynchronousSendAndReceive(StatefulSocket state, string request)
		{
			var byteData = Encoding.UTF8.GetBytes(request);

			SocketError err;
			state.Socket.Send(byteData, 0, byteData.Length, SocketFlags.None, out err);

			int len;
			state.Socket.ReceiveTimeout = 5000; // longer delay to start sending
			while ((len = state.Socket.Receive(state.ReadBuffer, 0, state.ReadBuffer.Length, SocketFlags.None, out err)) > 0)
			{
				state.Socket.ReceiveTimeout = 500; // shorter delay to read stream from socket buffer
				state.ResultData.Append(Encoding.UTF8.GetString(state.ReadBuffer,0,len));
			}
			state.Socket.Close();
		}
	}
}