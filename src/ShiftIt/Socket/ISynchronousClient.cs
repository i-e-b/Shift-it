namespace ShiftIt
{
	public interface ISynchronousClient
	{
		string GetString (string url);

		string RawImmediate(string host, int port, string rawRequest);
	}
}