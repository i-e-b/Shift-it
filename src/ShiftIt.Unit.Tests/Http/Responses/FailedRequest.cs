using System;
using System.IO;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Http.Internal;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.Responses
{
	[TestFixture]
	public class FailedRequest
	{
		IHttpResponseParser _subject;
		Stream _rawSample;
		IHttpResponse _result;

		[SetUp]
		public void setup()
		{
			_rawSample = HttpSample.FailedResponse();
			_subject = new HttpResponseParser();
			_result = _subject.Parse(_rawSample, TimeSpan.Zero);
		}

		[TearDown]
		public void Cleanup()
		{
			_result.Dispose();
		}

		[Test]
		public void headers_are_marked_complete()
		{
			Assert.That(_result.HeadersComplete, Is.True);
		}

		[Test]
		public void can_get_status_code_and_class()
		{
			Assert.That(_result.StatusCode, Is.EqualTo(500));
			Assert.That(_result.StatusClass, Is.EqualTo(StatusClass.ServerError));
		}

		[Test]
		public void status_message_is_available()
		{
			Assert.That(_result.StatusMessage, Is.EqualTo("Really not OK"));
		}

		[Test]
		public void http_headers_are_correct()
		{
			Assert.That(_result.Headers["Date"], Is.EqualTo("Sun, 03 Feb 2013 13:19:56 GMT"));
		}

		[Test]
		public void can_read_body_correctly()
		{
			Assert.That(_result.BodyReader.ReadStringToLength(), Is.EqualTo(""));
		}
	}
}
