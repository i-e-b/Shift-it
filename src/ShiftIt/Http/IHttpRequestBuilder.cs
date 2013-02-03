using System;

namespace ShiftIt.Http
{
	public interface IHttpRequestBuilder
	{
		IHttpRequestBuilder Get(Uri target);
	}
}
