using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ShiftIt.Http
{
	[Serializable]
	[ComVisible(false)]
	public class TimeoutException : SocketException
	{
		public TimeoutException() : base((int)SocketError.TimedOut)
		{
		}

		protected TimeoutException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
		}
	}
}
