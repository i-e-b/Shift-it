using System;
using System.Text;
using System.Threading;

namespace ShiftIt.Socket
{
	/// <summary>
	/// Used to track an async TCP/IP data, and maintain all data received over the async operation.
	/// </summary>
	internal class StatefulSocket : IDisposable
	{
		public StatefulSocket()
		{
			_socket = null;
			ReadBuffer = new byte[BufferSize];
			ResultData = new StringBuilder();
		}

		public StatefulSocket(System.Net.Sockets.Socket socket)
			: this()
		{
			_socket = socket;
		}

		System.Net.Sockets.Socket _socket;
		public System.Net.Sockets.Socket Socket { get { return _socket; } }

		public const int BufferSize = 1024;

		public readonly byte[] ReadBuffer;

		public StringBuilder ResultData;

		~StatefulSocket()
		{
			Dispose();
		}

		public void Dispose()
		{
			var sock = Interlocked.Exchange(ref _socket, null);
			if (sock == null) return;
			if (sock.Connected)
			{
				sock.Disconnect(false);
				sock.Close();
			}
			sock.Dispose();
		}
	}
}