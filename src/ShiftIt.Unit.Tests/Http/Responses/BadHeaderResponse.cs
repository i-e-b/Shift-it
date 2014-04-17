using System;
using System.IO;
using NUnit.Framework;
using ShiftIt.Internal.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.Responses
{
	[TestFixture]
	public class BadHeaderResponse
	{
		IHttpResponseParser _subject;
		Stream _rawSample;

		[SetUp]
		public void setup()
		{
			_rawSample = HttpSample.BadHeader();
			_subject = new HttpResponseParser();
		}

		[Test]
		public void the_response_up_to_the_bad_header_is_displayed_in_the_exception_message()
		{
			var ex = Assert.Throws<ArgumentException>(() => _subject.Parse(_rawSample, TimeSpan.FromSeconds(1)));
			Assert.That(ex.Message,
			            Is.EqualTo("Bad header -- BadHeader\r\nFull headers:HTTP/1.1 200 OK\r\n" +
			                       "Date: Sun, 03 Feb 2013 13:19:56 GMT\r\n" +
			                       "Server: Microsoft-IIS/6.0\r\n" +
			                       "X-Powered-By: ASP.NET\r\n" +
			                       "Pragma: no-cache\r\n" +
			                       "Content-Length: 96\r\n" +
			                       "BadHeader\r\n" +
			                       "Content-Type: text/html\r\n" +
			                       "Expires: Sun, 03 Feb 2013 13:18:56 GMT\r\n" +
			                       "Cache-control: no-store\r\n\r"));
		}
	}
}