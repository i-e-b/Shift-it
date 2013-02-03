using System;

namespace ShiftIt.Http
{
	public interface IHttpRequestBuilder
	{
		IHttpRequestBuilder Get(Uri target);
		IHttpRequestBuilder SetHeader(string name, string value);
		IHttpRequestBuilder AddHeader(string name, string value);
	}
}
