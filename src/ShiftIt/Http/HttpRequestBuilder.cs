using System;
using System.Globalization;
using System.Text;

namespace ShiftIt.Http
{
	public class HttpRequestBuilder : IHttpRequestBuilder
	{
		string _verb;
		string _url;
		Uri _target;

		public IHttpRequestBuilder Get(Uri target)
		{
			_target = target;
			_verb = "GET";	
			_url = target.ToString();
			return this;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			Action crlf = () => sb.Append("\r\n");
			Action<string> a = s => sb.Append(s);
			Action<int> n = v => sb.Append(v.ToString(CultureInfo.InvariantCulture));
			Action<string> k = s => { sb.Append(s); sb.Append(": "); };

			a(_verb); a(" "); a(_url); a(" HTTP/1.1");
			crlf();
			k("Host"); a(_target.Host); a(":");n(_target.Port);
			crlf();
			crlf();


			return sb.ToString();
		}
	}
}