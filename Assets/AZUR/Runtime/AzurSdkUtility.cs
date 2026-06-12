using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AZUR
{
    internal static class AzurSdkUtility
    {
        public static Dictionary<string, object> ToMutable(IReadOnlyDictionary<string, object> source)
        {
            return source == null ? new Dictionary<string, object>() : new Dictionary<string, object>(source);
        }

        public static string ToInvariantString(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value switch
            {
                float number => number.ToString(CultureInfo.InvariantCulture),
                double number => number.ToString(CultureInfo.InvariantCulture),
                decimal number => number.ToString(CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        }

        public static string ParamsToLog(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return "{}";
            }

            var builder = new StringBuilder("{");
            var isFirst = true;
            foreach (var pair in parameters)
            {
                if (!isFirst)
                {
                    builder.Append(", ");
                }

                builder.Append(pair.Key);
                builder.Append(": ");
                builder.Append(ToInvariantString(pair.Value));
                isFirst = false;
            }

            builder.Append('}');
            return builder.ToString();
        }
    }
}
