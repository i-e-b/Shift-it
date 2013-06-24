using System;

namespace ShiftIt.Internal.Socket
{
	public interface IHttpResponseStream:IDisposable
	{
		long ExpectedLength { get; }
		TimeSpan Timeout { get; set; }

		string ReadStringToLength();
		string ReadStringToTimeout();

		byte[] ReadBytesToLength();
		byte[] ReadBytesToTimeout();

		int Read(byte[] buffer, int offset, int count);
	}
}