using System.IO;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Http.Internal;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.Responses
{
	[TestFixture]
	public class UncompressedWithBody
	{
		IHttpResponseParser _subject;
		Stream _rawSample;
		IHttpResponse _result;

		[SetUp]
		public void setup()
		{
			_rawSample = HttpSample.SimpleResponse();
			_subject = new HttpResponseParser();
			_result = _subject.Parse(_rawSample);
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
		public void can_read_body_correctly()
		{
			Assert.That(_result.BodyReader.ReadToEnd(), Is.EqualTo("<html><head><title>A html page</title></head><body> <h1>Hi</h1><p>Hello there!</p></body></html>"));
		}
	}
}
