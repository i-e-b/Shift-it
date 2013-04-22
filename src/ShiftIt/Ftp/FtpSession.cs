using System;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace ShiftIt.Ftp {
	/// <summary>
	/// A class to send and receive files over FTP, using sockets connections
	/// instead of the .Net built in methods.
	/// </summary>
	public class FtpSession : IFtpSession
	{

		private string _remoteHost, _remotePath, _remoteUser, _remotePass, _mes;
		private int _remotePort, _bytes;
		private Socket _clientSocket;
		private DirectoryListMode _listMode;

		private int _retValue;
		private Boolean _debug;
		private Boolean _logined;
		private string _reply;

		/// <summary>
		/// Gets or sets whether existing remote files are deleted prior to uploads.
		/// This does not affect downloads.
		/// </summary>
		public bool ShouldOverwrite { get; set; }

		/// <summary>
		/// Gets or sets whether ftp file transfers will be continued from partial uploads.
		/// Use with caution.
		/// </summary>
		public bool ShouldContinue { get; private set; }

		/// <summary>
		/// Gets or sets whether ftp connections are active or passive
		/// </summary>
		public bool PassiveMode { get; set; }

		const int BlockSize = 512;

		readonly Byte[] _buffer = new Byte[BlockSize];
		readonly Encoding ASCII = Encoding.ASCII;

		/// <summary>
		/// Create a new FTP transfer agent.
		/// </summary>
		public FtpSession () {
			_remoteHost = "localhost";
			_remotePath = ".";
			_remoteUser = "anonymous";
			_remotePass = "example@example.com";
			_remotePort = 21;
			_debug = false;
			_logined = false;
			PassiveMode = true;
			_listMode = DirectoryListMode.PlatformList;
		}

		/// <summary>
		/// Return a rough URI representation of this factory's connection settings
		/// </summary>
		public override string ToString () {
			return "ftp://" + _remoteUser + ":" + _remotePass + "@" + _remoteHost + ":" + _remotePort + "/" + _remotePath;
		}

		public void Dispose()
		{
			Close();
		}

		/// <summary>
		/// Gets or sets the command mode used to return directory listings
		/// </summary>
		public DirectoryListMode ListMode {
			get { return _listMode; }
			set { _listMode = value; }
		}

		/// <summary>
		/// Set the name or IP addres of the FTP server to connect to.
		/// </summary>
		/// <param name="remoteHost">Server name</param>
		public void SetRemoteHost (string remoteHost) {
			_remoteHost = remoteHost;
		}

		/// <summary>
		/// Return the name of the current FTP server.
		/// </summary>
		/// <returns>Server name</returns>
		public string GetRemoteHost () {
			return _remoteHost;
		}

		/// <summary>
		/// Set the port number to use for FTP.
		/// </summary>
		/// <param name="remotePort">Port number</param>
		public void SetRemotePort (int remotePort) {
			_remotePort = remotePort;
		}

		/// <summary>
		/// Return the current port number.
		/// </summary>
		/// <returns>Current port number</returns>
		public int GetRemotePort () {
			return _remotePort;
		}

		/// <summary>
		/// Set the remote directory path.
		/// </summary>
		/// <param name="remotePath">The remote directory path</param>
		public void SetRemotePath (string remotePath) {
			_remotePath = remotePath;
		}

		/// <summary>
		/// Return the current remote directory path.
		/// </summary>
		/// <returns>The current remote directory path.</returns>
		public string GetRemotePath () {
			return _remotePath;
		}

		/// <summary>
		/// Set the user name to use for logging into the remote server.
		/// </summary>
		/// <param name="remoteUser">Username</param>
		public void SetRemoteUser (string remoteUser) {
			_remoteUser = remoteUser;
		}

		/// <summary>
		/// Set the password to user for logging into the remote server.
		/// </summary>
		/// <param name="remotePass">Password</param>
		public void SetRemotePass (string remotePass) {
			_remotePass = remotePass;
		}

		/// <summary>
		/// Return a string array containing the remote directory's file list.
		/// </summary>
		/// <param name="mask">Filename mask to apply to list.
		/// This is server dependent, but filters like *.txt, *.exe usually work.</param>
		public string[] GetFileList (string mask) {

			if (!_logined) {
				Login();
			}

			Socket cSocket = CreateDataSocket();

			switch (_listMode) {
				case DirectoryListMode.PlatformList:
					SendCommand("LIST  " + mask);
					break;
				default:
					SendCommand("NLST " + mask);
					break;
			}

			if (!(_retValue == 150 || _retValue == 125)) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			_mes = "";

			while (true) {

				System.Threading.Thread.Sleep(100);
				int bytes = cSocket.Receive(_buffer, _buffer.Length, 0);
				System.Threading.Thread.Sleep(100);
				_mes += ASCII.GetString(_buffer, 0, bytes);
				System.Threading.Thread.Sleep(100);

				if (cSocket.Available < 1) {
					break;
				}
			}

			char[] seperator = { '\n', '\r' };
			string[] mess = _mes.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

			cSocket.Close();

			ReadReply();

			if (_retValue != 226)
			{
				if (_reply.Length > 4) {
					throw new IOException(_reply.Substring(4), _retValue);
				}
				throw new IOException(_reply, _retValue);
			}
			return mess;

		}

		/// <summary>
		/// Return the size of a file.
		/// </summary>
		/// <param name="fileName">Full name of a file in the current directory</param>
		public long GetFileSize (string fileName) {

			if (!_logined) {
				Login();
			}

			SendCommand("SIZE " + fileName);
			long size;

			if (_retValue == 213) {
				size = Int64.Parse(_reply.Substring(4));
			} else {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			return size;

		}

		/// <summary>
		/// Login to the remote server.
		/// If needed, username and password should have been given before calling.
		/// </summary>
		public void Login () {

			_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPHostEntry hostInfo = GetHostInfo(_remoteHost);
			_clientSocket.SendTimeout = _clientSocket.ReceiveTimeout = 30000;

			try {
				_clientSocket.Connect(hostInfo.AddressList, _remotePort);
			} catch (Exception) {
				throw new IOException("Can't connect to remote server");
			}

			ReadReply();
			if (_retValue != 220) {
				Close();
				throw new IOException(_reply.Substring(4), _retValue);
			}
			if (_debug)
				Console.WriteLine("USER " + _remoteUser);

			SendCommand("USER " + _remoteUser);

			if (!(_retValue == 331 || _retValue == 230)) {
				Cleanup();
				throw new IOException(_reply.Substring(4), _retValue);
			}

			if (_retValue != 230) {
				if (_debug)
					Console.WriteLine("PASS xxx");

				SendCommand("PASS " + _remotePass);
				if (!(_retValue == 230 || _retValue == 202)) {
					Cleanup();
					throw new IOException(_reply.Substring(4), _retValue);
				}
			}

			_logined = true;
			Console.WriteLine("Connected to " + _remoteHost);

			Chdir(_remotePath);

		}

		/// <summary>
		/// Set the data transfer mode between binary and text.
		/// </summary>
		/// <param name="mode">If true, set binary mode for downloads (8 bit); Else set ASCII text mode (7 bit).</param>
		public void SetBinaryMode (Boolean mode) {

			if (mode) {
				SendCommand("TYPE I");
			} else {
				SendCommand("TYPE A");
			}
			if (_retValue != 200) {
				throw new IOException(_reply.Substring(4), _retValue);
			}
		}

		/// <summary>
		/// Download a file to the Assembly's local directory, keeping the same file name.
		/// Always resets file's download progress.
		/// </summary>
		/// <param name="remFileName">Name of file on remote server</param>
		public void Download (string remFileName) {
			Download(remFileName, "", false);
		}

		/// <summary>
		/// Download a remote file to the Assembly's local directory,
		/// keeping the same file name, and set the resume flag.
		/// </summary>
		/// <param name="remFileName">Name of file on remote server</param>
		/// <param name="resume">if true, try to continue a previous download</param>
		public void Download (string remFileName, Boolean resume) {
			Download(remFileName, "", resume);
		}

		/// <summary>
		/// Download a remote file to a local file name which can include
		/// a path. The local file name will be created or overwritten,
		/// but the path must exist.
		/// </summary>
		/// <param name="locFileName">Local file name (may be a full path)</param>
		/// <param name="remFileName">Remote file name</param>
		public void Download (string remFileName, string locFileName) {
			Download(remFileName, locFileName, false);
		}

		/// <summary>
		/// Download a remote file to a local file name which can include
		/// a path, and set the resume flag. The local file name will be
		/// created or overwritten, but the path must exist.
		/// </summary>
		/// <param name="locFileName">Local file name (may be a full path)</param>
		/// <param name="remFileName">Remote file name</param>
		/// <param name="resume">if true, try to continue a previous download</param>
		public void Download (string remFileName, string locFileName, Boolean resume) {
			if (!_logined) {
				Login();
			}

			SetBinaryMode(true);

			Console.WriteLine("Downloading file " + remFileName + " from " + _remoteHost + "/" + _remotePath);

			if (locFileName.Equals("")) {
				locFileName = remFileName;
			}

			if (!File.Exists(locFileName)) {
				Stream st = File.Create(locFileName);
				st.Close();
			}

			var output = new FileStream(locFileName, FileMode.Open);

			var cSocket = CreateDataSocket();

			if (resume) {

				long offset = output.Length;

				if (offset > 0) {
					SendCommand("REST " + offset);
					if (_retValue != 350) {
						//Some servers may not support resuming.
						offset = 0;
					}
				}

				if (offset > 0) {
					if (_debug) {
						Console.WriteLine("seeking to " + offset);
					}
					long npos = output.Seek(offset, SeekOrigin.Begin);
					Console.WriteLine("new pos=" + npos);
				}
			}

			SendCommand("RETR " + remFileName);

			if (!(_retValue == 150 || _retValue == 125)) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			while (true) {

				_bytes = cSocket.Receive(_buffer, _buffer.Length, 0);
				output.Write(_buffer, 0, _bytes);

				if (cSocket.Available < 1) {
					break;
				}
			}

			output.Close();
			if (cSocket.Connected) {
				cSocket.Close();
			}

			Console.WriteLine("");

			ReadReply();

			if (!(_retValue == 226 || _retValue == 250)) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

		}

		/// <summary>
		/// Upload a file to the current remote directory.
		/// </summary>
		/// <param name="fileName">Full local path and filename to upload</param>
		public void Upload (string fileName) {
			Upload(fileName, false);
		}

		public void Upload (string fileName, Boolean resume) {
			Upload(fileName, Path.GetFileName(fileName), resume);
		}

		/// <summary>
		/// Upload a file and set the resume flag.
		/// </summary>
		/// <param name="fileName">Full local path and filename to upload</param>
		/// <param name="remoteFileName">file name as it should be on the remote server</param>
		/// <param name="resume">if true, try to continue a previous upload</param>
		public void Upload (string fileName, string remoteFileName, Boolean resume) {
			ShouldContinue = false;
			var dest = remoteFileName.Replace("/", "");
			if (!_logined) {
				Login();
			}

			var cSocket = (PassiveMode) ? (CreateDataSocket()) : (CreateDataPort());
			cSocket.SendTimeout = cSocket.ReceiveTimeout = 30000;
			long offset = 0;

			if (resume) {

				try {

					SetBinaryMode(true);
					offset = GetFileSize(dest);
					Console.WriteLine("Resuming upload from " + offset);
				} catch (Exception) {
					offset = 0;
					Console.WriteLine("Failed to get offset, resending whole file ");
				}
			}

			if (offset > 0) {
				ShouldContinue = true;
				SendCommand("REST " + offset);
				if (_retValue != 350) {
					//Remote server may not support resuming.
// ReSharper disable RedundantAssignment
					offset = 0;
// ReSharper restore RedundantAssignment
				}
			}

			// Note: must strip stray slashes out of the name, otherwise
			// FTP server will regard this as an absolute path.
			SendCommand("STOR " + dest);


			if (!(_retValue == 125 || _retValue == 150)) {
				ShouldContinue = false;
				throw new IOException(_reply.Substring(4), _retValue);
			}

			if (!PassiveMode) {
				var old = cSocket;
				cSocket = old.Accept(); // This is blocking. Will wait for a LONG time!
				old.Close();
			}
			cSocket.SendTimeout = cSocket.ReceiveTimeout = 30000;

			// open input stream to read source file
			if (!File.Exists(fileName)) throw new IOException("Specified local file not accessible");

			Console.WriteLine("Uploading file " + fileName + " to " + _remotePath + " as " + remoteFileName);
			cSocket.SendFile(fileName);
			cSocket.Disconnect(false);

			Console.WriteLine("");

			if (cSocket.Connected) {
				cSocket.Close();
			}

			ShouldContinue = false; // all data sent
			ReadReply();

			if (!(_retValue == 226 || _retValue == 250)) {
				throw new IOException(_reply.Substring(4), _retValue);
			}
		}

		/// <summary>
		/// Delete a file from the remote FTP server.
		/// </summary>
		/// <param name="fileName">File in the current remote directory</param>
		public void DeleteRemoteFile (string fileName) {

			if (!_logined) {
				Login();
			}

			SendCommand("DELE " + fileName);

			if (_retValue != 250) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

		}

		/// <summary>
		/// Rename a file on the remote FTP server.
		/// </summary>
		/// <param name="oldFileName">File name in the current remote directory</param>
		/// <param name="newFileName">New file name</param>
		public void RenameRemoteFile (string oldFileName, string newFileName) {

			if (!_logined) {
				Login();
			}

			SendCommand("RNFR " + oldFileName);

			if (_retValue != 350) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			//  known problem
			//  rnto will not take care of existing file.
			//  i.e. It will overwrite if newFileName exist
			SendCommand("RNTO " + newFileName);
			if (_retValue != 250) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

		}

		/// <summary>
		/// Create a directory on the remote FTP server as
		/// a child of the current working directory
		/// </summary>
		/// <param name="dirName">New directory name</param>
		public void Mkdir (string dirName) {

			if (!_logined) {
				Login();
			}

			SendCommand("MKD " + dirName);

			if (!(_retValue == 250 || _retValue == 257)) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

		}

		/// <summary>
		/// List the current working directory
		/// </summary>
		/// <returns>Current working directory, result of PWD command</returns>
		public string Pwd () {
			if (!_logined) {
				Login();
			}

			SendCommand("PWD");

			if (!(_retValue == 250 || _retValue == 257)) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			if (_reply.Contains("\"")) {
				int left = _reply.IndexOf("\"", StringComparison.Ordinal) + 1;
				int right = _reply.LastIndexOf("\"", StringComparison.Ordinal) - left;
				_remotePath = _reply.Substring(left, right);
				return _remotePath;
			}
			return _reply.Length > 4 ? _reply.Substring(4) : _reply;
		}

		/// <summary>
		/// Delete a directory on the remote FTP server.
		/// </summary>
		/// <param name="dirName">Old directory name</param>
		public void Rmdir (string dirName) {

			if (!_logined) {
				Login();
			}

			SendCommand("RMD " + dirName);

			if (_retValue != 250) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

		}

		/// <summary>
		/// Change the current working directory on the remote FTP server.
		/// </summary>
		/// <param name="dirName">New directory</param>
		public void Chdir (string dirName) {

			if (dirName.Equals(".")) {
				return;
			}

			if (!_logined) {
				Login();
			}

			SendCommand("CWD " + dirName);

			if (_retValue != 250) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			_remotePath = dirName;

			Console.WriteLine("Current directory is " + _remotePath);

		}

		/// <summary>
		/// Close the FTP connection.
		/// </summary>
		public void Close () {

			if (_clientSocket != null) {
				SendCommand("QUIT");
			}

			Cleanup();
			Console.WriteLine("Closing...");
		}

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
		public void EnsureRemotePath (string path, bool relative) {
			string fwd = GetRemotePath();
			string[] parts = path.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
			if (parts == null || parts.Length < 1) throw new ArgumentException("EnsureRemotePath: Specified path was empty");

			if (!relative) Chdir("/");

			int idx = 0;

			// See how far we get before bits are missing.
			for (; idx < parts.Length; idx++) {
				try {
					Chdir(parts[idx]);
				} catch (IOException) {
					break;
				}
			}

			// If we get here, then the path part at 'parts[idx]' is missing.
			for (; idx < parts.Length; idx++) {
				try {
					Mkdir(parts[idx]);
					Chdir(parts[idx]);
				} catch (IOException ioex) {
					throw new IOException("EnsureRemotePath: Couldn't create directories. Check permissions and name conflicts", ioex);
				}
			}

			// finish in the same working directory as before:
			Chdir(fwd);
		}

		/// <summary>
		/// Cancel data transfer in progress
		/// </summary>
		public void Abort () {
			SendCommand("ABOR");
			if (_retValue != 226 && _retValue != 426) throw new IOException("Abort failed");
			if (_retValue != 426) ReadReply(); // to get 426 message off queue
			if (_retValue != 426) throw new IOException("Abort failed");
		}

		/// <summary>
		/// Set debug mode.
		/// This causes diagnostic information to be printed to the console.
		/// </summary>
		public void SetDebug (Boolean debug) {
			_debug = debug;
		}

		#region Arcane Inner Workings
		private void ReadReply () {
			_mes = "";
			_reply = ReadLine();
			_retValue = Int32.Parse(_reply.Substring(0, 3));
		}

		private void Cleanup () {
			if (_clientSocket != null) {
				_clientSocket.Close();
				_clientSocket = null;
			}
			_logined = false;
		}

		private string ReadLine () {

			while (true) {
				_bytes = _clientSocket.Receive(_buffer, _buffer.Length, 0);
				_mes += ASCII.GetString(_buffer, 0, _bytes);
				if (_bytes < _buffer.Length) {
					break;
				}
			}

			char[] seperator = { '\n' };
			string[] mess = _mes.Split(seperator);

			if (_mes.Length > 2) {
				// find first non-ignore message:
				foreach (string line in mess) {
					if (line.Length > 3)
						if (line.Substring(3, 1) == " ") {
							_mes = line;
							break;
						}
				}
				//mes = mess[mess.Length - 2];
			} else {
				_mes = mess[0];
			}

			if (_mes.Length < 4) {
				if (_debug)
					foreach (string l in mess) {
						Console.WriteLine("!   " + l);
					}

				throw new IOException("Server returned an invalid message");
			}

			if (!_mes.Substring(3, 1).Equals(" ")) {
				return ReadLine();
			}

			if (_debug) {
				for (int k = 0; k < mess.Length - 1; k++) {
					Console.WriteLine(mess[k]);
				}
			}
			return _mes;
		}

		private void SendCommand (String command) {

			Byte[] cmdBytes = Encoding.ASCII.GetBytes((command + "\r\n").ToCharArray());
			_clientSocket.Send(cmdBytes, cmdBytes.Length, 0);
			ReadReply();
		}

		private Socket CreateDataPort () {
			if (_debug) Console.WriteLine("   preparing port listener  ");
			int port = 1663;

			// create the socket
			var listenSocket = new Socket(AddressFamily.InterNetwork,
											 SocketType.Stream,
											 ProtocolType.Tcp);

			// bind the listening socket to the port
			var hostIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
			var ftp_ip = hostIP.ToString().Replace(".", ",");
			while (true) {
				try {
					var ep = new IPEndPoint(hostIP, port);
					listenSocket.Bind(ep);
					listenSocket.Listen(100);
					break;
				} catch {
					port++;
					if (port > 49000) throw new IOException("Couldn't establish a port to open");
				}
			}
			int p2 = port % 256;
			int p1 = (port - p2) / 256;
			string port_cmd = "PORT " + ftp_ip + "," + p1 + "," + p2;


			if (_debug) Console.WriteLine(port_cmd + " (Opening a data socket for transfer)");

			SendCommand(port_cmd);
			if (_retValue != 200 && _retValue != 227) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			/* This has to be done *after* the STOR message is sent and
			 * the correct status code returned.
			try {
				listenSocket.Accept();
			} catch (Exception) {
				throw new IOException("Didn't get connection from remote server");
			}*/

			return listenSocket;
		}

		private Socket CreateDataSocket () {
			if (_debug) Console.WriteLine("PASV (Opening a data socket for transfer)");
			SendCommand("PASV");

			if (_retValue != 227) {
				throw new IOException(_reply.Substring(4), _retValue);
			}

			int index1 = _reply.IndexOf('(');
			int index2 = _reply.IndexOf(')');
			if (index1 < 0 || index2 < 0) throw new IOException("Malformed PASV reply: " + _reply);

			string ipData = _reply.Substring(index1 + 1, index2 - index1 - 1);
			var parts = new int[7];

			int len = ipData.Length;
			int partCount = 0;
			string buf = "";

			for (int i = 0; i < len && partCount <= 6; i++) {

				char ch = Char.Parse(ipData.Substring(i, 1));
				if (Char.IsDigit(ch))
					buf += ch;
				else if (ch != ',') {
					throw new IOException("Malformed PASV reply: " + _reply);
				}

				if (ch == ',' || i + 1 == len) {

					try {
						parts[partCount++] = Int32.Parse(buf);
						buf = "";
					} catch (Exception) {
						throw new IOException("Malformed PASV reply: " + _reply);
					}
				}
			}

			string ipAddress = parts[0] + "." + parts[1] + "." +
			  parts[2] + "." + parts[3];

			int port = (parts[4] << 8) + parts[5];

			var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.SendTimeout = s.ReceiveTimeout = 30000;
			var hostInfo = GetHostInfo(ipAddress);

			try {
				s.Connect(hostInfo.AddressList, port);
			} catch (Exception) {
				throw new IOException("Can't connect to remote server");
			}

			return s;
		}

		private static IPHostEntry GetHostInfo (string addr) {
			IPHostEntry hostInfo;
			try {
				hostInfo = Dns.GetHostEntry(addr);
				if (hostInfo.AddressList == null || hostInfo.AddressList.Length < 1)
					throw new Exception();
			} catch {
				try {
					hostInfo = Dns.GetHostEntry(addr);
				} catch {
					throw new Exception("Could not resolve host name to any IP addresses: (" + addr + ")");
				}
			}

			return hostInfo;
		}
		#endregion
	}
}
