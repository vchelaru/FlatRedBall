using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Content.Instructions;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Renames a state category and every reference to it, so generated code keeps compiling. A category is
    /// referenced by its type name and by its property, Current{Category}State:
    /// <list type="bullet">
    /// <item>variable types, unqualified ("DashCategory") or qualified ("Entities.PlayerBall.DashCategory"), nullable
    /// or not, in any element;</item>
    /// <item>the Current{Category}State variable in the owner and the elements that inherit the category;</item>
    /// <item>what sets that property by name: states and excluded-variable lists in those elements, events on it,
    /// object variables on instances of those elements, and variables tunneling into those instances.</item>
    /// </list>
    /// Validation is the caller's. Code is not regenerated and nothing is saved.
    /// </summary>
    public static class StateSaveCategoryRenameLogic
    {
        /// <returns>The elements that changed, the owner first.</returns>
        public static List<GlueElement> Rename(GlueProjectSave project, GlueElement owner, StateSaveCategory category, string newName)
        {
            var oldName = category.Name;
            var changed = new List<GlueElement> { owner };
            if (oldName == newName)
            {
                return changed;
            }
            void MarkChanged(GlueElement element)
            {
                if (!changed.Contains(element))
                {
                    changed.Add(element);
                }
            }

            var qualifiedPrefix = owner.Name.Replace("\\", ".").Replace("/", ".") + ".";
            var oldQualified = qualifiedPrefix + oldName;
            var newQualified = qualifiedPrefix + newName;
            var oldProperty = "Current" + oldName + "State";
            var newProperty = "Current" + newName + "State";

            var allElements = project.Screens.Cast<GlueElement>().Concat(project.Entities).ToList();

            // The owner and the elements that inherit the category without defining their own of the same name.
            var family = allElements
                .Where(element => element == owner || (InheritsFrom(project, element, owner) && element.GetStateCategoryRecursively(oldName) == category))
                .ToList();
            var familyNames = family.Select(element => element.Name).ToHashSet();

            // Resolve every reference before renaming anything; resolution reads the category's name.
            var typedVariables = allElements
                .SelectMany(element => element.CustomVariables.Select(variable => (element, variable)))
                .Where(pair => RefersToCategory(pair.variable, pair.element))
                .ToList();
            var instances = allElements
                .SelectMany(element => element.AllNamedObjects.Select(nos => (element, nos)))
                .Where(pair => pair.nos.SourceType == SourceType.Entity)
                .Where(pair => familyNames.Contains(pair.nos.SourceClassType))
                .ToList();
            var tunneling = allElements
                .SelectMany(element => element.CustomVariables.Select(variable => (element, variable)))
                .Where(pair => pair.variable.SourceObjectProperty == oldProperty && !string.IsNullOrEmpty(pair.variable.SourceObject))
                .Where(pair => pair.element.GetNamedObjectRecursively(pair.variable.SourceObject) is NamedObjectSave nos &&
                    instances.Any(instance => instance.nos == nos))
                .ToList();

            category.Name = newName;

            foreach (var (element, variable) in typedVariables)
            {
                variable.Type = RenameType(variable.Type);
                if (family.Contains(element) && variable.Name == oldProperty)
                {
                    variable.Name = newProperty;
                }
                MarkChanged(element);
            }

            foreach (var element in family)
            {
                foreach (var instruction in element.AllStates.SelectMany(state => state.InstructionSaves))
                {
                    if (RenameInstruction(instruction))
                    {
                        MarkChanged(element);
                    }
                }
                foreach (var otherCategory in element.StateCategoryList)
                {
                    var index = otherCategory.ExcludedVariables.IndexOf(oldProperty);
                    if (index != -1)
                    {
                        otherCategory.ExcludedVariables[index] = newProperty;
                        MarkChanged(element);
                    }
                }
                foreach (var eventResponse in element.Events.Where(item => item.SourceVariable == oldProperty))
                {
                    eventResponse.SourceVariable = newProperty;
                    MarkChanged(element);
                }
            }

            foreach (var (element, nos) in instances)
            {
                foreach (InstructionSave instruction in nos.InstructionSaves)
                {
                    if (RenameInstruction(instruction))
                    {
                        MarkChanged(element);
                    }
                }
            }

            foreach (var (element, variable) in tunneling)
            {
                variable.SourceObjectProperty = newProperty;
                MarkChanged(element);
            }

            return changed;

            bool RefersToCategory(CustomVariable variable, GlueElement element)
            {
                var bare = StripNullable(variable.Type);
                if (bare == null)
                {
                    return false;
                }
                if (bare == oldQualified)
                {
                    return true;
                }
                return bare == oldName && ObjectFinderCore.Self.GetStateSaveCategory(variable, element).Category == category;
            }

            string RenameType(string type)
            {
                var suffix = type.EndsWith("?") ? "?" : "";
                return StripNullable(type) == oldQualified ? newQualified + suffix : newName + suffix;
            }

            bool RenameInstruction(InstructionSave instruction)
            {
                var didChange = false;
                if (instruction.Member == oldProperty)
                {
                    instruction.Member = newProperty;
                    didChange = true;
                }
                var bareType = StripNullable(instruction.Type);
                if (bareType == oldQualified || (bareType == oldName && instruction.Member == newProperty))
                {
                    instruction.Type = RenameType(instruction.Type);
                    didChange = true;
                }
                return didChange;
            }
        }

        static string StripNullable(string type) => type?.EndsWith("?") == true ? type.Substring(0, type.Length - 1) : type;

        static bool InheritsFrom(GlueProjectSave project, GlueElement element, GlueElement possibleBase)
        {
            var visited = new HashSet<GlueElement>();
            var current = element;
            while (!string.IsNullOrEmpty(current?.BaseElement) && visited.Add(current))
            {
                current = project.GetElement(current.BaseElement);
                if (current == possibleBase)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
