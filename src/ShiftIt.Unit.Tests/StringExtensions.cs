using System.Collections.Generic;

namespace ShiftIt.Unit.Tests
{
	public static class StringExtensions
	{
		public static IEnumerable<string> Lines(this string src)
		{
			var breaks = new[]{'\r','\n'};
			int left = 0, right;
			while ((right = src.IndexOfAny(breaks, left)) >= 0)
			{
				yield return src.Substring(left, right - left);
				while (right < src.Length && (src[right] == '\r' || src[right] == '\n')) right++;

				left = right;
				if (left >= src.Length) yield break;
			}
		}

		public static int CountOf(this string haystack, string needle)
		{
			int f=0, c = 0;
			while ((f = haystack.IndexOf(needle, f)) >= 0)
			{
				c++; f+=needle.Length;
			}
			return c;
		}
	}
}
