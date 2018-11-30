using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
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

				var body = reader.ReadStringToLength();

				Console.WriteLine(Write(result.Headers));
				Console.WriteLine(body);

				Assert.That(result.StatusMessage, Is.EqualTo("OK"));
				Assert.That(body, Contains.Substring("<html"));
				Assert.That(body, Contains.Substring("</html>"));
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
		public void read_from_common_site()
		{
            var rq = new HttpRequestBuilder().Get(new Uri("http://datagenetics.com/blog.html")).Build();
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
		public void head_from_common_site()
		{
            var rq = new HttpRequestBuilder().Head(new Uri("http://datagenetics.com/blog.html")).Build();
			using (var result = _subject.Request(rq))
			{
				Assert.That(result.StatusCode == 200, "Expected 200, but got "+result.StatusCode+" ("+result.StatusMessage+")");
			}
		}

		[Test]
		public void reading_the_body_of_a_head_request_results_in_a_timeout()
		{
            var rq = new HttpRequestBuilder().Head(new Uri("http://datagenetics.com/blog.html")).Build();

			_subject.Timeout = TimeSpan.FromSeconds(0.5);

			using (var result = _subject.Request(rq))
			{
				var exception = Assert.Throws<Http.TimeoutException>(() => result.BodyReader.ReadStringToLength());
				Assert.That(exception.ErrorCode, Is.EqualTo((int)SocketError.TimedOut));
			}
		}

        [Test]
        public void upload_progress_gives_call_back () {
            // note, this all happens on one thread, so you want to respond quickly!
            _subject.Timeout = TimeSpan.FromSeconds(5);

            var progressList = new List<long>();
            var uploadData = Encoding.ASCII.GetBytes("Here is some data I will send");

            using (var testServer = new TestServer(AcceptSlow))
            {
                var rq = new HttpRequestBuilder()
                    .Post(new Uri("http://localhost:" + testServer.Port + "/whatever"))
                    .Build(uploadData);

                using (var result = _subject.Request(rq, tx_bytes=>{ progressList.Add(tx_bytes);}))
                {
                    result.BodyReader.ReadStringToLength();

                    Assert.That(progressList, Is.Not.Empty);
                    Assert.That(progressList.Last(), Is.EqualTo(uploadData.Length));
                }
            }
        }
        
	    [Test]
	    public void download_progress_gives_call_back () {
	        // note, this all happens on one thread, so you want to respond quickly!
	        _subject.Timeout = TimeSpan.FromSeconds(5);

	        var progressList = new List<long>();

	        using (var testServer = new TestServer(AcceptSlow))
	        {
	            var rq = new HttpRequestBuilder()
	                .Post(new Uri("http://localhost:" + testServer.Port + "/whatever"))
	                .Build("Here is some data I will send");

	            using (var result = _subject.Request(rq))
	            {
	                result.BodyReader.ReadStringToLength(tx_bytes=>{ progressList.Add(tx_bytes);});
	                Assert.That(progressList, Is.Not.Empty);
	                Assert.That(progressList.Last(), Is.EqualTo(sentDataLength));
	            }
	        }
	    }

        long sentDataLength; 
	    private string AcceptSlow(HttpListenerRequest rq, HttpListenerResponse tx)
	    {

            var data = Encoding.ASCII.GetBytes("Hello, world. Here is my slow reply.");
            var repeats = 10; // make the response a bit bigger

            tx.ContentType = "text/plain";

	        sentDataLength = data.Length * repeats;
            tx.ContentLength64 = sentDataLength;
            tx.StatusCode = 200;

            for (int i = 0; i < repeats; i++)
            {
                tx.OutputStream.Write(data, 0, data.Length);
            }
            tx.OutputStream.Flush();

            return null;
	    }

	    [Test, Explicit]
		public void connection_to_rabbit_mq_api()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:15672/api/overview")).BasicAuthentication("guest", "guest").Build();
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
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:15672/api/vhosts")).BasicAuthentication("guest", "guest").Build();
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
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:15672/api/vhosts")).BasicAuthentication("guest", "guest").Build();
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
			var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:15672/api/vhosts")).BasicAuthentication("guest", "guest").Build();
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
			Assert.That(end, Is.GreaterThan(start));
		}

		static long Connections()
		{
			return IPGlobalProperties.GetIPGlobalProperties()
									 .GetTcpIPv4Statistics()
									 .CurrentConnections;
		}
	}
}
