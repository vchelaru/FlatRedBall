using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;

namespace GlueCsvEditor.KnownValues
{
    public class FrbStateValueRetriever : IKnownValueRetriever
    {
        public IEnumerable<string> GetKnownValues(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
                return new string[0];

            // Try and get the FRB IElement for the fully qualified type name
            if (fullTypeName.IndexOf('.') < 0)
                return new string[0];

            fullTypeName = fullTypeName.Trim();

            string elementName = fullTypeName.Remove(fullTypeName.LastIndexOf('.'));
            string stateTypeName = fullTypeName.Substring(fullTypeName.LastIndexOf('.') + 1);

            // Convert the element name to GlueProject SaveClass format
            if (!elementName.Contains("Entities") && !elementName.Contains("Screens"))
                return new string[0];

            elementName = elementName.Substring(elementName.IndexOf(".", StringComparison.Ordinal) + 1)
                                     .Replace(".", "/");

            var element = ObjectFinder.Self.GetIElement(elementName);
            if (element == null)
                return new string[0];

            // First see if this is a uncategorized
            if (stateTypeName == "VariableState")
            {
                // Loop through the element's uncategorized state values
                //  and loop through all states for categories that are marked as shared
                var states = element.States.Select(x => x.Name);
                states = element.StateCategoryList
                                .Where(x => x.SharesVariablesWithOtherCategories)
                                .Aggregate(states, (current, cat) => current.Union(cat.States.Select(x => x.Name)));

                return states.Distinct().OrderBy(x => x).ToList();
            }

            // Otherwise see if the state category with the same name exists
            var category = element.StateCategoryList.FirstOrDefault(x => x.Name.Equals(stateTypeName, StringComparison.OrdinalIgnoreCase));
            if (category == null)
                return new string[0];

            return category.States
                           .Select(x => x.Name)
                           .OrderBy(x => x)
                           .ToList();
        }
    }
}
