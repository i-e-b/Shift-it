using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Internal.Socket;

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
		public void read_from_iana()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://www.iana.org/domains/reserved")).Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadStringToLength();
				
				Console.WriteLine(Write(result.Headers));

				Console.WriteLine("Expected " + result.BodyReader.ExpectedLength + ", got " + body.Length);
				Console.WriteLine(body);
				Assert.That(body, Contains.Substring("<html"));
				Assert.That(body, Contains.Substring("</html>"), "didn't read to end");
				Assert.That(result.BodyReader.Complete, Is.True, "reader did not complete");
				
				// this server returns an invalid length!
				Assert.That(result.BodyReader.ExpectedLength, Is.GreaterThanOrEqualTo(body.Length), "length was not correct");
			}
		}

		[Test]
		public void can_read_chunked_responses_as_one_complete_response ()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://www.httpwatch.com/httpgallery/chunked/")).Build();
			using (var result = _subject.Request(rq))
			{
				var reader = result.BodyReader;

				Assert.That(reader, Is.InstanceOf<HttpChunkedResponseStream>());

				var body = reader.ReadStringToLength();

				Console.WriteLine(Write(result.Headers));
				Console.WriteLine(body);

				Assert.That(result.StatusMessage, Is.EqualTo("OK"));
				Assert.That(body, Contains.Substring("<html"));
			}
		}

		[Test]
		public void can_read_chunked_responses_in_parts ()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://www.httpwatch.com/httpgallery/chunked/")).Build();
			using (var result = _subject.Request(rq))
			{
				var reader = result.BodyReader;

				Assert.That(reader, Is.InstanceOf<HttpChunkedResponseStream>());

				string bodyChunk = "wrong";

				Console.WriteLine(Write(result.Headers));
				while (!reader.Complete)
				{
					var chunk = reader.ReadStringToTimeout();
					Console.WriteLine(">>> " + chunk);
					if (!string.IsNullOrEmpty(chunk)) bodyChunk = chunk;
				}


				Assert.That(result.StatusMessage, Is.EqualTo("OK"));
				Assert.That(bodyChunk, Contains.Substring("</html"));
			}
		}

		[Test]
		public void correctly_sends_query_strings ()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://www.google.co.uk/?q=wipquazin")).Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadStringToLength();

				Console.WriteLine(Write(result.Headers));
				Console.WriteLine(body);

				Assert.That(body, Contains.Substring("wipquazin"));
			}
		}

		[Test]
		public void read_from_wikipedia()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://en.wikipedia.org/wiki/Ternary_search_tree")).Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadStringToLength();

				Console.WriteLine(Write(result.Headers));
				Console.WriteLine(body);

				Assert.That(result.StatusMessage, Is.EqualTo("OK"));
				Assert.That(body, Contains.Substring("<html"));
			}
		}

		static string Write(IDictionary<string, string> headers)
		{
			return string.Join("\r\n", headers.Keys.Select(k => k + ": " + headers[k]));
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

			_subject.Timeout = TimeSpan.FromSeconds(0.5);

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
				/*using (*/
				var stream = result.BodyReader;/*)*/
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
