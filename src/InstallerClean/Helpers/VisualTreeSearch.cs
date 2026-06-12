using System.Windows;
using System.Windows.Media;

namespace InstallerClean.Helpers;

internal static class VisualTreeSearch
{
    /// <summary>
    /// Depth-first enumeration of <paramref name="root"/>'s visual
    /// descendants of type <typeparamref name="T"/>. Reaches
    /// template-generated controls that have no field or x:Name (the
    /// detail windows' GridViewColumnHeader instances), so callers can
    /// set automation properties on them.
    /// </summary>
    public static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;
            foreach (var nested in Descendants<T>(child))
                yield return nested;
        }
    }
}
