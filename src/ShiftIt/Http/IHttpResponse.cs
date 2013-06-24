using System;
using System.Collections.Generic;
using System.IO;
using ShiftIt.Internal.Socket;

namespace ShiftIt.Http
{
	public interface IHttpResponse: IDisposable
	{
		bool HeadersComplete { get; }
		int StatusCode { get; }
		StatusClass StatusClass { get; }
		string StatusMessage { get; }
		IDictionary<string, string> Headers { get; }

		IHttpResponseStream BodyReader { get; }
		Stream RawBodyStream { get; }
	}
}