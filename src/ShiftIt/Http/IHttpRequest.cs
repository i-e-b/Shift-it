using System;
using System.IO;

namespace ShiftIt.Http
{
	/// <summary>
	/// Data required to make a HTTP request
	/// </summary>
	public interface IHttpRequest
	{
		/// <summary>
		/// Target resource
		/// </summary>
		Uri Target { get; }

		/// <summary>
		/// Verb used
		/// </summary>
		string Verb { get; }

		/// <summary>
		/// Headers
		/// </summary>
		string RequestHead();

		/// <summary>
		/// Body data stream
		/// </summary>
		Stream DataStream { get; }

		/// <summary>
		/// Length of body data
		/// </summary>
		long DataLength { get; }

		/// <summary>
		/// Returns true if a HTTPS resource is being requested.
		/// </summary>
		bool Secure { get; }
	}
}