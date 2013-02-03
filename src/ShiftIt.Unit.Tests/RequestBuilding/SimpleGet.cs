using System;
using System.Linq;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Unit.Tests.RequestBuilding
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
			Assert.That(_subject.ToString(), Is.StringStarting("GET "));
		}
		/*GET http://localhost:15672/api/queues/ HTTP/1.1
Host: localhost:15672
Connection: keep-alive
Cache-Control: max-age=0
Authorization: Basic Z3Vlc3Q6Z3Vlc3Q=
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,* /*;q=0.8
User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17
Accept-Encoding: gzip,deflate,sdch
Accept-Language: en-GB,en;q=0.8,en-US;q=0.6
Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3
Cookie: auth=Z3Vlc3Q6Z3Vlc3Q%3D; m=34e2:|1da9:t|64b8:t|796a:t|47ba:t
*/
		[Test]
		public void http_version_is_given_as_1_1()
		{
			Assert.That(_subject.ToString().Lines().First(), Is.StringEnding(" HTTP/1.1"));
		}

		[Test]
		public void request_path_is_given()
		{
			Assert.That(_subject.ToString().Lines().First(), Is.StringContaining(target.ToString()));
		}

		[Test]
		public void default_headers_are_written_correctly()
		{
			Assert.That(_subject.ToString().Lines(), Contains.Item("Host: www.example.com:80"));
			Assert.That(_subject.ToString().Lines(), Contains.Item("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"));
		}

		[Test]
		public void custom_headers_are_written_correctly()
		{
			Assert.That(_subject.ToString().Lines(), Contains.Item("User-Agent: Phil's face"));
		}

		[Test]
		public void message_ends_in_two_sets_of_crlf_and_has_no_other_instance_of_crlfcrlf()
		{
			Assert.That(_subject.ToString(), Is.StringEnding("\r\n\r\n"));
			Assert.That(_subject.ToString().CountOf("\r\n\r\n"), Is.EqualTo(1));
		}

		[Test]
		public void can_add_several_values_to_a_header()
		{
			_subject.AddHeader("X-Plane", "one").AddHeader("X-Plane", "two");
			Assert.That(_subject.ToString().Lines(), Contains.Item("X-Plane: one,two"));
		}
	}
}