using System;
using System.Net.NetworkInformation;
using System.Threading;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Integration.Tests
{
	[TestFixture]//, Explicit, Description("These connect to the internet")]
    public class HttpTests
    {
		IHttpClient _subject;

		[SetUp]
		public void setup()
		{
			_subject = new HttpClient();
		}

		[Test, Description("This server returns a compressed response, so checks our length logic")]
		public void read_from_iana ()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://www.iana.org/domains/example")).Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadStringToLength(); // this server returns an invalid length!

				Console.WriteLine("Expected " + result.BodyReader.ExpectedLength + ", got " + body.Length);
				Console.WriteLine(body);
				Assert.That(body, Contains.Substring("<html"));
				Assert.That(body, Contains.Substring("</html>"), "didn't read to end");
				Assert.That(result.BodyReader.ExpectedLength, Is.EqualTo(body.Length), "length was not correct");
			}
		}

		[Test]
		public void read_from_wikipedia()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://en.wikipedia.org/wiki/Ternary_search_tree")).Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadStringToLength();

				Assert.That(result.StatusMessage, Is.EqualTo("OK"));
				Assert.That(body, Contains.Substring("<html"));
			}
		}

		[Test]
		public void head_from_wikipedia()
		{
			var rq = new HttpRequestBuilder().Head(new Uri("http://en.wikipedia.org/wiki/Ternary_search_tree")).Build();
			using (var result = _subject.Request(rq))
			{
				Assert.That(result.StatusCode == 200);
			}
		}

		[Test]
		public void reading_the_body_of_a_head_request_results_in_a_timeout()
		{
			var rq = new HttpRequestBuilder().Head(new Uri("http://en.wikipedia.org/wiki/Ternary_search_tree")).Build();
			using (var result = _subject.Request(rq))
			{
				Assert.Throws<TimeoutException>(() => result.BodyReader.ReadStringToLength());
			}
		}

		[Test]
		public void connection_to_rabbit_mq_api()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:55672/api/overview")).BasicAuthentication("guest", "guest").Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadStringToLength();

				Console.WriteLine(body);
				Assert.That(body, Contains.Substring("{\"management_version"));
			}
		}

		[Test, Explicit]
		public void leak_test_over_result_object()
		{
			var start = Connections();
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:55672/api/vhosts")).BasicAuthentication("guest", "guest").Build();
			for (int i = 0; i < 512; i++)
			{
				using (var result = _subject.Request(rq))
				{
					Assert.That(start, Is.Not.EqualTo(Connections()));
					Console.WriteLine(result.BodyReader.ReadStringToLength());
				}
			}
			Thread.Sleep(500);
			var end = Connections();

			Console.WriteLine("Before: " + start + ", after: " + end);
			Assert.That(end, Is.LessThanOrEqualTo(start));
		}
		
		[Test, Explicit]
		public void leak_test_over_stream_object()
		{
			var start = Connections();
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:55672/api/vhosts")).BasicAuthentication("guest", "guest").Build();
			for (int i = 0; i < 512; i++)
			{
				var result = _subject.Request(rq);
				using (var stream = result.BodyReader)
				{
					Assert.That(start, Is.Not.EqualTo(Connections()));
					Console.WriteLine(stream.ReadStringToLength());
				}
			}
			Thread.Sleep(500);
			var end = Connections();

			Console.WriteLine("Before: " + start + ", after: " + end);
			Assert.That(end, Is.LessThanOrEqualTo(start));
		}

		[Test, Explicit, Description("Don't dispose, to prove that the test is valid")]
		public void leak_test_anti_test()
		{
			var start = Connections();
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:55672/api/vhosts")).BasicAuthentication("guest", "guest").Build();
			for (int i = 0; i < 110; i++)
			{
				var result = _subject.Request(rq);
				/*using (*/var stream = result.BodyReader;/*)*/
				{
					Assert.That(start, Is.Not.EqualTo(Connections()));
					Console.WriteLine(stream.ReadStringToLength());
				}
			}
			Thread.Sleep(500);
			var end = Connections();

			Console.WriteLine("Before: " + start + ", after: " + end);
			Assert.That(end, Is.LessThanOrEqualTo(start));
		}

		static long Connections()
		{
			return IPGlobalProperties.GetIPGlobalProperties()
			                         .GetTcpIPv4Statistics()
			                         .CurrentConnections;
		}
    }
}
