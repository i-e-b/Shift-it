using System;

namespace ShiftIt.Ftp
{
	public interface IFtpSession : IDisposable
	{
		/// <summary>
		/// Gets or sets whether existing remote files are deleted prior to uploads.
		/// This does not affect downloads.
		/// </summary>
		bool ShouldOverwrite { get; set; }

		/// <summary>
		/// Gets or sets whether ftp file transfers will be continued from partial uploads.
		/// Use with caution.
		/// </summary>
		bool ShouldContinue { get; }

		/// <summary>
		/// Gets or sets whether ftp connections are active or passive
		/// </summary>
		bool PassiveMode { get; set; }

		/// <summary>
		/// Gets or sets the command mode used to return directory listings
		/// </summary>
		DirectoryListMode ListMode { get; set; }

		/// <summary>
		/// Return a rough URI representation of this factory's connection settings
		/// </summary>
		string ToString ();

		/// <summary>
		/// Set the name or IP addres of the FTP server to connect to.
		/// </summary>
		/// <param name="remoteHost">Server name</param>
		void SetRemoteHost (string remoteHost);

		/// <summary>
		/// Return the name of the current FTP server.
		/// </summary>
		/// <returns>Server name</returns>
		string GetRemoteHost ();

		/// <summary>
		/// Set the port number to use for FTP.
		/// </summary>
		/// <param name="remotePort">Port number</param>
		void SetRemotePort (int remotePort);

		/// <summary>
		/// Return the current port number.
		/// </summary>
		/// <returns>Current port number</returns>
		int GetRemotePort ();

		/// <summary>
		/// Set the remote directory path.
		/// </summary>
		/// <param name="remotePath">The remote directory path</param>
		void SetRemotePath (string remotePath);

		/// <summary>
		/// Return the current remote directory path.
		/// </summary>
		/// <returns>The current remote directory path.</returns>
		string GetRemotePath ();

		/// <summary>
		/// Set the user name to use for logging into the remote server.
		/// </summary>
		/// <param name="remoteUser">Username</param>
		void SetRemoteUser (string remoteUser);

		/// <summary>
		/// Set the password to user for logging into the remote server.
		/// </summary>
		/// <param name="remotePass">Password</param>
		void SetRemotePass (string remotePass);

		/// <summary>
		/// Return a string array containing the remote directory's file list.
		/// </summary>
		/// <param name="mask">Filename mask to apply to list.
		/// This is server dependent, but filters like *.txt, *.exe usually work.</param>
		string[] GetFileList (string mask);

		/// <summary>
		/// Return the size of a file.
		/// </summary>
		/// <param name="fileName">Full name of a file in the current directory</param>
		long GetFileSize (string fileName);

		/// <summary>
		/// Login to the remote server.
		/// If needed, username and password should have been given before calling.
		/// </summary>
		void Login ();

		/// <summary>
		/// Set the data transfer mode between binary and text.
		/// </summary>
		/// <param name="mode">If true, set binary mode for downloads (8 bit); Else set ASCII text mode (7 bit).</param>
		void SetBinaryMode (Boolean mode);

		/// <summary>
		/// Download a file to the Assembly's local directory, keeping the same file name.
		/// Always resets file's download progress.
		/// </summary>
		/// <param name="remFileName">Name of file on remote server</param>
		void Download (string remFileName);

		/// <summary>
		/// Download a remote file to the Assembly's local directory,
		/// keeping the same file name, and set the resume flag.
		/// </summary>
		/// <param name="remFileName">Name of file on remote server</param>
		/// <param name="resume">if true, try to continue a previous download</param>
		void Download (string remFileName, Boolean resume);

		/// <summary>
		/// Download a remote file to a local file name which can include
		/// a path. The local file name will be created or overwritten,
		/// but the path must exist.
		/// </summary>
		/// <param name="locFileName">Local file name (may be a full path)</param>
		/// <param name="remFileName">Remote file name</param>
		void Download (string remFileName, string locFileName);

		/// <summary>
		/// Download a remote file to a local file name which can include
		/// a path, and set the resume flag. The local file name will be
		/// created or overwritten, but the path must exist.
		/// </summary>
		/// <param name="locFileName">Local file name (may be a full path)</param>
		/// <param name="remFileName">Remote file name</param>
		/// <param name="resume">if true, try to continue a previous download</param>
		void Download (string remFileName, string locFileName, Boolean resume);

		/// <summary>
		/// Upload a file to the current remote directory.
		/// </summary>
		/// <param name="fileName">Full local path and filename to upload</param>
		void Upload (string fileName);

		void Upload (string fileName, Boolean resume);

		/// <summary>
		/// Upload a file and set the resume flag.
		/// </summary>
		/// <param name="fileName">Full local path and filename to upload</param>
		/// <param name="remoteFileName">file name as it should be on the remote server</param>
		/// <param name="resume">if true, try to continue a previous upload</param>
		void Upload (string fileName, string remoteFileName, Boolean resume);

		/// <summary>
		/// Delete a file from the remote FTP server.
		/// </summary>
		/// <param name="fileName">File in the current remote directory</param>
		void DeleteRemoteFile (string fileName);

		/// <summary>
		/// Rename a file on the remote FTP server.
		/// </summary>
		/// <param name="oldFileName">File name in the current remote directory</param>
		/// <param name="newFileName">New file name</param>
		void RenameRemoteFile (string oldFileName, string newFileName);

		/// <summary>
		/// Create a directory on the remote FTP server as
		/// a child of the current working directory
		/// </summary>
		/// <param name="dirName">New directory name</param>
		void Mkdir (string dirName);

		/// <summary>
		/// List the current working directory
		/// </summary>
		/// <returns>Current working directory, result of PWD command</returns>
		string Pwd ();

		/// <summary>
		/// Delete a directory on the remote FTP server.
		/// </summary>
		/// <param name="dirName">Old directory name</param>
		void Rmdir (string dirName);

		/// <summary>
		/// Change the current working directory on the remote FTP server.
		/// </summary>
		/// <param name="dirName">New directory</param>
		void Chdir (string dirName);

		/// <summary>
		/// Close the FTP connection.
		/// </summary>
		void Close ();

		/// <summary>
		/// Check that the specified path exists.
		/// If it doesn't, then it is created (if possible).
		/// </summary>
		/// <remarks>
		/// Unless this method throws an exception, it should return in the same working directory
		/// as when it is called. If remote path is set to ".", the method will exit in the deepest folder
		/// created by this method.
		/// </remarks>
		/// <param name="path">Path to ensure</param>
		/// <param name="relative">If true, path is treated as relative to the working directory.
		/// Otherwise, it is assumed to be an absolute path on the server.</param>
		void EnsureRemotePath (string path, bool relative);

		/// <summary>
		/// Cancel data transfer in progress
		/// </summary>
		void Abort ();

		/// <summary>
		/// Set debug mode.
		/// This causes diagnostic information to be printed to the console.
		/// </summary>
		void SetDebug (Boolean debug);
	}
}