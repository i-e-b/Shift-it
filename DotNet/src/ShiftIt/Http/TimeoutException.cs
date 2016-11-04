using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ShiftIt.Http
{
	/// <summary>
	/// Exception for timeouts that happen across socket connections
	/// </summary>
	[Serializable]
	[ComVisible(false)]
	public class TimeoutException : SocketException
	{
		/// <summary>
		/// New timeout exception
		/// </summary>
		public TimeoutException() : base((int)SocketError.TimedOut)
		{
		}

		/// <summary>
		/// Internal
		/// </summary>
		protected TimeoutException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
		}
	}
}
