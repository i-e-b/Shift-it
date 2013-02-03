using System;

namespace ShiftIt.Socket
{
	public interface IExpectedLengthStream:IDisposable
	{
		long ExpectedLength { get; }
		
		string ReadStringToLength();
		string ReadStringToTimeout();

		byte[] ReadBytesToLength();
		byte[] ReadBytesToTimeout();

		int Read(byte[] buffer, int offset, int count);
	}
}