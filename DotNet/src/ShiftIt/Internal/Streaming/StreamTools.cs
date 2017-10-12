using System;
using System.IO;
using ShiftIt.Internal.Streaming;
using TimeoutException = ShiftIt.Http.TimeoutException;

namespace ShiftIt.Internal.Socket
{
	/// <summary>
	/// Helpers for byte streams
	/// </summary>
	public static class StreamTools
	{
		/// <summary>
		/// Default buffer size for transfers. 64KiB
		/// </summary>
		public const int DefaultBufferSize = 64 * 1024;
		
		/// <summary>
		/// Copy a specific number of bytes from a source to a destination stream, with a timeout.
		/// The timeout is measured from last data received.
		/// </summary>
		/// <param name="source">Stream to read from</param>
		/// <param name="dest">Stream to write to</param>
		/// <param name="length">Maximum length to read</param>
		/// <param name="timeout">Maximum time to wait for data</param>
		public static long CopyBytesToLength(Stream source, Stream dest, long length, TimeSpan timeout)
		{
			var bufferSize = (int)Math.Min(length, DefaultBufferSize);
			if (bufferSize < 256) bufferSize = DefaultBufferSize;
			long read = 0;
			var buf = new byte[bufferSize];
			long remaining;
			var realLength = AdjustedLength(source, length, read, bufferSize);

			Func<int> now = () => Environment.TickCount;
			int[] lastData = {now()};
			Func<int> waiting = () => now() - lastData[0];

			while ((remaining = realLength - read) > 0)
			{
				var len = remaining > bufferSize ? bufferSize : (int)remaining;
				var got = source.Read(buf, 0, len);

				if (got > 0) lastData[0] = now();
				else
				{
					realLength = AdjustedLength(source, length, read, bufferSize);
					if (waiting() > timeout.TotalMilliseconds) throw new TimeoutException();
				}

				read += got;
				dest.Write(buf, 0, got);
			}
			return read;
		}

		static long AdjustedLength(Stream source, long length, long read, int bufferSize)
		{
			if (!(source is ISelfTerminatingStream)) return length;

			var done = ((ISelfTerminatingStream)source).IsComplete();
			if (done) return read;
			return long.MaxValue; // keep going
		}


		/// <summary>
		/// Copy bytes from a source to a destination stream, with a timeout.
		/// </summary>
		/// <param name="source">Stream to read from</param>
		/// <param name="dest">Stream to write to</param>
		public static void CopyBytesToTimeout(Stream source, Stream dest)
		{
			try { CopyStreams(source, dest, DefaultBufferSize); }
			catch (TimeoutException) { }
		}

        private static void CopyStreams(Stream src, Stream dst, int bufSz) {
            var buf = new byte[bufSz];
            int read;

            while ((read = src.Read(buf, 0, bufSz)) > 0)
            {
                dst.Write(buf, 0, read);
            }
        }
	}
}