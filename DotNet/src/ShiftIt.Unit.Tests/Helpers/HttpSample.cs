using System.IO;

namespace ShiftIt.Unit.Tests.Helpers
{
	public class HttpSample
	{
		public static Stream SimpleResponse()
		{
			return Sample("uncompressed");
		}
		
		public static Stream ChunkedResponse()
		{
			return Sample("chunked_plain");
		}

		public static Stream EmptyResponse()
		{
			return Sample("empty");
		}

		public static Stream GzippedResponse()
		{
			return Sample("gzipped");
		}
		
		public static Stream PlainTextWithIncorrectGzipHeader()
		{
			return Sample("uncompressed_but_marked_as_gzip");
		}
		
		public static Stream BadHeader()
		{
			return Sample("bad_header");
		}

		static Stream Sample(string f)
		{
			return File.OpenRead(Path.Combine(".", "Helpers", f+".txt"));
		}

		public static Stream WithDuplicatedHeaders()
		{
			return Sample("duplicated_headers");
		}

		public static Stream FailedResponse()
		{
			return Sample("failed");
		}

		public static Stream WithMixedCaseHeaders()
		{
			return Sample("mixed_case_headers");
		}
	}
}