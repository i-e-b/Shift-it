using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace ShiftIt.Internal.Http
{
	/// <summary>
	/// Constructs POST body stream for application/x-www-form-urlencoded requests.
	/// </summary>
	public class ObjectToRequestStreamConverter : IObjectToRequestStreamConverter
	{
		private const byte equalsSign = (byte) '=';
		private const byte and = (byte) '&';
		private const byte plus = (byte)'+';
		private const string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

		/// <summary>
		/// Does the actual work using <see cref="TypeDescriptor"/>.
		/// </summary>
		/// <param name="value">object to encode</param>
		/// <returns>POST body stream, rewound to the beginning.</returns>
		public Stream ConvertToStream(object value)
		{
			var result = new MemoryStream();
			var enumerable = value as IEnumerable;
			if (enumerable != null)
			{
				var firstParameter = true;
				foreach (var enumerableValue in enumerable)
				{
					WriteValueObject(enumerableValue, firstParameter, result);
					firstParameter = false;
				}
			}
			else
			{
				WriteValueObject(value, true, result);
			}

			result.Position = 0;
			return result;
		}

		private void WriteValueObject([NotNull]object value, bool firstParameter, [NotNull]MemoryStream result)
		{
			var properties = TypeDescriptor.GetProperties(value);
			foreach (PropertyDescriptor property in properties)
			{
				WriteProperty(value, firstParameter, result, property);
				firstParameter = false;
			}
		}

		private void WriteProperty([NotNull]object valueProvider, bool firstParameter, [NotNull]MemoryStream result, [NotNull]PropertyDescriptor property)
		{
			if (!firstParameter)
				result.WriteByte(and);

			UrlEncodeAndWrite(property.Name, result);
			result.WriteByte(equalsSign);

			var propertyValue = property.GetValue(valueProvider);
			var formattableValue = propertyValue as IFormattable;

			string stringValue = null;
			if (formattableValue != null)
				stringValue = formattableValue.ToString(null, CultureInfo.InvariantCulture);
			else if (propertyValue != null)
				stringValue = propertyValue.ToString();

			if (stringValue != null)
			{
				UrlEncodeAndWrite(stringValue, result);
			}
		}

		// RFC:  http://www.w3.org/TR/html5/forms.html#application/x-www-form-urlencoded-encoding-algorithm
		private void UrlEncodeAndWrite([NotNull]string value, [NotNull]MemoryStream memoryStream)
		{
			foreach (char symbol in value)
			{
				if (unreservedChars.IndexOf(symbol) != -1)
				{
					memoryStream.WriteByte((byte)symbol);
				}
				else if (symbol == ' ')
				{
					memoryStream.WriteByte(plus);
				}
				else if (symbol < 255)
				{
					var encodedValue = Encoding.ASCII.GetBytes(String.Format("%{0:X2}", (byte)symbol));
					memoryStream.Write(encodedValue, 0, encodedValue.Length);
				}
				else
				{
					var bytes = Encoding.UTF8.GetBytes(new[] { symbol });
					foreach (var @byte in bytes)
					{
						var encodedValue = Encoding.ASCII.GetBytes(String.Format("%{0:X2}", @byte));
						memoryStream.Write(encodedValue, 0, encodedValue.Length);
					}
				}
			}
		}
	}
}
