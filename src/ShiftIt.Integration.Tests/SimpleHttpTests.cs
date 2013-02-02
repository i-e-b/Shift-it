using System;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Integration.Tests
{
	[TestFixture]
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
		public void can_query_rabbit_mq_server()
		{
			var result = _subject.RawImmediate("localhost", 15672, @"GET http://localhost:15672/api/queues/ HTTP/1.1
Host: localhost:15672
Connection: keep-alive
Cache-Control: max-age=0
Authorization: Basic Z3Vlc3Q6Z3Vlc3Q=
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17
Accept-Encoding: gzip,deflate,sdch
Accept-Language: en-GB,en;q=0.8,en-US;q=0.6
Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3
Cookie: auth=Z3Vlc3Q6Z3Vlc3Q%3D; m=34e2:|1da9:t|64b8:t|796a:t|47ba:t

");
			Console.WriteLine(result);
		}
    }
}
