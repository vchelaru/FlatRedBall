using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using FlatRedBall.AnimationEditorForms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue.IO;

namespace AnimationEditorPlugin
{
    public class TextureCoordinateSelectionLogic
    {
        public void GetTextureCoordinates(NamedObjectSave nos, out float leftTextureCoordinate, out float rightTextureCoordinate, out float topTextureCoordinate, out float bottomTextureCoordinate, out string fullFileName)
        {
            var textureVar = nos.InstructionSaves.FirstOrDefault(item => item.Member == "Texture");
            var leftTextureCoordinateVar = nos.InstructionSaves.FirstOrDefault(item => item.Member == "LeftTexturePixel");
            var rightTextureCoordinateVar = nos.InstructionSaves.FirstOrDefault(item => item.Member == "RightTexturePixel");
            var topTextureCoordinateVar = nos.InstructionSaves.FirstOrDefault(item => item.Member == "TopTexturePixel");
            var bottomTextureCoordinateVar = nos.InstructionSaves.FirstOrDefault(item => item.Member == "BottomTexturePixel");


            leftTextureCoordinate = 0;
            rightTextureCoordinate = 1;
            topTextureCoordinate = 0;
            bottomTextureCoordinate = 1;

            fullFileName = null;

            if (textureVar != null && textureVar.Value != null)
            {
                var fileVariableName = textureVar.Value as string;

                var container = nos.GetContainer();

                var rfs = container.ReferencedFiles.FirstOrDefault((item) =>
                {
                    return FileManager.RemovePath(FileManager.RemoveExtension(item.Name)) == fileVariableName;
                });

                if(rfs == null)
                {
                    var allBaseElements = container.BaseElements();
                    foreach(var baseElement in allBaseElements)
                    {
                        rfs = baseElement.ReferencedFiles.FirstOrDefault((item) =>
                        {
                            return FileManager.RemovePath(FileManager.RemoveExtension(item.Name)) == fileVariableName;
                        });

                        if (rfs != null)
                        {
                            break;
                        }
                    }
                }



                if (rfs != null)
                {
                    fullFileName = FlatRedBall.Glue.ProjectManager.MakeAbsolute(rfs.Name, true);
                }
            }

            if (leftTextureCoordinateVar != null && leftTextureCoordinateVar.Value != null)
            {
                leftTextureCoordinate = (float)leftTextureCoordinateVar.Value;
            }

            if (rightTextureCoordinateVar != null && rightTextureCoordinateVar.Value != null)
            {
                rightTextureCoordinate = (float)rightTextureCoordinateVar.Value;
            }

            if (topTextureCoordinateVar != null && topTextureCoordinateVar.Value != null)
            {
                topTextureCoordinate = (float)topTextureCoordinateVar.Value;
            }

            if (bottomTextureCoordinateVar != null && bottomTextureCoordinateVar.Value != null)
            {
                bottomTextureCoordinate = (float)bottomTextureCoordinateVar.Value;
            }

            bool needsFile = rightTextureCoordinateVar == null || bottomTextureCoordinateVar == null;

            if(needsFile)
            {
                if(System.IO.File.Exists(fullFileName))
                {
                    var size = Glue.IO.ImageHeader.GetDimensions(fullFileName);
                    if(rightTextureCoordinateVar == null)
                    {
                        rightTextureCoordinate = size.Width;
                    }
                    if(bottomTextureCoordinateVar == null)
                    {
                        bottomTextureCoordinate = size.Height;
                    }
                }
                
            }
        }

        //internal void HandleCoordinateChanged(TextureCoordinateSelectionWindow selectionWindow, NamedObjectSave nos)
        //{
        //    AssignTextureCoordinateValues(selectionWindow, nos);

        //    GlueCommands.Self.GluxCommands.SaveGlux();

        //    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

        //    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
        //}

        
        //private static void AssignTextureCoordinateValues(TextureCoordinateSelectionWindow selectionWindow, NamedObjectSave nos)
        //{
        //    var texture = selectionWindow.CurrentTexture;
        //    var rectangle = selectionWindow.RectangleSelector;

        //    if (texture != null)
        //    {
        //        // Victor Chelaru September 24, 2016
        //        // The user may invert the rectangle and not know it.
        //        // Even though there are valid reasons to invert, allowing
        //        // it here probably causes more harm than good. Therefore, I'm
        //        // adding the Min/Max calls to the rectangle values.
        //        GlueCommands.Self.GluxCommands.SetVariableOn(
        //            nos,
        //            "LeftTexturePixel",
        //            typeof(float),
        //            System.Math.Min( rectangle.Left, rectangle.Right));

        //        GlueCommands.Self.GluxCommands.SetVariableOn(
        //            nos,
        //            "RightTexturePixel",
        //            typeof(float),
        //            System.Math.Max(rectangle.Left, rectangle.Right));

        //        GlueCommands.Self.GluxCommands.SetVariableOn(
        //            nos,
        //            "TopTexturePixel",
        //            typeof(float),
        //            System.Math.Min(rectangle.Top, rectangle.Bottom));
                                                                                                                                                                                                                                   
        //        GlueCommands.Self.GluxCommands.SetVariableOn(
        //            nos,
        //            "BottomTexturePixel",
        //            typeof(float),
        //            System.Math.Max(rectangle.Top, rectangle.Bottom));
        //    }
        //}

        //public void RefreshSpriteDisplay(TextureCoordinateSelectionWindow selectionWindow)
        //{
        //    var nos = GlueState.Self.CurrentNamedObjectSave;

        //    float leftTextureCoordinate;
        //    float rightTextureCoordinate;
        //    float topTextureCoordinate;
        //    float bottomTextureCoordinate;
        //    string fullFileName;

        //    GetTextureCoordinates(nos, out leftTextureCoordinate, out rightTextureCoordinate, out topTextureCoordinate, out bottomTextureCoordinate, out fullFileName);


        //    selectionWindow.ShowSprite(fullFileName, topTextureCoordinate, bottomTextureCoordinate, leftTextureCoordinate, rightTextureCoordinate);
        //}

    }
}
