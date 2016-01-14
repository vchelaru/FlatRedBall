using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TileGraphicsPlugin.Controllers;

namespace TileGraphicsPlugin.CodeGeneration
{
    class LevelCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            bool shouldGenerate = GetIfShouldGenerate(element);

            if (shouldGenerate)
            {
                var ifBlock = codeBlock.If("CurrentTileMap != null");
                ifBlock.Line("CurrentTileMap.AnimateSelf();");
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            GeneratePreloadLevel(codeBlock, element);

            GenerateInitializeLevel(codeBlock, element);

            return codeBlock;
        }

        private void GenerateInitializeLevel(ICodeBlock codeBlock, IElement element)
        {
            #region /////////////////////////////////Early out////////////////////////////////
            bool shouldGenerate = GetIfShouldGenerate(element);

            if (!shouldGenerate)
            {
                return;
            }

            ///////////////////////////////End early out/////////////////////////////
            #endregion


            codeBlock.Line("FlatRedBall.TileGraphics.LayeredTileMap CurrentTileMap;");
            var function = codeBlock.Function("void", "InitializeLevel", "string levelName");

            GenerateInitializeLevelObjects(function);

            GenerateInitializeCamera(function);

            GenerateAddCollisionAndEntities(function);

            GenerateInitializeAnimations(function);
        }

        private static bool GetIfShouldGenerate(IElement element)
        {
            bool shouldContinue = true;

            bool isScreen = element is ScreenSave;

            if (!isScreen)
            {
                shouldContinue = false;
            }

            if (shouldContinue)
            {
                var variable = element.GetCustomVariable(AddLevelController.UsesTmxLevelFilesVariableName);

                if (variable == null || variable.DefaultValue == null || (variable.DefaultValue is bool) == false || (bool)(variable.DefaultValue) == false)
                {
                    shouldContinue = false;
                }
            }
            return shouldContinue;
        }

        private static void GenerateInitializeLevelObjects(ICodeBlock function)
        {
            // Load the file and store it off:
            function.Line("CurrentTileMap = GetFile(levelName) as FlatRedBall.TileGraphics.LayeredTileMap;");

            // Add it to managers
            function.Line("CurrentTileMap.AddToManagers();");

            // Get the CSV
            function.Line("var levelInfoObject = GetFile(levelName + \"Info\");");
        }

        private static void GenerateInitializeCamera(ICodeBlock function)
        {
            // adjust the Camera min/maxes:

            function.Line("// This sets the min and max values for the Camera so it can't view beyond the edge of the map");
            function.Line("// If you don't like it, you an add this to your CustomInitialize:");
            function.Line("// FlatRedBall.Camera.Main.ClearBorders();");
            function.Line("FlatRedBall.Camera.Main.SetBordersAtZ(0, -CurrentTileMap.Height, CurrentTileMap.Width, 0, 0);");
        }

        private static void GenerateAddCollisionAndEntities(ICodeBlock function)
        {
            var ifCode = function.If("levelInfoObject is System.Collections.Generic.List<DataTypes.TileMapInfo>");
            {
                ifCode.Line("FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddCollisionFrom(" +
                    "SolidCollisions, CurrentTileMap, levelInfoObject as System.Collections.Generic.List<DataTypes.TileMapInfo>);");

                ifCode.Line("FlatRedBall.TileEntities.TileEntityInstantiator.CreateEntitiesFrom(" +
                    "CurrentTileMap, levelInfoObject as System.Collections.Generic.List<DataTypes.TileMapInfo>);");

            }
            var elseCode = ifCode.End().Else();
            {
                elseCode.Line("FlatRedBall.TileCollisions.TileShapeCollectionLayeredTileMapExtensions.AddCollisionFrom(" +
                    "SolidCollisions, CurrentTileMap, levelInfoObject as System.Collections.Generic.Dictionary<string, DataTypes.TileMapInfo>);");

                elseCode.Line("FlatRedBall.TileEntities.TileEntityInstantiator.CreateEntitiesFrom(" +
                    "CurrentTileMap, levelInfoObject as System.Collections.Generic.Dictionary<string, DataTypes.TileMapInfo>);");
            }
        }

        private static void GenerateInitializeAnimations(ICodeBlock function)
        {
            function.Line("// initialize the animations:");
            function.Line("Dictionary<string, FlatRedBall.Graphics.Animation.AnimationChain> animationDictionary = new Dictionary<string, FlatRedBall.Graphics.Animation.AnimationChain>();");

            var outerForEach = function.ForEach("var item in levelInfoObject as System.Collections.Generic.List<DataTypes.TileMapInfo>");
            {
                var ifBlock = outerForEach.If("item.EmbeddedAnimation != null && item.EmbeddedAnimation.Count != 0");
                {
                    ifBlock.Line("FlatRedBall.Graphics.Animation.AnimationChain newChain = new FlatRedBall.Graphics.Animation.AnimationChain();");
                    ifBlock.Line("newChain.Name = item.Name + \"Animation\";");

                    ifBlock.Line("animationDictionary.Add(item.Name, newChain);");


                    var innerForEach = ifBlock.ForEach("var frameSave in item.EmbeddedAnimation");
                    {
                        innerForEach.Line("var frame = new FlatRedBall.Graphics.Animation.AnimationFrame();");

                        innerForEach.Line("int index = 0;");

                        innerForEach.Line("int.TryParse(frameSave.TextureName, out index);");

                        innerForEach.Line("frame.Texture = CurrentTileMap.MapLayers[index].Texture;");

                        innerForEach.Line("frame.FrameLength = frameSave.FrameLength;");

                        innerForEach.Line("// We use pixel coords in the Save:");
                        innerForEach.Line("frame.LeftCoordinate = frameSave.LeftCoordinate / frame.Texture.Width;");
                        innerForEach.Line("frame.RightCoordinate = frameSave.RightCoordinate / frame.Texture.Width;");

                        innerForEach.Line("frame.TopCoordinate = frameSave.TopCoordinate / frame.Texture.Height;");
                        innerForEach.Line("frame.BottomCoordinate = frameSave.BottomCoordinate / frame.Texture.Height;");
                        innerForEach.Line("// finish others");

                        innerForEach.Line("newChain.Add(frame);");
                    }
                }
            }

            function.Line("CurrentTileMap.Animation = new FlatRedBall.TileGraphics.LayeredTileMapAnimation(animationDictionary);");
        }

        private void GeneratePreloadLevel(ICodeBlock codeBlock, IElement element)
        {

        }
    }
}
