using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	public class SimplePost
	{
		IHttpRequestBuilder _subject;
		Uri _target;
		string _data;
		string _localTarget;
		IHttpRequest _request;

		[SetUp]
		public void Setup()
		{
			_subject = new HttpRequestBuilder();
			_target = new Uri("http://www.example.com/my/path");
			_localTarget = "/my/path";
			_data = Lorem.Ipsum();

			_request = _subject.Post(_target).SetHeader("User-Agent", "Phil's face").Build(_data);
		}

		[Test]
		public void post_verb_is_given()
		{
			Assert.That(_request.RequestHead, Is.StringStarting("POST "));
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
		public void message_ends_in_two_sets_of_crlf_and_has_no_other_instance_of_crlfcrlf()
		{
			Assert.That(_request.RequestHead, Is.StringEnding("\r\n\r\n"));
			Assert.That(_request.RequestHead.CountOf("\r\n\r\n"), Is.EqualTo(1));
		}
	}
}