namespace FTP_CLClient
{
	/// <summary>
	/// List of common directory listing modes for FTP
	/// </summary>
	public enum DirectoryListMode {
		/// <summary>
		/// Standardised list mode. Returns only file/directory names
		/// </summary>
		NameList,

		/// <summary>
		/// Platform specific list mode. Returns more details than NameList, but output varies from server to server.
		/// </summary>
		PlatformList
	}
}