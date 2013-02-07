using System;
using System.Linq;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Http.Internal;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	public class SimpleGet
	{
		IHttpRequestBuilder _subject;
		Uri target;

		[SetUp]
		public void setup()
		{
			_subject = new HttpRequestBuilder();
			target = new Uri("http://www.example.com/my/path");

			_subject.Get(target).SetHeader("User-Agent", "Phil's face");
		}

		[Test]
		public void get_verb_is_given()
		{
			Assert.That(_subject.Build().RequestHead(), Is.StringStarting("GET "));
		}
		
		[Test]
		public void http_version_is_given_as_1_1()
		{
			Assert.That(_subject.Build().RequestHead().Lines().First(), Is.StringEnding(" HTTP/1.1"));
		}

		[Test]
		public void request_path_is_given()
		{
			Assert.That(_subject.Build().RequestHead().Lines().First(), Is.StringContaining(target.ToString()));
		}

		[Test]
		public void default_headers_are_written_correctly()
		{
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("Host: www.example.com:80"));
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("Accept: */*"));
		}

		[Test]
		public void custom_headers_are_written_correctly()
		{
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("User-Agent: Phil's face"));
		}

		[Test]
		public void message_ends_in_two_sets_of_crlf_and_has_no_other_instance_of_crlfcrlf()
		{
			Assert.That(_subject.Build().RequestHead(), Is.StringEnding("\r\n\r\n"));
			Assert.That(_subject.Build().RequestHead().CountOf("\r\n\r\n"), Is.EqualTo(1));
		}

		[Test]
		public void can_add_several_values_to_a_header()
		{
			_subject.AddHeader("X-Plane", "one").AddHeader("X-Plane", "two");
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("X-Plane: one,two"));
		}
	}
}