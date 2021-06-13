using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Instructions.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.VariableDisplay
{
    class NamedObjectVariableChangeLogic
    {
        public static void ReactToValueSet(NamedObjectSave instance, string memberName, object value, out bool makeDefault)
        {
            // If setting AnimationChianList to null then also null out the CurrentChainName to prevent
            // runtime errors.
            //
            makeDefault = false;
            var ati = instance.GetAssetTypeInfo();
            var foundVariable = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == memberName);
            if(foundVariable?.Type == nameof(AnimationChainList))
            {
                if(value is string && ((string)value) == "<NONE>")
                {
                    value = null;
                    makeDefault = true;

                    // Let's also set the CurrentChainName to null
                    GlueCommands.Self.GluxCommands.SetVariableOn(
                        instance,
                        "CurrentChainName",
                        null);
                }
            }


            PerformStandardVariableAssignments(instance, memberName, value);

        }

        private static void PerformStandardVariableAssignments(NamedObjectSave instance, string memberName, object value)
        {
            // If we ignore the next refresh, then AnimationChains won't update when the user
            // picks an AnimationChainList from a combo box:
            //RefreshLogic.IgnoreNextRefresh();
            GlueCommands.Self.GluxCommands.SetVariableOn(
                instance,
                memberName,
                value);


            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

            // let's make the UI faster:

            // Get this on the UI thread, but use it in the async call below
            var currentElement = GlueState.Self.CurrentElement;

            GlueCommands.Self.GluxCommands.SaveGlux();

            if(currentElement != null)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(currentElement);
            }
        }
    }
}
