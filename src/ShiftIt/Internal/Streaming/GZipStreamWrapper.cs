using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// Wrap a gzip stream to expose the end-of-stream logic.
	/// This is horrible.
	/// </summary>
	public class GZipStreamWrapper : Stream, ISelfTerminatingStream
	{
		readonly GZipStream _source;
		readonly Func<bool> _isDone;

		/// <summary>
		/// Wrap gzip stream
		/// </summary>
		public GZipStreamWrapper(GZipStream source)
		{
			_source = source;
			_isDone = GzipFinished(source) ?? (() => true);
		}

		static Func<bool> GzipFinished(GZipStream ins)
		{
			try
			{
				var g = ins.GetType().GetField("deflateStream", BindingFlags.Instance | BindingFlags.NonPublic);
				if (g == null) return null;
				var s = (DeflateStream)g.GetValue(ins);
				if (s == null) return null;
				var f = s.GetType().GetField("inflater", BindingFlags.Instance | BindingFlags.NonPublic);
				if (f == null) return null;
				var fo = f.GetValue(s);
				var avf = fo.GetType().GetProperty("AvailableOutput");
				var bterm = fo.GetType().GetField("bfinal", BindingFlags.Instance | BindingFlags.NonPublic);
				if (avf == null || bterm == null) return null;

				return () => 
					(int) avf.GetValue(fo, new object[0]) == 0 // no more data in decode buffer
					&& (int)bterm.GetValue(fo) == 1; // have reached end of stream
				
			}
			catch (Exception)
			{
				return null;
			}		}

		/// <summary>
		/// Returns true if the stream has terminated,
		/// false otherwise.
		/// </summary>
		public bool IsComplete()
		{
			return _isDone();
		}

		#region Pass-through
		public override void Flush()
		{
			_source.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _source.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_source.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _source.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_source.Write(buffer, offset, count);
		}

		public override bool CanRead
		{
			get { return _source.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _source.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return _source.CanWrite; }
		}

		public override long Length
		{
			get { return _source.Length; }
		}

		public override long Position
		{
			get { return _source.Position; }
			set { _source.Position = value; }
		}
		#endregion


	}
}