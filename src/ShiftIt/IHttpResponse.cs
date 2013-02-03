using System;
using System.Collections.Generic;
using ShiftIt.Socket;

namespace ShiftIt
{
	public interface IHttpResponse: IDisposable
	{
		bool HeadersComplete { get; }
		int StatusCode { get; }
		Http.StatusClass StatusClass { get; }
		IDictionary<string, string> Headers { get; }
		IExpectedLengthStream BodyReader { get; }
	}
}