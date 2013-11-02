using System;
using System.IO;

namespace ShiftIt.Http
{
	/// <summary>
	/// Helper for building HTTP requests
	/// </summary>
	public interface IHttpRequestBuilder
	{
		/// <summary>
		/// Request a target resource
		/// </summary>
		IHttpRequestBuilder Get(Uri target);

		/// <summary>
		/// Request the headers of a resource, excluding the resource itself.
		/// </summary>
		IHttpRequestBuilder Head(Uri target);

		/// <summary>
		/// Post data to a target resource. You should use the `Data` or `StringData`
		/// methods to provide this data.
		/// </summary>
		IHttpRequestBuilder Post(Uri target);

		/// <summary>
		/// Put data to a target resource. You should use the `Data` or `StringData`
		/// methods to provide this data.
		/// </summary>
		IHttpRequestBuilder Put(Uri target);

		/// <summary>
		/// Request that a resource be deleted.
		/// </summary>
		IHttpRequestBuilder Delete(Uri target);

		/// <summary>
		/// Make a custom request
		/// </summary>
		/// <param name="verb">Verb of request</param>
		/// <param name="target">Target resource</param>
		IHttpRequestBuilder Verb(string verb, Uri target);

		/// <summary>
		/// Set the value of a header field, replacing any existing values
		/// </summary>
		IHttpRequestBuilder SetHeader(string name, string value);

		/// <summary>
		/// Add a value to a header field, supplimenting any existing values
		/// </summary>
		IHttpRequestBuilder AddHeader(string name, string value);

		/// <summary>
		/// Set the MIME types to accept for the resource.
		/// Defaults to */* if not provided. 
		/// </summary>
		IHttpRequestBuilder Accept(string mimeTypes);

		/// <summary>
		/// Provide basic authentication details for the target resource.
		/// WARNING: this will be sent in the clear. Use only in internal networks
		/// or over SSL connections.
		/// </summary>
		IHttpRequestBuilder BasicAuthentication(string userName, string password);

		/// <summary>
		/// Build the request, providing a data stream for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		/// <param name="stream">Data stream</param>
		/// <param name="length">Length of data. Must be provided.</param>
		IHttpRequest Build(Stream stream, long length);

		/// <summary>
		/// Build the request, providing string data for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		IHttpRequest Build(string data);

		/// <summary>
		/// Build the request, providing object data for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		/// <example><code>
		/// // sends out "targetId=142&amp;value=Hello%2C+Jim"
		/// builder.Build(new {targetId = 142, value = "Hello, Jim" });
		/// </code></example>
		IHttpRequest Build(object data);

		/// <summary>
		/// Build the request
		/// </summary>
		IHttpRequest Build();
	}
}
