using System.IO;

namespace ShiftIt.Http
{
	public class HttpResponseParser : IHttpResponseParser
	{
		public IHttpResponse Parse(Stream rawResponse)
		{
			return new HttpReponse(rawResponse);
		}
	}
}