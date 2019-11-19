using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Parsing;
using System.Windows.Forms;
using FlatRedBall.Glue.Factories;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Elements;
using Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.SetVariable
{
    class EntitySaveSetVariableLogic
    {
        internal void ReactToEntityChangedValue(string changedMember, object oldValue)
        {
            EntitySave entitySave = EditorLogic.CurrentEntitySave;

            #region BaseEntity changed

            if (changedMember == "BaseEntity")
            {
                // Not sure why we want to return here.  Maybe the user used
                // to have this set to something but now is undoing it
                //if (string.IsNullOrEmpty(entitySave.BaseEntity))
                //{
                //    return;
                //}
                ReactToChangedBaseEntity(oldValue, entitySave);
            }

            #endregion

            #region CreatedByOtherEntities changed

            else if (changedMember == "CreatedByOtherEntities")
            {
                HandleCreatedByOtherEntitiesSet(entitySave);
            }

            #endregion

            #region PooledByFactory

            else if (changedMember == nameof(entitySave.PooledByFactory) && (bool)oldValue != entitySave.PooledByFactory)
            {
                if (entitySave.PooledByFactory)
                {
                    // We should ask the user
                    // if Glue should set the reset
                    // variables for all contained objects
                    string message = "Would you like to add reset variables for all contained objects (recommended)";

                    DialogResult result = MessageBox.Show(message, "Add reset variables?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        FactoryManager.Self.SetResetVariablesForEntitySave(entitySave);
                    }
                }
                else // user set it to false
                {
                    var hasResetVariables = entitySave.AllNamedObjects.Any(item => item.VariablesToReset?.Any() == true);
                    if(hasResetVariables)
                    {
                        string message = "Would you like to remove reset variables for all contained objects? Select 'Yes' if you added reset variables earlier for pooling";

                        var dialogResult = MessageBox.Show(message, "Remove reset variables?", MessageBoxButtons.YesNo);

                        if(dialogResult == DialogResult.Yes)
                        {
                            FactoryManager.Self.RemoveResetVariablesForEntitySave(entitySave);
                        }
                    }
                }

                FactoryCodeGenerator.AddGeneratedPerformanceTypes();
                FactoryCodeGenerator.UpdateFactoryClass(entitySave);
            }

            #endregion

            #region Click Broadcast
            // Vic says:  I don't think we need this anymore
            else if (changedMember == "ClickBroadcast")
            {
                if (string.IsNullOrEmpty((string)oldValue) &&
                    !entitySave.ImplementsIClickable
                    )
                {
                    // Let the user know that this won't do anything unless the entity implements IClickable
                    string message = "The Click Broadcast message will not be broadcasted unless this " +
                        "Entity is made IClickable.  Would you like to make it IClickable?";

                    DialogResult result =
                        MessageBox.Show(message, "Make IClickable?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        entitySave.ImplementsIClickable = true;

                    }
                }
            }
            #endregion

            #region ImplementsIWindow

            else if (changedMember == "ImplementsIWindow")
            {
                if (entitySave.ImplementsIWindow && !entitySave.ImplementsIVisible)
                {
                    MessageBox.Show("IWindows must also be IVisible.  Automatically setting Implements IVisible to true");

                    entitySave.ImplementsIVisible = true;
                }

                RegenerateAllContainersForNamedObjectsThatUseCurrentEntity();

            }

            #endregion

            #region ImplementsIVisible

            else if (changedMember == "ImplementsIVisible")
            {
                ReactToChangedImplementsIVisible(oldValue, entitySave);
            }

            #endregion

            #region ImplementsIClickable
            else if (changedMember == "ImplementsIClickable")
            {
                RegenerateAllContainersForNamedObjectsThatUseCurrentEntity();
            }

            #endregion

            #region ItemType

            else if (changedMember == "ItemType")
            {
                EntitySave itemTypeEntity = ObjectFinder.Self.GetEntitySave(entitySave.ItemType);

                if (itemTypeEntity != null)
                {
                    if (!itemTypeEntity.CreatedByOtherEntities)
                    {
                        MessageBox.Show("The Entity " + entitySave.ItemType + " must be \"Created By Other Entities\" to be used as an Item Type");
                        entitySave.ItemType = null;
                    }
                }

            }

            #endregion

            #region ClassName

            else if (changedMember == "ClassName")
            {
                List<NamedObjectSave> allNamedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(EditorLogic.CurrentElement);

                List<IElement> containers = new List<IElement>();

                foreach (NamedObjectSave nos in allNamedObjects)
                {
                    IElement element = nos.GetContainer();

                    if (!containers.Contains(element))
                    {
                        containers.Add(element);
                    }
                }

                foreach (IElement element in containers)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                }
            }

            #endregion
        }

        private static void HandleCreatedByOtherEntitiesSet(EntitySave entitySave)
        {
            if (entitySave.CreatedByOtherEntities == true)
            {
                FactoryCodeGenerator.AddGeneratedPerformanceTypes();
                FactoryCodeGenerator.UpdateFactoryClass(entitySave);
                ProjectManager.SaveProjects();
            }
            else
            {
                FactoryCodeGenerator.RemoveFactory(entitySave);
                ProjectManager.SaveProjects();
            }


            List<EntitySave> entitiesToRefresh = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entitySave);
            entitiesToRefresh.AddRange(entitySave.GetAllBaseEntities());
            entitiesToRefresh.Add(entitySave);

            // We need to re-generate all objects that use this Entity
            foreach (EntitySave entityToRefresh in entitiesToRefresh)
            {
                List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entityToRefresh.Name);

                foreach (NamedObjectSave nos in namedObjects)
                {
                    IElement namedObjectContainer = nos.GetContainer();

                    if (namedObjectContainer != null)
                    {
                        CodeWriter.GenerateCode(namedObjectContainer);
                    }
                }
            }
            PropertyGridHelper.UpdateDisplayedPropertyGridProperties();
        }

        private static void ReactToChangedBaseEntity(object oldValue, EntitySave entitySave)
        {
            bool isValidBase = GetIfCurrentEntityBaseIsValid(entitySave);

            if (isValidBase == false)
            {
                entitySave.BaseEntity = (string)oldValue;
                MainGlueWindow.Self.PropertyGrid.Refresh();
            }
            else
            {

                List<CustomVariable> variablesBefore = new List<CustomVariable>();
                variablesBefore.AddRange(entitySave.CustomVariables);

                entitySave.UpdateFromBaseType();

                AskToPreserveVariables(entitySave, variablesBefore);
            }
            PropertyGridHelper.UpdateEntitySaveDisplay();
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
                        MessageBox.Show("There is a duplicate named object:\n\n" + derivedNamedObjects[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }

                    if (baseReferencedFiles.Contains(derivedNamedObjects[i]))
                    {
                        MessageBox.Show("There is a file and object both named:\n\n" + derivedNamedObjects[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }
                }

                for (int i = 0; i < derivedReferencedFiles.Count; i++)
                {
                    if (baseNamedObjectsIncludingSetByDerived.Contains(derivedReferencedFiles[i]))
                    {
                        MessageBox.Show("There is a file and object both named:\n\n" + derivedReferencedFiles[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }

                    if (baseReferencedFiles.Contains(derivedReferencedFiles[i]))
                    {
                        MessageBox.Show("There are two files named:\n\n" + derivedReferencedFiles[i] + "\n\nThe base class cannot be set");
                        isValidBase = false;
                        break;
                    }
                }
            }


            if (isValidBase && ProjectManager.VerifyInheritanceGraph(entitySave) == ProjectManager.CheckResult.Failed)
            {
                isValidBase = false;
            }

            return isValidBase;
        }

        public static List<EntitySave> GetAllEntitiesThatThisInherits(string derivedEntity)
        {
            List<EntitySave> listToReturn = new List<EntitySave>();

            EntitySave entity = ObjectFinder.Self.GetEntitySave(derivedEntity);

            entity.GetAllBaseEntities(listToReturn);

            return listToReturn;
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

        private static void ReactToChangedImplementsIVisible(object oldValue, EntitySave entitySave)
        {
            #region If the user turned IVisible off, see if there is a "Visible" Exposed Variable
            if (((bool)oldValue) == true)
            {
                CustomVariable variableToRemove = entitySave.GetCustomVariable("Visible");
                if (variableToRemove != null)
                {
                    List<string> throwawayList = new List<string>();

                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "This entity has a \"Visible\" variable exposed.  This variable is no longer valid.  What would you like to do?";
                    mbmb.AddButton("Remove this variable", DialogResult.Yes);
                    mbmb.AddButton("Keep this as a non-functional Variable (it will no longer control the object's visibility)", DialogResult.No);

                    DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                    if (result == DialogResult.Yes)
                    {
                        ProjectManager.RemoveCustomVariable(variableToRemove, throwawayList);
                    }
                    else
                    {
                        // No need to do anything
                    }
                }
            }
            #endregion

            #region If the user turned IVisible on, see if there are any NamedObjectSaves that reference Elements that are not IVisible

            if (entitySave.ImplementsIVisible)
            {
                foreach (NamedObjectSave nos in entitySave.AllNamedObjects)
                {
                    if (nos.SourceType == SourceType.Entity || nos.IsList)
                    {

                        EntitySave nosEntitySave = null;

                        if (nos.SourceType == SourceType.Entity)
                        {
                            nosEntitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassType);
                        }
                        else
                        {
                            nosEntitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType);
                        }

                        if (nosEntitySave != null && nosEntitySave.ImplementsIVisible == false)
                        {
                            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                            mbmb.MessageText = entitySave + " implements IVisible, but its object " + nos + " does not.  Would would you like to do?";

                            mbmb.AddButton("Make " + nosEntitySave + " implement IVisible", DialogResult.Yes);
                            mbmb.AddButton("Ignore " + nos + " when setting Visible on " + entitySave, DialogResult.No);
                            mbmb.AddButton("Do nothing - this will likely cause compile errors so this must be fixed manually", DialogResult.Cancel);

                            DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                            if (result == DialogResult.Yes)
                            {
                                nosEntitySave.ImplementsIVisible = true;

                                GlueCommands.Self.GenerateCodeCommands
                                    .GenerateElementAndReferencedObjectCodeTask(nosEntitySave);
                            }
                            else if (result == DialogResult.No)
                            {
                                nos.IncludeInIVisible = false;
                            }
                            else if (result == DialogResult.Cancel)
                            {
                                // do nothing - the user better fix this!
                            }
                        }
                    }
                }
            }
            #endregion

            #region If it's a ScrollableEntityList, then the item it's using must also be an IVisible

            if (entitySave.ImplementsIVisible && entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
            {
                EntitySave itemTypeAsEntity = ObjectFinder.Self.GetEntitySave(entitySave.ItemType);

                if (itemTypeAsEntity != null && itemTypeAsEntity.ImplementsIVisible == false)
                {
                    MessageBox.Show("The item type " + itemTypeAsEntity.ToString() + " must also implement IVisible.  Glue will do this now");

                    itemTypeAsEntity.ImplementsIVisible = true;

                    // Gotta regen this thing
                    var entityForItem = ObjectFinder.Self.GetIElement(entitySave.ItemType);
                    CodeWriter.GenerateCode(entityForItem);
                }
            }

            #endregion
        }

        private static void RegenerateAllContainersForNamedObjectsThatUseCurrentEntity()
        {
            List<NamedObjectSave> namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(EditorLogic.CurrentEntitySave);
            List<IElement> elementsToGenerate = new List<IElement>();
            foreach (NamedObjectSave nos in namedObjects)
            {
                IElement element = nos.GetContainer();

                if (!elementsToGenerate.Contains(element))
                {
                    elementsToGenerate.Add(element);
                }
            }

            foreach (IElement element in elementsToGenerate)
            {
                CodeWriter.GenerateCode(element);
            }
        }

    }
}
