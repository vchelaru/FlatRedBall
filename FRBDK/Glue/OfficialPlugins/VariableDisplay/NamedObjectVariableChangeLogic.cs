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
        public static void ReactToValueSet(NamedObjectSave instance, TypedMemberBase typedMember, object value, DataGridItem instanceMember, Type memberType)
        {
            instanceMember.IsDefault = false;

            TryAdjustingValue(instance, typedMember, ref value, instanceMember);

            PerformStandardVariableAssignments(instance, typedMember, value, instanceMember, memberType);

        }

        private static void TryAdjustingValue(NamedObjectSave instance, TypedMemberBase typedMember, ref object value, DataGridItem instanceMember)
        {
            if(typedMember.MemberType == typeof(AnimationChainList))
            {
                if(value is string && ((string)value) == "<NONE>")
                {
                    value = null;
                    instanceMember.IsDefault = true;

                    // Let's also set the CurrentChainName to null
                    GlueCommands.Self.GluxCommands.SetVariableOn(
                        instance,
                        "CurrentChainName",
                        typeof(string),
                        null);
                }
            }
        }

        private static void PerformStandardVariableAssignments(NamedObjectSave instance, TypedMemberBase typedMember, object value, DataGridItem instanceMember, Type memberType)
        {
            // If we ignore the next refresh, then AnimationChains won't update when the user
            // picks an AnimationChainList from a combo box:
            //RefreshLogic.IgnoreNextRefresh();
            GlueCommands.Self.GluxCommands.SetVariableOn(
                instance,
                typedMember.MemberName,
                memberType,
                value);


            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

            // let's make the UI faster:

            // Get this on the UI thread, but use it in the async call below
            var currentElement = GlueState.Self.CurrentElement;


            TaskManager.Self.AddAsyncTask(() =>
            {
                // don't send an entire frefresh command, we'll refresh
                // just the variables (prevents a full camera reset)
                bool sendRefreshCommands = false;
                GlueCommands.Self.GluxCommands.SaveGlux(sendRefreshCommands);

                GlueCommands.Self.GlueViewCommands.SendRefreshVariablesCommand();

                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);

            },
            "Saving .glux and regenerating the code for the current element");
        }
    }
}
