using EntityPerformancePlugin.Models;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityPerformancePluginCore.CodeGenerators
{
    class UpdateDependenciesCodeGenerator : ElementComponentCodeGenerator
    {
        public ProjectManagementValues Values { get; set; }


        public override void GenerateUpdateDependencies(ICodeBlock codeBlock, GlueElement element)
        {
            if(element is ScreenSave)
            {
                foreach(var nos in element.NamedObjects)
                {
                    UpdateDependenciesForNos(nos, codeBlock);
                }
            }
        }

        private void UpdateDependenciesForNos(NamedObjectSave nos, ICodeBlock codeBlock)
        {
            if (nos.IsList && !nos.DefinedByBase)
            {
                var listGenericType = nos.SourceClassGenericType;

                var possibleEntityGenericType =
                    ObjectFinder.Self.GetEntitySave(listGenericType);

                if (possibleEntityGenericType != null)
                {

                    var managementValues =
                        Values?.EntityManagementValueList?.FirstOrDefault(item => item.Name == possibleEntityGenericType.Name);

                    if(managementValues?.PropertyManagementMode == EntityPerformancePlugin.Enums.PropertyManagementMode.SelectManagedProperties)
                    {
                        // August 20, 2020
                        // Should we always update dependencies? or only if any contained objects have parent updates?
                        // I guess it shoudl be if any objects are fully managed or have a parent/child relationship updated, but I'll worry
                        // about that later
                        var shouldUpdateDependencies =
                            true;

                            //managementValues.SelectedProperties.Contains("Attachment");

                        if(shouldUpdateDependencies)
                        {
                            codeBlock.ForEach($"var item in {nos.InstanceName}")
                                .Line("item.UpdateDependencies(currentTime);");
                        }

                    }
                }
            }
        }
    }
}
