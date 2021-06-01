using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeTradeSearch
{
    // Custom Extensions
    internal static class CustomEx
    {
        #region String Extensions

        public static bool IsEmpty(this string owner, bool orNull = true)
        {
            return orNull ? string.IsNullOrEmpty(owner) : owner == "";
        }

        public static string Join(this string[] owner, string separator)
        {
            return owner.Length > 1 ? string.Join(separator, owner) : owner[0];
        }

        public static int ToInt(this string owner, int @default = 0)
        {
            return owner.IsEmpty() ? @default : int.Parse(owner);
        }

        public static double ToDouble(this string owner, double @default = 0)
        {
            return owner.IsEmpty() ? @default : double.Parse(owner);
        }

        public static string Value(this string[] owner, int index, string @default = null)
        {
            return owner.Length > index ? owner[index] : @default;
        }

        public static bool WithIn(this string owner, string[] value)
        {
            return owner != null && Array.Exists(value, x => x.Equals(owner));
        }

        public static bool WithIn(this string owner, double minimum, double maximum)
        {
            return !owner.IsEmpty() && double.Parse(owner).WithIn(minimum, maximum);
        }

        #endregion

        #region Number Extensions

        public static bool WithIn(this double owner, double minimum, double maximum)
        {
            return owner >= minimum && owner <= maximum;
        }

        #endregion
    }
}
