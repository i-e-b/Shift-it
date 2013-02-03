using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace ShiftIt.Http.Internal
{
	public class HttpReponse : IHttpResponse
	{
		Stream _rawResponse;
		IDictionary<string, string> _headers;

		/// <summary>
		/// Reads synchronously until headers are complete, then 
		/// provides the remaining data in a stream
		/// </summary>
		public HttpReponse(Stream rawResponse)
		{
			_rawResponse = rawResponse;
			Headers = new Dictionary<string,string>();
			ReadStatusLine(NextLine(rawResponse));

			foreach (var headerLine in NonBlankLines(rawResponse)) AddHeader(headerLine);
			HeadersComplete = true;

			BodyReader = RestOfStreamDecompressed(rawResponse);
		}

		TextReader RestOfStreamDecompressed(Stream rawResponse)
		{
			if (rawResponse.Position == rawResponse.Length) return null; // no body
			rawResponse.ReadByte(); // eat one spare byte
			if (!Headers.ContainsKey("Content-Encoding")) return new StreamReader(rawResponse); // plain body

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
		

		static string NextLine(Stream stream)
		{
			var sb = new StringBuilder();
			int b;
			int s = 0;
			while ((b = stream.ReadByte()) >= 0)
			{
				if (b == '\r' || b == '\n')
				{
					if (s == 2) break;
					if (s == 1 && b != '\n') break;
					s++;
				}
				else
				{
					s = 2;
					sb.Append((char) b);
				}
			}
			return sb.ToString();
		}
		static IEnumerable<string> NonBlankLines(Stream rawResponse)
		{
			while (true)
			{
				var line = NextLine(rawResponse);
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
				StatusClass = (StatusClass)(StatusCode-(StatusCode%100));
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
			var stream = Interlocked.Exchange(ref _rawResponse, null);
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