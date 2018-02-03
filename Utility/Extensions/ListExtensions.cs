using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Eee.Utility.Extensions.ListExtensions
{
    public static class ListExtensions
    {
        public static string ToDelimited<T>(this IEnumerable<T> items, string prepend, string postpent, string delimiter, int startAt=0)
        {
            StringBuilder builder = new StringBuilder();
            bool notFirst = false;
            int index = 0;
            foreach (T item in items)
            {
                int oldIndex = index++;
                if (oldIndex < startAt)
                    continue;

                if (notFirst)
                    builder.Append(delimiter);
                else
                    notFirst = true;

                builder.Append(prepend);
                if ( null != item)
                    builder.Append(item.ToString());
                builder.Append(postpent);
            }

            return builder.ToString();
        }

        public static string ToCSV<T>(this IEnumerable<T> values, Dictionary<Type, Func<T, string>> toStringMap = null)
        {
            if (values.Count() <= 0)
                return "";

            StringBuilder sb = new StringBuilder();
            bool addDelimiter = false;
            foreach (var value in values)
            {
                if (addDelimiter)
                    sb.Append(',');
                else
                    addDelimiter = true;

                string val;
                if (null != toStringMap)
                {
                    Func<T, string> toStringFunc;
                    if (toStringMap.TryGetValue(value.GetType(), out toStringFunc))
                        val = toStringFunc(value);
                    else
                        val = value.ToString();
                }
                else
                    val = value.ToString();

                bool isQuoted = val.Contains('\r') || val.Contains('\n') || val.Contains('"') || val.Contains(',');
                if (isQuoted)
                    sb.Append('"');

                sb.Append(val.Replace("\"", "\"\""));

                if (isQuoted)
                    sb.Append('"');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates a new list with only the items from the original list that match the predicate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static List<T> Subset<T>(this List<T> list, Predicate<T> pred)
        {
            List<T> subset = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (pred(item))
                    subset.Add(item);
            }
            return subset;
        }

        /// <summary>
        /// Creates a new list that is the union of all the given lists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="lists"></param>
        /// <returns></returns>
        public static List<T> Union<T>(this List<T> list, params List<T>[] lists)
        {
            List<T> union = new List<T>(list);
            for (int i = 0; i < lists.Length; i++)
            {
                union.AddRange(lists[i]);
            }

            return union;
        }
    }
}
