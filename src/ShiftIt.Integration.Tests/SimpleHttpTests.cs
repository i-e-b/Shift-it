using System;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Integration.Tests
{
	[TestFixture]
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
			var result = _subject.GetString("http://www.iana.org/domains/example", 4000);
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("<html"));
		}
    }
}
