using System.IO;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Unit.Tests.Responses
{
	[TestFixture]
	public class ResponseParsing
	{
		IHttpResponseParser _subject;
		TextReader _rawSample;
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
	}
}
