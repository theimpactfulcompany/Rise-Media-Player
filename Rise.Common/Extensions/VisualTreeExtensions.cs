using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Rise.Common.Extensions
{
    public static class VisualTreeExtensions
    {
        /// <summary>
        /// Tries to find the specified visual child with the specified name.
        /// </summary>
        /// <typeparam name="ChildItem">The kind of item to find.</typeparam>
        /// <param name="obj">Object where search will happen.</param>
        /// <param name="name">Name of the item to find.</param>
        /// <returns>The item if it's found, null otherwise.</returns>
        public static ChildItem FindVisualChild<ChildItem>(this DependencyObject obj, string name)
            where ChildItem : FrameworkElement
        {
            if (obj == null)
                return null;

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is ChildItem item && item.Name == name)
                    return item;

                ChildItem childOfChild = child.FindVisualChild<ChildItem>(name);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        /// <summary>
        /// Tries to find the specified visual child.
        /// </summary>
        /// <typeparam name="ChildItem">The kind of item to find.</typeparam>
        /// <param name="obj">Object where search will happen.</param>
        /// <returns>The item if it's found, null otherwise.</returns>
        public static ChildItem FindVisualChild<ChildItem>(this DependencyObject obj)
            where ChildItem : DependencyObject
        {
            if (obj == null)
                return null;

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is ChildItem item)
                    return item;

                ChildItem childOfChild = child.FindVisualChild<ChildItem>();
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        /// <summary>
        /// Tries to find all children of specified kind.
        /// </summary>
        /// <param name="parent">Where to find the item.</param>
        /// <param name="recurse">Whether or not the search should be recursive.</param>
        /// <returns>All child items found.</returns>
        public static IEnumerable<ChildItem> GetChildren<ChildItem>
            (this DependencyObject parent, bool recurse = false)
                where ChildItem : DependencyObject
        {
            if (parent == null)
                yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is ChildItem item)
                    yield return item;

                if (recurse && child != null)
                {
                    foreach (var grandChild in child.GetChildren<ChildItem>(true))
                        yield return grandChild;
                }
            }
        }

        /// <summary>
        /// Tries to find the specified visual parent.
        /// </summary>
        /// <typeparam name="ParentItem">The kind of item to find.</typeparam>
        /// <param name="obj">Object where search will happen.</param>
        /// <returns>The item if it's found, null otherwise.</returns>
        public static ParentItem FindVisualParent<ParentItem>(this DependencyObject obj)
            where ParentItem : DependencyObject
        {
            while (obj != null)
            {
                DependencyObject parent = VisualTreeHelper.GetParent(obj);
                if (parent is ParentItem typedParent)
                    return typedParent;

                obj = parent;
            }

            return null;
        }
    }
}
