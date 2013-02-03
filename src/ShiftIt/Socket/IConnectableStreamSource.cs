using System;
using System.IO;

namespace ShiftIt.Socket
{
	public interface IConnectableStreamSource
	{
		Stream Connect(Uri connectionTarget, TimeSpan connectionTimeout);
	}
}