using System;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace FtpLib {
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

	/// <summary>
	/// A class to send and receive files over FTP, using sockets connections
	/// instead of the .Net built in methods.
	/// </summary>
	public class FTPFactory {

		private string remoteHost, remotePath, remoteUser, remotePass, mes;
		private int remotePort, bytes;
		private Socket clientSocket;
		private DirectoryListMode listMode;

		private int retValue;
		private Boolean debug;
		private Boolean logined;
		private string reply;
		private bool passive_mode;
		private bool should_overwrite;
		private bool should_continue;

		/// <summary>
		/// Gets or sets whether existing remote files are deleted prior to uploads.
		/// This does not affect downloads.
		/// </summary>
		public bool ShouldOverwrite {
			get { return should_overwrite; }
			set { should_overwrite = value; }
		}

		/// <summary>
		/// Gets or sets whether ftp file transfers will be continued from partial uploads.
		/// Use with caution.
		/// </summary>
		public bool ShouldContinue {
			get { return should_continue; }
		}

		/// <summary>
		/// Gets or sets whether ftp connections are active or passive
		/// </summary>
		public bool PassiveMode {
			get { return passive_mode; }
			set { passive_mode = value; }
		}

		private static int BLOCK_SIZE = 512;

		Byte[] buffer = new Byte[BLOCK_SIZE];
		Encoding ASCII = Encoding.ASCII;

		/// <summary>
		/// Create a new FTP transfer agent.
		/// </summary>
		public FTPFactory () {
			remoteHost = "localhost";
			remotePath = ".";
			remoteUser = "anonymous";
			remotePass = "example@example.com";
			remotePort = 21;
			debug = false;
			logined = false;
			passive_mode = true;
			listMode = DirectoryListMode.PlatformList;
		}

		/// <summary>
		/// Return a rough URI representation of this factory's connection settings
		/// </summary>
		public override string ToString () {
			return "ftp://" + remoteUser + ":" + remotePass + "@" + remoteHost + ":" + remotePort + "/" + remotePath;
		}

		/// <summary>
		/// Gets or sets the command mode used to return directory listings
		/// </summary>
		public DirectoryListMode ListMode {
			get { return listMode; }
			set { listMode = value; }
		}

		/// <summary>
		/// Set the name or IP addres of the FTP server to connect to.
		/// </summary>
		/// <param name="remoteHost">Server name</param>
		public void setRemoteHost (string remoteHost) {
			this.remoteHost = remoteHost;
		}

		/// <summary>
		/// Return the name of the current FTP server.
		/// </summary>
		/// <returns>Server name</returns>
		public string getRemoteHost () {
			return remoteHost;
		}

		/// <summary>
		/// Set the port number to use for FTP.
		/// </summary>
		/// <param name="remotePort">Port number</param>
		public void setRemotePort (int remotePort) {
			this.remotePort = remotePort;
		}

		/// <summary>
		/// Return the current port number.
		/// </summary>
		/// <returns>Current port number</returns>
		public int getRemotePort () {
			return remotePort;
		}

		/// <summary>
		/// Set the remote directory path.
		/// </summary>
		/// <param name="remotePath">The remote directory path</param>
		public void setRemotePath (string remotePath) {
			this.remotePath = remotePath;
		}

		/// <summary>
		/// Return the current remote directory path.
		/// </summary>
		/// <returns>The current remote directory path.</returns>
		public string getRemotePath () {
			return remotePath;
		}

		/// <summary>
		/// Set the user name to use for logging into the remote server.
		/// </summary>
		/// <param name="remoteUser">Username</param>
		public void setRemoteUser (string remoteUser) {
			this.remoteUser = remoteUser;
		}

		/// <summary>
		/// Set the password to user for logging into the remote server.
		/// </summary>
		/// <param name="remotePass">Password</param>
		public void setRemotePass (string remotePass) {
			this.remotePass = remotePass;
		}

		/// <summary>
		/// Return a string array containing the remote directory's file list.
		/// </summary>
		/// <param name="mask">Filename mask to apply to list.
		/// This is server dependent, but filters like *.txt, *.exe usually work.</param>
		public string[] getFileList (string mask) {

			if (!logined) {
				login();
			}

			Socket cSocket = createDataSocket();

			switch (listMode) {
				case DirectoryListMode.PlatformList:
					sendCommand("LIST  " + mask);
					break;
				case DirectoryListMode.NameList:
				default:
					sendCommand("NLST " + mask);
					break;
			}

			if (!(retValue == 150 || retValue == 125)) {
				throw new IOException(reply.Substring(4), retValue);
			}

			mes = "";

			while (true) {

				System.Threading.Thread.Sleep(100);
				int bytes = cSocket.Receive(buffer, buffer.Length, 0);
				System.Threading.Thread.Sleep(100);
				mes += ASCII.GetString(buffer, 0, bytes);
				System.Threading.Thread.Sleep(100);

				if (cSocket.Available < 1) {
					break;
				}
			}

			char[] seperator = { '\n' };
			string[] mess = mes.Split(seperator);

			cSocket.Close();

			readReply();

			if (retValue != 226) {
				if (reply.Length > 4) {
					throw new IOException(reply.Substring(4), retValue);
				} else {
					throw new IOException(reply, retValue);
				}
			}
			return mess;

		}

		/// <summary>
		/// Return the size of a file.
		/// </summary>
		/// <param name="fileName">Full name of a file in the current directory</param>
		public long getFileSize (string fileName) {

			if (!logined) {
				login();
			}

			sendCommand("SIZE " + fileName);
			long size = 0;

			if (retValue == 213) {
				size = Int64.Parse(reply.Substring(4));
			} else {
				throw new IOException(reply.Substring(4), retValue);
			}

			return size;

		}

		/// <summary>
		/// Login to the remote server.
		/// If needed, username and password should have been given before calling.
		/// </summary>
		public void login () {

			clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPHostEntry hostInfo = GetHostInfo(remoteHost);
			clientSocket.SendTimeout = clientSocket.ReceiveTimeout = 30000;

			try {
				clientSocket.Connect(hostInfo.AddressList, remotePort);
			} catch (Exception) {
				throw new IOException("Can't connect to remote server");
			}

			readReply();
			if (retValue != 220) {
				close();
				throw new IOException(reply.Substring(4), retValue);
			}
			if (debug)
				Console.WriteLine("USER " + remoteUser);

			sendCommand("USER " + remoteUser);

			if (!(retValue == 331 || retValue == 230)) {
				cleanup();
				throw new IOException(reply.Substring(4), retValue);
			}

			if (retValue != 230) {
				if (debug)
					Console.WriteLine("PASS xxx");

				sendCommand("PASS " + remotePass);
				if (!(retValue == 230 || retValue == 202)) {
					cleanup();
					throw new IOException(reply.Substring(4), retValue);
				}
			}

			logined = true;
			Console.WriteLine("Connected to " + remoteHost);

			chdir(remotePath);

		}

		/// <summary>
		/// Set the data transfer mode between binary and text.
		/// </summary>
		/// <param name="mode">If true, set binary mode for downloads (8 bit); Else set ASCII text mode (7 bit).</param>
		public void setBinaryMode (Boolean mode) {

			if (mode) {
				sendCommand("TYPE I");
			} else {
				sendCommand("TYPE A");
			}
			if (retValue != 200) {
				throw new IOException(reply.Substring(4), retValue);
			}
		}

		/// <summary>
		/// Download a file to the Assembly's local directory, keeping the same file name.
		/// Always resets file's download progress.
		/// </summary>
		/// <param name="remFileName">Name of file on remote server</param>
		public void download (string remFileName) {
			download(remFileName, "", false);
		}

		/// <summary>
		/// Download a remote file to the Assembly's local directory,
		/// keeping the same file name, and set the resume flag.
		/// </summary>
		/// <param name="remFileName">Name of file on remote server</param>
		/// <param name="resume">if true, try to continue a previous download</param>
		public void download (string remFileName, Boolean resume) {
			download(remFileName, "", resume);
		}

		/// <summary>
		/// Download a remote file to a local file name which can include
		/// a path. The local file name will be created or overwritten,
		/// but the path must exist.
		/// </summary>
		/// <param name="locFileName">Local file name (may be a full path)</param>
		/// <param name="remFileName">Remote file name</param>
		public void download (string remFileName, string locFileName) {
			download(remFileName, locFileName, false);
		}

		/// <summary>
		/// Download a remote file to a local file name which can include
		/// a path, and set the resume flag. The local file name will be
		/// created or overwritten, but the path must exist.
		/// </summary>
		/// <param name="locFileName">Local file name (may be a full path)</param>
		/// <param name="remFileName">Remote file name</param>
		/// <param name="resume">if true, try to continue a previous download</param>
		public void download (string remFileName, string locFileName, Boolean resume) {
			if (!logined) {
				login();
			}

			setBinaryMode(true);

			Console.WriteLine("Downloading file " + remFileName + " from " + remoteHost + "/" + remotePath);

			if (locFileName.Equals("")) {
				locFileName = remFileName;
			}

			if (!File.Exists(locFileName)) {
				Stream st = File.Create(locFileName);
				st.Close();
			}

			FileStream output = new FileStream(locFileName, FileMode.Open);

			Socket cSocket = createDataSocket();

			long offset = 0;

			if (resume) {

				offset = output.Length;

				if (offset > 0) {
					sendCommand("REST " + offset);
					if (retValue != 350) {
						//throw new IOException(reply.Substring(4), retValue);
						//Some servers may not support resuming.
						offset = 0;
					}
				}

				if (offset > 0) {
					if (debug) {
						Console.WriteLine("seeking to " + offset);
					}
					long npos = output.Seek(offset, SeekOrigin.Begin);
					Console.WriteLine("new pos=" + npos);
				}
			}

			sendCommand("RETR " + remFileName);

			if (!(retValue == 150 || retValue == 125)) {
				throw new IOException(reply.Substring(4), retValue);
			}

			while (true) {

				bytes = cSocket.Receive(buffer, buffer.Length, 0);
				output.Write(buffer, 0, bytes);

				if (cSocket.Available < 1) {
					break;
				}
			}

			output.Close();
			if (cSocket.Connected) {
				cSocket.Close();
			}

			Console.WriteLine("");

			readReply();

			if (!(retValue == 226 || retValue == 250)) {
				throw new IOException(reply.Substring(4), retValue);
			}

		}

		/// <summary>
		/// Upload a file to the current remote directory.
		/// </summary>
		/// <param name="fileName">Full local path and filename to upload</param>
		public void upload (string fileName) {
			upload(fileName, false);
		}

		public void upload (string fileName, Boolean resume) {
			upload(fileName, Path.GetFileName(fileName), resume);
		}

		/// <summary>
		/// Upload a file and set the resume flag.
		/// </summary>
		/// <param name="fileName">Full local path and filename to upload</param>
		/// <param name="resume">if true, try to continue a previous upload</param>
		public void upload (string fileName, string remoteFileName, Boolean resume) {
			should_continue = false;
			string dest = remoteFileName.Replace("/", "");
			if (!logined) {
				login();
			}

			Socket cSocket = (passive_mode) ? (createDataSocket()) : (createDataPort());
			cSocket.SendTimeout = cSocket.ReceiveTimeout = 30000;
			long offset = 0;

			if (resume) {

				try {

					setBinaryMode(true);
					offset = getFileSize(dest);
					Console.WriteLine("Resuming upload from " + offset);
				} catch (Exception) {
					offset = 0;
					Console.WriteLine("Failed to get offset, resending whole file ");
				}
			}

			if (offset > 0) {
				should_continue = true;
				sendCommand("REST " + offset);
				if (retValue != 350) {
					//throw new IOException(reply.Substring(4), retValue);
					//Remote server may not support resuming.
					offset = 0;
				}
			}

			// Note: must strip stray slashes out of the name, otherwise
			// FTP server will regard this as an absolute path.
			sendCommand("STOR " + dest);


			if (!(retValue == 125 || retValue == 150)) {
				should_continue = false;
				throw new IOException(reply.Substring(4), retValue);
			}

			if (!passive_mode) {
				Socket old = cSocket;
				cSocket = old.Accept(); // This is blocking. Will wait for a LONG time!
				old.Close();
			}
			cSocket.SendTimeout = cSocket.ReceiveTimeout = 30000;

			// open input stream to read source file
			if (!File.Exists(fileName)) throw new IOException("Specified local file not accessible");

			Console.WriteLine("Uploading file " + fileName + " to " + remotePath + " as " + remoteFileName);
			cSocket.SendFile(fileName);
			cSocket.Disconnect(false);

			Console.WriteLine("");

			if (cSocket.Connected) {
				cSocket.Close();
			}

			should_continue = false; // all data sent
			readReply();

			if (!(retValue == 226 || retValue == 250)) {
				throw new IOException(reply.Substring(4), retValue);
			}
		}

		/// <summary>
		/// Delete a file from the remote FTP server.
		/// </summary>
		/// <param name="fileName">File in the current remote directory</param>
		public void deleteRemoteFile (string fileName) {

			if (!logined) {
				login();
			}

			sendCommand("DELE " + fileName);

			if (retValue != 250) {
				throw new IOException(reply.Substring(4), retValue);
			}

		}

		/// <summary>
		/// Rename a file on the remote FTP server.
		/// </summary>
		/// <param name="oldFileName">File name in the current remote directory</param>
		/// <param name="newFileName">New file name</param>
		public void renameRemoteFile (string oldFileName, string newFileName) {

			if (!logined) {
				login();
			}

			sendCommand("RNFR " + oldFileName);

			if (retValue != 350) {
				throw new IOException(reply.Substring(4), retValue);
			}

			//  known problem
			//  rnto will not take care of existing file.
			//  i.e. It will overwrite if newFileName exist
			sendCommand("RNTO " + newFileName);
			if (retValue != 250) {
				throw new IOException(reply.Substring(4), retValue);
			}

		}

		/// <summary>
		/// Create a directory on the remote FTP server as
		/// a child of the current working directory
		/// </summary>
		/// <param name="dirName">New directory name</param>
		public void mkdir (string dirName) {

			if (!logined) {
				login();
			}

			sendCommand("MKD " + dirName);

			if (!(retValue == 250 || retValue == 257)) {
				throw new IOException(reply.Substring(4), retValue);
			}

		}

		/// <summary>
		/// List the current working directory
		/// </summary>
		/// <returns>Current working directory, result of PWD command</returns>
		public string pwd () {
			if (!logined) {
				login();
			}

			sendCommand("PWD");

			if (!(retValue == 250 || retValue == 257)) {
				throw new IOException(reply.Substring(4), retValue);
			}

			if (reply.Contains("\"")) {
				int left = reply.IndexOf("\"") + 1;
				int right = reply.LastIndexOf("\"") - left;
				remotePath = reply.Substring(left, right);
				return remotePath;
			} else {
				if (reply.Length > 4) return reply.Substring(4);
				else return reply;
			}
		}

		/// <summary>
		/// Delete a directory on the remote FTP server.
		/// </summary>
		/// <param name="dirName">Old directory name</param>
		public void rmdir (string dirName) {

			if (!logined) {
				login();
			}

			sendCommand("RMD " + dirName);

			if (retValue != 250) {
				throw new IOException(reply.Substring(4), retValue);
			}

		}

		/// <summary>
		/// Change the current working directory on the remote FTP server.
		/// </summary>
		/// <param name="dirName">New directory</param>
		public void chdir (string dirName) {

			if (dirName.Equals(".")) {
				return;
			}

			if (!logined) {
				login();
			}

			sendCommand("CWD " + dirName);

			if (retValue != 250) {
				throw new IOException(reply.Substring(4), retValue);
			}

			this.remotePath = dirName;

			Console.WriteLine("Current directory is " + remotePath);

		}

		/// <summary>
		/// Close the FTP connection.
		/// </summary>
		public void close () {

			if (clientSocket != null) {
				sendCommand("QUIT");
			}

			cleanup();
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
		public void ensureRemotePath (string path, bool relative) {
			string fwd = getRemotePath();
			string[] parts = path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
			if (parts == null || parts.Length < 1) throw new ArgumentException("EnsureRemotePath: Specified path was empty");

			if (!relative) chdir("/");

			int idx = 0;

			// See how far we get before bits are missing.
			for (; idx < parts.Length; idx++) {
				try {
					chdir(parts[idx]);
				} catch (IOException) {
					break;
				}
			}

			// If we get here, then the path part at 'parts[idx]' is missing.
			for (; idx < parts.Length; idx++) {
				try {
					mkdir(parts[idx]);
					chdir(parts[idx]);
				} catch (IOException ioex) {
					throw new IOException("EnsureRemotePath: Couldn't create directories. Check permissions and name conflicts", ioex);
				}
			}

			// finish in the same working directory as before:
			chdir(fwd);
		}

		/// <summary>
		/// Cancel data transfer in progress
		/// </summary>
		public void abort () {
			sendCommand("ABOR");
			if (retValue != 226 && retValue != 426) throw new IOException("Abort failed");
			if (retValue != 426) readReply(); // to get 426 message off queue
			if (retValue != 426) throw new IOException("Abort failed");
		}

		/// <summary>
		/// Set debug mode.
		/// This causes diagnostic information to be printed to the console.
		/// </summary>
		public void setDebug (Boolean debug) {
			this.debug = debug;
		}

		#region Arcane Inner Workings



		private void readReply () {
			mes = "";
			reply = readLine();
			retValue = Int32.Parse(reply.Substring(0, 3));
		}

		private void cleanup () {
			if (clientSocket != null) {
				clientSocket.Close();
				clientSocket = null;
			}
			logined = false;
		}

		private string readLine () {

			while (true) {
				bytes = clientSocket.Receive(buffer, buffer.Length, 0);
				mes += ASCII.GetString(buffer, 0, bytes);
				if (bytes < buffer.Length) {
					break;
				}
			}

			char[] seperator = { '\n' };
			string[] mess = mes.Split(seperator);

			if (mes.Length > 2) {
				// find first non-ignore message:
				foreach (string line in mess) {
					if (line.Length > 3)
						if (line.Substring(3, 1) == " ") {
							mes = line;
							break;
						}
				}
				//mes = mess[mess.Length - 2];
			} else {
				mes = mess[0];
			}

			if (mes.Length < 4) {
				if (debug)
					foreach (string l in mess) {
						Console.WriteLine("!   " + l);
					}

				throw new IOException("Server returned an invalid message");
			}

			if (!mes.Substring(3, 1).Equals(" ")) {
				return readLine();
			}

			if (debug) {
				for (int k = 0; k < mess.Length - 1; k++) {
					Console.WriteLine(mess[k]);
				}
			}
			return mes;
		}

		private void sendCommand (String command) {

			Byte[] cmdBytes = Encoding.ASCII.GetBytes((command + "\r\n").ToCharArray());
			clientSocket.Send(cmdBytes, cmdBytes.Length, 0);
			readReply();
		}

		private Socket createDataPort () {
			if (debug) Console.WriteLine("   preparing port listener  ");
			int port = 1663;

			// create the socket
			Socket listenSocket = new Socket(AddressFamily.InterNetwork,
											 SocketType.Stream,
											 ProtocolType.Tcp);

			// bind the listening socket to the port
			//IPAddress.Broadcast
			IPAddress hostIP = Dns.Resolve(Dns.GetHostName()).AddressList[0];
			string ftp_ip = hostIP.ToString().Replace(".", ",");
			IPEndPoint ep = null;
			while (true) {
				try {
					ep = new IPEndPoint(hostIP, port);
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


			if (debug) Console.WriteLine(port_cmd + " (Opening a data socket for transfer)");

			sendCommand(port_cmd);
			if (retValue != 200 && retValue != 227) {
				throw new IOException(reply.Substring(4), retValue);
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

		private Socket createDataSocket () {
			if (debug) Console.WriteLine("PASV (Opening a data socket for transfer)");
			sendCommand("PASV");

			if (retValue != 227) {
				throw new IOException(reply.Substring(4), retValue);
			}

			int index1 = reply.IndexOf('(');
			int index2 = reply.IndexOf(')');
			if (index1 < 0 || index2 < 0) throw new IOException("Malformed PASV reply: " + reply);

			string ipData = reply.Substring(index1 + 1, index2 - index1 - 1);
			int[] parts = new int[7];

			int len = ipData.Length;
			int partCount = 0;
			string buf = "";

			for (int i = 0; i < len && partCount <= 6; i++) {

				char ch = Char.Parse(ipData.Substring(i, 1));
				if (Char.IsDigit(ch))
					buf += ch;
				else if (ch != ',') {
					throw new IOException("Malformed PASV reply: " + reply);
				}

				if (ch == ',' || i + 1 == len) {

					try {
						parts[partCount++] = Int32.Parse(buf);
						buf = "";
					} catch (Exception) {
						throw new IOException("Malformed PASV reply: " + reply);
					}
				}
			}

			string ipAddress = parts[0] + "." + parts[1] + "." +
			  parts[2] + "." + parts[3];

			int port = (parts[4] << 8) + parts[5];

			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.SendTimeout = s.ReceiveTimeout = 30000;
			IPHostEntry hostInfo = GetHostInfo(ipAddress);

			try {
				s.Connect(hostInfo.AddressList, port);
			} catch (Exception) {
				throw new IOException("Can't connect to remote server");
			}

			return s;
		}

		private IPHostEntry GetHostInfo (string addr) {
			IPHostEntry hostInfo = null;
			try {
				hostInfo = Dns.GetHostEntry(addr);
				if (hostInfo.AddressList == null || hostInfo.AddressList.Length < 1)
					throw new Exception();
			} catch {
				try {
					hostInfo = Dns.Resolve(addr);
				} catch {
					throw new Exception("Could not resolve host name to any IP addresses: (" + addr + ")");
				}
			}

			return hostInfo;
		}
		#endregion
	}
}
