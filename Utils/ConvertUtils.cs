using RotMG.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace RotMG.Utils
{
    public static class ConvertUtils
    {
        private static SHA1Managed _sHA1Managed = new SHA1Managed();

        public static int[] ToIntArray(this string value, string seperator)
        {
            string[] seperated = value.Split(seperator, StringSplitOptions.None);
            return seperated.Select(k => k.Contains("-") ? int.Parse(k) : (int)Convert.ToUInt32(k, 16)).ToArray();
        }

        public static string ToSHA1(this string value) => Convert.ToBase64String(_sHA1Managed.ComputeHash(Encoding.UTF8.GetBytes(value)));

        public static Position ToPosition(this IntPoint point) 
        {
            return new Position(point.X, point.Y);
        }

        public static IntPoint ToIntPoint(this Position position)
        {
            return new IntPoint((int)position.X, (int)position.Y);
        }
    }
}
