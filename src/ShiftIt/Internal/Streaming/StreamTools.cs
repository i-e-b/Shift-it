using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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
			long read = 0;
			var buf = new byte[bufferSize];
			long remaining;
			var realLength = length;

			Func<int> now = () => Environment.TickCount;
			int[] lastData = {now()};
			Func<int> waiting = () => now() - lastData[0];

			Func<int> gzipLength = GzipLengthAvailable(source);

			while ((remaining = realLength - read) > 0)
			{
				// Deal with crappy GZipStream behaviour:
				if (gzipLength != null)
				{
					var glen = gzipLength();
					if (glen > 0) realLength = read + glen; // adjust length
					else realLength = read + (bufferSize * 2); // keep going
				}

				var len = remaining > bufferSize ? bufferSize : (int)remaining;
				var got = source.Read(buf, 0, len);

				if (got > 0) lastData[0] = now();
				else if (waiting() > timeout.TotalMilliseconds) throw new TimeoutException();

				read += got;
				dest.Write(buf, 0, got);
			}
			return read;
		}

		/// <summary>
		/// Function so that: If the stream is a GZipStream, and the length is known,
		/// return that length. Otherwise return zero.
		/// Returns null if stream is not a GZipStream.
		/// </summary>
		public static Func<int> GzipLengthAvailable(Stream ins)
		{
			try
			{
				if (!(ins is GZipStream)) return null;
				var g = ins.GetType().GetField("deflateStream", BindingFlags.Instance | BindingFlags.NonPublic);
				if (g == null) return null;
				var s = (DeflateStream)g.GetValue(ins);
				if (s == null) return null;
				var f = s.GetType().GetField("inflater", BindingFlags.Instance | BindingFlags.NonPublic);
				if (f == null) return null;
				var fo = f.GetValue(s);
				var avf = fo.GetType().GetProperty("AvailableOutput");
				return () => (int)avf.GetValue(fo, new object[0]);
			}
			catch (Exception)
			{
				return null;
			}		}

		/// <summary>
		/// Copy bytes from a source to a destination stream, with a timeout.
		/// </summary>
		/// <param name="source">Stream to read from</param>
		/// <param name="dest">Stream to write to</param>
		public static void CopyBytesToTimeout(Stream source, Stream dest)
		{
			try { source.CopyTo(dest, DefaultBufferSize); }
			catch (TimeoutException) { }
		}
	}
}