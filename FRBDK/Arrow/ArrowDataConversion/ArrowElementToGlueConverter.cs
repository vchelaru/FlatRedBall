using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace ArrowDataConversion
{
    public class ArrowElementToGlueConverter
    {
        #region Fields

        SpriteSaveConverter mSpriteSaveConverter = new SpriteSaveConverter();
        CircleSaveConverter mCircleSaveConverter = new CircleSaveConverter();
        AxisAlignedRectangleSaveConverter mRectangleSaveConverter = new AxisAlignedRectangleSaveConverter();


        ArrowElementInstanceToNosConverter mArrowElementInstanceConverter = new ArrowElementInstanceToNosConverter();
        #endregion


        public IElement ToGlueIElement(ArrowElementSave arrowElement)
        {
            List<string> referencedFiles = new List<string>();


            IElement glueElement;

            if (arrowElement.ElementType == ElementType.Screen)
            {
                glueElement = new ScreenSave();
                glueElement.Name = "Screens/" + arrowElement.Name;
            }
            else
            {
                glueElement = new EntitySave();
                glueElement.Name = "Entities/" + arrowElement.Name;
            }


            AddSpritesToElement(arrowElement, glueElement, referencedFiles);

            AddCirclesToElement(arrowElement, glueElement);
            AddRectanglesToElement(arrowElement, glueElement);

            AddElementInstancesToElement(arrowElement, glueElement);

            AddReferencedFileSaves(referencedFiles, glueElement);

            AddCustomVariables(glueElement);

            return glueElement;
        }

        private void AddCustomVariables(IElement glueElement)
        {
            if (glueElement.GetCustomVariable("X") == null)
            {
                glueElement.CustomVariables.Add(new CustomVariable(){ Type="float", DefaultValue = 0.0f, Name = "X"});
            }
            if (glueElement.GetCustomVariable("Y") == null)
            {
                glueElement.CustomVariables.Add(new CustomVariable() { Type = "float", DefaultValue = 0.0f, Name = "Y" });
            }

        }

        private void AddElementInstancesToElement(ArrowElementSave arrowElement, IElement glueElement)
        {
            foreach (var instance in arrowElement.ElementInstances)
            {
                NamedObjectSave nosToAdd = mArrowElementInstanceConverter.ArrowElementInstanceToNos(instance);

                glueElement.NamedObjects.Add(nosToAdd);
            }
        }

        private void AddReferencedFileSaves(List<string> referencedFiles, IElement glueElement)
        {
            FlatRedBall.Utilities.StringFunctions.RemoveDuplicates(referencedFiles);

            foreach (var file in referencedFiles)
            {
                ReferencedFileSave rfs = new ReferencedFileSave();
                rfs.Name = file;

                string runtimeType = GetRuntimeTypeForExtension(FileManager.GetExtension(rfs.Name));

                rfs.RuntimeType = runtimeType;

                glueElement.ReferencedFiles.Add(rfs);

            }
        }

        private string GetRuntimeTypeForExtension(string extension)
        {
            switch (extension)
            {
                case "bmp":
                case "tga":
                case "png":
                    return "Microsoft.Xna.Framework.Graphics.Texture2D";
                default:
                    return "NOT KNOWN";


            }

        }

        private void AddSpritesToElement(ArrowElementSave arrowElement, IElement glueElemement, List<string> referencedFiles)
        {
            foreach (var sprite in arrowElement.Sprites)
            {
                NamedObjectSave nos = mSpriteSaveConverter.SpriteSaveToNamedObjectSave(sprite);

                glueElemement.NamedObjects.Add(nos);

                sprite.GetReferencedFiles(referencedFiles);
            }
        }

        private void AddCirclesToElement(ArrowElementSave arrowElement, IElement glueElemement)
        {
            foreach (var circle in arrowElement.Circles)
            {
                NamedObjectSave nos = mCircleSaveConverter.CircleSaveToNamedObjectSave(circle);

                glueElemement.NamedObjects.Add(nos);
            }
        }

        private void AddRectanglesToElement(ArrowElementSave arrowElement, IElement glueElemement)
        {
            foreach (var rectangle in arrowElement.Rectangles)
            {
                NamedObjectSave nos = mRectangleSaveConverter.RectangleSaveToNamedObjectSave(rectangle);

                glueElemement.NamedObjects.Add(nos);
            }

        }
    }
}
