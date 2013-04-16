using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;

namespace ShiftIt.Internal.Socket
{
	public class SocketStreamFactory : IConnectableStreamSource
	{
		public Stream ConnectUnsecured(Uri connectionTarget, TimeSpan connectionTimeout)
		{
			var s = new SocketStream(
				new System.Net.Sockets.Socket(
					AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
					{Blocking = true});
			s.Socket.SendTimeout = s.Socket.ReceiveTimeout = (int)connectionTimeout.TotalMilliseconds;
			s.Socket.Connect(connectionTarget.Host, connectionTarget.Port);
			return s;
		}

		public Stream ConnectSSL(Uri connectionTarget, TimeSpan connectionTimeout)
		{
			var stream = new SslStream(
				ConnectUnsecured(connectionTarget, connectionTimeout)
				);
			stream.AuthenticateAsClient(connectionTarget.Host);
			return stream;
		}
	}
}