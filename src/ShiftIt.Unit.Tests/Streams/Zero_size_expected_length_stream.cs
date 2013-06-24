using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using ShiftIt.Internal.Socket;

namespace ShiftIt.Unit.Tests.Streams
{
	[TestFixture]
	public class Zero_size_expected_length_stream
	{
		[Test]
		public void a_zero_length_uncompressed_stream_should_result_in_an_empty_string()
		{
			var src = new MemoryStream();
			var subject = new HttpSingleResponseStream(src, 0);

			var result = subject.ReadStringToLength();

			Assert.That(result, Is.EqualTo(""));
		}

		[Test]
		public void a_zero_length_compressed_stream_should_result_in_an_empty_string()
		{
			var src = new MemoryStream();
			var cmp = new DeflateStream(src, CompressionMode.Decompress);
			var subject = new HttpSingleResponseStream(cmp, 0);

			var result = subject.ReadStringToLength();

			Assert.That(result, Is.EqualTo(""));
		}

	}
}