using System.IO;

namespace ShiftIt.Unit.Tests.Helpers
{
	public class HttpSample
	{
		public static TextReader SimpleResponse()
		{
			return Sample("uncompressed");
		}

		public static TextReader EmptyResponse()
		{
			return Sample("empty");
		}

		public static TextReader GzippedResponse()
		{
			return Sample("gzipped");
		}

		static TextReader Sample(string f)
		{
			return new StringReader(File.ReadAllText(".\\Helpers\\"+f+".txt"));
		}
	}
}