namespace ShiftIt
{
	public interface IHttpClient
	{
		string GetString (string url, int timeOut);
	}
}