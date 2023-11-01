using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Content;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteFrame;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Factories
{
    public class FactoryManager
    {
        static FactoryManager mSelf;

        public static FactoryManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new FactoryManager();
                }
                return mSelf;
            }
        }

        static Dictionary<NamedObjectSave, List<string>> mResetVariablesToAdd = new Dictionary<NamedObjectSave, List<string>>();

        public void AddResetVariablesForPooling_Click()
        {
            // Search:  pool, pooling, variable reset, variablereset, add reset variables for pooling
            mResetVariablesToAdd.Clear();

            EntitySave entitySave = GlueState.Self.CurrentEntitySave;

            SetResetVariablesForEntitySave(entitySave);

        }


        public async Task SetResetVariablesForEntitySave(EntitySave entitySave)
        {
            // set reset variables
            bool hasNamedObjectEntities = false;

            List<NamedObjectSave> skippedNoses = new List<NamedObjectSave>();

            #region Loop through the NamedObjects to identify which variables should be added
            foreach (NamedObjectSave nos in entitySave.NamedObjects)
            {
                hasNamedObjectEntities |= AddResetVariablesFor(skippedNoses, nos);
            }

            // let's make sure to get all the NOS's that are defined in base types too:
            foreach (var element in entitySave.GetAllBaseEntities())
            {
                foreach (NamedObjectSave nos in element.NamedObjects)
                {
                    if (!nos.DefinedByBase && !nos.SetByDerived)
                    {
                        hasNamedObjectEntities |= AddResetVariablesFor(skippedNoses, nos);
                    }
                }
            }

            if (skippedNoses.Count != 0)
            {
                string message = "Couldn't add reset variables for the following objects.  This may cause pooling to behave improperly for these objects:";

                foreach (NamedObjectSave skipped in skippedNoses)
                {
                    message += "\n " + skipped.InstanceName;
                }

                MessageBox.Show(message);
            }
            #endregion

            #region See if there are any variables to add

            bool areAnyVariablesBeingAdded = false;

            foreach (KeyValuePair<NamedObjectSave, List<string>> kvp in mResetVariablesToAdd)
            {
                NamedObjectSave nos = kvp.Key;
                List<string> variables = kvp.Value;

                // reverse loop here because we're removing from the list
                for (int i = variables.Count - 1; i > -1; i--)
                {
                    string variable = variables[i];

                    if (nos.VariablesToReset.Contains(variable))
                    {
                        variables.Remove(variable);
                    }
                    else
                    {
                        areAnyVariablesBeingAdded = true;
                    }
                }
            }
            #endregion

            #region If there are any variables, ask the user if resetting should be added

            if (areAnyVariablesBeingAdded)
            {
                DialogResult result = MessageBox.Show("Glue will add some best-guess variables to be reset " +
                    "to the objects in your Entity.  Existing variables will be preserved.  You may " +
                    "need to manually add additional variables depending on the logic contained in your Entity." +
                    "\n\nAdd variables?", "Add Variables for " + entitySave.Name + "?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    foreach (KeyValuePair<NamedObjectSave, List<string>> kvp in mResetVariablesToAdd)
                    {
                        NamedObjectSave nos = kvp.Key;
                        List<string> variables = kvp.Value;

                        foreach (string s in variables)
                        {
                            Plugins.PluginManager.ReceiveOutput("Added reset variable " + s + " in object " + nos);
                        }


                        nos.VariablesToReset.AddRange(variables);
                    }
                }

                GluxCommands.Self.SaveProjectAndElements();

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            }
            else
            {
                Plugins.PluginManager.ReceiveOutput("No reset variables added to " + entitySave);
            }

            #endregion

            #region As the user if Glue should reset variables for all NamedObjects which are Entities

            if (hasNamedObjectEntities)
            {
                DialogResult result = MessageBox.Show(
                    "Would you like to set reset variables for all contained objects which reference Entities inside " + entitySave.Name + "?  This " +
                    "action is recommended.", "Reset objects referencing Entities?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    foreach (NamedObjectSave nos in entitySave.NamedObjects)
                    {
                        if (nos.SourceType == SourceType.Entity)
                        {
                            EntitySave containedEntitySave = nos.GetReferencedElement() as EntitySave;

                            if (containedEntitySave != null)
                            {
                                SetResetVariablesForEntitySave(containedEntitySave);

                            }
                        }
                    }
                }
            }
            #endregion

            #region Check for inheritance and ask if all derived Entities should have their variables reset too

            List<EntitySave> inheritingEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entitySave);

            if (inheritingEntities.Count != 0)
            {
                string message =
                    "Would you like to set reset variables for Entities which inherit from " + entitySave.Name + "?  " +
                    "The following Entities will be modified:\n\n";

                foreach (EntitySave inheritingEntity in inheritingEntities)
                {
                    message += inheritingEntity.Name + "\n";
                }

                message += "\nThis " +
                    "action is recommended.";

                DialogResult result = MessageBox.Show(message,
                    "Reset Entities inheriting from this?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    foreach (EntitySave inheritingEntity in inheritingEntities)
                    {
                        SetResetVariablesForEntitySave(inheritingEntity);
                    }
                }
            }

            #endregion

            foreach (NamedObjectSave nos in entitySave.NamedObjects)
            {
                StringFunctions.RemoveDuplicates(nos.VariablesToReset);
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entitySave);

            foreach(var element in entitySave.GetAllBaseEntities())
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            }

            GluxCommands.Self.SaveProjectAndElements();
        }

        internal void RemoveResetVariablesForEntitySave(EntitySave entitySave)
        {
            foreach(var namedObject in entitySave.AllNamedObjects)
            {
                namedObject.VariablesToReset.Clear();
            }

            CodeWriter.GenerateCode(entitySave);

            foreach (var element in entitySave.GetAllBaseEntities())
            {
                CodeWriter.GenerateCode(element);
            }

            GluxCommands.Self.SaveProjectAndElements();
        }

        public static bool AddResetVariablesFor(NamedObjectSave nos)
        {
            List<NamedObjectSave> throwaway = new List<NamedObjectSave>();
            mResetVariablesToAdd.Clear();
            AddResetVariablesFor(throwaway, nos);

            bool toReturn = false;

            // This is a one-shot function, so we're going to remove mResetVariablesToAdd
            if (mResetVariablesToAdd.ContainsKey(nos))
            {
                toReturn = true;
                foreach(var value in mResetVariablesToAdd[nos])
                {
                    nos.VariablesToReset.Add(value);
                }
            }

            return toReturn;
        }

        private static bool AddResetVariablesFor(List<NamedObjectSave> skippedNoses, NamedObjectSave nos)
        {
            bool hasNamedObjectEntities = nos.SourceType == SourceType.Entity;

            List<string> variables = new List<string>();
            if (!mResetVariablesToAdd.ContainsKey(nos))
            {
                mResetVariablesToAdd.Add(nos, variables);

                bool isPositionedObject = nos.SourceType == SourceType.Entity;


                #region If it isn't an Entity, then do a check on its type to see if it's a PositionedObject

                if (!isPositionedObject)
                {
                    string typeString = "";

                    switch (nos.SourceType)
                    {
                        case SourceType.File:
                            if (!string.IsNullOrEmpty(nos.SourceName))
                            {
                                int indexOfParen = nos.SourceName.LastIndexOf('(') + 1;
                                typeString = nos.SourceName.Substring(indexOfParen, nos.SourceName.Length - indexOfParen - 1);
                            }
                            break;
                        case SourceType.FlatRedBallType:
                            typeString = nos.SourceClassType;
                            break;
                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(typeString))
                    {
                        Type type = TypeManager.GetTypeFromString(typeString);

                        isPositionedObject = typeof(PositionedObject).IsAssignableFrom(type);
                    }
                }

                #endregion

                #region If it is a PositionedObject, then reset its PositionedObject-related variables

                if (isPositionedObject)
                {
                    variables.Add("X");
                    variables.Add("Y");
                    variables.Add("Z");

                    variables.Add("XVelocity");
                    variables.Add("YVelocity");
                    variables.Add("ZVelocity");



                    variables.Add("RotationX");
                    variables.Add("RotationY");
                    variables.Add("RotationZ");

                    variables.Add("RotationXVelocity");
                    variables.Add("RotationYVelocity");
                    variables.Add("RotationZVelocity");

                }
                else
                {
                    skippedNoses.Add(nos);
                }

                #endregion


                if (nos.SourceType == SourceType.File &&
                    !string.IsNullOrEmpty(nos.SourceFile) &&
                    !string.IsNullOrEmpty(nos.SourceName))
                {
                    if (nos.SourceName.EndsWith("(Sprite)") || nos.SourceName.EndsWith("(SpriteFrame)"))
                    {
                        AddResetVariablesForSpriteOrSpriteFrame(nos, variables);
                    }
                }
                
                if(nos.SourceType == SourceType.FlatRedBallType && (nos.SourceClassType == "Sprite" || nos.SourceClassType == "SpriteFrame"))
                {
                    AddResetVariablesForSpriteOrSpriteFrame(nos, variables);
                }
            }
            return hasNamedObjectEntities;
        }

        private static void AddResetVariablesForSpriteOrSpriteFrame(NamedObjectSave nos, List<string> variables)
        {

            // todo - support resetting animation chains on Sprites that aren't using a .scnx
            if (nos.SourceType == SourceType.File && !string.IsNullOrEmpty(nos.SourceFile))
            {
                string fullFile = GlueCommands.Self.GetAbsoluteFileName(nos.SourceFile, true);
                var ses = SpriteEditorScene.FromFile(fullFile);

                int endingIndex = nos.SourceName.LastIndexOf('(');

                string nameWithoutType = nos.SourceName.Substring(0, endingIndex - 1);
                SpriteSave ss = ses.FindSpriteByName(nameWithoutType);

                string animationChainFileName = "";
                if (ss != null)
                {
                    animationChainFileName = ss.AnimationChainsFile;
                }
                else
                {
                    SpriteFrameSave sfs = ses.FindSpriteFrameSaveByName(nameWithoutType);
                    if (sfs != null)
                    {
                        animationChainFileName = sfs.ParentSprite.AnimationChainsFile;
                    }
                }

                if (!string.IsNullOrEmpty(animationChainFileName))
                {
                    variables.Add("CurrentChainName");
                    variables.Add("CurrentFrameIndex");
                }
            }

            variables.Add("Alpha");
            variables.Add("AlphaRate");
        }
    }
}
