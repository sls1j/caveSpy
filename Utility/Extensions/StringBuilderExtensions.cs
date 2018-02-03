using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrow.Utility.Extensions
{
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Appends a formatted line to a string builder
        /// </summary>
        /// <param name="sb">The StringBuilder instance</param>
        /// <param name="format">The format of the string</param>
        /// <param name="args">The arguments for the format</param>
        public static void FormatLine(this StringBuilder sb, string format, params object[] args)
        {
            sb.AppendLine( string.Format(format, args) );
        }
    }
}
