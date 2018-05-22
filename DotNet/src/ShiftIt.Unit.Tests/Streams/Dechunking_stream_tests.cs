using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using ShiftIt.Internal.Socket;
using ShiftIt.Internal.Streaming;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Streams
{
	[TestFixture]
	public class Dechunking_stream_tests
	{
		PushbackInputStream _rawSample;
		HttpChunkedStreamWrapper _subject;
		const string expected = "<response><build buildId=\"72759\" buildTypeId=\"bt76\" text=\"Execute test\" successful=\"true\" buildTypeSuccessful=\"true\" remainingTime=\"2m:43s\" exceededEstimatedDurationTime=\"&lt; 1s\" elapsedTime=\"1m:28s\" completedPercent=\"35\" hasArtifacts=\"false\" /></response>";
		
		[SetUp]
		public void setup()
		{
			_rawSample = new PushbackInputStream(HttpSample.ChunkedResponse());
			var buf = new byte[74];
			_rawSample.Read(buf, 0, 74); // skip HTTP headers
			_subject = new HttpChunkedStreamWrapper(_rawSample, TimeSpan.FromSeconds(1));
		}

		[Test]
		public void should_be_able_to_read_entire_stream ()
		{
			Assert.That(_subject.IsComplete(), Is.False);

			var ms = new MemoryStream();
			StreamTools.CopyBytesToTimeout(_subject, ms, null);
			var result = Encoding.UTF8.GetString(ms.ToArray());

			Assert.That(result, Is.EqualTo(expected));
			Assert.That(_subject.IsComplete(), Is.True);
		}
	}
}