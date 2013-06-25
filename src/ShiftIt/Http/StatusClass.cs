namespace ShiftIt.Http
{
	/// <summary>
	/// Classes of HTTP status codes
	/// </summary>
	public enum StatusClass
	{
		/// <summary>
		/// Invalid response
		/// </summary>
		Invalid = 0,

		/// <summary>
		/// Informational (1xx)
		/// </summary>
		Information = 100,

		/// <summary>
		/// Success (2xx)
		/// </summary>
		Success = 200,

		/// <summary>
		/// Redirection (3xx)
		/// </summary>
		Redirection = 300,

		/// <summary>
		/// Client error (4xx)
		/// </summary>
		ClientError = 400,

		/// <summary>
		/// Server error (5xx)
		/// </summary>
		ServerError = 500
	}
}