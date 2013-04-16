using System;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	public class HttpsDetection
	{
		IHttpRequestBuilder _secured;
		IHttpRequestBuilder _unsecured;

		[SetUp]
		public void some_requests ()
		{
			_secured =  new HttpRequestBuilder().Get(new Uri("https://secure.example.com"));
			_unsecured = new HttpRequestBuilder().Get(new Uri("http://www.example.com"));
		}

		[Test]
		public void unsecured_requests_have_secured_equal_to_false ()
		{
			Assert.That(_unsecured.Build().Secure, Is.False);
		}

		[Test]
		public void secured_requests_have_secured_equal_to_true ()
		{
			Assert.That(_secured.Build().Secure, Is.True);
		}
	}
}