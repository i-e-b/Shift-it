using System;
using System.Collections.Generic;

namespace ShiftIt.Http
{
	public interface IHttpResponse: IDisposable
	{
		bool HeadersComplete { get; }
		int StatusCode { get; }
		Http.StatusClass StatusClass { get; }
		IDictionary<string, string> Headers { get; }
	}
}