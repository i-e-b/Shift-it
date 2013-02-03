using System;
using System.IO;

namespace ShiftIt
{
	public interface IHttpRequest
	{
		Uri Target { get; }
		string RequestHead();
		Stream DataStream { get; }
	}
}