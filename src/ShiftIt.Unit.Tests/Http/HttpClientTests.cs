using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ShiftIt.Unit.Tests.Http
{
	public class HttpClientTests
	{
		HttpClient _subject;

		[SetUp]
		public void setup()
		{
			_subject = new HttpClient();
		}
	}
}
