namespace ShiftIt.Socket
{
	public interface IExpectedLengthStream
	{
		long ExpectedLength { get; }
		
		string ReadStringToLength();
		string ReadStringToTimeout();

		byte[] ReadBytesToLength();
		byte[] ReadBytesToTimeout();

		int Read(byte[] buffer, int offset, int count);
	}
}