using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using ShiftIt.Internal.Socket;

namespace ShiftIt.Http.Internal
{
	/// <summary>
	/// Wrapper for HTTP response streams
	/// </summary>
	public class HttpReponse : IHttpResponse
	{
		Stream _rawResponse;

		private readonly ISet<string> _singleItemHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Content-Length"
			};

		/// <summary>
		/// Reads synchronously until headers are complete, then 
		/// provides the remaining data in a stream
		/// </summary>
		public HttpReponse(Stream rawResponse, TimeSpan timeout)
		{
			_rawResponse = rawResponse;
			Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			ReadStatusLine(NextLine(rawResponse));

			foreach (var headerLine in NonBlankLines(rawResponse)) AddHeader(headerLine);
			HeadersComplete = true;

			RawBodyStream = RestOfStreamDecompressed(rawResponse);

			if (IsChunked())
			{
				BodyReader = new HttpChunkedResponseStream(RawBodyStream) {Timeout = timeout};
			}
			else
			{
				BodyReader = new HttpSingleResponseStream(RawBodyStream, ReportedBodyLength()) {Timeout = timeout};
			}
		}

		bool IsChunked()
		{
			return Headers.ContainsKey("Transfer-Encoding")
				&& Headers["Transfer-Encoding"].ToLowerInvariant() == "chunked";
		}

		int ReportedBodyLength()
		{
			return Headers.ContainsKey("Content-Length") ? GetContentLength() : 0;
		}

		private int GetContentLength()
		{
			return int.Parse(Headers["Content-Length"]);
		}

		Stream RestOfStreamDecompressed(Stream rawResponse)
		{
			rawResponse.ReadByte(); // eat one spare byte
			if (rawResponse is SocketStream) ((SocketStream)rawResponse).ResetCounts();
			if (!Headers.ContainsKey("Content-Encoding")) return rawResponse; // plain body

			switch (Headers["Content-Encoding"])
			{
				case "gzip": return GzipUnwrap(rawResponse);
				case "deflate": return DeflateUnwrap(rawResponse);
				default: throw new Exception("Unknown compression scheme: " + Headers["Content-Encoding"]);
			}
		}

		static Stream DeflateUnwrap(Stream rawResponse)
		{
			return new DeflateStream(rawResponse, CompressionMode.Decompress, true);
		}

		static Stream GzipUnwrap(Stream rawResponse)
		{
			return new GZipStream(rawResponse, CompressionMode.Decompress, true);
		}

		void AddHeader(string headerLine)
		{
			var parts = headerLine.Split(new[] { ": " }, StringSplitOptions.None);
			if (parts.Length < 2)
			{
				throw new ArgumentException("Bad header -- " + headerLine);
			}
			var name = parts[0];
			var value = parts[1];

			lock (Headers)
			{
				if (!Headers.ContainsKey(name))
				{
					Headers.Add(name, value);
					return;
				}
			}
			if (_singleItemHeaders.Contains(name)) Headers[name] = value;
			else Headers[name] += "," + value;
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
					sb.Append((char)b);
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
			var parts = statusLine.Split(new[] { ' ' }, 3);
			if (parts.Length > 1)
			{
				StatusCode = int.Parse(parts[1]);
				StatusClass = (StatusClass)(StatusCode - (StatusCode % 100));
			}
			if (parts.Length > 2) StatusMessage = parts[2];
		}

		/// <summary>
		/// Returns true once all headers have been read.
		/// The body stream can be used at this point.
		/// </summary>
		public bool HeadersComplete { get; private set; }

		/// <summary>
		/// Status code returned by server.
		/// If the status code is zero, there was a protocol error.
		/// </summary>
		public int StatusCode { get; private set; }

		/// <summary>
		/// General class of the status code
		/// </summary>
		public StatusClass StatusClass { get; private set; }

		/// <summary>
		/// Status message returned by server.
		/// </summary>
		public string StatusMessage { get; private set; }

		/// <summary>
		/// Headers returned by server
		/// </summary>
		public IDictionary<string, string> Headers { get; private set; }

		/// <summary>
		/// The HTTP body stream wrapped in a decoder class
		/// </summary>
		public IHttpResponseStream BodyReader { get; private set; }

		/// <summary>
		/// The raw body stream. This will be consumed if you use the BodyReader.
		/// </summary>
		public Stream RawBodyStream { get; private set; }

		/// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var stream = Interlocked.Exchange(ref _rawResponse, null);
			if (stream == null) return;
			stream.Dispose();
		}

		/// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		~HttpReponse()
		{
			Dispose(false);
		}
	}
}