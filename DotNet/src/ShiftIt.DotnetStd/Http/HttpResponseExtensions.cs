﻿using System.Net;

namespace ShiftIt.Http
{
	/// <summary>
	/// Extensions for IHttpResponse
	/// </summary>
	public static class HttpResponseExtensions
	{
		/// <summary>
		/// Converts StatusCode integer into HttpStatusCode enum.
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static HttpStatusCode HttpStatusCode(this IHttpResponse response)
		{
			return (HttpStatusCode)response.StatusCode;
		}
	}
}
