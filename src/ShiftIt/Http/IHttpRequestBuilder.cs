using System;
using System.IO;

namespace ShiftIt.Http
{
	public interface IHttpRequestBuilder
	{
		IHttpRequestBuilder Get(Uri target);
		IHttpRequestBuilder Post(Uri target);
		IHttpRequestBuilder Put(Uri target);
		IHttpRequestBuilder Delete(Uri target);

		IHttpRequestBuilder SetHeader(string name, string value);
		IHttpRequestBuilder AddHeader(string name, string value);

		IHttpRequestBuilder Data(Stream stream, long length);
		IHttpRequestBuilder StringData(string data);

		IHttpRequestBuilder BasicAuthentication(string userName, string password);

		IHttpRequest Build();
	}
}
