﻿using System.IO;
using System.Net.Sockets;
using System.Threading;
using ShiftIt.Http;

namespace ShiftIt.Internal.Socket
{
	/// <summary>
	/// Stream abstraction 
	/// </summary>
	public class SocketStream : Stream
	{

		/// <summary>
		/// Create a disconnected stream
		/// </summary>
		public SocketStream() { }

		/// <summary>
		/// Create a stream wrapper for a socket
		/// </summary>
		/// <param name="socket">socket to wrap</param>
		public SocketStream(System.Net.Sockets.Socket socket)
		{
			_socket = socket;
		}

		System.Net.Sockets.Socket _socket;

		/// <summary>
		/// Underlying socket used by this stream
		/// </summary>
		public System.Net.Sockets.Socket Socket { get { return _socket; } }

		/// <summary>
		/// Dispose of stream and socket.
		/// </summary>
		~SocketStream()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			var sock = Interlocked.Exchange(ref _socket, null);
			if (sock == null) return;
			if (sock.Connected)
			{
				sock.Disconnect(false);
			}
			sock.Dispose();
			base.Dispose(disposing);
		}

		/// <summary> Does nothing </summary>
		public override void Flush() { }

		/// <summary>
		/// Reads from the underlying socket into a provided buffer.
		/// </summary>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param><param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream. </param><param name="count">The maximum number of bytes to be read from the current stream. </param><exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. </exception><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support reading. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
		public override int Read(byte[] buffer, int offset, int count)
		{
			SocketError err;
			int len = _socket.Receive(buffer, offset, count, SocketFlags.None, out err);
			if (err != SocketError.Success && err != SocketError.WouldBlock)
			{
				if (err == SocketError.TimedOut)
					throw new TimeoutException();
				throw new SocketException((int) err);
			}
			Position += len;
			return len;
		}

		/// <summary>
		/// Writes a sequence of bytes to the underlying socket.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param><param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param><param name="count">The number of bytes to be written to the current stream. </param><filterpriority>1</filterpriority>
		public override void Write(byte[] buffer, int offset, int count)
		{
			SocketError err;
			_socket.Send(buffer, offset, count, SocketFlags.None, out err);
			if (err != SocketError.Success)
			{
				if (err == SocketError.TimedOut)
					throw new TimeoutException();
				throw new SocketException((int)err);
			}
			_writtenLength += count;
		}

		/// <summary>
		/// Sets read and write counts (Position, Length) to 0
		/// </summary>
		public void ResetCounts()
		{
			_writtenLength = 0;
			Position = 0;
		}

		long _writtenLength;
		/// <summary>
		/// Number of bytes written to socket
		/// </summary>
		public override long Length { get { return _writtenLength; } }

		/// <summary>
		/// Number of bytes read from socket
		/// </summary>
		public override long Position { get; set; }

		/**<summary> No action </summary>*/ public override long Seek(long offset, SeekOrigin origin) { return 0; }
		/**<summary> No action </summary>*/ public override void SetLength(long value) {  }

		/**<summary> No action </summary>*/ public override bool CanRead { get { return true; } }
		/**<summary> No action </summary>*/ public override bool CanSeek { get { return false; } }
		/**<summary> No action </summary>*/ public override bool CanWrite { get { return true; } } 
	}
}