using System.Net;
using NSubstitute;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Unit.Tests.Extensions
{
	[TestFixture]
	public class HttpResponseExtensionsTests
	{
		[Test]
		[TestCase(200, HttpStatusCode.OK)]
		[TestCase(404, HttpStatusCode.NotFound)]
		[TestCase(500, HttpStatusCode.InternalServerError)]
		public void Should_convert_IHttpResult_StatusCode_to_HttpStatusCode(int statusCode, HttpStatusCode expectedHttpStatusCode)
		{
			var response = Substitute.For<IHttpResponse>();
			response.StatusCode.Returns(statusCode);
			Assert.That(response.HttpStatusCode(), Is.EqualTo(expectedHttpStatusCode));
		}
	}
}
