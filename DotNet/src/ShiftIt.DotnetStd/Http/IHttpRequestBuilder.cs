using System;
using System.IO;
using JetBrains.Annotations;

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
		[NotNull]IHttpRequestBuilder Get([NotNull]Uri target);

		/// <summary>
		/// Request the headers of a resource, excluding the resource itself.
		/// </summary>
        [NotNull]IHttpRequestBuilder Head([NotNull]Uri target);

		/// <summary>
		/// Post data to a target resource. You should use the stream or string data options of the `Build`
		/// or `BuildForm` methods to provide this data.
		/// </summary>
        [NotNull]IHttpRequestBuilder Post([NotNull]Uri target);

		/// <summary>
		/// Put data to a target resource. You should use the stream or string data options of the `Build`
		/// or `BuildForm` methods to provide this data.
		/// </summary>
        [NotNull]IHttpRequestBuilder Put([NotNull]Uri target);

		/// <summary>
		/// Request that a resource be deleted.
		/// </summary>
        [NotNull]IHttpRequestBuilder Delete([NotNull]Uri target);

		/// <summary>
		/// Make a custom request
		/// </summary>
		/// <param name="verb">Verb of request</param>
		/// <param name="target">Target resource</param>
        [NotNull]IHttpRequestBuilder Verb([NotNull]string verb, [NotNull]Uri target);

		/// <summary>
		/// Set the value of a header field, replacing any existing values
		/// </summary>
        [NotNull]IHttpRequestBuilder SetHeader([NotNull]string name, [NotNull]string value);

		/// <summary>
		/// Add a value to a header field, supplimenting any existing values
		/// </summary>
        [NotNull]IHttpRequestBuilder AddHeader([NotNull]string name, [NotNull]string value);

		/// <summary>
		/// Set the MIME types to accept for the resource.
		/// Defaults to */* if not provided. 
		/// </summary>
        [NotNull]IHttpRequestBuilder Accept([NotNull]string mimeTypes);

		/// <summary>
		/// Provide basic authentication details for the target resource.
		/// WARNING: this will be sent in the clear. Use only in internal networks
		/// or over SSL connections.
		/// </summary>
        [NotNull]IHttpRequestBuilder BasicAuthentication([NotNull]string userName, [NotNull]string password);

		/// <summary>
		/// Build the request, providing a data stream for the request. It will be sent to the target resource's
		/// hosting server.
		/// </summary>
		/// <param name="stream">Data stream</param>
		/// <param name="length">Length of data. Must be provided.</param>
        [NotNull]IHttpRequest Build([NotNull]Stream stream, long length);
        
	    /// <summary>
	    /// Build the request, providing a data buffer for the request. It will be sent to the target resource's
	    /// hosting server.
	    /// </summary>
	    /// <param name="uploadData">Bytes to be uploaded. The entire buffer will be sent</param>
        [NotNull]IHttpRequest Build([NotNull]byte[] uploadData);

		/// <summary>
		/// Build the request, providing string data for the request. It will be sent to the target resource's
		/// hosting server. The string will be UTF-8 encoded.
		/// </summary>
        [NotNull]IHttpRequest Build([NotNull]string data);

		/// <summary>
		/// Build the request, providing object data for the request.
		/// Each public property on the object will be sent as a form value to the target resource's
		/// hosting server. This will force Content-Type to application/x-www-form-urlencoded
		/// </summary>
		/// <example><code>
		/// // sends out "targetId=142&amp;value=Hello%2C+Jim"
		/// builder.Build(new {targetId = 142, value = "Hello, Jim" });
		/// </code></example>
        [NotNull]IHttpRequest BuildForm([NotNull]object data);

		/// <summary>
		/// Build the request, suppling no data.
		/// </summary>
        [NotNull]IHttpRequest Build();
	}
}
