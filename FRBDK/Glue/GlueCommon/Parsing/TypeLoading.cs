using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlatRedBall.Glue.Controls;

namespace FlatRedBall.Glue.Parsing
{
    /// <summary>
    /// Moved out of <c>TypeManager.LoadAdditionalTypes(Assembly, string)</c> (#2276) - its only coupling
    /// was the <c>DialogService.ShowMessage</c> call on the <c>ReflectionTypeLoadException</c> branch, now
    /// routed through <paramref name="errorReporting"/> (<see cref="IErrorReportingCore"/>). <c>TypeManager</c>
    /// still owns the resulting list (<c>mAdditionalTypes</c>) and just forwards to this method.
    /// </summary>
    public static class TypeLoading
    {
        public static IReadOnlyList<Type> GetAdditionalTypes(Assembly assembly, string namespaceFilter, IErrorReportingCore errorReporting)
        {
            try
            {
                Type[] types = assembly.GetTypes();
                if (string.IsNullOrEmpty(namespaceFilter))
                {
                    return types;
                }
                else
                {
                    return types.Where(type => type.FullName.StartsWith(namespaceFilter)).ToList();
                }
            }
            catch (ReflectionTypeLoadException)
            {
                errorReporting.ShowMessage("Encountered exception while trying to load " + assembly.FullName +
                    "\nThis is likely because the assembly is using a different version of the .NET framework.");
                return Array.Empty<Type>();
            }
            catch (TypeLoadException)
            {
                return Array.Empty<Type>();
            }
            catch (Exception)
            {
                return Array.Empty<Type>();
            }
        }
    }
}
