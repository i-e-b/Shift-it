using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	public class StreamPut
	{
		IHttpRequestBuilder _subject;
		Uri target;
		string _data;
		Stream _dataStream;
		string _localTarget;

		[SetUp]
		public void setup()
		{
			_subject = new HttpRequestBuilder();
			target = new Uri("http://www.example.com/my/path");			_localTarget = "/my/path";
			_data = Lorem.Ipsum();
			_dataStream = new MemoryStream(Encoding.UTF8.GetBytes(_data));

			_subject.Put(target).Data(_dataStream, _data.Length).SetHeader("User-Agent", "Phil's face");
		}

		[Test]
		public void post_verb_is_given()
		{
			Assert.That(_subject.Build().RequestHead(), Is.StringStarting("PUT "));
		}

		[Test]
		public void content_length_is_given()
		{
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("Content-Length: " + _data.Length));
		}

		[Test]
		public void content_is_written()
		{
			Assert.That(new StreamReader(_subject.Build().DataStream).ReadToEnd(), Is.EqualTo(_data));
		}

		[Test]
		public void http_version_is_given_as_1_1()
		{
			Assert.That(_subject.Build().RequestHead().Lines().First(), Is.StringEnding(" HTTP/1.1"));
		}

		[Test]
		public void request_path_is_given()
		{
			Assert.That(_subject.Build().RequestHead().Lines().First(), Is.StringContaining(_localTarget));
		}

		[Test]
		public void default_headers_are_written_correctly()
		{
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("Host: www.example.com:80"));
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("Accept: */*"));
		}
	}
}