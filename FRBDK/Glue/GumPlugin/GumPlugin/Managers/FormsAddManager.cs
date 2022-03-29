using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes.Behaviors;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPluginCore.Managers
{
    class FormsAddManager
    {
        public static async Task GenerateBehaviors()
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                bool didAdd = false;

                foreach (var control in FormsControlInfo.AllControls)
                {
                    if(!string.IsNullOrEmpty(control.BehaviorName))
                    {
                        var newBehavior = CreateBehaviorSaveFrom(control);
                        if(newBehavior.Name == null)
                        {
                            System.Diagnostics.Debugger.Break();
                        }
                        if (AddIfDoesntHave(newBehavior))
                        {
                            didAdd = true;
                        }
                    }
                }

                if (didAdd)
                {
                    await GumPluginCommands.Self.SaveGumxAsync();
                }
            }, "Adding Gum Forms Behaviors");
        }

        public static bool AddIfDoesntHave(BehaviorSave behaviorSave)
        {
            var project = AppState.Self.GumProjectSave;

            bool doesProjectAlreadyHaveBehavior =
                project.Behaviors.Any(item => item.Name == behaviorSave.Name);

            if (!doesProjectAlreadyHaveBehavior)
            {
                GumPluginCommands.Self.AddBehavior(behaviorSave);
            }
            // in case it's changed, or in case the user has somehow corrupted their behavior, force save it
            GumPluginCommands.Self.SaveBehavior(behaviorSave);

            return doesProjectAlreadyHaveBehavior == false;
        }

        public static BehaviorSave CreateBehaviorSaveFrom(FormsControlInfo controlInfo)
        {
            BehaviorSave toReturn = new BehaviorSave();
            toReturn.Name = controlInfo.BehaviorName;

            foreach (var gumStateCategory in controlInfo.GumStateCategory)
            {
                var category = new Gum.DataTypes.Variables.StateSaveCategory();
                toReturn.Categories.Add(category);
                category.Name = gumStateCategory.Name;

                if (gumStateCategory.States != null)
                {
                    foreach (var stateName in gumStateCategory.States)
                    {
                        category.States.Add(new Gum.DataTypes.Variables.StateSave { Name = stateName });
                    }
                }
            }

            toReturn.RequiredInstances.AddRange(controlInfo.RequiredInstances);

            return toReturn;
        }
    }
}
