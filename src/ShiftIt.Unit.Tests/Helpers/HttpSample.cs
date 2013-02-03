using System.IO;

namespace ShiftIt.Unit.Tests.Helpers
{
	public class HttpSample
	{
		public static Stream SimpleResponse()
		{
			return Sample("uncompressed");
		}

		public static Stream EmptyResponse()
		{
			return Sample("empty");
		}

		public static Stream GzippedResponse()
		{
			return Sample("gzipped");
		}

		static Stream Sample(string f)
		{
			return File.OpenRead(".\\Helpers\\"+f+".txt");
		}
	}
}