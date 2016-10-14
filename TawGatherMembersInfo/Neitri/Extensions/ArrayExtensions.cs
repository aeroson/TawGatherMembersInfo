using System;
using System.Linq;

namespace Neitri
{
	public static class ArrayExtensions
	{
		/// <summary>
		/// Unififed extension method that retuns number of elements of ICollection, array and others
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dst"></param>
		/// <returns></returns>
		public static long Size<T>(this T[] dst)
		{
			return dst.LongLength;
		}

		/// <summary>
		/// Returns comma separated values of ToString() of elements
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dst"></param>
		/// <returns></returns>
		public static string ToBetterString<T>(this T[] dst)
		{
			return string.Join(", ", dst.Select(e => e.ToString()).ToArray());
		}
	}
}