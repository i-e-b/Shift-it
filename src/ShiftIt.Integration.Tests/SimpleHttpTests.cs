using System;
using NUnit.Framework;

namespace ShiftIt.Integration.Tests
{
	[TestFixture]
    public class SimpleHttpTests
    {
		[Test]
		public void can_download_from_http_server()
		{
			var result = HttpClient.GetString("http://www.amazon.co.uk", 4000);
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("<html"));
		}
    }
}
