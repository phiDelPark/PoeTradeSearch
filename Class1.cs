using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PoeTradeSearch
{
    public static class Class1
    {
        public static IEnumerable<T> GetChildrenOfType<T>(this DependencyObject parent, bool recursive = true)
            where T : DependencyObject
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    yield return (T)child;

                if (recursive)
                {
                    foreach (T childOfChild in GetChildrenOfType<T>(child))
                        yield return childOfChild;
                }
            }
        }

        public static IEnumerable<Control> GetChildrenOfNotType<T>(this DependencyObject parent, bool recursive = true)
            where T : DependencyObject
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is Control && !(child is T))
                    yield return (Control)child;

                if (recursive)
                {
                    foreach (Control childOfChild in GetChildrenOfNotType<T>(child))
                        yield return childOfChild;
                }
            }
        }
    }
}
