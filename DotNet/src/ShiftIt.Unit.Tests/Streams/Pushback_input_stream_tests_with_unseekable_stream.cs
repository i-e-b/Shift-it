using System;
using System.IO;
using System.Text;
using NSubstitute;
using NUnit.Framework;
using ShiftIt.Internal.Streaming;

namespace ShiftIt.Unit.Tests.Streams
{
	[TestFixture]
	public class Pushback_input_stream_tests_with_unseekable_stream
	{
		Stream _underlying;
		PushbackInputStream _subject;
		readonly byte[] _buffer = new byte[100];
		const string _message = "Hello, world!";
		readonly byte[] _sample_data = Encoding.ASCII.GetBytes(_message);

		[SetUp]
		public void setup()
		{
			_underlying = new NonSeekable(_sample_data);
			_subject = new PushbackInputStream(_underlying);
		}

		[Test]
		public void can_read_underlying_stream ()
		{
			var length = _subject.Read(_buffer, 0, 100);
			var result = Encoding.ASCII.GetString(_buffer, 0, length);

			Assert.That(result, Is.EqualTo(_message));
			Assert.That(length, Is.EqualTo(_sample_data.Length));
		}

		[Test]
		public void can_unread_single_byte_reads ()
		{
			var a = _subject.ReadByte();
			_subject.UnRead(new[]{(byte)a}, 0, 1);
			var b = _subject.ReadByte();

			Assert.That(a, Is.EqualTo(b));
		}
		
		[Test]
		public void can_unread_single_bytes ()
		{
			var a = _subject.ReadByte();
			_subject.UnReadByte((byte)a);
			var b = _subject.ReadByte();

			Assert.That(a, Is.EqualTo(b));
		}

		[Test]
		public void unread_data_can_be_read_again ()
		{
			_subject.Read(_buffer, 0, 4);
			Assert.That(_subject.Position, Is.EqualTo(4));

			_subject.UnRead(_buffer, 0, 4);
			Assert.That(_subject.Position, Is.EqualTo(0));


			var length = _subject.Read(_buffer, 4, 100); // note, we're writing further forward

			var result = Encoding.ASCII.GetString(_buffer, 0, length + 4);
			Assert.That(result, Is.EqualTo("HellHello, world!"));
		}

		[Test]
		public void can_read_and_unread_entire_stream ()
		{
			_subject.Read(_buffer, 0, _sample_data.Length);
			_subject.UnRead(_buffer, 0,  _sample_data.Length);

			var length = _subject.Read(_buffer, 0, 100); // note, we're writing further forward
			
			Assert.That(_subject.Position, Is.EqualTo(length));

			var result = Encoding.ASCII.GetString(_buffer, 0, length);
			Assert.That(result, Is.EqualTo(_message));
		}

		/// <summary>
		/// A very minimal stream that only supports reading
		/// </summary>
		class NonSeekable : Stream
		{
			readonly byte[] _sampleData;
			readonly int _length;
			int _offset;

			public NonSeekable(byte[] sampleData)
			{
				_sampleData = sampleData;
				_offset = 0;
				_length = sampleData.Length;
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				var l = Math.Min(_length - _offset, _offset + count);
				Array.Copy(_sampleData, _offset, buffer, offset, l);

				_offset += l;
				return l;
			}

			public override void Flush() {throw new NotSupportedException(); }
			public override long Seek(long offset, SeekOrigin origin){throw new NotSupportedException(); }
			public override void SetLength(long value) {throw new NotSupportedException(); }
			public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
			public override bool CanRead { get { return true; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return false; } }
			public override long Length { get { return 0; } }
			public override long Position { get { return _offset; } set { throw new NotSupportedException(); } }
		}
	}
}
