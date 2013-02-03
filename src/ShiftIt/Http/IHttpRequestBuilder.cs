using System;
using System.IO;

namespace ShiftIt.Http
{
	public interface IHttpRequestBuilder
	{
		IHttpRequestBuilder Get(Uri target);
		IHttpRequestBuilder SetHeader(string name, string value);
		IHttpRequestBuilder AddHeader(string name, string value);
		IHttpRequestBuilder Post(Uri target);
		IHttpRequestBuilder Put(Uri target);

		IHttpRequestBuilder Data(Stream stream, int length);
		IHttpRequestBuilder StringData(string data);

		IHttpRequest Build();
	}
}
