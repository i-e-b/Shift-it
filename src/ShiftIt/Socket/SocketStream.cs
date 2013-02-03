using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ShiftIt.Socket
{
	public class SocketStream : Stream
	{

		public SocketStream() { }

		public SocketStream(System.Net.Sockets.Socket socket)
		{
			_socket = socket;
		}

		System.Net.Sockets.Socket _socket;
		public System.Net.Sockets.Socket Socket { get { return _socket; } }

		~SocketStream()
		{
			Dispose();
		}

		protected override void Dispose(bool disposing)
		{
			var sock = Interlocked.Exchange(ref _socket, null);
			if (sock == null) return;
			if (sock.Connected)
			{
				sock.Disconnect(false);
				sock.Close();
			}
			sock.Dispose();
			base.Dispose(disposing);
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count)
		{
			SocketError err;
			int len = _socket.Receive(buffer, offset, count, SocketFlags.None, out err);
			_socket.ReceiveTimeout = 500;
			if (err != SocketError.Success)
			{
				Console.WriteLine("Socket error during transfer: "+err);
			}
			return len;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			SocketError err;
			_socket.Send(buffer, offset, count, SocketFlags.None, out err);
			if (err != SocketError.Success)
			{
				throw new IOException("Socket error during transfer", (int)err);
			}
		}
		
		public override long Seek(long offset, SeekOrigin origin) { return 0; }
		public override void SetLength(long value) {  }

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return true; } } 
		public override long Length { get { return 0; } }
		public override long Position { get; set; }
	}
}