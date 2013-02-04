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
		Http.StatusClass StatusClass { get; }
		IDictionary<string, string> Headers { get; }

		IExpectedLengthStream BodyReader { get; }
		Stream RawBodyStream { get; }
	}
}