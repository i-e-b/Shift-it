using System;
using System.IO;
using System.Net;
using NSubstitute;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Internal.Http;
using ShiftIt.Internal.Socket;

namespace ShiftIt.Unit.Tests.Http.HttpClient
{
	[TestFixture]
	public class RequestOrThrow
	{
		private IHttpClient _client;
		private IHttpRequest _mockRequest;
		private IHttpResponse _response;
		private const string _dummyStatusMessage = "dummy status message";

		[SetUp]
		public void SetUp()
		{
			var streamSource = Substitute.For<IConnectableStreamSource>();
			streamSource.ConnectUnsecured(Arg.Any<Uri>(), Arg.Any<TimeSpan>()).Returns(new MemoryStream());

			_response = Substitute.For<IHttpResponse>();
			_response.StatusClass.Returns(StatusClass.ClientError);
			_response.StatusCode.Returns((int)HttpStatusCode.NotFound);
			_response.StatusMessage.Returns(_dummyStatusMessage);

			var parser = Substitute.For<IHttpResponseParser>();
			parser.Parse(Arg.Any<Stream>(), Arg.Any<TimeSpan>()).Returns(_response);

			_mockRequest = Substitute.For<IHttpRequest>();
			_mockRequest.Target.Returns(new Uri("http://nic"));

			_client = new ShiftIt.Http.HttpClient(streamSource, parser);
		}

		[Test]
		public void Should_throw_when_response_is_not_successful()
		{
			var ex = Assert.Throws<HttpTransferException>(() => _client.RequestOrThrow(_mockRequest));
			Assert.That(ex.Target, Is.EqualTo(new Uri("http://nic")));
			Assert.That(ex.StatusCode, Is.EqualTo(404));
			Assert.That(ex.Message, Is.StringContaining(_dummyStatusMessage));
		}

		[Test]
		public void Should_dispose_response_when_not_successful()
		{
			Assert.That(() => _client.RequestOrThrow(_mockRequest), Throws.TypeOf<HttpTransferException>());

			_response.Received().Dispose();
		}

		[Test]
		public void Should_not_dispose_response_when_successful()
		{
			_response = Substitute.For<IHttpResponse>();
			_response.StatusClass.Returns(StatusClass.Success);

			Assert.That(() => _client.RequestOrThrow(_mockRequest), Throws.TypeOf<HttpTransferException>());

			_response.DidNotReceive().Dispose();
		}
	}
}
