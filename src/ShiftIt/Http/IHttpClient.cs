namespace ShiftIt.Http
{
	public interface IHttpClient
	{
		IHttpResponse Request(IHttpRequest request);
		void CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest);

		byte[] CrossLoad(IHttpRequest loadRequest, IHttpRequestBuilder storeRequest, string hashAlgorithmName);
	}
}