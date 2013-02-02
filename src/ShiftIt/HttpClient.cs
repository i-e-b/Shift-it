using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ShiftIt
{
	/// <summary>
	/// HttpClient class
	/// </summary>
	public static class HttpClient
	{
		/// <summary>
		/// Calls the target site using an HTTP/1.0 GET.
		/// </summary>
		/// <param name="url">Request URL</param>
		/// <param name="timeOut">Timeout to wait for response, in milliseconds.</param>
		public static string GetString (string url, int timeOut)
		{
			Socket		sock			= null;
			string		path			= null;				// path portion of request

			try
			{
				// we don't support SSL!
				if (url.StartsWith ("https://"))
					return null;

				// parse up the request string
				var host = url.StartsWith ("http://") ? url.Substring (7) : url;

				// split at the first path delimiter
				int			pos;
				if (-1 != (pos = host.IndexOf ('/')))
				{
					path = host.Substring (pos);
					host = host.Substring (0, pos);
				}

				// make sure path is correct
				if (String.IsNullOrEmpty (path) || !path.StartsWith ("/"))
					path = String.Concat ("/", (path ?? ""));

				// connect to server
				sock = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {Blocking = true};
				sock.Connect (host, 80);

				// create state object
				var	state = new SocketState (sock);

				// build request
				string		request = String.Format (
					@"GET {0} HTTP/1.1
HOST: {1}
Accept: text/*
Cache-Control: no-cache
Content-Length: 0

", path, host);

				// send request and wait for it to complete
				Send (state, request);
				state.Done.WaitOne (5000, false);
				state.Done.Reset ();

				// get the response
				StartReceive (state);
				state.Done.WaitOne ();

				// convert the response bytes
				string		response = state.Data.ToString ();				// body of HTTP response

				// check for HTTP header end
				int			hdrLen;				// length of header block
				if (-1 == (hdrLen = response.IndexOf("\r\n\r\n", StringComparison.Ordinal)))
					return null;

				// parse out header data
				string []	data = response.Substring (0, hdrLen).Replace ("\r\n", "\n").Split ('\n');				// header block converted to string []
				if (0 == data.Length)
					return null;

				// get response status code
				string []	parts = data [0].Split (' ');
				int			status = Int32.Parse (parts [1]);				// HTTP response code
				if (200 != status)
					return null;

				// turn header array collection into a collection
				var headers = new Dictionary<string, string> ();				// HTTP headers
				foreach (string hdr in data)
				{
					if (-1 == (pos = hdr.IndexOf (' ')))
						continue;
					if (hdr.StartsWith ("HTTP"))
						continue;
					headers.Add (hdr.Substring (0, pos), hdr.Substring (pos + 1));
				}

				return response.Substring (hdrLen + 4);
			}
			catch
			{
				return null;
			}
			finally
			{
				// close the connection
				if (null != sock && sock.Connected)
				{
					sock.Disconnect (false);
					sock.Close ();
				}
			}
		}

		/// <summary>
		/// Sends string data over the socket.
		/// </summary>
		private static void Send (SocketState state, string data)
		{
			// Convert the string data to byte data using ASCII encoding.
			byte [] byteData = Encoding.ASCII.GetBytes (data);

			// Begin sending the data to the remote device.
			state.Socket.BeginSend (byteData, 0, byteData.Length, 0, SendCallback, state);
		}

		/// <summary>
		/// Send data callback.
		/// </summary>
		/// <param name="ar"></param>
		private static void SendCallback (IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				var	state		= ar.AsyncState as SocketState;
				Socket		client		= state.Socket;

				// Complete sending the data to the remote device.
				client.EndSend (ar);

				// Signal that all bytes have been sent.
				state.Done.Set ();
			}
			catch
			{ }
		}

		/// <summary>
		/// Main receive thread entry point
		/// </summary>
		private static void StartReceive (SocketState state)
		{
			if (null == state)
				return;

			state.Socket.BeginReceive (
				state.ReadBuffer,
				0,
				state.ReadBuffer.Length,
				SocketFlags.None,
				OnReceiveData,
				state
				);
		}

		/// <summary>
		/// Receive data callback.
		/// </summary>
		private static void OnReceiveData (IAsyncResult result)
		{
			SocketState		state;
			int				read;

			if (null == result)
				return;

			if (null == (state = result.AsyncState as SocketState))
				return;

			// get socket
			Socket sock = state.Socket;

			// end the read
			if (0 != (read = sock.EndReceive (result)))
			{
				state.Data.Append(Encoding.ASCII.GetString(state.ReadBuffer, 0, read));

				// start another read
				sock.BeginReceive (
					state.ReadBuffer,
					0,
					state.ReadBuffer.Length,
					SocketFlags.None,
					OnReceiveData,
					state
					);
			}
			else
			{
				if (state.Data.Length > 0)
				{
					state.Socket.Close ();
					state.Done.Set ();
				}
			}
		}
	}
}