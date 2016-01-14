using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.SaveClasses
{

    public static class IBehaviorContainerHelper
    {

        //public static List<string> GetFulfilledRequirements<T>(T container) where T : IBehaviorContainer, INamedObjectContainer
        //{
        //    List<string> listToReturn = new List<string>();

        //    for (int i = 0; i < container.CustomVariables.Count; i++)
        //    {
        //        string fulfilledRequirement = container.CustomVariables[i].FulfillsRequirement;
        //        if (!string.IsNullOrEmpty(fulfilledRequirement)
        //            && fulfilledRequirement != "<NONE>")
        //        {

        //            listToReturn.Add(fulfilledRequirement);
        //        }
        //    }

        //    for (int i = 0; i < container.NamedObjects.Count; i++)
        //    {
        //        string fulfilledRequirement = container.NamedObjects[i].FulfillsRequirement;

        //        if (!string.IsNullOrEmpty(fulfilledRequirement) &&
        //            fulfilledRequirement != "<NONE>")
        //        {
        //            listToReturn.Add(fulfilledRequirement);
        //        }
        //    }

        //    return listToReturn;
        //}


        //public static string GetFulfillerName<T>(T container, BehaviorRequirement requirement) where T : IBehaviorContainer, INamedObjectContainer
        //{
        //    string requirementStringToMatch = requirement.ToString();

        //    for (int i = 0; i < container.CustomVariables.Count; i++)
        //    {
        //        string fulfilledRequirement = container.CustomVariables[i].FulfillsRequirement;

        //        if (requirementStringToMatch == fulfilledRequirement)
        //        {
        //            return container.CustomVariables[i].Name;
        //        }
        //    }

        //    for (int i = 0; i < container.NamedObjects.Count; i++)
        //    {
        //        string fulfilledRequirement = container.NamedObjects[i].FulfillsRequirement;

        //        if (requirementStringToMatch == fulfilledRequirement)
        //        {
        //            return container.NamedObjects[i].InstanceName;
        //        }
        //    }

        //    return null;

        //}

        //public static BehaviorSave GetBehavior(IBehaviorContainer container, string behaviorName)
        //{
        //    for (int i = 0; i < container.Behaviors.Count; i++)
        //    {
        //        if (container.Behaviors[i].Name == behaviorName)
        //        {
        //            return container.Behaviors[i];
        //        }
        //    }

        //    return null;
        //}

        //public static bool ContainsBehavior<T>(T container, string behaviorName) where T : IBehaviorContainer
        //{
        //    return GetBehavior(container, behaviorName) != null;
        //}





        public static void UpdateCustomVariablesFromBaseType(IElement behaviorContainer)
        {

            IElement baseElement = null;

            if (behaviorContainer is ScreenSave)
            {
                baseElement = ObjectFinder.Self.GetScreenSave(behaviorContainer.BaseObject);
            }
            else
            {
                baseElement = ObjectFinder.Self.GetEntitySave(behaviorContainer.BaseObject);
            }



            List<CustomVariable> customVariablesBeforeUpdate = new List<CustomVariable>();

            for (int i = 0; i < behaviorContainer.CustomVariables.Count; i++)
            {
                if (behaviorContainer.CustomVariables[i].DefinedByBase)
                {
                    customVariablesBeforeUpdate.Add(behaviorContainer.CustomVariables[i]);
                }
            }

            List<CustomVariable> newCustomVariables = null;

            //EntitySave entity = ProjectManager.GetEntitySave(mBaseEntity);

            if (baseElement != null)
            {
                newCustomVariables = baseElement.GetCustomVariablesToBeSetByDerived();
            }
            else
            {
                newCustomVariables = new List<CustomVariable>();
            }

            // See if there are any objects to be removed.
            for (int i = customVariablesBeforeUpdate.Count - 1; i > -1; i--)
            {
                bool contains = false;

                for (int j = 0; j < newCustomVariables.Count; j++)
                {
                    if (customVariablesBeforeUpdate[i].Name == newCustomVariables[j].Name &&
                        customVariablesBeforeUpdate[i].DefinedByBase)
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    // We got a NamedObject we should remove
                    behaviorContainer.CustomVariables.Remove(customVariablesBeforeUpdate[i]);
                    customVariablesBeforeUpdate.RemoveAt(i);
                }
            }

            // Next, see if there are any objects to be added
            for (int i = 0; i < newCustomVariables.Count; i++)
            {
                bool alreadyContainedAsDefinedByBase = false;
                for (int j = 0; j < customVariablesBeforeUpdate.Count; j++)
                {
                    if (customVariablesBeforeUpdate[j].Name == newCustomVariables[i].Name &&
                        customVariablesBeforeUpdate[j].DefinedByBase)
                    {
                        alreadyContainedAsDefinedByBase = true;
                    }
                }

                if (!alreadyContainedAsDefinedByBase)
                {
                    // There isn't a variable by this
                    // name that is already DefinedByBase, 
                    // but there may still be a variable that
                    // is just a regular variable - and in that
                    // case we want to connect the existing variable
                    // with the variable in the base
                    CustomVariable existingInDerived = behaviorContainer.GetCustomVariable(newCustomVariables[i].Name);
                    if (existingInDerived != null)
                    {
                        existingInDerived.DefinedByBase = true;
                    }
                    else
                    {
                        CustomVariable customVariable = newCustomVariables[i].Clone();

                        // March 4, 2012
                        // We used to not
                        // change the SourceObject
                        // or the SourceObjectProperty
                        // values; however, this new variable
                        // should behave like an exposed variable,
                        // so it shouldn't have any SourceObject or
                        // SourceObjectProperty.  If it did, then it
                        // will access the base NamedObjectSave to set
                        // its property, and this could cause compilation
                        // errors if the NOS in the base is marked as private.
                        // It may also avoid raising events defined in the base.
                        customVariable.SourceObject = null;
                        customVariable.SourceObjectProperty = null;

                        customVariable.DefinedByBase = true;
                        // We'll assume that this thing is going to be the acutal definition
                        customVariable.SetByDerived = false;

                        behaviorContainer.CustomVariables.Add(customVariable);
                    }
                }
            }
        }

    }



    public static class BehaviorSaveExtensionMethods
    {
        //public static void UpdateFilePair(this BehaviorSave behaviorSave)
        //{
        //    #region Copy to local project if necessary

        //    bool shouldLocalProjectFileExist = behaviorSave.FileLocation == FileLocation.ProjectPrimarySharedSecondary ||
        //        behaviorSave.FileLocation == FileLocation.SharedPrimaryProjectSecondary ||
        //        behaviorSave.FileLocation == FileLocation.ProjectOnly;

        //    string localProjectLocation = FileManager.RelativeDirectory + "Behaviors/" + behaviorSave.Name + ".cs";

        //    string sharedLocation = BehaviorManager.BehaviorFolder + behaviorSave.Name + ".cs";

        //    if (FileManager.FileExists(sharedLocation) && !FileManager.FileExists(localProjectLocation))
        //    {
        //        string projectBehaviorsDirectory = FileManager.RelativeDirectory + "Behaviors/";

        //        if (!Directory.Exists(projectBehaviorsDirectory))
        //        {
        //            Directory.CreateDirectory(projectBehaviorsDirectory);
        //        }

        //        File.Copy(sharedLocation, localProjectLocation);
        //    }

        //    #endregion

        //    #region Copy to shared if necessary

        //    bool shouldSharedProjectFileExist = behaviorSave.FileLocation == FileLocation.ProjectPrimarySharedSecondary ||
        //        behaviorSave.FileLocation == FileLocation.SharedPrimaryProjectSecondary ||
        //        behaviorSave.FileLocation == FileLocation.SharedOnly;

        //    if (FileManager.FileExists(localProjectLocation) && !FileManager.FileExists(sharedLocation))
        //    {
        //        if (!Directory.Exists(BehaviorManager.BehaviorFolder))
        //        {
        //            Directory.CreateDirectory(BehaviorManager.BehaviorFolder);
        //        }

        //        File.Copy(localProjectLocation, sharedLocation);
        //    }


        //    #endregion


        //}


    }
}
