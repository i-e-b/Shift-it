using System.IO;

namespace ShiftIt.Http
{
	public interface IHttpResponseParser
	{
		IHttpResponse Parse(Stream rawResponse);
	}
}