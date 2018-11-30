﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using ShiftIt.Http;
using ShiftIt.Internal.Socket;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Internal.Http
{

	/// <summary>
	/// Wrapper for HTTP response streams
	/// </summary>
	public class HttpResponse : IHttpResponse
	{
		Stream _rawResponse;
		private readonly StringBuilder _debugResponse;

		private readonly ISet<string> _singleItemHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Content-Length"
			};

		/// <summary>
		/// Reads synchronously until headers are complete, then 
		/// provides the remaining data in a stream
		/// </summary>
		public HttpResponse(Stream rawResponse, TimeSpan timeout)
		{
			_debugResponse = new StringBuilder();
			_rawResponse = rawResponse;
			Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			ReadStatusLine(NextLine(_rawResponse));

			foreach (var headerLine in NonBlankLines(_rawResponse)) AddHeader(headerLine);
			HeadersComplete = true;

			_rawResponse.ReadByte(); // eat one spare byte
			if (_rawResponse is SocketStream) ((SocketStream)_rawResponse).ResetCounts();

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
			return new DeflateStream(rawResponse, CompressionMode.Decompress, true);
		}

		static Stream GzipUnwrap(PushbackInputStream rawResponse)
		{
			return new GZipStreamWrapper(rawResponse);
		}

		void AddHeader(string headerLine)
		{
			var parts = headerLine.Split(new[] { ": " }, StringSplitOptions.None);
			if (parts.Length < 2)
			{
				throw new ArgumentException(FormatError(headerLine));
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

		private string FormatError(string headerLine)
		{
			ReadRestOfHeaderIntoDebugResponse();
			return string.Format("Bad header -- {0}{1}Full headers:{2}", headerLine, "\r\n", _debugResponse);
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

		string NextLine(Stream stream)
		{
			var sb = new StringBuilder();
			int b;
			int s = 0;
			
			while ((b = stream.ReadByte()) >= 0)
			{
				_debugResponse.Append((char) b);
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
		IEnumerable<string> NonBlankLines(Stream rawResponse)
		{
			while (true)
			{
				var line = NextLine(rawResponse);
				if (string.IsNullOrWhiteSpace(line))
					yield break;
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