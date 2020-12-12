using FlatRedBall.Glue.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ElementExtensionMethods
    {
        public static bool UpdateFromBaseType(this IElement elementSave)
        {
            bool haveChangesOccurred = false;
            if (ObjectFinder.Self.GlueProject != null)
            {
                haveChangesOccurred |= NamedObjectContainerHelper.UpdateNamedObjectsFromBaseType(elementSave);

                UpdateCustomVariablesFromBaseType(elementSave);
            }
            return haveChangesOccurred;

        }

        public static void UpdateCustomVariablesFromBaseType(IElement elementToUpdate)
        {

            IElement baseElement = null;
            baseElement = ObjectFinder.Self.GetBaseElement(elementToUpdate);

            List<CustomVariable> customVariablesBeforeUpdate = new List<CustomVariable>();

            for (int i = 0; i < elementToUpdate.CustomVariables.Count; i++)
            {
                if (elementToUpdate.CustomVariables[i].DefinedByBase)
                {
                    customVariablesBeforeUpdate.Add(elementToUpdate.CustomVariables[i]);
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
                    elementToUpdate.CustomVariables.Remove(customVariablesBeforeUpdate[i]);
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
                    CustomVariable existingInDerived = elementToUpdate.GetCustomVariable(newCustomVariables[i].Name);
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

                        elementToUpdate.CustomVariables.Add(customVariable);
                    }
                }
            }

            // update the category
            foreach(var customVariable in elementToUpdate.CustomVariables)
            {
                if (customVariable.DefinedByBase)
                {
                    var baseVariable = baseElement.CustomVariables.FirstOrDefault(item => item.Name == customVariable.Name);
                    if(!string.IsNullOrEmpty(baseVariable?.Category))
                    {
                        customVariable.Category = baseVariable.Category;
                    }
                }
            }
        }


    }
}
