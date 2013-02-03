using System;
using System.Collections.Generic;
using System.IO;

namespace ShiftIt.Http
{
	public interface IHttpResponse: IDisposable
	{
		bool HeadersComplete { get; }
		int StatusCode { get; }
		Http.StatusClass StatusClass { get; }
		IDictionary<string, string> Headers { get; }
		TextReader BodyReader { get; }
	}
}