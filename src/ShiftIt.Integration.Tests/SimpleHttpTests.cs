using System;
using NUnit.Framework;
using ShiftIt.Http.Internal;

namespace ShiftIt.Integration.Tests
{
	[TestFixture]//, Explicit, Description("These connect to the internet")]
    public class SimpleHttpTests
    {
		IHttpClient _subject;

		[SetUp]
		public void setup()
		{
			_subject = new HttpClient();
		}

		[Test]
		public void can_download_from_http_server()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://www.iana.org/domains/example")).Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadToEnd();

				Console.WriteLine(body);
				Assert.That(body, Contains.Substring("<html"));
			}
		}

		[Test]
		public void read_from_wikipedia ()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("http://en.wikipedia.org/wiki/Ternary_search_tree")).Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadToEnd();

				Console.WriteLine(body);
				Assert.That(body, Contains.Substring("<html"));
			}
		}

		[Test]
		public void connection_to_rabbit_mq_api () {
		var rq = new HttpRequestBuilder().Get(new Uri("http://localhost:15672/api/overview")).BasicAuthentication("guest","guest").Build();
			using (var result = _subject.Request(rq))
			{
				var body = result.BodyReader.ReadToEnd();

				Console.WriteLine(body);
				Assert.That(body, Contains.Substring("{\"management_version"));
			}
		}
    }
}
