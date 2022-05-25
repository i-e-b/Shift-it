using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using ShiftIt.Http;
using ShiftIt.Internal.Socket;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Internal.Http
{

	/// <summary>
	/// Wrapper for HTTP response streams
	/// </summary>
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class HttpResponse : IHttpResponse
	{
		/// <summary>
		/// String used to join duplicated headers. Defaults to <code>","</code>
		/// This affects all Http Responses.
		/// </summary>
		public static string HeaderConcatenationString = ",";
		
        /// <summary>
        /// Raw response headers from the server. For diagnostics only.
        /// </summary>
		[NotNull]public readonly MemoryStream RawHeaders;

		[NotNull]Stream _rawResponse;
		[NotNull]private readonly ISet<string> _singleItemHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Content-Length" };

		/// <summary>
		/// Reads synchronously until headers are complete, then 
		/// provides the remaining data in a stream
		/// </summary>
		public HttpResponse(Stream rawResponse, TimeSpan timeout)
		{
			_rawResponse = rawResponse ?? throw new ArgumentNullException(nameof(rawResponse));

			RawHeaders = new MemoryStream();
			Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			ExactHeaders = new List<string>();
			ReadStatusLine(NextLine(_rawResponse));

			foreach (var headerLine in NonBlankLines(_rawResponse)) AddHeader(headerLine);
			HeadersComplete = true;

			_rawResponse.ReadByte(); // eat one spare byte
			if (_rawResponse is SocketStream stream) stream.ResetCounts();

			RawBodyStream = _rawResponse;

			// I am scared of this code:
			var buffered = new PushbackInputStream(_rawResponse);
			var dechunked = IsChunked() ? (Stream)new HttpChunkedStreamWrapper(buffered, timeout) : buffered;
			var decompressed = RestOfStreamDecompressed(dechunked);
	
			BodyReader = new HttpResponseStream(decompressed, ReportedBodyLength()) {Timeout = timeout};
		}

		bool IsChunked()
		{
			return Headers.ContainsKey("Transfer-Encoding")
				&& Headers["Transfer-Encoding"]?.ToLowerInvariant() == "chunked";
		}

		int ReportedBodyLength()
		{
			return Headers.ContainsKey("Content-Length") ? GetContentLength() : 0;
		}

		private int GetContentLength()
		{
			return int.Parse(Headers["Content-Length"] ?? "0");
		}

		Stream RestOfStreamDecompressed(Stream unchunked)
		{
			if (!Headers.ContainsKey("Content-Encoding")) return unchunked; // plain body

			var pushbackStream = new PushbackInputStream(unchunked);

			switch (Headers["Content-Encoding"])
			{
				case "gzip": return GzipUnwrap(pushbackStream);
				case "deflate": return DeflateUnwrap(unchunked); // no good way to determine this
				default: throw new Exception("Unknown compression scheme: " + Headers["Content-Encoding"]);
			}
		}


		static Stream DeflateUnwrap(Stream rawResponse)
		{
            if (rawResponse == null) throw new ArgumentNullException(nameof(rawResponse));
			return new DeflateStream(rawResponse, CompressionMode.Decompress, true);
		}

		static Stream GzipUnwrap(PushbackInputStream rawResponse)
		{
			return new GZipStreamWrapper(rawResponse);
		}

		void AddHeader(string headerLine)
		{
			ExactHeaders?.Add(headerLine);
			
			var parts = headerLine?.Split(new[] { ": " }, StringSplitOptions.None);
			if (parts == null || parts.Length < 2)
			{
				throw new ArgumentException(FormatError(headerLine));
			}
			var name = parts[0] ?? throw new ArgumentException(FormatError(headerLine));
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
			else Headers[name] += HeaderConcatenationString + value;
		}

		private string FormatError(string headerLine)
		{
			ReadRestOfHeaderIntoDebugResponse();
			var headersAsString = Encoding.UTF8.GetString(RawHeaders.ToArray());
			return $"Bad header -- {headerLine}\r\nFull headers:{headersAsString}";
		}

		private void ReadRestOfHeaderIntoDebugResponse()
		{
			while (true)
			{
				var line = NextLine(_rawResponse);
				if (string.IsNullOrWhiteSpace(line))
					break;
			}
		}

		[NotNull]string NextLine([NotNull]Stream stream)
		{
			var sb = new StringBuilder();
			int b;
			int s = 0;
			
			while ((b = stream.ReadByte()) >= 0)
			{
				RawHeaders.WriteByte((byte)b);
				if (b == '\r' || b == '\n')
				{
					if (s == 2) {break;}
					if (s == 1 && b != '\n') break;
					s++;
				}
				else
				{
					s = 2;
					sb.Append((char)b);
				}
			}

			var nextLine = sb.ToString();
			return nextLine;
		}

		[NotNull]IEnumerable<string> NonBlankLines(Stream rawResponse)
		{
            if (rawResponse == null) yield break;

			while (true)
			{
				var line = NextLine(rawResponse);
				if (string.IsNullOrWhiteSpace(line))
					yield break;
				yield return line;
			}
		}

		void ReadStatusLine([NotNull]string statusLine)
		{
			var parts = statusLine.Split(new[] { ' ' }, 3);
			if (parts.Length > 1)
			{
				StatusCode = int.Parse(parts[1] ?? throw new Exception("Response status line was not valid"));
				StatusClass = (StatusClass)(StatusCode - (StatusCode % 100));
			}
			if (parts.Length > 2) StatusMessage = parts[2];
		}

		/// <summary>
		/// Returns true once all headers have been read.
		/// The body stream can be used at this point.
		/// </summary>
		public bool HeadersComplete { get; }

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
		/// Headers returned by server, with duplicates concatenated into single string values
		/// </summary>
		public IDictionary<string, string> Headers { get; }
		
		/// <summary>
		/// Headers lines returned by server, in the same order as supplied, and with no transformation.
		/// </summary>
		public ICollection<string> ExactHeaders { get; }

		/// <summary>
		/// The HTTP body stream wrapped in a decoder class
		/// </summary>
		public IHttpResponseStream BodyReader { get; }

		/// <summary>
		/// The raw body stream. This will be consumed if you use the BodyReader.
		/// </summary>
		public Stream RawBodyStream { get; }

        /// <inheritdoc />
        public byte[] RawHeaderData
        {
            get
            {
                RawHeaders.Seek(0, SeekOrigin.Begin);
                return RawHeaders.ToArray();
            }
        }

        /// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Internal dispose
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			var stream = Interlocked.Exchange(ref _rawResponse, null);
			if (stream == null) return;
			stream.Dispose();
		}

		/// <summary>
		/// Dispose of the underlying stream
		/// </summary>
		~HttpResponse()
		{
			Dispose(false);
		}
	}
}