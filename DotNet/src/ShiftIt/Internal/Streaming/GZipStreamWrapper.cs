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
		readonly PushbackInputStream _source;
		Stream _decompressedStream;
		Func<bool> _isDone;

		/// <summary>
		/// Wrap gzip stream
		/// </summary>
		public GZipStreamWrapper(PushbackInputStream source)
		{
			_source = source;
			_isDone = () => false;
			_decompressedStream = null;
		}


		/// <summary>
		/// Read decompressed data from the source
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_decompressedStream == null) _decompressedStream = PrepareGzip(_source);

			return _decompressedStream.Read(buffer, offset, count);
		}

		Stream PrepareGzip(PushbackInputStream src)
		{
			if (LooksLikeGzip(src))
			{
				var gz = new GZipStream(src, CompressionMode.Decompress, true);
				_isDone = GzipFinished(gz) ?? (() => true);
				return gz;
			}
			_isDone = () => true;
			return src;
		}

		static bool LooksLikeGzip(PushbackInputStream s)
		{
			var magic = new byte[10];
			var len = s.Read(magic, 0, 10);
			s.UnRead(magic, 0, 10); // always reset the stream.

			return (len == 10 && magic[0] == 0x1F && magic[1] == 0x8B);
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
		/**<summary>Not used</summary>*/public override void Flush()
		{
			throw new NotSupportedException();
		}

		/**<summary>Not used</summary>*/public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/**<summary>Not used</summary>*/public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/**<summary>Not used</summary>*/public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		/**<summary>Not used</summary>*/public override bool CanRead
		{
			get { return _source.CanRead; }
		}

		/**<summary>Not used</summary>*/public override bool CanSeek
		{
			get { return false; }
		}

		/**<summary>Not used</summary>*/public override bool CanWrite
		{
			get { return false; }
		}

		/**<summary>Not used</summary>*/public override long Length
		{
			get { return _source.Length; }
		}

		/**<summary>Not used</summary>*/public override long Position
		{
			get { return _source.Position; }
			set { _source.Position = value; }
		}
		#endregion

		/// <summary>
		/// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
		/// </summary>
		/// <filterpriority>1</filterpriority>
		public override void Close()
		{
			if (_decompressedStream != null) _decompressedStream.Close();
			_source.Close();
		}

	}
}