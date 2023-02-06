using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using CheckResult = FlatRedBall.Glue.ProjectManager.CheckResult;

namespace GlueFormsCore.Managers
{
    public static class InheritanceManager
    {
        //Used to prevent recursive references and inheritence
        public static int VerificationId
        {
            get;
            set;
        }

        static InheritanceManager()
        {
            VerificationId = 0;
        }

        // The name is vague and the method does a ton, so it might be good to figure out a way to break this up
        public static void UpdateAllDerivedElementFromBaseValues(bool regenerateCode, GlueElement currentElement = null)
        {
            currentElement = currentElement ?? GlueState.Self.CurrentElement;

            // This method can be slow, so we should store off the base elements to regenerate and only do those, in tasks, one by one:
            //HashSet<GlueElement> toRegenerate = new HashSet<GlueElement>();

            if (currentElement is EntitySave currentEntity)
            {
                List<EntitySave> derivedEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(currentEntity.Name);

                List<NamedObjectSave> nosList = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(currentEntity.Name);

                for (int i = 0; i < derivedEntities.Count; i++)
                {
                    EntitySave entitySave = derivedEntities[i];

                    nosList.AddRange(ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entitySave.Name));

                    GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(entitySave);

                    // Update the tree nodes
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(entitySave);

                    if (regenerateCode)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(entitySave);
                    }
                }

