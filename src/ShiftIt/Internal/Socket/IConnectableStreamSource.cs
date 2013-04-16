using System;
using System.IO;

namespace ShiftIt.Internal.Socket
{
	public interface IConnectableStreamSource
	{
		Stream ConnectUnsecured(Uri connectionTarget, TimeSpan connectionTimeout);
		Stream ConnectSSL(Uri connectionTarget, TimeSpan connectionTimeout);
	}
}