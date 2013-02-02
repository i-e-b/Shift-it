using System;

namespace ShiftIt
{
	public interface IHttpClient
	{
		string GetString (string url);

		string RawImmediate(string host, int port, string rawRequest);
	}
}