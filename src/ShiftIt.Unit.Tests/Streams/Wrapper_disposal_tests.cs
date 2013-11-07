using System;
using System.IO;
using System.IO.Compression;
using NSubstitute;
using NUnit.Framework;
using ShiftIt.Internal.Socket;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Unit.Tests.Streams
{
	[TestFixture]
	public class Wrapper_disposal_tests
	{
		[Test]
		public void pushback_input_disposal ()
		{
			var under = Substitute.For<Stream>();
			var subject = new PushbackInputStream(under);
			subject.Dispose();
			under.Received().Close();
		}
		
		[Test]
		public void chunked_wrapper_disposal ()
		{
			var under = Substitute.For<Stream>();
			var subject = new HttpChunkedStreamWrapper(under, TimeSpan.Zero);
			subject.Dispose();
			under.Received().Close();
		}
		
		[Test]
		public void gzip_wrapper_disposal  ()
		{
			var under = Substitute.For<PushbackInputStream>();
			var subject = new GZipStreamWrapper(under);
			subject.Dispose();
			under.Received().Close();
		}

		[Test]
		public void hashing_stream_disposal ()
		{
			var under = Substitute.For<Stream>();
			var subject = new HashingReadStream(under, System.Security.Cryptography.MD5.Create());
			subject.Dispose();
			under.Received().Close();
		}

		[Test]
		public void http_stream_disposing ()
		{
			var under = Substitute.For<Stream>();
			var subject = new HttpResponseStream(under, 0);
			subject.Dispose();
			under.Received().Close();
		}
	}
}