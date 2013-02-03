using System;

namespace ShiftIt.Socket
{
	public interface IConnectable
	{
		void Connect(Uri connectionTarget, TimeSpan connectionTimeout);
	}
}