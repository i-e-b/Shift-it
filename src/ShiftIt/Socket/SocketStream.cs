using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ShiftIt.Socket
{
	internal class SocketStream : Stream
	{
		public SocketStream()
		{
			_socket = null;
			ReadBuffer = new byte[BufferSize];
			ResultData = new StringBuilder();
		}

		public SocketStream(System.Net.Sockets.Socket socket)
			: this()
		{
			_socket = socket;
		}

		System.Net.Sockets.Socket _socket;
		public System.Net.Sockets.Socket Socket { get { return _socket; } }

		public const int BufferSize = 1024;

		public readonly byte[] ReadBuffer;

		public StringBuilder ResultData;

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
			if (err != SocketError.Success)
			{
				throw new IOException("Socket error during transfer", (int)err);
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