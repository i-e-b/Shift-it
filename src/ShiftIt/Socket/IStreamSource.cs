using System.IO;

namespace ShiftIt.Socket
{
	public interface IStreamSource
	{
		Stream AsStream();
	}
}