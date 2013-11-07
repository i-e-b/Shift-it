using System;
using System.IO;
using NSubstitute;
using NUnit.Framework;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Unit.Tests.Streams
{
	[TestFixture]
	public class Pushback_input_stream_tests_with_seekable_stream
	{
		Stream _underlying;
		PushbackInputStream _subject;
		readonly byte[] _buffer = new byte[100];

		[SetUp]
		public void setup()
		{
			_underlying = Substitute.For<Stream>();
			_underlying.Read(Arg.Any<byte[]>(), 0, 100).Returns(99);
			_underlying.CanSeek.Returns(true);
			_underlying.Seek(-100, SeekOrigin.Current).Returns(1);
			_underlying.Seek(10, SeekOrigin.Begin).Returns(9);

			_subject = new PushbackInputStream(_underlying);
		}

		[Test]
		public void unreading_results_in_a_seek ()
		{
			_subject.UnRead(_buffer, 0, 100);
			_underlying.Received().Seek(-100, SeekOrigin.Current);
			Assert.That(_subject.Position, Is.EqualTo(1));
		}

		[Test]
		public void reading_is_passed_to_underlying_stream_and_correct_length_returned ()
		{
			_subject.Read(_buffer, 0, 100);
			_underlying.Received().Read(_buffer, 0, 100);
			Assert.That(_subject.Position, Is.EqualTo(99));
		}

		[Test]
		public void directly_setting_position_gives_a_not_supported_exception ()
		{
			Assert.Throws<NotSupportedException>(() => _subject.Position = 10);
		}

		[Test]
		public void seek_is_passed_directly_to_underlying_stream ()
		{
			_subject.Seek(10, SeekOrigin.Begin);
			_underlying.Received().Seek(10, SeekOrigin.Begin);
			Assert.That(_subject.Position, Is.EqualTo(9));
		}
	}
}