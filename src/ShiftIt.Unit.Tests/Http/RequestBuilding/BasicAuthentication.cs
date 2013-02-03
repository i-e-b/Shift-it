using System;
using System.Linq;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Http.Internal;
using ShiftIt.Unit.Tests.Helpers;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	public class BasicAuthentication
	{
		IHttpRequestBuilder _subject;
		Uri target;

		[SetUp]
		public void setup()
		{
			_subject = new HttpRequestBuilder();
			target = new Uri("http://www.example.com/my/path");

			_subject.Get(target).BasicAuthentication("Aladdin","open sesame");
		}

		[Test]
		public void correct_basic_authentication_string_is_used ()
		{
			Assert.That(_subject.Build().RequestHead().Lines(),
				Contains.Item("Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ=="));
		}
		
	}
}