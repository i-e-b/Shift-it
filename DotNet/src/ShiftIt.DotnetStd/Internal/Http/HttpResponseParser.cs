﻿using System;
using System.IO;
using ShiftIt.Http;

namespace ShiftIt.Internal.Http
{
	/// <summary>
	/// Parser for http responses
	/// </summary>
	public class HttpResponseParser : IHttpResponseParser
	{
		/// <summary>
		/// Return a HTTP response wrapper appropriate for the response stream.
		/// </summary>
		/// <param name="rawResponse">Raw HTTP response stream</param>
		/// <param name="timeout">Connection and data timeout</param>
		/// <returns>A HTTP response parser</returns>
		public IHttpResponse Parse(Stream rawResponse, TimeSpan timeout)
		{
            if (rawResponse == null) throw new ArgumentNullException(nameof(rawResponse));
			return new HttpResponse(rawResponse, timeout);
		}
	}
}