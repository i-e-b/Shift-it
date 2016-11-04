namespace ShiftIt.Internal.Streaming
{
	/// <summary>
	/// Represents a stream that has an internal mechanism
	/// to represent end-of-data
	/// </summary>
	public interface ISelfTerminatingStream
	{
		/// <summary>
		/// Returns true if the stream has terminated,
		/// false otherwise.
		/// </summary>
		bool IsComplete();
	}
}