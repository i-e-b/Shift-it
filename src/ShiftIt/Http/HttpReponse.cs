using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ShiftIt.Http
{
	public class HttpReponse : IHttpResponse
	{
		TextReader _rawResponse;

		/// <summary>
		/// Reads synchronously until headers are complete, then 
		/// provides the remaining data in a stream
		/// </summary>
		public HttpReponse(TextReader rawResponse)
		{
			_rawResponse = rawResponse;
			Headers = new Dictionary<string,string>();
			ReadStatusLine(rawResponse.ReadLine());

			foreach (var headerLine in NonBlankLines(rawResponse)) AddHeader(headerLine);
			HeadersComplete = true;

		}

		void AddHeader(string headerLine)
		{
			var parts = headerLine.Split(new[]{": "}, StringSplitOptions.None);
			Headers.Add(parts[0], parts[1]);
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
		public IDictionary<string, string> Headers { get; private set; }

		public void Dispose()
		{
			var stream = Interlocked.Exchange(ref _rawResponse, null);
			if (stream == null) return;
			stream.Close();
			stream.Dispose();
		}
	}
}