                foreach (NamedObjectSave nos in nosList)
                {
                    nos.UpdateCustomProperties();

                    var element = nos.GetContainer();

                    if (element != null && regenerateCode)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(element);
                    }

                }
            }
            else if (currentElement is ScreenSave currentScreenSave)
            {
                List<ScreenSave> derivedScreens = ObjectFinder.Self.GetAllScreensThatInheritFrom(currentScreenSave.Name);

                for (int i = 0; i < derivedScreens.Count; i++)
                {
                    ScreenSave screenSave = derivedScreens[i];
                    GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(screenSave);

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screenSave);

                    if (regenerateCode)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeAsync(screenSave);
                    }
                }
            }
        }

        public static void ReactToChangedBaseScreen(object oldValue, ScreenSave screenSave)
        {
            if (VerifyInheritanceGraph(screenSave) == CheckResult.Failed)
            {
                screenSave.BaseScreen = (string)oldValue;
            }
            else
            {
                //screenSave.UpdateFromBaseType();
                GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(screenSave);
            }
        }

        public static void ReactToChangedBaseEntity(string oldValue, EntitySave entitySave)
        {
            bool isValidBase = GetIfCurrentEntityBaseIsValid(entitySave);

            if (isValidBase == false)
            {
                entitySave.BaseEntity = (string)oldValue;
                MainGlueWindow.Self.PropertyGrid.Refresh();
            }
            else
            {
                var oldEntity = ObjectFinder.Self.GetEntitySave(oldValue);
                var newEntity = ObjectFinder.Self.GetEntitySave(entitySave.BaseEntity);

                HashSet<ScreenSave> screensToRegenerate = new HashSet<ScreenSave>();

                if (oldEntity != null)
                {
                    var allObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(oldEntity);
                    var screens = allObjects.Select(item => item.GetContainer())
                        .Where(item => item as ScreenSave != null)
                        .Select(item => item as ScreenSave);

                    foreach (var screen in screens)
                    {
                        screensToRegenerate.Add(screen);
                    }
                }

                if (newEntity != null)
                {
                    var allObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(newEntity);
                    var screens = allObjects.Select(item => item.GetContainer())
                        .Where(item => item as ScreenSave != null)
                        .Select(item => item as ScreenSave);

                    foreach (var screen in screens)
                    {
                        screensToRegenerate.Add(screen);
                    }
                }

                foreach (var screen in screensToRegenerate)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screen);
                }

                List<CustomVariable> variablesBefore = new List<CustomVariable>();

                variablesBefore.AddRange(entitySave.CustomVariables);

                GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(entitySave);

                AskToPreserveVariables(entitySave, variablesBefore);
            }
            if(entitySave == GlueState.Self.CurrentEntitySave)
            {
                PropertyGridHelper.UpdateEntitySaveDisplay();
            }
        }

        private static bool GetIfCurrentEntityBaseIsValid(EntitySave entitySave)
        {
            bool isValidBase = true;

            // Gotta check the new inhertiance tree to make sure that there are no duplicate object names
            if (!string.IsNullOrEmpty(entitySave.BaseEntity))
            {
                List<EntitySave> thisAndDerivedFromThisList;
                List<EntitySave> baseEntities = new List<EntitySave>();

                thisAndDerivedFromThisList = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entitySave.Name);
                thisAndDerivedFromThisList.Add(entitySave);

                EntitySave newBase = ObjectFinder.Self.GetEntitySave(entitySave.BaseEntity);

                if (newBase != null)
                {

                    baseEntities = GetAllEntitiesThatThisInherits(entitySave.BaseEntity);
                    baseEntities.Add(newBase);
                }

                List<string> derivedNamedObjects = new List<string>();
                List<string> baseNamedObjects = new List<string>();
                List<string> baseNamedObjectsIncludingSetByDerived = new List<string>();

                List<string> derivedReferencedFiles = new List<string>();
                List<string> baseReferencedFiles = new List<string>();

                foreach (EntitySave es in baseEntities)
                {
                    foreach (NamedObjectSave nos in es.NamedObjects)
                    {
                        if (!nos.SetByDerived)
                        {
                            baseNamedObjects.Add(nos.InstanceName);
                        }
                        baseNamedObjectsIncludingSetByDerived.Add(nos.InstanceName);
                    }


                    foreach (ReferencedFileSave rfs in es.ReferencedFiles)
                    {
                        baseReferencedFiles.Add(rfs.GetInstanceName());
                    }
                }

                foreach (EntitySave es in thisAndDerivedFromThisList)
                {
                    foreach (NamedObjectSave nos in es.NamedObjects)
                    {
                        if (!nos.DefinedByBase)
                        {
                            derivedNamedObjects.Add(nos.InstanceName);
                        }
                    }

                    foreach (ReferencedFileSave rfs in es.ReferencedFiles)
                    {
                        derivedReferencedFiles.Add(rfs.GetInstanceName());
                    }
                }



                for (int i = 0; i < derivedNamedObjects.Count; i++)
                {
                    if (baseNamedObjects.Contains(derivedNamedObjects[i]))
                    {
                        System.Windows.MessageBox.Show("There is a duplicate named object:\n\n" + derivedNamedObjects[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }

                    if (baseReferencedFiles.Contains(derivedNamedObjects[i]))
                    {
                        System.Windows.MessageBox.Show("There is a file and object both named:\n\n" + derivedNamedObjects[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }
                }

                for (int i = 0; i < derivedReferencedFiles.Count; i++)
                {
                    if (baseNamedObjectsIncludingSetByDerived.Contains(derivedReferencedFiles[i]))
                    {
                        System.Windows.MessageBox.Show("There is a file and object both named:\n\n" + derivedReferencedFiles[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }

                    if (baseReferencedFiles.Contains(derivedReferencedFiles[i]))
                    {
                        System.Windows.MessageBox.Show("There are two files named:\n\n" + derivedReferencedFiles[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }
                }
            }


            if (isValidBase && VerifyInheritanceGraph(entitySave) == CheckResult.Failed)
            {
                isValidBase = false;
            }

            return isValidBase;
        }

        private static void AskToPreserveVariables(EntitySave entitySave, List<CustomVariable> variablesBefore)
        {
            foreach (CustomVariable oldVariable in variablesBefore)
            {
                if (entitySave.GetCustomVariableRecursively(oldVariable.Name) == null)
                {
                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    string message = "The variable\n\n" + oldVariable.ToString() + "\n\nIs no longer part of the Entity.  What do you want to do?";

                    mbmb.MessageText = message;

                    mbmb.AddButton("Add a new variable with the same name and type to " + entitySave.Name, DialogResult.Yes);
                    mbmb.AddButton("Nothing - the variable will go away", DialogResult.No);

                    DialogResult result = mbmb.ShowDialog();

                    if (result == DialogResult.Yes)
                    {
                        CustomVariable newVariable = new CustomVariable();
                        newVariable.Type = oldVariable.Type;
                        newVariable.Name = oldVariable.Name;
                        newVariable.DefaultValue = oldVariable.DefaultValue;
                        newVariable.SourceObject = oldVariable.SourceObject;
                        newVariable.SourceObjectProperty = oldVariable.SourceObjectProperty;

                        newVariable.Properties = new List<PropertySave>();
                        newVariable.Properties.AddRange(oldVariable.Properties);

                        newVariable.HasAccompanyingVelocityProperty = oldVariable.HasAccompanyingVelocityProperty;
                        newVariable.CreatesEvent = oldVariable.CreatesEvent;
                        newVariable.IsShared = oldVariable.IsShared;

                        if (!string.IsNullOrEmpty(oldVariable.OverridingPropertyType))
                        {
                            newVariable.OverridingPropertyType = oldVariable.OverridingPropertyType;
                            newVariable.TypeConverter = oldVariable.TypeConverter;
                        }

                        newVariable.CreatesEvent = oldVariable.CreatesEvent;

                        entitySave.CustomVariables.Add(newVariable);

                    }

                }
            }
        }

        private static List<EntitySave> GetAllEntitiesThatThisInherits(string derivedEntity)
        {
            List<EntitySave> listToReturn = new List<EntitySave>();

            EntitySave entity = ObjectFinder.Self.GetEntitySave(derivedEntity);

            entity.GetAllBaseEntities(listToReturn);

            return listToReturn;
        }

        internal static CheckResult VerifyInheritanceGraph(INamedObjectContainer node)
        {
            var project = GlueState.Self.CurrentGlueProject;
            if (project != null)
            {
                VerificationId++;
                string resultString = "";

                if (InheritanceVerificationHelper(ref node, ref resultString) == CheckResult.Failed)
                {
                    System.Windows.Forms.MessageBox.Show("This assignment has created an inheritence cycle containing the following classes:\n\n" +
                                    resultString +
                                    "\nThe assignment will be undone.");
                    node.BaseObject = null;
                    return CheckResult.Failed;
                }

            }

            return CheckResult.Passed;
        }

        private static CheckResult InheritanceVerificationHelper(ref INamedObjectContainer node, ref string cycleString)
        {
            //Assign the current VerificationId to identify nodes that have been visited
            node.VerificationIndex = VerificationId;

            //Travel upward through the inheritence tree from this object, stopping when either the
            //tree stops, or you reach a node that's already been visited.
            if (!string.IsNullOrEmpty(node.BaseObject))
            {
                INamedObjectContainer baseNode = ObjectFinder.Self.GetNamedObjectContainer(node.BaseObject);

                if (baseNode == null)
                {
                    // We do nothing - the base object for this
                    // Entity doesn't exist, so we'll continue as if this thing doesn't really have
                    // a base Entity.  The user will have to address this in the Glue UI
                }
                else if (baseNode.VerificationIndex != VerificationId)
                {

                    //If baseNode verification failed, add this node's name to the list and return Failed
                    if (InheritanceVerificationHelper(ref baseNode, ref cycleString) == CheckResult.Failed)
                    {
                        cycleString = (node as FlatRedBall.Utilities.INameable).Name + "\n" + cycleString;
                        return CheckResult.Failed;
                    }

                }
                else
                {
                    //If the basenode has already been visited, begin the cycleString and return Failed

                    cycleString = (node as FlatRedBall.Utilities.INameable).Name + "\n" +
                                    (baseNode as FlatRedBall.Utilities.INameable).Name + "\n";

                    return CheckResult.Failed;
                }
            }


            return CheckResult.Passed;
        }
    }
}
