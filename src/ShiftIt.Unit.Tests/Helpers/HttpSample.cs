﻿using System.IO;

namespace ShiftIt.Unit.Tests.Responses
{
	public class HttpSample
	{
		public static TextReader SimpleResponse()
		{
			return new StringReader(File.ReadAllText(".\\Helpers\\uncompressed.txt"));
		}

		public static TextReader EmptyResponse()
		{
			return new StringReader(File.ReadAllText(".\\Helpers\\empty.txt"));
		}
	}
}