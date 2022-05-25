using System;
using System.IO;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Internal.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.Responses
{
	[TestFixture]
	public class WithDuplicatedHeaders
	{
		IHttpResponseParser _subject;
		Stream _rawSample;
		IHttpResponse _result;

		[SetUp]
		public void setup()
		{
			_rawSample = HttpSample.WithDuplicatedHeaders();
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
		public void can_get_status_code_and_class()
		{
			Assert.That(_result.StatusCode, Is.EqualTo(200));
			Assert.That(_result.StatusClass, Is.EqualTo(StatusClass.Success));
		}

		[Test]
		public void http_headers_are_correct()
		{
			Assert.That(_result.Headers["Date"], Is.EqualTo("Sun, 03 Feb 2013 13:19:56 GMT"));
			Assert.That(_result.Headers["Server"], Is.EqualTo("Microsoft-IIS/6.0"));
			Assert.That(_result.Headers["X-Powered-By"], Is.EqualTo("ASP.NET"));
			Assert.That(_result.Headers["Pragma"], Is.EqualTo("no-cache"));
			Assert.That(_result.Headers["Content-Length"], Is.EqualTo("96"));
			Assert.That(_result.Headers["Content-Type"], Is.EqualTo("text/html"));
			Assert.That(_result.Headers["Expires"], Is.EqualTo("Sun, 03 Feb 2013 13:18:56 GMT"));
			Assert.That(_result.Headers["Cache-control"], Is.EqualTo("no-store"));
		}

		[Test]
		public void duplicated_http_headers_are_correct_use_most_recent_value()
		{
			// check we get the right deduplicated value
			Assert.That(_result.Headers["Content-Length"], Is.EqualTo("96"));
			
			// check the "exact headers" list keeps both
			var exactHeaders = string.Join("\n", _result.ExactHeaders);
			var expected = "Date: Sun, 03 Feb 2013 13:19:56 GMT\n" +
			               "Server: Microsoft-IIS/6.0\n" +
			               "X-Powered-By: ASP.NET\n" +
			               "Pragma: no-cache\n" +
			               "Content-Length: 90\n" +
			               "Content-Type: text/html\n" +
			               "Expires: Sun, 03 Feb 2013 13:18:56 GMT\n" +
			               "Cache-control: no-store\n" +
			               "Content-Length: 96";
			
			Assert.That(exactHeaders, Is.EqualTo(expected));
		}
	}
}