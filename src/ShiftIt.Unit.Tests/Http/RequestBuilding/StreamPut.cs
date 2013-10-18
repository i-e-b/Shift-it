using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	[SuppressMessage("Design", "CA1001", Justification = "Field is being cleaned in teardown.")]
	public class StreamPut
	{
		IHttpRequestBuilder _subject;
		Uri _target;
		string _data;
		Stream _dataStream;
		string _localTarget;
		IHttpRequest _request;

		[SetUp]
		public void Setup()
		{
			_subject = new HttpRequestBuilder();
			_target = new Uri("http://www.example.com/my/path");
			_localTarget = "/my/path";
			_data = Lorem.Ipsum();
			_dataStream = new MemoryStream(Encoding.UTF8.GetBytes(_data));

			_request = _subject.Put(_target).SetHeader("User-Agent", "Phil's face").Build(_dataStream, _data.Length);
		}

		[TearDown]
		public void Teardown()
		{
			_dataStream.Dispose();
		}

		[Test]
		public void post_verb_is_given()
		{
			Assert.That(_request.RequestHead, Is.StringStarting("PUT "));
		}

		[Test]
		public void content_length_is_given()
		{
			Assert.That(_request.RequestHead.Lines(), Contains.Item("Content-Length: " + _data.Length));
		}

		[Test]
		public void content_is_written()
		{
			Assert.That(new StreamReader(_request.DataStream).ReadToEnd(), Is.EqualTo(_data));
		}

		[Test]
		public void http_version_is_given_as_1_1()
		{
			Assert.That(_request.RequestHead.Lines().First(), Is.StringEnding(" HTTP/1.1"));
		}

		[Test]
		public void request_path_is_given()
		{
			Assert.That(_request.RequestHead.Lines().First(), Is.StringContaining(_localTarget));
		}

		[Test]
		public void default_headers_are_written_correctly()
		{
			Assert.That(_request.RequestHead.Lines(), Contains.Item("Host: www.example.com:80"));
			Assert.That(_request.RequestHead.Lines(), Contains.Item("Accept: */*"));
		}
	}
}