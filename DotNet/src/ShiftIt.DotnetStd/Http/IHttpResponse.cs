using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Http
{
	/// <summary>
	/// Wrapper for HTTP response streams
	/// </summary>
	public interface IHttpResponse: IDisposable
	{
		/// <summary>
		/// Returns true once all headers have been read.
		/// The body stream can be used at this point.
		/// </summary>
		bool HeadersComplete { get; }

		/// <summary>
		/// Status code returned by server.
		/// If the status code is zero, there was a protocol error.
		/// </summary>
		int StatusCode { get; }
		
		/// <summary>
		/// General class of the status code
		/// </summary>
		StatusClass StatusClass { get; }
		
		/// <summary>
		/// Status message returned by server.
		/// </summary>
		string StatusMessage { get; }

		/// <summary>
		/// Headers returned by server, with duplicates concatenated into single string values
		/// </summary>
        [NotNull]IDictionary<string, string> Headers { get; }

		/// <summary>
		/// Headers lines returned by server, in the same order as supplied, and with no transformation.
		/// This is useful if you are using HttpClient as part of a proxy.
		/// </summary>
		[NotNull]ICollection<string> ExactHeaders { get; }
		
		/// <summary>
		/// The HTTP body stream wrapped in a decoder class
		/// </summary>
        [NotNull]IHttpResponseStream BodyReader { get; }

		/// <summary>
		/// The raw body stream. This will be consumed if you use the BodyReader.
		/// </summary>
		Stream RawBodyStream { get; }
        
        /// <summary>
        /// Raw response headers from the server. For diagnostics only.
        /// </summary>
        byte[] RawHeaderData { get; }
    }
}