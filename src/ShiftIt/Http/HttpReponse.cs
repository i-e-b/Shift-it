using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ShiftIt.Http
{
	public class HttpReponse : IHttpResponse
	{
		TextReader _textResponse;
		IDictionary<string, string> _headers;

		/// <summary>
		/// Reads synchronously until headers are complete, then 
		/// provides the remaining data in a stream
		/// </summary>
		public HttpReponse(Stream rawResponse)
		{
			_textResponse = new StreamReader(rawResponse);
			Headers = new Dictionary<string,string>();
			ReadStatusLine(_textResponse.ReadLine());

			foreach (var headerLine in NonBlankLines(_textResponse)) AddHeader(headerLine);
			HeadersComplete = true;

			BodyReader = RestOfStreamDecompressed(rawResponse);
		}

		TextReader RestOfStreamDecompressed(Stream rawResponse)
		{
			if (_textResponse.Peek() < 0) return null; // no body
			if (!Headers.ContainsKey("Content-Encoding")) return _textResponse; // plain body

			// StreamReader does a greedy-read. We need to be a bt smarter!
			rawResponse.Seek(692, SeekOrigin.Begin); // hack!
			///if (rawResponse.Position != 692) throw new Exception("wrong!");

			switch (Headers["Content-Encoding"])
			{
				case "gzip": return GzipUnwrap(rawResponse);
				case "deflate": return DeflateUnwrap(rawResponse);
				default: throw new Exception("Unknown compression scheme: "+Headers["Content-Encoding"]);
			}
		}

		TextReader DeflateUnwrap(Stream rawResponse)
		{
			return new StreamReader(new DeflateStream(rawResponse, CompressionMode.Decompress, true));
		}

		TextReader GzipUnwrap(Stream rawResponse)
		{
			return new StreamReader(new GZipStream(rawResponse, CompressionMode.Decompress, true));
		}

		void AddHeader(string headerLine)
		{
			var parts = headerLine.Split(new[]{": "}, StringSplitOptions.None);
			lock (_headers)
			{
				if (!_headers.ContainsKey(parts[0]))
				{
					_headers.Add(parts[0], parts[1]);
					return;
				}
			}
			Headers[parts[0]] += "," + parts[1];
		}

		static IEnumerable<string> NonBlankLines(TextReader rawResponse)
		{
			while (true)
			{
				var line = rawResponse.ReadLine();
				if (string.IsNullOrWhiteSpace(line)) yield break;
				yield return line;
			}
		}

		void ReadStatusLine(string statusLine)
		{
			var parts = statusLine.Split(new[]{' '}, 3);
			if (parts.Length > 1)
			{
				StatusCode = int.Parse(parts[1]);
				StatusClass = (Http.StatusClass)(StatusCode-(StatusCode%100));
			}
			if (parts.Length > 2) StatusMessage = parts[2];
		}

		public bool HeadersComplete { get; private set; }
		public int StatusCode { get; private set; }
		public StatusClass StatusClass { get; private set; }
		public string StatusMessage { get; private set; }
		public IDictionary<string, string> Headers
		{
			get { return _headers; }
			private set { _headers = value; }
		}

		public TextReader BodyReader { get; private set; }

		public void Dispose()
		{
			var stream = Interlocked.Exchange(ref _textResponse, null);
			if (stream == null) return;
			stream.Close();
			stream.Dispose();
		}
		~HttpReponse()
		{
			Dispose();
		}
	}
}