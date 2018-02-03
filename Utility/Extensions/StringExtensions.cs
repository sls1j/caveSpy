using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Bee.Eee.Utility.Extensions.StringExtensions
{
    public static class StringExtensions
    {
        public static bool TryParseGuid(this string value, out Guid guidValue)
        {
            try
            {
                guidValue = new Guid(value);
                return true;
            }
            catch (Exception)
            {
                guidValue = Guid.Empty;
                return false;
            }
        }

		/// <summary>
		/// This extension takes an arbitrary string and creates a Guid based on it. There are no recognizable
		/// elements from the string in the Guid, but the same string is guaranteed to generate the same Guid.
		/// </summary>
		public static Guid ToGuid(this string value)
		{
			using (var hasher = System.Security.Cryptography.MD5.Create())
				return new Guid(hasher.ComputeHash(Encoding.Default.GetBytes(value)));
		}

        public static List<string> ParseCSVRow(this string csv)
        {
            List<string> results = new List<string>();

            bool insideQuote = false;
            int index = 0;
            int fieldStart = index;
            StringBuilder word = new StringBuilder();
            while (index < csv.Length)
            {
                char c = csv[index];
                if (insideQuote)
                {
                    if (c == '"')
                    {
                        if (index + 1 < csv.Length && csv[index + 1] == '"')
                        {
                            word.Append('"');
                            index++;
                        }
                        else
                            insideQuote = false;
                    }
                    else
                        word.Append(c);
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                            insideQuote = true;
                            break;
                        case ',':
                            results.Add(word.ToString());
                            word = new StringBuilder();
                            break;
                        default:
                            word.Append(c);
                            break;
                    }
                }

                index++;
            }

            // if there is nothing left to parse so be it.
            if (word.Length > 0)
                results.Add(word.ToString());

            // if it ends with a command then the last field is an empty string
            if (csv.Length > 0 && csv.Last() == ',')
                results.Add(string.Empty);

            if (insideQuote)
                throw new InvalidCastException("Unable to convert string to csv list!  Quoting wrong.");

            return results;
        }

        /// <summary>
        /// Compares two strings using invariant culture and ignoring case.  
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <returns>True if they are the same false otherwise.</returns>
        public static bool EqualIC( this string a, string b )
        {
            if ( null == a )
                return null == b;

            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatInv(string formatString, params object[] parameters)
        {
            
            return string.Format(CultureInfo.InvariantCulture, formatString, parameters);
        }

        public static void AppendFormatLine(this StringBuilder sb, string formatString, params object[] parameters)
        {
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, formatString, parameters);
            sb.AppendLine();
        }

        public static void AppendFormatInv(this StringBuilder sb, string formatString, params object[] parameters)
        {
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, formatString, parameters);
        }
    }
}
