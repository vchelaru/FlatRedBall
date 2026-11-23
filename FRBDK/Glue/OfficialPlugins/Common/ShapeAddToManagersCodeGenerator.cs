using System.Linq;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.Common;

/// <summary>
/// Generates the add-to-managers line for a shape ATI's <c>AddToManagersFunc</c>.
/// </summary>
internal static class ShapeAddToManagersCodeGenerator
{
    /// <param name="element">The element whose code is being generated.</param>
    /// <param name="nos">The shape instance, or null when the element itself is the shape (an entity whose
    /// base is the shape type). The shape is then <c>this</c>, and its variables live on the element's
    /// IsContainer object, if it has one.</param>
    /// <param name="layerName">The layer expression, or null/empty to add unlayered.</param>
    /// <param name="unlayeredAddMethod">The ShapeManager method that adds the shape unlayered, such as "AddCircle".</param>
    public static string Generate(IElement element, NamedObjectSave nos, string layerName, string unlayeredAddMethod)
    {
        var instanceName = nos?.InstanceName ?? "this";

        if (string.IsNullOrEmpty(layerName))
        {
            return $"FlatRedBall.Math.Geometry.ShapeManager.{unlayeredAddMethod}({instanceName});";
        }

        var variableOwner = nos ?? element?.AllNamedObjects.FirstOrDefault(item => item.IsContainer);
        var visibleVariable = variableOwner?.GetCustomVariable("Visible")?.Value;

        var isVisible = visibleVariable == null || visibleVariable is true;
        string toReturn = "";
        if (isVisible)
        {
            // Adding unlayered flips visible to true. Adding layered doesn't, so we have to do
            // it explicitly here:
            toReturn += $"{instanceName}.Visible = true;";
        }
        toReturn += $"FlatRedBall.Math.Geometry.ShapeManager.AddToLayer({instanceName}, {layerName});";
        return toReturn;
    }
}
