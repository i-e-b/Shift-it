using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ShiftIt.Internal.Http;

namespace ShiftIt.Http
{
	/// <summary>
	/// HTTP requests builder and request object
	/// </summary>
	public class HttpRequestBuilder : IHttpRequestBuilder
	{
		string _verb;
		string _url;
		readonly Dictionary<string, List<string>> _headers;
		private Uri _target;

		/// <summary>
		/// Start building a new request
		/// </summary>
		public HttpRequestBuilder()
		{
			_headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
			{
				{"Accept-Encoding",new List<string> {"gzip", "deflate"}}
			};
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
		/// Build the request, providing a data stream for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		/// <param name="stream">Data stream</param>
		/// <param name="length">Length of data. Must be provided.</param>
		public IHttpRequest Build(Stream stream, long length)
		{
			return new HttpRequest(_target, _verb, stream, length, RequestHead(length));
		}

		/// <summary>
		/// Build the request, providing string data for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		public IHttpRequest Build(string data)
		{
			var bytes = Encoding.UTF8.GetBytes(data);
			var dataLength = bytes.Length;
			return new HttpRequest(_target, _verb, new MemoryStream(bytes), dataLength, RequestHead(dataLength));
		}
		
		/// <summary>
		/// Build the request, providing object data for the request.
		/// Each public property on the object will be sent as a form value to the target resource's
		/// hosting server. This will force Content-Type to application/x-www-form-urlencoded
		/// </summary>
		/// <example><code>
		/// // sends out "targetId=142&amp;value=Hello%2C+Jim"
		/// builder.Build(new {targetId = 142, value = "Hello, Jim" });
		/// </code></example>
		public IHttpRequest BuildForm(object data)
		{
			SetHeader("Content-Type", "application/x-www-form-urlencoded");

			var dataStream = new ObjectToRequestStreamConverter().ConvertToStream(data);
			var dataLength = dataStream.Length;
				
			return new HttpRequest(_target, _verb, dataStream, dataLength, RequestHead(dataLength));
		}

		/// <summary>
		/// Build the request, suppling no data.
		/// </summary>
		public IHttpRequest Build()
		{
			return new HttpRequest(_target, _verb, null, -1, RequestHead(-1));
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

		string RequestHead(long dataLength)
		{
			var sb = new StringBuilder();
			Action crlf = () => sb.Append("\r\n");
			Action<string> a = s => sb.Append(s);
			Action<long> n = v => sb.Append(v.ToString(CultureInfo.InvariantCulture));
			Action<string> k = s => { sb.Append(s); sb.Append(": "); };

			a(_verb); a(" "); a(_url); a(" HTTP/1.1");
			crlf();

			foreach (var header in _headers)
			{
				k(header.Key); a(string.Join(",", header.Value.ToArray())); crlf();
			}

			if (dataLength >= 0)
			{
				k("Content-Length"); n(dataLength); crlf();
			}
			crlf();

			return sb.ToString();
		}

		void StdVerb(string verb, Uri target)
		{
			_target = target;
			SetHeader("Host", string.Format("{0}:{1}", target.Host, target.Port));
			_verb = verb;
			_url = target.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
			Accept("*/*");
		}

		private class HttpRequest : IHttpRequest
		{
			private readonly Uri _target;
			private readonly string _verb;
			private readonly Stream _dataStream;
			private readonly long _dataLength;
			private readonly string _requestHead;
			private readonly bool _secure;

			public HttpRequest(Uri target, string verb, Stream dataStream, long dataLength, string requestHead)
			{
				_target = target;
				_verb = verb;
				_dataStream = dataStream;
				_dataLength = dataLength;
				_requestHead = requestHead;
				_secure = _target.Scheme.ToLowerInvariant() == "https";
			}

			public Uri Target
			{
				get { return _target; }
			}

			public string Verb
			{
				get { return _verb; }
			}

			public string RequestHead
			{
				get { return _requestHead; }
			}

			public Stream DataStream
			{
				get { return _dataStream; }
			}

			public long DataLength
			{
				get { return _dataLength; }
			}

			public bool Secure
			{
				get { return _secure; }
			}
		}
	}
}