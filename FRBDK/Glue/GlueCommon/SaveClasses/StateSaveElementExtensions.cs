using System.Collections.Generic;
using System.Linq;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>StateSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): the
    /// name/category lookups that relate a <see cref="StateSave"/> to its containing element. They
    /// take the container as a parameter or, for <see cref="StateSaveToString"/>, resolve it through
    /// <see cref="IObjectFinderCore"/> instead of reaching for <c>ObjectFinder.Self</c>. Lives here
    /// (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See #2276. Named
    /// differently from the original class (not a forwarding stub) to avoid a duplicate-type clash now
    /// that both assemblies are visible together via <c>Glue.csproj</c>'s <c>ProjectReference</c> to
    /// <c>GlueCommon</c>; the two non-extension static calls
    /// (<see cref="GetStateTypeFromCurrentVariableName"/>, <see cref="StateSaveToString"/>) had their
    /// call sites updated to the new class name.
    /// </summary>
    public static class StateSaveElementExtensions
    {
        // This function incorrectly
        // assumes that all variables
        // that represent states will be
        // named Current<StateType>State. This
        // is not true if the variable is tunneled,
        // so we really shouldn't use this in most places.
        public static string GetStateTypeFromCurrentVariableName(string memberName)
        {
            if (memberName == "CurrentState")
            {
                return "VariableState";
            }
            else
            {
                string possibleCategory = memberName.Substring("Current".Length);

                possibleCategory = possibleCategory.Substring(0, possibleCategory.Length - "State".Length);
                return possibleCategory;
            }
        }

        public static bool ContainsCategoryName(this List<StateSaveCategory> categoryList, string name)
        {
            foreach (StateSaveCategory category in categoryList)
            {
                if (category.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetExposedVariableName(this StateSave stateSave, IElement container)
        {
            if (container.States.Contains(stateSave))
            {
                return "CurrentState";
            }
            else
            {
                foreach (var category in container.StateCategoryList.Where(item => item.States.Contains(stateSave)))
                {
                    return "Current" + category.Name + "State";
                }
            }

            return "CurrentState";
        }

        public static string GetEnumTypeName(this StateSave stateSave, IElement container)
        {
            if (container.States.Contains(stateSave))
            {
                return "VariableState";
            }
            else
            {
                foreach (var category in container.StateCategoryList.Where(item => item.States.Contains(stateSave)))
                {
                    return category.Name;
                }
            }

            return "VariableState";

        }

        public static string StateSaveToString(StateSave stateSave)
        {
            return stateSave.Name + "(State in " + ObjectFinderCore.Self.GetElementContaining(stateSave) + ")";
        }
    }
}
