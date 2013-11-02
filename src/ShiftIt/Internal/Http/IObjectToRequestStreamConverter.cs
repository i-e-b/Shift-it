using System.IO;

namespace ShiftIt.Internal.Http
{
	public interface IObjectToRequestStreamConverter
	{
		Stream ConvertToStream(object value);
	}
}
