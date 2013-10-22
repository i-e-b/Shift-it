using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace ShiftIt.Http
{
	/// <summary>
	/// Exception thrown when a non-sucess result is returned from
	/// a method that does not expose a IHttpResponse
	/// </summary>
	[Serializable]
	public class HttpTransferException : Exception
	{
		/// <summary>
		/// Headers returned by failing call
		/// </summary>
		public IDictionary<string, string> Headers { get; set; }
		/// <summary>
		/// Original target for failing call
		/// </summary>
		public Uri Target { get; set; }
		/// <summary>
		/// Status code returned by target
		/// </summary>
		public int StatusCode { get; set; }

		/// <summary>
		/// Create an exception object for returned headers, target and status
		/// </summary>
		public HttpTransferException(IDictionary<string, string> headers, Uri target, int statusCode, string statusMessage):
			base(string.Format("Target: {0} failed with status [{1}] : '{2}'", target, statusCode, statusMessage))
		{
			Headers = headers;
			Target = target;
			StatusCode = statusCode;
		}

		protected HttpTransferException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
			Headers = (IDictionary<string, string>) serializationInfo.GetValue("Headers", typeof (IDictionary<string, string>));
			Target = (Uri)serializationInfo.GetValue("Target", typeof(Uri));
			StatusCode = serializationInfo.GetInt32("StatusCode");
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Headers", Headers);
			info.AddValue("Target", Target);
			info.AddValue("StatusCode", StatusCode);
		}
	}
}