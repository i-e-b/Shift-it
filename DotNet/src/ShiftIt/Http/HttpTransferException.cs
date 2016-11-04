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

		/// <summary>
		/// Internal exception constructor
		/// </summary>
		protected HttpTransferException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
			Headers = (IDictionary<string, string>) serializationInfo.GetValue("Headers", typeof (IDictionary<string, string>));
			Target = (Uri)serializationInfo.GetValue("Target", typeof(Uri));
			StatusCode = serializationInfo.GetInt32("StatusCode");
		}

		/// <summary>
		/// Sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param><param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination. </param><exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic). </exception><filterpriority>2</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter"/></PermissionSet>
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