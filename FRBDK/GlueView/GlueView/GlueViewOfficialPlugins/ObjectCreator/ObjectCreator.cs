using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using GlueView.Facades;
using FlatRedBall.Utilities;
using FlatRedBall.Math.Geometry;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;

namespace GlueViewOfficialPlugins.ObjectCreator
{
    public class ObjectCreator
    {
        bool CanContainInstanceOf(IElement container, string typeInQuestion)
        {
            EntitySave typeAsEntitySave = ObjectFinder.Self.GetEntitySave(typeInQuestion);

            if (typeAsEntitySave != null)
            {
                // If the container is the same type or base type of the typeAsEntitySave, don't allow it
                if (typeAsEntitySave == container || typeAsEntitySave.InheritsFrom(container.Name))
                {
                    return false;
                }
            }
            return true;
        }

        public void CreateNamedObject(SourceType sourceType, string namedObjectType)
        {
            IElement element = GlueViewState.Self.CurrentElement;
            ///////////////////////Early Out//////////////////////
            if (element == null)
            {
                return;
            }

            if (CanContainInstanceOf(element, namedObjectType) == false)
            {
                MessageBox.Show("Cannot add " + namedObjectType + " to " + element.Name + " because this would cause an infinite loop");
            }

            /////////////////////End Early Out////////////////////
            
            //PositionedObject whatToAdd = null;

            NamedObjectSave newNos = new NamedObjectSave();
            newNos.SourceType = sourceType;
            newNos.SourceClassType = namedObjectType;

            if (newNos.SourceType == SourceType.FlatRedBallType)
            {
                newNos.InstanceName = namedObjectType;
            }
            else if (newNos.SourceType == SourceType.Entity)
            {
                newNos.InstanceName = FileManager.RemovePath(namedObjectType) + "Instance";
            }
            newNos.AddToManagers = true;
            newNos.AttachToContainer = true;
            newNos.CallActivity = true;

            StringFunctions.MakeNameUnique<NamedObjectSave>(newNos, element.NamedObjects);

            element.NamedObjects.Add(newNos);

            bool is2D = (element is EntitySave && ((EntitySave)element).Is2D) || SpriteManager.Camera.Orthogonal;

            //// This will need to change as the plugin grows in complexity
            if (namedObjectType == typeof(AxisAlignedRectangle).Name)
            {
                AddAxisAlignedRectangleValues(newNos, is2D);
            }
            else if (namedObjectType == typeof(Circle).Name)
            {
                AddCircleValues(newNos, is2D);
            }
            else if (namedObjectType == typeof(Sprite).Name)
            {
                AddSpriteValues(newNos, is2D);
            }
            else if (namedObjectType == typeof(Text).Name)
            {
                AddTextValues(newNos, is2D);
            }

            GlueViewCommands.Self.GlueProjectSaveCommands.SaveGlux();
            GlueViewCommands.Self.ElementCommands.ReloadCurrentElement();

        }

        private static void AddCircleValues(NamedObjectSave newNos, bool is2D)
        {

            CustomVariableInNamedObject cvino = GetOrCreateInstruction(newNos, "Radius");
            cvino.Type = "Single";

            if (is2D)
            {
                cvino.Value = 16.0f;

            }
            else
            {
                cvino.Value = 1.0f;
            }
        }

        private static void AddAxisAlignedRectangleValues(NamedObjectSave newNos, bool is2D)
        {
            //    whatToAdd = new AxisAlignedRectangle();
            CustomVariableInNamedObject scaleX = GetOrCreateInstruction(newNos, "ScaleX");
            scaleX.Type = "Single";

            CustomVariableInNamedObject scaleY = GetOrCreateInstruction(newNos, "ScaleY");
            scaleY.Type = "Single";
            
            if (is2D)
            {
                scaleX.Value = 16.0f;
                scaleY.Value = 16.0f;

            }
            else
            {
                scaleX.Value = 1.0f;
                scaleY.Value = 1.0f;
            }
        }

        private static void AddSpriteValues(NamedObjectSave newNos, bool is2D)
        {
            //    whatToAdd = new AxisAlignedRectangle();
            CustomVariableInNamedObject scaleX = GetOrCreateInstruction(newNos, "ScaleX");
            scaleX.Type = "Single";

            CustomVariableInNamedObject scaleY = GetOrCreateInstruction(newNos, "ScaleY");
            scaleY.Type = "Single";

            if (is2D)
            {
                scaleX.Value = 16.0f;
                scaleY.Value = 16.0f;

                CustomVariableInNamedObject pixelSize = GetOrCreateInstruction(newNos, "PixelSize");
                pixelSize.Type = "Single";
            }
            else
            {
                scaleX.Value = 1.0f;
                scaleY.Value = 1.0f;
            }            

        }


        private static void AddTextValues(NamedObjectSave newNos, bool is2D)
        {
            CustomVariableInNamedObject scale = GetOrCreateInstruction(newNos, "Scale");
            CustomVariableInNamedObject spacing = GetOrCreateInstruction(newNos, "Spacing");
            CustomVariableInNamedObject newLineDistance = GetOrCreateInstruction(newNos, "NewLineDistance");
            CustomVariableInNamedObject displayText = GetOrCreateInstruction(newNos, "DisplayText");


            scale.Value = 8;
            scale.Type = "Single";

            spacing.Value = 8;
            spacing.Type = "Single";

            newLineDistance.Value = 12;
            newLineDistance.Type = "Single";

            displayText.Value = newNos.InstanceName;
            displayText.Type = "string";
        }

        private static CustomVariableInNamedObject GetOrCreateInstruction(NamedObjectSave nos, string variableName)
        {
            foreach (var instruction in nos.InstructionSaves)
            {
                if (instruction.Member == variableName)
                {
                    return instruction;
                }
            }

            CustomVariableInNamedObject cvino = new CustomVariableInNamedObject();
            nos.InstructionSaves.Add(cvino);
            return cvino;

        }

    }
}
