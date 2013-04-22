using System;
using System.IO;

namespace ShiftIt.Http.Internal
{
	public class HttpResponseParser : IHttpResponseParser
	{
		public IHttpResponse Parse(Stream rawResponse, TimeSpan timeout)
		{
			return new HttpReponse(rawResponse, timeout);
		}
	}
}