using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

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

		public bool RemoteCertificateValidationCallback (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			return true;
		}


		public Stream ConnectSSL(Uri connectionTarget, TimeSpan connectionTimeout)
		{
			var stream = new SslStream(ConnectUnsecured(connectionTarget, connectionTimeout),
			                           false,
			                           RemoteCertificateValidationCallback);
			stream.AuthenticateAsClient(connectionTarget.Host);
			return stream;
		}
	}
}