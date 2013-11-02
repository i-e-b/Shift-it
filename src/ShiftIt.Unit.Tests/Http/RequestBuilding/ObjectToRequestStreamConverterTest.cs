using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShiftIt.Http;
using ShiftIt.Internal.Http;
using System.IO;
using System.Text;
using System.Web;

namespace ShiftIt.Unit.Tests.Http.RequestBuilding
{
	[TestFixture]
	public class ObjectToRequestStreamConverterTest
	{
		IObjectToRequestStreamConverter _subject;

		[SetUp]
		public void SetUp()
		{
			_subject = new ObjectToRequestStreamConverter();
		}

		private static IEnumerable<TestCaseData> TestData
		{
			get
			{
				yield return new TestCaseData(new { }, string.Empty);
				yield return new TestCaseData(new { batchId = 1234 }, "batchId=1234");
				yield return new TestCaseData(new { batchId = 1234, isComplete = false }, "batchId=1234&isComplete=False");
				yield return new TestCaseData(new { q = (string)null }, "q=");
				yield return new TestCaseData(new { i1 = 12345, i2 = 0.898, i3 = 8.9900m }, "i1=12345&i2=0.898&i3=8.9900");

				yield return new TestCaseData(new object[]
				{
					new { batchId = 1234, isComplete = false },
					new { batchId = 134, isComplete = true },
					new { batchId = 14, otherId = true }
				}, "batchId=1234&isComplete=False&batchId=134&isComplete=True&batchId=14&otherId=True");

				yield return
					new TestCaseData(new
					{
						Name_ = "Jonathan Doe",
						Age = "23",
						Formula = "a + b == 13%!_-.~&"
					},
						"Name_=Jonathan+Doe&Age=23&Formula=a+%2B+b+%3D%3D+13%25%21_-.~%26");

				yield return new TestCaseData(new
					{
						// ព្រះ​ពុទ្ឋឃោសា
						v = "\u1796\u17d2\u179a\u17c7\u200b\u1796\u17bb\u1791\u17d2\u178b\u1783\u17c4\u179f\u17b6"
					},
					"v=%E1%9E%96%E1%9F%92%E1%9E%9A%E1%9F%87%E2%80%8B%E1%9E%96%E1%9E%BB%E1%9E%91%E1%9F%92%E1%9E%8B%E1%9E%83%E1%9F%84%E1%9E%9F%E1%9E%B6");

				yield return new TestCaseData(new
					{
						\u1796\u17d2\u179a\u17c7_\u1796\u17bb\u1791\u17d2\u178b\u1783\u17c4\u179f\u17b6 = "1"
					},
					"%E1%9E%96%E1%9F%92%E1%9E%9A%E1%9F%87_%E1%9E%96%E1%9E%BB%E1%9E%91%E1%9F%92%E1%9E%8B%E1%9E%83%E1%9F%84%E1%9E%9F%E1%9E%B6=1");
			}
		}

		[Test]
		[TestCaseSource("TestData")]
		public void Should_format_data_as_expected(object actual, string expected)
		{
			var data = (MemoryStream)_subject.ConvertToStream(actual);
			var stringData = Encoding.ASCII.GetString(data.ToArray());
			Assert.That(stringData, Is.EqualTo(expected));
		}

		[Test]
		public void Request_builder_throws_when_content_type_for_data_doesnt_match()
		{
			var exc = Assert.Throws<ArgumentException>(
				() => new HttpRequestBuilder().Put(new Uri("http://test")).Build(new {}));
			Assert.That(exc, Has.Message.EqualTo(
				"Automated data serialisation is only supported for Content-Type:application/x-www-form-urlencoded."));
		}

		[Test]
		public void Request_builder_doesnt_throw_when_content_type_for_data_is_correct()
		{
			Assert.DoesNotThrow(
				() => new HttpRequestBuilder().Put(new Uri("http://test")).AddHeader("Content-Type", "application/x-www-form-urlencoded")
					.Build(new { }));
		}
	}
}
