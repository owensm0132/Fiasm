using System;
using System.Collections.Generic;
using System.Text;

namespace Fiasm.Data
{
    public static class Helpers
    {
        public static string Clamp(this string value, int maxSize)
        {
            if (string.IsNullOrEmpty(value) || maxSize < 1) return value;
            return value.Length <= maxSize ?
                value :
                value.Substring(0, maxSize);
        }
    }
}
