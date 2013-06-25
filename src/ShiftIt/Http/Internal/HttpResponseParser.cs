using System;
using System.IO;

namespace ShiftIt.Http.Internal
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
			return new HttpReponse(rawResponse, timeout);
		}
	}
}