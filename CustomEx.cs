using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

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

        public static bool WithIn(this string owner, params string[] args)
        {
            return owner != null && Array.Exists(args, x => x.Equals(owner));
        }

        public static bool WithIn(this string owner, double minimum, double maximum)
        {
            return !owner.IsEmpty() && double.Parse(owner).WithIn(minimum, maximum);
        }

        public static int ToInt(this string owner, int @default = 0)
        {
            try
            {
                return owner.IsEmpty() ? @default : int.Parse(owner);
            }
            catch
            {
                return 0;
            }
        }

        public static double ToDouble(this string owner, double @default = 0)
        {
            return owner.IsEmpty() ? @default : double.Parse(owner);
        }

        public static string ToTitleCase(this string owner)
        {
            if (owner == null) return owner;
            string[] words = owner.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                words[i] = char.ToUpper(words[i][0])
                    + (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
            }
            return string.Join(" ", words);
        }

        public static string RepEx(this string owner, string pattern, string replacement)
        {
            return System.Text.RegularExpressions.Regex.Replace(owner, pattern, replacement);
        }

        public static string Escape(this string owner)
        {
            return System.Text.RegularExpressions.Regex.Escape(owner);
        }

        #endregion

        #region Strings Extensions

        public static string Join(this string[] owner, string separator)
        {
            return owner.Length > 1 ? string.Join(separator, owner) : owner[0];
        }

        public static string Join(this string[] owner, char separator)
        {
            return owner.Length > 1 ? string.Join(separator.ToString(), owner) : owner[0];
        }

        public static string Value(this string[] owner, int index, string @default = null)
        {
            return owner.Length > index ? owner[index] : @default;
        }

        #endregion

        #region Number Extensions

        public static bool WithIn(this double owner, double minimum, double maximum)
        {
            return owner >= minimum && owner <= maximum;
        }

        #endregion

        #region Array Extensions

        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie) action(i);
        }

        #endregion

        public static void BInvoke(this FrameworkElement sender, Delegate method)
        {
            sender.Dispatcher.BeginInvoke(DispatcherPriority.Background, method);
        }
    }
}
