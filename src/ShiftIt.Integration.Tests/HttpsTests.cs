using System;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Integration.Tests
{
	[TestFixture]
	public class HttpsTests
	{
		IHttpClient _subject;

		[SetUp]
		public void setup()
		{
			_subject = new HttpClient();
		}

		[Test, Description("Nuget uses https throughout")]
		public void read_from_nuget ()
		{
			var rq = new HttpRequestBuilder().Get(new Uri("https://nuget.org/")).Build();
			using (var result = _subject.Request(rq))
			{
				Assert.That(result.StatusClass, Is.EqualTo(StatusClass.Success), result.StatusMessage);
				var body = result.BodyReader.ReadStringToTimeout(); // this server returns an invalid length!

				Console.WriteLine("Expected " + result.BodyReader.ExpectedLength + ", got " + body.Length);
				Console.WriteLine(body);
				Assert.That(body, Contains.Substring("<html"));
			}
		}
		 
	}
}