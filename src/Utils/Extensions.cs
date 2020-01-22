using System;
using System.Collections.Generic;
using System.Linq;

namespace Torpedo
{
    static class ListExtensions
    {
        private static Random rnd = new Random();

        public static T Random<T>(this IEnumerable<T> me)
        {
            var arr = me.ToArray();
            return arr[rnd.Next(arr.Length)];
        }
    }

    static class Base64
    {
        public static byte[] DecodeWithoutPadding(string str)
        {
            str += new String('=', 4 - (str.Length % 4));
            return Convert.FromBase64String(str);
        }
    }
}