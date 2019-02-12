using System.IO;
using JetBrains.Annotations;

namespace ShiftIt.Internal.Http
{
	/// <summary>
	/// Contract for converting the public properties of an object into a readable stream
	/// </summary>
	public interface IObjectToRequestStreamConverter
	{
		/// <summary>
		/// Convert the public properties of an object into a readable stream
		/// </summary>
        [NotNull]Stream ConvertToStream([NotNull]object value);
	}
}
