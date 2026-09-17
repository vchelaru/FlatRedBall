namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Moved out of <c>IElementHelper</c> (in <c>Glue.csproj</c>, net8.0-windows): these methods only touch
    /// an <see cref="IElement"/>'s own state and variable lists, plus
    /// <see cref="ElementExtensions.ContainsCustomVariableRecursively"/>, which already routes through
    /// <see cref="IObjectFinderCore"/>. Lives here (net8.0, no WPF) so it and its tests can build and run
    /// on Linux/macOS. See issue #2276. Named differently from the original class (not a forwarding stub)
    /// to avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class ElementStateExtensions
    {
        public static void SortStatesToCustomVariables(this IElement element)
        {
            foreach (StateSave stateSave in element.AllStates)
            {
                stateSave.SortInstructionSaves(element.CustomVariables);
            }
        }

        public static void CleanUnusedVariablesFromStates(this IElement element)
        {
            foreach (StateSave state in element.AllStates)
            {
                for (int i = state.InstructionSaves.Count - 1; i > -1; i--)
                {
                    // Make sure this variable is good:
                    if (!element.ContainsCustomVariableRecursively(state.InstructionSaves[i].Member))
                    {
                        state.InstructionSaves.RemoveAt(i);
                    }
                }
            }
        }

        public static void RemoveState(this IElement elementToRemoveFrom, StateSave stateSave)
        {
            if (elementToRemoveFrom.States.Contains(stateSave))
            {
                elementToRemoveFrom.States.Remove(stateSave);
            }
            else
            {
                foreach (StateSaveCategory category in elementToRemoveFrom.StateCategoryList)
                {
                    if (category.States.Contains(stateSave))
                    {
                        category.States.Remove(stateSave);
                        return;
                    }
                }
            }
        }
    }
}
