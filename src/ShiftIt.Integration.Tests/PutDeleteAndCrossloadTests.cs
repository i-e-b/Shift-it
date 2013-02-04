using System;
using System.Net.NetworkInformation;
using System.Threading;
using NUnit.Framework;
using ShiftIt.Http;

namespace ShiftIt.Integration.Tests
{
	[TestFixture]
	public class PutDeleteAndCrossloadTests
	{
		IHttpClient _client;

		IHttpRequest _putRq;
		IHttpRequest _deleteRq;
		IHttpRequest _getRq;
		Uri _uri_src, _uri_dst;
		IHttpRequest _deleteRq2;

		[SetUp]
		public void setup()
		{
			_client = new HttpClient();
			_uri_src = new Uri("http://encoderraid0.7digital.local/testing/files/hello.txt");
			_uri_dst = new Uri("http://encoderraid0.7digital.local/testing/files/hello2.txt");
			_putRq = new HttpRequestBuilder().Put(_uri_src).StringData("Hello, world").Build();
			_getRq = new HttpRequestBuilder().Get(_uri_src).Build();
			_deleteRq = new HttpRequestBuilder().Delete(_uri_src).Build();
			_deleteRq2 = new HttpRequestBuilder().Delete(_uri_dst).Build();
		}

		[Test, Explicit]
		public void can_put_then_delete_a_file ()
		{
			_client.Request(_putRq).Dispose();
			using (var result = _client.Request(_getRq))
			{
				var body = result.BodyReader.ReadStringToLength();
				Assert.That(body, Is.EqualTo("Hello, world"));
			}

			_client.Request(_deleteRq).Dispose();
			using (var result = _client.Request(_getRq))
			{
				Assert.That(result.StatusClass, Is.EqualTo(StatusClass.ClientError)); // I'll accept 404 or 401
			}
		}

		[Test, Explicit]
		public void can_pump_a_stream_from_a_get_to_a_put ()
		{
			_client.Request(_putRq).Dispose();
			using (var getTx = _client.Request(_getRq)) // get source
			{
				var putRq = new HttpRequestBuilder().Put(_uri_dst)
					.Data(getTx.RawBodyStream, getTx.BodyReader.ExpectedLength).Build();
				using (var putTx = _client.Request(putRq)) // write out to dest
				{
					Assert.That(putTx.StatusClass, Is.EqualTo(StatusClass.Success));
				}

				
				var getRq = new HttpRequestBuilder().Get(_uri_dst).Build();
				using (var result = _client.Request(getRq)) // check dest is correct
				{
					var body = result.BodyReader.ReadStringToLength();
					Assert.That(body, Is.EqualTo("Hello, world"));
				}
			}
			_client.Request(_deleteRq).Dispose();
			_client.Request(_deleteRq2).Dispose();
		}

		[Test, Explicit]
		public void can_use_crossload_feature ()
		{
			_client.Request(_putRq).Dispose(); // put a file in place

			var load = new HttpRequestBuilder().Get(_uri_src).Build();
			var store = new HttpRequestBuilder().Put(_uri_dst);

			_client.CrossLoad(load, store); // copy from src to dest


			var getRq = new HttpRequestBuilder().Get(_uri_dst).Build();
			using (var result = _client.Request(getRq)) // check dest is correct
			{
				var body = result.BodyReader.ReadStringToLength();
				Assert.That(body, Is.EqualTo("Hello, world"));
			}
			_client.Request(_deleteRq).Dispose();
			_client.Request(_deleteRq2).Dispose();
		}


		
		[Test, Explicit]
		public void leak_test_over_cross_load()
		{
			var start = Connections();
			
			_client.Request(_putRq).Dispose(); // put a file in place
			var load = new HttpRequestBuilder().Get(_uri_src).Build();
			var store = new HttpRequestBuilder().Put(_uri_dst);

			for (int i = 0; i < 51; i++) // WebDAV is very slow... don't do too many!
			{
				_client.CrossLoad(load, store); // copy from src to dest
			}
			_client.Request(_deleteRq).Dispose();
			_client.Request(_deleteRq2).Dispose();

			Thread.Sleep(1000);
			var end = Connections();

			Console.WriteLine("Before: " + start + ", after: " + end);
			Assert.That(end, Is.LessThanOrEqualTo(start));
		}
		static long Connections()
		{
			return IPGlobalProperties.GetIPGlobalProperties()
			                         .GetTcpIPv4Statistics()
			                         .CurrentConnections;
		}
	}
}
