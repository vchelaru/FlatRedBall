using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.Wireframe;
using Glue.IO;
using RenderingLibrary.Graphics;
using Gum.Managers;
using Gum.ToolStates;

namespace FlatRedBall.Gum.Converters
{
    public class GumInstanceToGlueNamedObjectSave
    {
        public GumProjectSave GumProjectSave { get; set; }

        private NamedObjectSave CreateNosFor(InstanceSave instance, IElement container)
        {

            NamedObjectSave nos = new NamedObjectSave();

            string name = instance.Name;

            // See if this name is already used by RFS's
            var allRfss = container.GetAllReferencedFileSavesRecursively();
            while (allRfss.Any(item => item.GetInstanceName() == name) || container.ReferencedFiles.Any(item => item.Name == name))
            {
                name = name + instance.BaseType;
            }

            nos.InstanceName = name;
            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instance, instance.ParentContainer);
            if (instance.BaseType == "Sprite")
            {
                nos.SourceType = SourceType.FlatRedBallType;
                nos.SourceClassType = "Sprite";

                float width = rvf.GetValue<float>("Width");
                float height = rvf.GetValue<float>("Height");

                if (width == 0 && height == 0)
                {
                    nos.SetPropertyValue("TextureScale", 1.0f);
                }
                else
                {
                    // Eventually handle width/height
                    nos.SetPropertyValue("Width", width);
                    nos.SetPropertyValue("Height", height);
                }

                SetPositionValuesOn(nos, instance);

                string texture = rvf.GetValue<string>("SourceFile");

                string fileInstanceName = FileManager.RemoveExtension(FileManager.RemovePath(texture));

                var added = nos.SetPropertyValue("Texture", fileInstanceName);
                added.Type = "Texture2D";
            }
            return nos;
        }

        private void SetPositionValuesOn(Glue.SaveClasses.NamedObjectSave nos, InstanceSave instance)
        {
            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instance, instance.ParentContainer);
            float x = rvf.GetValue<float>("X");
            float y = rvf.GetValue<float>("Y");

            #region Adjust values according to object origin

            float effectiveWidth = GetEffectiveWidth(instance);
            float effectiveHeight = GetEffectiveHeight(instance);

            HorizontalAlignment horizontalOrigin = rvf.GetValue<HorizontalAlignment>("X Origin");
            VerticalAlignment verticalOrigin = rvf.GetValue<VerticalAlignment>("Y Origin");

            switch (horizontalOrigin)
            {
                case HorizontalAlignment.Left:
                    x += effectiveWidth / 2.0f;
                    break;
                case HorizontalAlignment.Right:
                    x -= effectiveWidth / 2.0f;
                    break;

            }

            switch (verticalOrigin)
            {
                case VerticalAlignment.Top:
                    y += effectiveHeight / 2.0f;
                    break;
                case VerticalAlignment.Bottom:
                    y -= effectiveHeight / 2.0f;
                    break;
            }

            #endregion

            #region Adjust values according to alignment

            float parentWidth = GetParentWidth(instance);
            float parentHeight = GetParentHeight(instance);

            PositionUnitType xUnits = rvf.GetValue<PositionUnitType>("X Units");
            PositionUnitType yUnits = rvf.GetValue<PositionUnitType>("Y Units");

            switch (xUnits)
            {
                case PositionUnitType.PixelsFromLeft:

                    x -= parentWidth / 2.0f;

                    break;
                case PositionUnitType.PixelsFromCenterX:
                    // do nothing
                    break;
                default:
                    throw new NotImplementedException();
                    break;
            }

            switch (yUnits)
            {
                case PositionUnitType.PixelsFromTop:
                    y -= parentHeight / 2.0f;
                    break;
                case PositionUnitType.PixelsFromCenterY:
                    // do nothing
                    break;
                default:

                    break;

            }

            #endregion

            nos.SetPropertyValue("X", x);
            // Invert Y because FRB uses positive Y is up
            nos.SetPropertyValue("Y", -y);


        }

        private float GetParentWidth(InstanceSave instance)
        {
            string parent = new RecursiveVariableFinder(instance, instance.ParentContainer).GetValue<string>("Parent");

            if (string.IsNullOrEmpty(parent))
            {
                return GumProjectSave.DefaultCanvasWidth;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private float GetParentHeight(InstanceSave instance)
        {
            string parent = new RecursiveVariableFinder(instance, instance.ParentContainer).GetValue<string>("Parent");

            if (string.IsNullOrEmpty(parent))
            {
                return GumProjectSave.DefaultCanvasHeight;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void AddNamedObjectSavesToGlueElement(global::Gum.DataTypes.ElementSave element, IElement toReturn, Dictionary<string, CopiedFileReference> copiedFiles)
        {
            foreach (var instance in element.Instances)
            {

                NamedObjectSave nos = CreateNosFor(instance, toReturn);

                toReturn.NamedObjects.Add(nos);
            }
        }



        public float GetEffectiveWidth(InstanceSave instance)
        {
            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instance, instance.ParentContainer);

            float width = rvf.GetValue<float>("Width");


            if (width == 0)
            {
                string sourceFile = rvf.GetValue<string>("SourceFile");
                if (instance.BaseType == "Sprite")
                {
                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        string fullFileName = FileManager.GetDirectory(GumProjectSave.FullFileName) + sourceFile;
                        width = ImageHeader.GetDimensions(fullFileName).Width;

                    }
                }
            }

            return width;

        }

        public float GetEffectiveHeight(InstanceSave instance)
        {
            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instance, instance.ParentContainer);

            float height = rvf.GetValue<float>("Height");


            if (height == 0)
            {
                string sourceFile = rvf.GetValue<string>("SourceFile");
                if (instance.BaseType == "Sprite")
                {
                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        string fullFileName = FileManager.GetDirectory(GumProjectSave.FullFileName) + sourceFile;
                        height = ImageHeader.GetDimensions(fullFileName).Height;

                    }
                }
            }

            return height;

        }
    }


}
