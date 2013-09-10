using System;
using System.Collections.Generic;

namespace ShiftIt.Http
{
	/// <summary>
	/// Exception thrown when a non-sucess result is returned from
	/// a method that does not expose a IHttpResponse
	/// </summary>
	public class HttpTransferException : Exception
	{
		/// <summary>
		/// Headers returned by failing call
		/// </summary>
		public IDictionary<string, string> Headers { get; set; }
		/// <summary>
		/// Original target for failing call
		/// </summary>
		public Uri Target { get; set; }
		/// <summary>
		/// Status code returned by target
		/// </summary>
		public int StatusCode { get; set; }

		/// <summary>
		/// Create an exception object for returned headers, target and status
		/// </summary>
		public HttpTransferException(IDictionary<string, string> headers, Uri target, int statusCode)
		{
			Headers = headers;
			Target = target;
			StatusCode = statusCode;
		}
	}
}