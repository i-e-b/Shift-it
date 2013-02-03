using System;
using NUnit.Framework;
using ShiftIt.Socket;

namespace ShiftIt.Integration.Tests
{
	[TestFixture, Explicit, Description("These connect to the internet")]
    public class SimpleHttpTests
    {
		ISynchronousClient _subject;

		[SetUp]
		public void setup()
		{
			_subject = new SynchronousClient();
		}

		[Test]
		public void can_download_from_http_server()
		{
			var result = _subject.GetString("http://www.iana.org/domains/example");
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("<html"));
		}

		[Test]
		public void hacker_news_has_a_protocol_violation ()
		{
			var result = _subject.RawImmediate("news.ycombinator.com",80,
@"GET http://news.ycombinator.com/ HTTP/1.1
Host: news.ycombinator.com
Connection: keep-alive
Cache-Control: max-age=0
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17
Accept-Encoding: gzip,deflate,sdch
Accept-Language: en-GB,en;q=0.8,en-US;q=0.6
Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3
");
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("<html"));
		}
    }
}
