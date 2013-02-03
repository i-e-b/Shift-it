using System;
using System.IO;

namespace ShiftIt.Http
{
	public interface IHttpRequest
	{
		Uri Target { get; }
		string RequestHead();
		Stream DataStream { get; }
	}
}