using System.IO;

namespace ShiftIt.Http
{
	public class HttpResponseParser : IHttpResponseParser
	{
		public IHttpResponse Parse(TextReader rawResponse)
		{
			return new HttpReponse(rawResponse);
		}
	}
}