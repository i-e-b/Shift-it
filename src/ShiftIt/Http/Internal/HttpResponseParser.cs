using System.IO;

namespace ShiftIt.Http.Internal
{
	public class HttpResponseParser : IHttpResponseParser
	{
		public IHttpResponse Parse(Stream rawResponse)
		{
			return new HttpReponse(rawResponse);
		}
	}
}