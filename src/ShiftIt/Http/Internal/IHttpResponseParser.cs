using System;
using System.IO;

namespace ShiftIt.Http.Internal
{
	public interface IHttpResponseParser
	{
		IHttpResponse Parse(Stream rawResponse, TimeSpan timeout);
	}
}