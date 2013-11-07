using System;
using System.IO;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Internal.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.Responses
{
	[TestFixture]
	public class UncompressedChunkedWithBody
	{
		IHttpResponseParser _subject;
		Stream _rawSample;
		IHttpResponse _result;

		[SetUp]
		public void setup()
		{
			_rawSample = HttpSample.ChunkedResponse();
			_subject = new HttpResponseParser();
			_result = _subject.Parse(_rawSample, TimeSpan.Zero);
		}

		[TearDown]
		public void cleanup()
		{
			_result.Dispose();
		}

		[Test]
		public void headers_are_marked_complete()
		{
			Assert.That(_result.HeadersComplete, Is.True);
		}

		[Test]
		public void status_message_is_available()
		{
			Assert.That(_result.StatusMessage, Is.EqualTo("OK"));
		}

		[Test]
		public void can_get_status_code_and_class()
		{
			Assert.That(_result.StatusCode, Is.EqualTo(200));
			Assert.That(_result.StatusClass, Is.EqualTo(StatusClass.Success));
		}

		[Test]
		public void can_read_body_correctly()
		{
			Assert.That(_result.BodyReader.ReadStringToLength(), Is.EqualTo("<response><build buildId=\"72759\" buildTypeId=\"bt76\" text=\"Execute test\" successful=\"true\" buildTypeSuccessful=\"true\" remainingTime=\"2m:43s\" exceededEstimatedDurationTime=\"&lt; 1s\" elapsedTime=\"1m:28s\" completedPercent=\"35\" hasArtifacts=\"false\" /></response>"));
		}
	}
}
