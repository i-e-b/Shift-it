using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ShiftIt.Http
{
	public class HttpRequestBuilder : IHttpRequestBuilder
	{
		string _verb;
		string _url;
		string _accept;
		Uri _target;
		readonly Dictionary<string, List<string>> _headers;

		public HttpRequestBuilder()
		{
			_headers = new Dictionary<string, List<string>>();
			DataLength = -1;
		}

		public IHttpRequestBuilder Get(Uri target)
		{
			StdVerb("GET", target);
			return this;
		}

		public IHttpRequestBuilder Post(Uri target, string data)
		{
			StdVerb("POST", target);
			DataStream = new StringReader(data);
			DataLength = data.Length;
			return this;
		}

		public IHttpRequestBuilder SetHeader(string name, string value)
		{
			lock (_headers)
			{
				if (!_headers.ContainsKey(name)) _headers.Add(name, new List<string>());
			}
			_headers[name].Clear();
			_headers[name].Add(value);
			return this;
		}

		public IHttpRequestBuilder AddHeader(string name, string value)
		{
			lock (_headers)
			{
				if (!_headers.ContainsKey(name)) _headers.Add(name, new List<string>());
			}
			_headers[name].Add(value);
			return this;
		}

		public string RequestHead()
		{
			var sb = new StringBuilder();
			Action crlf = () => sb.Append("\r\n");
			Action<string> a = s => sb.Append(s);
			Action<long> n = v => sb.Append(v.ToString(CultureInfo.InvariantCulture));
			Action<string> k = s => { sb.Append(s); sb.Append(": "); };

			a(_verb); a(" "); a(_url); a(" HTTP/1.1");
			crlf();
			k("Host"); a(_target.Host); a(":");n(_target.Port);crlf();
			k("Accept"); a(_accept); crlf();

			foreach (var header in _headers)
			{
				k(header.Key); a(string.Join(",", header.Value)); crlf();
			}

			if (DataLength >= 0)
			{
				k("Content-Length");n(DataLength);crlf();
			}
			crlf();

			return sb.ToString();
		}
		

		void StdVerb(string verb, Uri target)
		{
			_target = target;
			_verb = verb;	
			_url = target.ToString();
			_accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
		}
		public long DataLength { get; private set; }
		public TextReader DataStream { get; private set; }
	}
}