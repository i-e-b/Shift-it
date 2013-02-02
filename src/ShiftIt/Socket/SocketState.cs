using System;
using System.Text;
using System.Threading;

namespace ShiftIt.Socket
{
	/// <summary>
	/// SocketState class
	/// 
	/// Used to track an async TCP/IP data, and maintain all data received over the async operation.
	/// </summary>
	internal class SocketState:IDisposable
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public SocketState ()
		{
			Socket		= null;
			ReadBuffer	= new byte [BUFFER_SIZE];
			Data		= new StringBuilder ();
			Done		= new ManualResetEvent (false);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="socket"></param>
		public SocketState (System.Net.Sockets.Socket socket)
			: this ()
		{
			Socket = socket;
		}

		/// <summary>
		/// TCP/IP socket.
		/// </summary>
		public System.Net.Sockets.Socket Socket = null;

		/// <summary>
		/// Size of download buffer.
		/// </summary>
		public const int BUFFER_SIZE = 1024;

		/// <summary>
		/// Download buffer.
		/// </summary>
		public byte [] ReadBuffer;

		/// <summary>
		/// Downloaded data converted to a string builder.
		/// </summary>
		public StringBuilder Data;

		/// <summary>
		/// Reset event for synchronization.
		/// </summary>
		public ManualResetEvent Done;

		public void Dispose()
		{
			var sock = Interlocked.Exchange(ref Socket, null);
			if (null == sock || !sock.Connected) return;
			sock.Disconnect(false);
			sock.Close();
		}
	}
}