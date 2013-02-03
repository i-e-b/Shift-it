using System.IO;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Responses
{
	[TestFixture]
	public class CompressedResponse
	{
		IHttpResponseParser _subject;
		Stream _rawSample;
		IHttpResponse _result;

		[SetUp]
		public void setup()
		{
			_rawSample = HttpSample.GzippedResponse();
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
		public void http_headers_are_correct_with_duplicates_concatenated()
		{
			Assert.That(_result.Headers["Date"], Is.EqualTo("Mon, 21 Jan 2013 23:58:10 GMT"));
			Assert.That(_result.Headers["X-Cache"], Is.EqualTo("HIT from sq61.wikimedia.org,HIT from amssq44.esams.wikimedia.org,MISS from amssq32.esams.wikimedia.org"));
		}

		[Test]
		public void can_read_body_correctly()
		{
			Assert.That(_result.BodyReader.ReadToEnd(), Is.StringStarting("<html>"));
		}
	}
}
