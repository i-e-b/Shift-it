using System;
using System.Linq;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	public class Headers
	{
		IHttpRequestBuilder _subject;
		Uri _target;
		string _data;

		[SetUp]
		public void setup()
		{
			_subject = new HttpRequestBuilder();
			_target = new Uri("http://www.example.com/my/path");
			_data = Lorem.Ipsum();

			_subject.Post(_target).StringData(_data).SetHeader("User-Agent", "Phil's face");
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
		public void can_add_several_values_to_a_header()
		{
			_subject.AddHeader("X-Plane", "one").AddHeader("X-Plane", "two").AddHeader("X-Plane", "two").AddHeader("X-Plane", "one");
			Assert.That(_subject.Build().RequestHead().Lines(), Contains.Item("X-Plane: one,two"));
		}

		[Test]
		public void can_add_several_values_to_accept_encoding()
		{
			_subject.AddHeader("Accept-Encoding", "x-gzip").AddHeader("Accept-Encoding", "x-custom");
			var headers = _subject.Build().RequestHead();

			Assert.That(headers.Lines(), Contains.Item("Accept-Encoding: gzip,deflate,x-gzip,x-custom"));
			Assert.That(headers.Lines().Count(x => x.StartsWith("Accept-Encoding:")), Is.EqualTo(1));
		}

		[Test]
		public void can_add_several_values_to_accept()
		{
			_subject.Accept("application/json").AddHeader("Accept", "application/xml");
			var headers = _subject.Build().RequestHead();

			Assert.That(headers.Lines(), Contains.Item("Accept: application/json,application/xml"));
			Assert.That(headers.Lines().Count(x => x.StartsWith("Accept:")), Is.EqualTo(1));
		}

		[Test]
		public void set_header_removes_previous_value()
		{
			_subject.Accept("application/json").SetHeader("Accept", "application/xml").SetHeader("Accept", "text/html");
			var headers = _subject.Build().RequestHead();

			Assert.That(headers.Lines(), Contains.Item("Accept: text/html"));
			Assert.That(headers.Lines().Count(x => x.StartsWith("Accept:")), Is.EqualTo(1));
		}

		[Test]
		public void can_set_custom_host_header()
		{
			_subject.SetHeader("Host", "ww1.example.com:81");
			var headers = _subject.Build().RequestHead();

			Assert.That(((IHttpRequest)_subject).Target, Is.EqualTo(_target));
			Assert.That(headers.Lines(), Contains.Item("Host: ww1.example.com:81"));
			Assert.That(headers.Lines().Count(x => x.StartsWith("Host:")), Is.EqualTo(1));
		}
	}
}
