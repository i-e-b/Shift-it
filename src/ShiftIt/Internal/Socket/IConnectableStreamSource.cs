using System;
using System.IO;

namespace ShiftIt.Internal.Socket
{
	public interface IConnectableStreamSource
	{
		Stream Connect(Uri connectionTarget, TimeSpan connectionTimeout);
	}
}