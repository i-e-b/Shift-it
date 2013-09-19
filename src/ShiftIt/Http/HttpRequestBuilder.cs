using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ShiftIt.Http
{
	/// <summary>
	/// HTTP requests builder and request object
	/// </summary>
	public class HttpRequestBuilder : IHttpRequestBuilder, IHttpRequest
	{
		string _verb;
		string _url;
		readonly Dictionary<string, List<string>> _headers;

		/// <summary>
		/// Target resource
		/// </summary>
		public Uri Target { get; private set; }

		/// <summary>
		/// Verb used
		/// </summary>
		string IHttpRequest.Verb { get { return _verb; }}

		/// <summary>
		/// Start building a new request
		/// </summary>
		public HttpRequestBuilder()
		{
			_headers = new Dictionary<string, List<string>>
			{
				{"Accept-Encoding",new List<string> {"gzip", "deflate"}}
			};
			DataLength = -1;
		}

		/// <summary>
		/// Request a target resource
		/// </summary>
		public IHttpRequestBuilder Get(Uri target)
		{
			StdVerb("GET", target);
			return this;
		}

		/// <summary>
		/// Request the headers of a resource, excluding the resource itself.
		/// </summary>
		public IHttpRequestBuilder Head(Uri target)
		{
			StdVerb("HEAD", target);
			return this;
		}

		/// <summary>
		/// Post data to a target resource. You should use the `Data` or `StringData`
		/// methods to provide this data.
		/// </summary>
		public IHttpRequestBuilder Post(Uri target)
		{
			StdVerb("POST", target);
			return this;
		}

		/// <summary>
		/// Put data to a target resource. You should use the `Data` or `StringData`
		/// methods to provide this data.
		/// </summary>
		public IHttpRequestBuilder Put(Uri target)
		{
			StdVerb("PUT", target);
			return this;
		}

		/// <summary>
		/// Request that a resource be deleted.
		/// </summary>
		public IHttpRequestBuilder Delete(Uri target)
		{
			StdVerb("DELETE", target);
			return this;
		}

		/// <summary>
		/// Make a custom request
		/// </summary>
		/// <param name="verb">Verb of request</param>
		/// <param name="target">Target resource</param>
		public IHttpRequestBuilder Verb(string verb, Uri target)
		{
			StdVerb(verb, target);
			return this;
		}

		/// <summary>
		/// Set the MIME types to accept for the resource. Replaces existing header value.
		/// Defaults to */* if not provided. 
		/// </summary>
		public IHttpRequestBuilder Accept(string mimeTypes)
		{
			SetHeader("Accept", mimeTypes);
			return this;
		}

		/// <summary>
		/// Provide a data stream for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		/// <param name="stream">Data stream</param>
		/// <param name="length">Length of data. Must be provided.</param>
		public IHttpRequestBuilder Data(Stream stream, long length)
		{
			DataStream = stream;
			DataLength = length;
			return this;
		}

		/// <summary>
		/// Provide string data for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		public IHttpRequestBuilder StringData(string data)
		{
			var bytes = Encoding.UTF8.GetBytes(data);
			DataStream = new MemoryStream(bytes);
			DataLength = bytes.Length;
			return this;
		}

		/// <summary>
		/// Provide basic authentication details for the target resource.
		/// WARNING: this will be sent in the clear. Use only in internal networks
		/// or over SSL connections.
		/// </summary>
		public IHttpRequestBuilder BasicAuthentication(string userName, string password)
		{
			SetHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(
				userName + ":" + password)));
			return this;
		}

		/// <summary>
		/// Build the request
		/// </summary>
		public IHttpRequest Build()
		{
			return this;
		}

		/// <summary>
		/// Set the value of a header field, replacing any existing values
		/// </summary>
		public IHttpRequestBuilder SetHeader(string name, string value)
		{
			if (!_headers.ContainsKey(name))
			{
				_headers.Add(name, new List<string>());
			}
			_headers[name].Clear();
			_headers[name].Add(value);
			return this;
		}

		/// <summary>
		/// Add a value to a header field, supplimenting any existing values
		/// </summary>
		public IHttpRequestBuilder AddHeader(string name, string value)
		{
			if (!_headers.ContainsKey(name))
			{
				_headers.Add(name, new List<string>());
			}
			if (!_headers[name].Contains(value))
			{
				_headers[name].Add(value);
			}
			return this;
		}

		/// <summary>
		/// Headers
		/// </summary>
		public string RequestHead()
		{
			var sb = new StringBuilder();
			Action crlf = () => sb.Append("\r\n");
			Action<string> a = s => sb.Append(s);
			Action<long> n = v => sb.Append(v.ToString(CultureInfo.InvariantCulture));
			Action<string> k = s => { sb.Append(s); sb.Append(": "); };

			a(_verb); a(" "); a(_url); a(" HTTP/1.1");
			crlf();
			k("Host"); a(Target.Host); a(":"); n(Target.Port); crlf();

			foreach (var header in _headers)
			{
				k(header.Key); a(string.Join(",", header.Value)); crlf();
			}

			if (DataLength >= 0)
			{
				k("Content-Length"); n(DataLength); crlf();
			}
			crlf();

			return sb.ToString();
		}


		void StdVerb(string verb, Uri target)
		{
			Target = target;
			_verb = verb;
			_url = target.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
			Accept("*/*");
		}

		/// <summary>
		/// Length of body data
		/// </summary>
		public long DataLength { get; private set; }

		/// <summary>
		/// Returns true if a HTTPS resource is being requested.
		/// </summary>
		public bool Secure
		{
			get
			{
				if (Target == null) return false;
				return Target.Scheme.ToLowerInvariant() == "https";
			}
		}

		/// <summary>
		/// Body data stream
		/// </summary>
		public Stream DataStream { get; private set; }

	}
}