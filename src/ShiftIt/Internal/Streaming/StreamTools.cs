using System;
using System.IO;

namespace ShiftIt.Internal.Socket
{
	/// <summary>
	/// Helpers for byte streams
	/// </summary>
	public static class StreamTools
	{
		const int BufferSize = 4096;
		
		/// <summary>
		/// Copy a specific number of bytes from a source to a destination stream, with a timeout.
		/// The timeout is measured from last data received.
		/// </summary>
		/// <param name="source">Stream to read from</param>
		/// <param name="dest">Stream to write to</param>
		/// <param name="length">Maximum length to read</param>
		/// <param name="timeout">Maximum time to wait for data</param>
		public static void CopyBytesToLength(Stream source, Stream dest, long length, TimeSpan timeout)
		{
			long read = 0;
			var buf = new byte[BufferSize];
			long remaining;

			Func<int> now = () => Environment.TickCount;
			int[] lastData = {now()};
			Func<int> waiting = () => now() - lastData[0];

			while ((remaining = length - read) > 0)
			{
				var len = remaining > BufferSize ? BufferSize : (int)remaining;
				var got = source.Read(buf, 0, len);

				if (got > 0) lastData[0] = now();
				else if (waiting() > timeout.TotalMilliseconds) throw new TimeoutException("Timeout while reading from result stream");

				read += got;
				dest.Write(buf, 0, got);
			}
		}

		/// <summary>
		/// Copy bytes from a source to a destination stream, with a timeout.
		/// </summary>
		/// <param name="source">Stream to read from</param>
		/// <param name="dest">Stream to write to</param>
		public static void CopyBytesToTimeout(Stream source, Stream dest)
		{
			try { source.CopyTo(dest); }
			catch (TimeoutException) { }
		}
	}
}