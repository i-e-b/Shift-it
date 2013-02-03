using System;
using System.IO;
using System.Net.Sockets;

namespace ShiftIt.Socket
{
	public class SocketStreamFactory : IConnectableStreamSource
	{
		public Stream Connect(Uri connectionTarget, TimeSpan connectionTimeout)
		{
			var s = new SocketStream(
				new System.Net.Sockets.Socket(
					AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
					{Blocking = true});
			s.Socket.SendTimeout = s.Socket.ReceiveTimeout = (int)connectionTimeout.TotalMilliseconds;
			s.Socket.Connect(connectionTarget.Host, connectionTarget.Port);
			return s;
		}
	}
}